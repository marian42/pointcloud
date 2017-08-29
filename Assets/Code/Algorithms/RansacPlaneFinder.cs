using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RansacPlaneFinder : AbstractPlaneFinder {
	private const int randomSampleCount = 400;

	public RansacPlaneFinder(PointCloud pointCloud) : base(pointCloud) { }


	public override void Classify() {
		this.Classify(null);	
	}

	public void Classify(IEnumerable<int> indicesParam) {
		if (this.PointCloud.Normals == null || this.PointCloud.Normals.Length == 0) {
			this.PointCloud.EstimateNormals();
		}

		List<int> indices;
		if (indicesParam == null) {
			indices = Enumerable.Range(0, this.PointCloud.Points.Length).ToList();
		} else {
			indices = indicesParam.ToList();
		} 			
		
		while (indices.Count() > RansacPlaneFinder.randomSampleCount) {
			indices.RemoveAt(Random.Range(0, indices.Count));
		}

		var planes = indices.Select(i => new Plane(this.PointCloud.Normals[i], this.PointCloud.Points[i])).ToList();

		this.PlanesWithScore = planes.Select(plane => new Tuple<Plane, float>(plane, this.PointCloud.GetScore(plane)))
			.OrderByDescending(tuple => tuple.Value2).ToList();

		for (int i = 0; i < this.PlanesWithScore.Count; i++) {
			var plane = this.PlanesWithScore.ElementAt(i).Value1;
			for (int j = i + 1; j < this.PlanesWithScore.Count; j++) {
				if (RansacPlaneFinder.Similar(plane, this.PlanesWithScore[j].Value1, this.PointCloud.GroundPoint)) {
					this.PlanesWithScore.RemoveAt(j);
					j--;
				}
			}
		}
	}	

	public static bool Similar(Plane plane1, Plane plane2, Vector3 reference) {
		return Vector3.Angle(plane1.normal, plane2.normal) < 20.0f
			&& Mathf.Abs(plane1.GetDistanceToPoint(reference) - plane2.GetDistanceToPoint(reference)) < 2.0f;
	}
}
