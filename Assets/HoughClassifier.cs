using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class HoughClassifier {
	[SerializeField, HideInInspector]
	private PointCloud pointCloud;
	private List<Tuple<Plane, int>> houghPlanes;

	public HoughClassifier(PointCloud pointCloud) {
		this.pointCloud = pointCloud;
	}

	public void Classify() {
		Timekeeping.Reset();
		this.deleteQuads();
		this.houghTransform();
		this.displayPlanes();
		Debug.Log(Timekeeping.GetStatus());
	}

	private const int houghSpaceSize = 30;
	private readonly int[] ranges = new int[] { houghSpaceSize, houghSpaceSize, 100 };
	private readonly float[] min = new float[] { -1.4f, -1.4f, -6 };
	private readonly float[] max = new float[] { +1.4f, +1.4f, 0 };
	public const float MaxDistance = 0.4f;
	private const float minHitsRelative = 0.0f;
	private int[, ,] houghSpace;

	private static float map(float oldLower, float oldUpper, float newLower, float newUpper, float value) {
		return Mathf.LerpUnclamped(newLower, newUpper, (value - oldLower) / (oldUpper - oldLower));
	}

	private static int limit(int lower, int upper, int value) {
		return System.Math.Max(lower, System.Math.Min(upper, value));
	}

	private Plane getHoughPlane(int i0, int i1, int i2) {
		return new Plane(new Vector3(map(0, ranges[0], min[0], max[0], i0), 1.0f, map(0, ranges[1], min[1], max[1], i1)), map(0, ranges[2], min[2], max[2], i2));
	}

	private Plane getHoughPlane(int i0, int i1) {
		return new Plane(new Vector3(map(0, ranges[0], min[0], max[0], i0), 1.0f, map(0, ranges[1], min[1], max[1], i1)), 0);
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
				Plane plane = this.getHoughPlane(i0, i1);
				for (int i = 0; i < this.pointCloud.CenteredPoints.Length; i++) {
					float distance = -plane.GetDistanceToPoint(this.pointCloud.CenteredPoints[i]);
					int start = Mathf.FloorToInt(map(this.min[2], this.max[2], 0, this.ranges[2], distance - HoughClassifier.MaxDistance));
					int end = Mathf.CeilToInt(map(this.min[2], this.max[2], 0, this.ranges[2], distance + HoughClassifier.MaxDistance));
					if ((start >= 0 && start < ranges[2]) || (end >= 0 && end < ranges[2])) {
						for (int i2 = limit(0, ranges[2] - 1, start); i2 <= limit(0, ranges[2] - 1, end); i2++) {
							houghSpace[i0, i1, i2]++;
						}
					}
				}
			}
		}
		Timekeeping.CompleteTask("Transform to Hough");
		this.houghPlanes = new List<Tuple<Plane, int>>();
		int max = 0;
		for (int i0 = 0; i0 < ranges[0]; i0++) {
			for (int i1 = 0; i1 < ranges[1]; i1++) {
				for (int i2 = 0; i2 < ranges[2]; i2++) {
					if (houghSpace[i0, i1, i2] > minHitsRelative * this.pointCloud.Points.Length && this.isLocalMaximum(i0, i1, i2, 4)) {
						Plane plane = this.getHoughPlane(i0, i1, i2);
						this.houghPlanes.Add(new Tuple<Plane, int>(plane, houghSpace[i0, i1, i2]));
					}
					if (houghSpace[i0, i1, i2] > max) {
						max = houghSpace[i0, i1, i2];
					}
				}
			}
		}	

		Timekeeping.CompleteTask("Find planes");
		Debug.Log("Found " + this.houghPlanes.Count + " planes, best: " + max + " / " + this.pointCloud.Points.Length);
	}

	private void createDebugPlane(Plane plane) {
		GameObject planeGameObject = new GameObject();
		var planeBehaviour = planeGameObject.AddComponent<PlaneBehaviour>();
		planeBehaviour.Plane = plane;
		planeBehaviour.PointCloud = this.pointCloud;
		planeBehaviour.Classifier = this;
		planeGameObject.transform.parent = this.pointCloud.transform;
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
		foreach (var tuple in this.houghPlanes.OrderByDescending(t => t.Value2).Take(4)) {
			var plane = tuple.Value1;
			this.createDebugPlane(plane);
		}
	}

	public int GetScore(Plane plane, Vector3 point) {
		return Mathf.Abs(plane.GetDistanceToPoint(point)) < HoughClassifier.MaxDistance ? 1 : 0;
	}
}