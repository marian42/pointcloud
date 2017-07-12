﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.IO;
using UnityEditor;

[Serializable]
public class PlaneParameters {
	public Vector3 Normal;
	public float Distance;

	public PlaneParameters(Plane plane) {
		this.Normal = plane.normal;
		this.Distance = plane.distance;
	
	}
	public Plane GetPlane() {
		return new Plane(this.Normal, this.Distance);
	}
}

[SelectionBase]
public class PointCloud : MonoBehaviour {
	private const int pointsPerMesh = 60000;
	[SerializeField, HideInInspector]
	public Vector3[] Points;
	[SerializeField, HideInInspector]
	public Vector3[] CenteredPoints;
	[SerializeField, HideInInspector]
	public Color[] Colors;
	[SerializeField, HideInInspector]
	public Vector3[] Normals;
	
	public string Name;
	public string Folder;

	public Vector3 Center;
	public Vector3 GroundPoint;

	public BuildingMetadata Metadata;

	[SerializeField, HideInInspector]
	private PlaneParameters[] serializedPlanes;
	public IEnumerable<Plane> Planes {
		get {
			if (this.serializedPlanes == null) {
				return null;
			}
			return this.serializedPlanes.Select(pp => pp.GetPlane());
		}
		set {
			this.serializedPlanes = value.Select(plane => new PlaneParameters(plane)).ToArray();
		}
	}

	public void Load(string filename) {
		FileInfo fileInfo = new FileInfo(filename);
		this.Name = fileInfo.Name.Substring(0, fileInfo.Name.IndexOf('.'));
		this.Folder = fileInfo.Directory.FullName + "\\";
		this.loadMetadata();
		this.gameObject.name = this.Metadata.address;
		
		if (fileInfo.Extension == "xyz") {
			this.Points = XYZLoader.LoadFile(filename);
		} else if (fileInfo.Extension == ".points") {
			this.Points = XYZLoader.LoadPointFile(filename, this.Metadata);
		} else {
			throw new Exception("Unsupported file extension.");
		}

		this.moveToCenter();
		this.ResetColors(Color.red);
	}

	private void loadMetadata() {
		string filename = this.Folder + this.Name + ".json";
		this.Metadata = JsonUtility.FromJson<BuildingMetadata>(File.ReadAllText(filename));
	}

	public void ResetColors(Color color) {
		this.Colors = new Color[this.Points.Length];
		for (int i = 0; i < this.Points.Length; i++) {
			this.Colors[i] = color;
		}
	}

	public void Show() {
		this.deleteMeshes();
		for (int start = 0; start < Points.Length; start += pointsPerMesh) {
			this.createMeshObject(start, Math.Min(start + pointsPerMesh, Points.Length - 1));
		}
	}

	private void moveToCenter() {
		float minX = Points[0].x, minY = Points[0].y, minZ = Points[0].z, maxX = Points[0].x, maxY = Points[0].y, maxZ = Points[0].z;

		foreach (var point in this.Points) {
			if (point.x < minX) minX = point.x;
			if (point.y < minY) minY = point.y;
			if (point.x < minZ) minZ = point.z;
			if (point.x > maxX) maxX = point.x;
			if (point.y > maxY) maxY = point.y;
			if (point.x > maxZ) maxZ = point.z;
		}
		this.Center = new Vector3(Mathf.Lerp(minX, maxX, 0.5f), Mathf.Lerp(minY, maxY, 0.5f), Mathf.Lerp(minZ, maxZ, 0.5f));
		
		this.CenteredPoints = new Vector3[this.Points.Length];
		for (int i = 0; i < this.Points.Length; i++) {
			this.CenteredPoints[i] = this.Points[i] - this.Center;
		}

		this.GroundPoint = this.CenteredPoints.OrderBy(p => p.y).Skip(20).FirstOrDefault();
		this.transform.position = this.Center + Vector3.down * (this.Center.y + this.GroundPoint.y);
	}

