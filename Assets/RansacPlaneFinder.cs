using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RansacPlaneFinder : PlaneClassifier {
	private const int randomSampleCount = 1000;

	public RansacPlaneFinder(PointCloud pointCloud) : base(pointCloud) { }

	public override void Classify() {
		Timekeeping.Reset();

		this.PointCloud.EstimateNormals();
		Timekeeping.CompleteTask("Estimate normals");


		var indices = Enumerable.Range(0, this.PointCloud.Points.Length).ToList();
		
		while (indices.Count() > RansacPlaneFinder.randomSampleCount) {
			indices.RemoveAt(Random.Range(0, indices.Count));
		}

		var planes = indices.Select(i => new Plane(this.PointCloud.Normals[i], this.PointCloud.CenteredPoints[i])).ToList();
		Timekeeping.CompleteTask("Choose samples");

		this.PlanesWithScore = planes.Select(plane => new Tuple<Plane, float>(plane, this.getScore(plane)))
			.OrderByDescending(tuple => tuple.Value2).ToList();

		Timekeeping.CompleteTask("Score");

		for (int i = 0; i < this.PlanesWithScore.Count; i++) {
			var plane = this.PlanesWithScore.ElementAt(i).Value1;
			for (int j = i + 1; j < this.PlanesWithScore.Count; j++) {
				if (this.similar(plane, this.PlanesWithScore[j].Value1)) {
					this.PlanesWithScore.RemoveAt(j);
					j--;
				}
			}
		}

		Timekeeping.CompleteTask("Remove duplicates");

		Debug.Log(Timekeeping.GetStatus() + " -> " + planes.Count() + " planes out of " + this.PointCloud.Points.Length + " points.");
	}

	private float getScore(Plane plane) {
		const float maxDistance = HoughPlaneFinder.MaxDistance;
		float result = 0;

		foreach (var point in this.PointCloud.CenteredPoints) {
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
