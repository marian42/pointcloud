using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class HoughPlaneFinder : PlaneClassifier {

	private const int houghSpaceSize = 20;
	private readonly int[] ranges = new int[] { houghSpaceSize, houghSpaceSize, 200 };
	private readonly float[] min = new float[] { -1.4f, -1.4f, -6 };
	private readonly float[] max = new float[] { +1.4f, +1.4f, 4 };
	private const float minScoreRelative = 0.05f;
	private float[, ,] houghSpace;

	public HoughPlaneFinder(PointCloud pointCloud) : base(pointCloud) { }

	public override void Classify() {
		Timekeeping.Reset();
		this.houghTransform();
	}

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

	private int[] getPlaneParameters(Plane plane) {
		var normal = plane.normal / plane.normal.y;
		int i = Mathf.RoundToInt(map(min[0], max[0], 0, ranges[0], normal.x));
		int j = Mathf.RoundToInt(map(min[1], max[1], 0, ranges[1], normal.z));
		int k = Mathf.RoundToInt(map(min[2], max[2], 0, ranges[2], plane.distance));
		return new int[] { i, j, k };
	}

	private float getHoughScoreSafely(int i0, int i1, int i2) {
		if (i0 < 0 || i1 < 0 || i2 < 0 || i0 >= ranges[0] || i1 >= ranges[1] || i2 >= ranges[2]) {
			return 0;
		} else {
			return this.houghSpace[i0, i1, i2];
		}
	}

	private bool isLocalMaximum(int i0, int i1, int i2, int radius) {
		float score = this.houghSpace[i0, i1, i2];
		for (int j0 = i0 - radius; j0 <= i0 + radius; j0++) {
			for (int j1 = i1 - radius; j1 <= i1 + radius; j1++) {
				for (int j2 = i2 - radius; j2 <= i2 + radius; j2++) {
					if (this.getHoughScoreSafely(j0, j1, j2) > score) {
						return false;
					}
				}
			}
		}
		return true;
	}

	private void houghTransform() {
		houghSpace = new float[ranges[0], ranges[1], ranges[2]];
		Timekeeping.CompleteTask("Init Hough Space");
		for (int i0 = 0; i0 < ranges[0]; i0++) {
			for (int i1 = 0; i1 < ranges[1]; i1++) {
				Plane plane = this.getHoughPlane(i0, i1);
				for (int i = 0; i < this.PointCloud.CenteredPoints.Length; i++) {
					float distance = -plane.GetDistanceToPoint(this.PointCloud.CenteredPoints[i]);
					int start = Mathf.FloorToInt(map(this.min[2], this.max[2], 0, this.ranges[2], distance - HoughPlaneFinder.MaxDistance));
					int end = Mathf.CeilToInt(map(this.min[2], this.max[2], 0, this.ranges[2], distance + HoughPlaneFinder.MaxDistance));
					if ((start >= 0 && start < ranges[2]) || (end >= 0 && end < ranges[2])) {
						for (int i2 = limit(0, ranges[2] - 1, start); i2 <= limit(0, ranges[2] - 1, end); i2++) {
							float relativeDistance = Mathf.Abs(map(0, ranges[2], min[2], max[2], i2) - distance) / HoughPlaneFinder.MaxDistance;
							houghSpace[i0, i1, i2] += HoughPlaneFinder.getScore(relativeDistance);
						}
					}
				}
			}
		}
		Timekeeping.CompleteTask("Transform to Hough");
		this.PlanesWithScore = new List<Tuple<Plane, float>>();
		float maxScore = 0;
		for (int i0 = 0; i0 < ranges[0]; i0++) {
			for (int i1 = 0; i1 < ranges[1]; i1++) {
				for (int i2 = 0; i2 < ranges[2]; i2++) {
					if (houghSpace[i0, i1, i2] > minScoreRelative * this.PointCloud.Points.Length && this.isLocalMaximum(i0, i1, i2, 3)) {
						Plane plane = this.getHoughPlane(i0, i1, i2);
						this.PlanesWithScore.Add(new Tuple<Plane, float>(plane, houghSpace[i0, i1, i2]));
					}
					if (houghSpace[i0, i1, i2] > maxScore) {
						maxScore = houghSpace[i0, i1, i2];
					}
				}
			}
		}

		Timekeeping.CompleteTask("Find planes");
	}

	private static float getScore(float relativeDistance) {
		if (relativeDistance > 1.0f) {
			return 0;
		}
		return 1.0f - relativeDistance;
	}
}