	private void createMeshObject(int fromIndex, int toIndex) {
		var prefab = Resources.Load("Prefabs/PointMesh") as GameObject;
		var gameObject = GameObject.Instantiate(prefab) as GameObject;
		gameObject.layer = 8;
		gameObject.transform.parent = this.transform;
		gameObject.transform.localPosition = Vector3.zero;
		var mesh = new Mesh();
		gameObject.GetComponent<MeshFilter>().mesh = mesh;

		int[] indecies = new int[toIndex - fromIndex];
		Vector3[] meshPoints = new Vector3[toIndex - fromIndex];
		Color[] meshColors = new Color[toIndex - fromIndex];
		for (int i = fromIndex; i < toIndex; ++i) {
			indecies[i - fromIndex] = i - fromIndex;
			meshPoints[i - fromIndex] = this.CenteredPoints[i];
			meshColors[i - fromIndex] = this.Colors[i];
		}

		mesh.vertices = meshPoints;
		mesh.colors = meshColors;
		mesh.SetIndices(indecies, MeshTopology.Points, 0);
	}

	private void deleteMeshes() {
		var existingMeshes = new List<GameObject>();
		foreach (var child in this.transform) {
			if ((child as Transform).tag == "PointMesh") {
				existingMeshes.Add((child as Transform).gameObject);
			}
		}
		foreach (var existingQuad in existingMeshes) {
			GameObject.DestroyImmediate(existingQuad);
		}
	}

	public void EstimateNormals() {
		var pointHashSet = new PointHashSet(2.0f, this.CenteredPoints);
		this.Normals = new Vector3[this.Points.Length];

		const float neighbourRange = 2.0f;
		const int neighbourCount = 6;

		for (int i = 0; i < this.Points.Length; i++) {
			var point = this.CenteredPoints[i];
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

	public void DisplayNormals() {
		if (this.Normals == null) {
			Debug.LogError("No normals found.");
			return;
		}
		for (int i = 0; i < this.Normals.Length; i++) {
			var start = this.Points[i];
			Debug.DrawLine(start, start + this.Normals[i], Color.blue, 10.0f);
		}
	}

	public Vector2[] GetShape() {
		return XYZLoader.LoadFile(this.Folder + this.Name + ".xyzshape").Select(v => new Vector2(v.x - this.Center.x, v.z - this.Center.z)).ToArray();
	}

	public float GetScore(int index, Plane plane) {
		var point = this.CenteredPoints[index];
		float distance = Mathf.Abs(plane.GetDistanceToPoint(point)) / HoughPlaneFinder.MaxDistance;
		if (distance > 1) {
			return 0;
		}
		float result = 1.0f - distance;

		var normal = this.Normals[index];
		
		const float maxAngle = 60.0f;
		float angle = Vector3.Angle(plane.normal, normal) / maxAngle;
		if (angle > 1.0f) {
			return 0;
		} else {
			return result * (1.0f - angle);
		}
	}

	public float GetScore(Plane plane) {
		float result = 0;
		for (int i = 0; i < this.CenteredPoints.Length; i++) {
			result += this.GetScore(i, plane);
		}
		return result;
	}

	[MenuItem("File/Load pointcloud...")]
	public static void LoadSingle() {
		string selected = EditorUtility.OpenFilePanel("Load file", Application.dataPath + "/data/buildings/", null);
		if (selected.Any() && File.Exists(selected)) {
			GameObject gameObject = new GameObject();
			var pointCloud = gameObject.AddComponent<PointCloud>();
			pointCloud.Load(selected);
			pointCloud.Show();
			Selection.activeTransform = pointCloud.transform;
			SceneView.lastActiveSceneView.FrameSelected();
		}
	}

	[MenuItem("File/Load all pointclouds")]
	public static void LoadFolder() {
		var folder = new DirectoryInfo(Application.dataPath + "/data/buildings/");
		foreach (var xyzFile in folder.GetFiles()) {
			if (xyzFile.Extension.ToLower() != ".xyz" && xyzFile.Extension.ToLower() != ".points") {
				continue;
			}
			GameObject gameObject = new GameObject();
			var newPointCloud = gameObject.AddComponent<PointCloud>();
			newPointCloud.Load(xyzFile.FullName);
			newPointCloud.Show();
		}
	}
}
