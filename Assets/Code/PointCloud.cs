using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.IO;

public class PointCloud {

	public double[] Center {
		get;
		private set;
	}

	public Vector3[] Points {
		get;
		private set;
	}
	
	public Vector3[] Normals {
		get;
		private set;
	}
	
	public readonly string Name;
	public readonly FileInfo FileInfo;

	public Vector3 GroundPoint {
		get;
		private set;
	}

	public BuildingMetadata Metadata {
		get;
		private set;
	}

	public Dictionary<String, String> Stats;

	public List<Plane> Planes;

	public PointCloud(string filename) {
		this.FileInfo = new FileInfo(filename);
		this.Name = this.FileInfo.Name.Substring(0, this.FileInfo.Name.IndexOf('.'));
		this.Stats = new Dictionary<string, string>();
		this.load();
	}

	public PointCloud(string filename, BuildingMetadata metadata) {
		FileInfo fileInfo = new FileInfo(filename);		
		this.Metadata = metadata;

		this.load();
	}

	private void load() {
		
		if (this.FileInfo.Extension == ".xyz") {
			this.Points = XYZLoader.LoadXYZFile(this.FileInfo.FullName, this.Metadata);
		} else if (this.FileInfo.Extension == ".points") {
			this.loadMetadata();
			this.Center = this.Metadata.Coordinates;		
			this.Points = XYZLoader.LoadPointFile(this.FileInfo.FullName, this.Metadata);
		} else {
			throw new Exception("Unsupported file extension.");
		}

		this.findGroundPoint();
	}

	private void loadMetadata() {
		string filename = this.FileInfo.Directory + "/" + this.Name + ".json";
		if (File.Exists(filename)) {
			this.Metadata = JsonUtility.FromJson<BuildingMetadata>(File.ReadAllText(filename));
		}
	}

	private void findGroundPoint() {
		this.GroundPoint = this.Points.OrderBy(p => p.y).Skip(20).FirstOrDefault();
	}
	
	public void EstimateNormals() {
		var pointHashSet = new PointHashSet(2.0f, this.Points);
		this.Normals = new Vector3[this.Points.Length];

		const float neighbourRange = 2.0f;
		const int neighbourCount = 6;

		for (int i = 0; i < this.Points.Length; i++) {
			var point = this.Points[i];
			var neighbours = pointHashSet.GetPointsInRange(point, neighbourRange, true)
				.OrderBy(p => (point - p).magnitude)
				.SkipWhile(p => p == point)
				.Take(neighbourCount)
				.ToArray();

			this.Normals[i] = this.getPlaneNormal(neighbours).normalized;

			if (this.Normals[i].y < 0) {
				this.Normals[i] = this.Normals[i] * -1.0f;
			}
		}
	}

	private Vector3 getPlaneNormal(Vector3[] points) {
		// http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
		var centroid = points.Aggregate(Vector3.zero, (a, b) => a + b) / points.Length;

		float xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;

		foreach (var point in points) {
			var relative = point - centroid;
			xx += relative.x * relative.y;
			xy += relative.x * relative.y;
			xz += relative.x * relative.z;
			yy += relative.y * relative.y;
			yz += relative.y * relative.z;
			zz += relative.z * relative.z;
		}

		float detX = yy * zz - yz * yz;
		float detY = xx * zz - xz * xz;
		float detZ = xx * yy - xy * xy;
		float detMax = Mathf.Max(detX, Mathf.Max(detY, detZ));

		if (detMax == detX) {
			float a = (xz * yz - xy * zz) / detX;
			float b = (xy * yz - xz * yy) / detX;
			return new Vector3(1, a, b);
		} else if (detMax == detY) {
			float a = (yz * xz - xy * zz) / detY;
			float b = (xy * xz - yz * xx) / detY;
			return new Vector3(a, 1, b);
		} else {
			float a = (yz * xy - xz * yy) / detZ;
			float b = (xz * xy - yz * xx) / detZ;
			return new Vector3(a, b, 1);
		}
	}

	public Vector2[] GetShape() {
		var shape = XYZLoader.LoadXYZFile(this.FileInfo.Directory + "/" + this.Name + ".xyzshape", this.Metadata).Select(v => new Vector2(v.x, v.z));

		if (shape.First() == shape.Last()) {
			shape = shape.Skip(1);
		}
		return shape.ToArray();
	}

	public float GetScore(int index, Plane plane) {
		var point = this.Points[index];
		float distance = Mathf.Abs(plane.GetDistanceToPoint(point)) / HoughPlaneFinder.MaxDistance;
		if (distance > 1) {
			return 0;
		}
		return 1.0f - distance;
	}

	public float GetScore(Plane plane) {
		float result = 0;
		for (int i = 0; i < this.Points.Length; i++) {
			result += this.GetScore(i, plane);
		}
		return result;
	}
}
