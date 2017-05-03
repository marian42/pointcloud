using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class RoofClassifier {
	private PointCloud pointCloud;
	private Vector3[] points;
	private PointHashSet pointHashSet;
	private PointHashSet largeBucketHashSet;
	private IEnumerable<Ridge> ridges;

	public class Ridge {
		public readonly Vector3 Start;
		public readonly Vector3 End;
		public readonly Vector3 Direction;

		public Ridge(Vector3 start, Vector3 end) {
			this.Start = start;
			this.End = end;
			this.Direction = (end - start).normalized;
		}

		public Ray Ray {
			get {
				return new Ray(this.Start, this.Direction);
			}
		}

		/// <summary>
		/// Distance between start and the closest point to the supplied point that is on the ray of the ridge
		/// </summary>
		public float GetValue(Vector3 point) {
			return Vector3.Dot(point - this.Start, this.Direction);
		}

		public float GetDistance(Vector3 point) {
			return (point - (this.Start + this.Direction * this.GetValue(point))).magnitude;
		}

		public void DrawDebugLine() {
			Debug.DrawLine(this.Start, this.End, Color.blue, 40.0f);
		}
	}

	public RoofClassifier(PointCloud pointCloud) {
		this.pointCloud = pointCloud;
		this.points = pointCloud.Points;
		this.pointCloud.ResetColors(Color.red);
	}

	private IEnumerable<Vector3> findRidgePoints() {
		for (int i = 0; i < this.points.Length; i++) {
			Vector3 point = this.points[i];
			if (point.y < this.largeBucketHashSet.GetGroundHeight(point) + 4.0f) {
				continue;
			}
			bool isRidgePoint = true;
			foreach (Vector3 pointInRange in this.pointHashSet.GetPointsInRange(point, 1.0f, true)) {
				if (pointInRange.y > point.y + 0.2f) {
					isRidgePoint = false;
					break;
				}
			}
			if (isRidgePoint) {
				yield return point;
			}
		}
	}

	public IEnumerable<Ridge> GetRidges(IEnumerable<Vector3> points, float maxDistance, int minPointCount) {
		if (points.Count() < minPointCount) {
			yield break;
		}
		var remainingPoints = new HashSet<Vector3>(points);
		int maxTries = 50;

		while (remainingPoints.Any() && maxTries-- > 0) {
			Vector3 start = remainingPoints.TakeRandom();
			Vector3 end = start;
			while (end.Equals(start)) {
				end = remainingPoints.TakeRandom() - start;
			}

			Ridge ridge = new Ridge(start, end);
			var pointsOnRidge = remainingPoints.Where(p => ridge.GetDistance(p) < maxDistance).ToList();
			if (pointsOnRidge.Count < minPointCount) {
				continue;
			}
			var ordered = pointsOnRidge.OrderBy(p => ridge.GetValue(p));
			yield return new Ridge(ordered.First(), ordered.Last());

			if (remainingPoints.Count - pointsOnRidge.Count < minPointCount) {
				yield break;
			}
			foreach (var point in pointsOnRidge) {
				remainingPoints.Remove(point);
			}
		}

		yield break;
	}

	public void Classify() {
		Timekeeping.Reset();
		this.pointHashSet = new PointHashSet(2.0f, points);
		this.largeBucketHashSet = new PointHashSet(20.0f, points);
		Timekeeping.CompleteTask("Create Hashset");
		var ridgePoints = this.findRidgePoints().ToList();
		Timekeeping.CompleteTask("Find ridge points");
		var ridgePointHashSet = new PointHashSet(2.0f, ridgePoints.ToArray());
		var pointClusters = ridgePointHashSet.Cluster(0.6f, 40);
		Debug.Log(pointClusters.Count());
		Timekeeping.CompleteTask("Cluster");
		this.ridges = pointClusters.SelectMany(cluster => this.GetRidges(cluster, 1.0f, 10)).ToList();
		Timekeeping.CompleteTask("Find ridges");
		foreach (var cluster in pointClusters) {
			this.markClassified(cluster, Color.green);
		}
		Timekeeping.CompleteTask("Mark classified");
		this.displayRidges();
		Debug.Log(ridges.Count() + " ridges");
		Debug.Log(Timekeeping.GetStatus());
	}

	private void displayRidges() {
		var verts = new List<Vector3>();
		var triangles = new List<int>();

		foreach (var ridge in this.ridges) {
			int p = verts.Count();
			verts.Add(ridge.Start);
			verts.Add(ridge.End);
			verts.Add(ridge.End + Vector3.down * 4.0f);
			verts.Add(ridge.Start + Vector3.down * 4.0f);
			triangles.Add(p + 0);
			triangles.Add(p + 1);
			triangles.Add(p + 2);
			triangles.Add(p + 0);
			triangles.Add(p + 3);
			triangles.Add(p + 2);

			triangles.Add(p + 2);
			triangles.Add(p + 1);
			triangles.Add(p + 0);
			triangles.Add(p + 2);
			triangles.Add(p + 3);
			triangles.Add(p + 0);
		}
				
		var mesh = new Mesh();
		mesh.vertices = verts.ToArray();
		mesh.triangles = triangles.ToArray();

		var prefab = Resources.Load("RidgeMesh") as GameObject;
		var gameObject = GameObject.Instantiate(prefab) as GameObject;
		// gameObject.transform.parent = this.pointCloud.transform; // TODO this destroys the gameObject :(
		gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
		gameObject.transform.position = Vector3.zero;
	}

	private void markClassified(IEnumerable<Vector3> roofPoints, Color color) {
		foreach (var roofPoint in roofPoints) {
			this.pointCloud.Colors[this.pointHashSet.GetIndex(roofPoint)] = color;
		}
	}
}
