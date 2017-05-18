using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public abstract class PlaneClassifier {
	public const float MaxDistance = 0.4f;

	[SerializeField, HideInInspector]
	
	protected PointCloud PointCloud;

	[SerializeField, HideInInspector]
	
	public List<Tuple<Plane, float>> PlanesWithScore;

	public PlaneClassifier(PointCloud pointCloud) {
		this.PointCloud = pointCloud;
	}

	public abstract void Classify();

	public void DisplayPlanes(int count) {
		foreach (var tuple in this.PlanesWithScore.OrderByDescending(t => t.Value2).Take(count)) {
			var plane = tuple.Value1;
			PlaneBehaviour.DisplayPlane(plane, this.PointCloud);
		}
	}

	public static float GetScore(Plane plane, Vector3 point) {
		float distance = Mathf.Abs(plane.GetDistanceToPoint(point)) / HoughPlaneFinder.MaxDistance;
		if (distance > 1.0f) {
			return 0;
		}
		return HoughPlaneFinder.getScore(distance);
	}

	protected static float getScore(float relativeDistance) {
		return 1.0f - relativeDistance;
	}

	public enum Type {
		Hough,
		Ransac
	}

	public static PlaneClassifier Instantiate(Type type, PointCloud pointCloud) {
		switch (type) {
			case Type.Hough: return new HoughPlaneFinder(pointCloud);
			case Type.Ransac: return new RansacPlaneFinder(pointCloud);
			default: throw new NotImplementedException();
		}
	}
}
