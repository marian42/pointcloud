using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class HoughClassifier {
	private PointCloud pointCloud;
	private Vector3[] centeredPoints;
	private List<Plane> houghPlanes;

	public HoughClassifier(PointCloud pointCloud) {
		this.pointCloud = pointCloud;
	}

	public void Classify() {
		Timekeeping.Reset();
		this.deleteQuads();
		this.centerPoints();
		this.houghTransform();
		this.displayPlanes();
		Debug.Log(Timekeeping.GetStatus());
	}

	private void centerPoints() {
		this.centeredPoints = new Vector3[pointCloud.Points.Length];
		for (int i = 0; i < this.pointCloud.Points.Length; i++) {
			this.centeredPoints[i] = this.pointCloud.Points[i] - this.pointCloud.transform.position;
		}
	}

	private readonly int[] ranges = new int[] { 20, 20, 20 };
	private readonly float[] min = new float[] { -1.2f, -1.2f, -6 };
	private readonly float[] max = new float[] { +1.2f, +1.2f, -3 };
	private const float maxDistance = 0.3f;
	private const float minHits = 1000;
	private int[, ,] houghSpace;

	private Plane getHoughPlane(int i0, int i1, int i2) {
		return new Plane(new Vector3(Mathf.Lerp(min[0], max[0], i0 / (float)(ranges[0])), 1.0f, Mathf.Lerp(min[1], max[1], i1 / (float)(ranges[1]))).normalized, Mathf.Lerp(min[2], max[2], i2 / (float)(ranges[2])));
	}

	private int getHoughHitsSafely(int i0, int i1, int i2) {
		if (i0 < 0 || i1 < 0 || i2 < 0 || i0 >= ranges[0] || i1 >= ranges[1] || i2 >= ranges[2]) {
			return 0;
		} else {
			return this.houghSpace[i0, i1, i2];
		}
	}

	private bool isLocalMaximum(int i0, int i1, int i2, int radius) {
		int hits = this.houghSpace[i0, i1, i2];
		for (int j0 = i0 - radius; j0 <= i0 + radius; j0++) {
			for (int j1 = i1 - radius; j1 <= i1 + radius; j1++) {
				for (int j2 = i2 - radius; j2 <= i2 + radius; j2++) {
					if (this.getHoughHitsSafely(j0, j1, j2) > hits) {
						return false;
					}
				}
			}
		}
		return true;
	}

	private void houghTransform() {
		houghSpace = new int[ranges[0], ranges[1], ranges[2]];
		Timekeeping.CompleteTask("Init Hough Space");
		for (int i0 = 0; i0 < ranges[0]; i0++) {
			for (int i1 = 0; i1 < ranges[1]; i1++) {
				for (int i2= 0; i2 < ranges[2]; i2++) {
					Plane plane = this.getHoughPlane(i0, i1, i2);
					for (int i = 0; i < this.centeredPoints.Length; i++) {
						if (Mathf.Abs(plane.GetDistanceToPoint(this.centeredPoints[i])) < maxDistance) {
							houghSpace[i0, i1, i2]++;
						}
					}
				}
			}
		}
		Timekeeping.CompleteTask("Transform to Hough");
		this.houghPlanes = new List<Plane>();
		int max = 0;
		for (int i0 = 0; i0 < ranges[0]; i0++) {
			for (int i1 = 0; i1 < ranges[1]; i1++) {
				for (int i2 = 0; i2 < ranges[2]; i2++) {
					if (houghSpace[i0, i1, i2] > minHits && this.isLocalMaximum(i0, i1, i2, 2)) {
						Plane plane = this.getHoughPlane(i0, i1, i2);
						this.houghPlanes.Add(plane);
						this.createDebugPlane(plane, "Plane: " + houghSpace[i0, i1, i2] + " points, n: " + (plane.normal / plane.normal.y) + ", d: " + plane.distance);
					}
					if (houghSpace[i0, i1, i2] > max) {
						max = houghSpace[i0, i1, i2];
					}
				}
			}
		}
		Timekeeping.CompleteTask("Find planes");
		Debug.Log("best: " + max);
	}

	private void createDebugPlane(Plane plane, string name) {
		{
			var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			quad.name = name;
			quad.tag = "Quad";
			quad.transform.parent = this.pointCloud.transform;
			quad.transform.localScale = Vector3.one * 10.0f;
			quad.transform.localPosition = -plane.normal * plane.distance;
			quad.transform.rotation = Quaternion.LookRotation(plane.normal);
		}
		{
			var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			quad.name = name;
			quad.tag = "Quad";
			quad.transform.parent = this.pointCloud.transform;
			quad.transform.localScale = Vector3.one * 10.0f;
			quad.transform.localPosition = -plane.normal * plane.distance;
			quad.transform.rotation = Quaternion.LookRotation(-plane.normal);
		}
	}

	private void deleteQuads() {
		var existingQuads = new List<GameObject>();
		foreach (var child in this.pointCloud.transform) {
			if ((child as Transform).tag == "Quad") {
				existingQuads.Add((child as Transform).gameObject);
			}
		}
		foreach (var existingQuad in existingQuads) {
			GameObject.DestroyImmediate(existingQuad);
		}
	}

	private void displayPlanes() {
	}
}
