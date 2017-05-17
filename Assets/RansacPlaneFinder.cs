using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RansacPlaneFinder {
	[SerializeField, HideInInspector]
	private PointCloud pointCloud;

	private const int randomSampleCount = 1000;

	public RansacPlaneFinder(PointCloud pointCloud) {
		this.pointCloud = pointCloud;
	}

	public void Classify() {
		Timekeeping.Reset();

		this.pointCloud.EstimateNormals();
		Timekeeping.CompleteTask("Estimate normals");


		var indices = Enumerable.Range(0, this.pointCloud.Points.Length).ToList();
		
		while (indices.Count() > RansacPlaneFinder.randomSampleCount) {
			indices.RemoveAt(Random.Range(0, indices.Count));
		}

		var planes = indices.Select(i => new Plane(this.pointCloud.Normals[i], this.pointCloud.CenteredPoints[i])).ToList();
		Timekeeping.CompleteTask("Choose samples");

		planes = planes.Select(plane => new Tuple<Plane, float>(plane, this.getScore(plane)))
			.OrderByDescending(tuple => tuple.Value2)
			.Select(tuple => tuple.Value1)
			.ToList();

		Timekeeping.CompleteTask("Score");

		for (int i = 0; i < planes.Count; i++) {
			var plane = planes.ElementAt(i);
			for (int j = i + 1; j < planes.Count; j++) {
				if (this.similar(plane, planes[j])) {
					planes.RemoveAt(j);
					j--;
				}
			}
		}

		Timekeeping.CompleteTask("Remove duplicates");

		foreach (var plane in planes.Take(6)) {
			PlaneBehaviour.DisplayPlane(plane, this.pointCloud);
		}

		Debug.Log(Timekeeping.GetStatus() + " -> " + planes.Count() + " planes out of " + this.pointCloud.Points.Length + " points.");
	}

	private float getScore(Plane plane) {
		const float maxDistance = HoughPlaneFinder.MaxDistance;
		float result = 0;

		foreach (var point in this.pointCloud.CenteredPoints) {
			float distance = Mathf.Abs(plane.GetDistanceToPoint(point)) / maxDistance;
			if (distance > 1) {
				continue;
			}
			result += 1.0f - distance;
		}
		return result;
	}

	private bool similar(Plane plane1, Plane plane2) {
		return Vector3.Angle(plane1.normal, plane2.normal) < 20.0f
			&& Mathf.Abs(plane1.distance - plane2.distance) < 2.0f;
	}
}
