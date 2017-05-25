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

	private Vector3 groundPoint;

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
		return Mathf.Max(0.0f, 1.0f - distance);
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

	private bool isNonGroundPlane(Plane plane) {
		return Mathf.Abs(plane.GetDistanceToPoint(this.groundPoint)) > 2.0f
 			|| Vector3.Angle(Vector3.up, plane.normal) > 10.0f;
	}

	public void RemoveGroundPlanes() {
		this.groundPoint = this.PointCloud.CenteredPoints.OrderBy(p => p.y).Skip(20).FirstOrDefault();
		this.PlanesWithScore = this.PlanesWithScore.Where(tuple => this.isNonGroundPlane(tuple.Value1)).ToList();
	}
}
