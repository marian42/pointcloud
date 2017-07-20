using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public abstract class AbstractPlaneFinder {
	public const float MaxDistance = 0.4f;

	[SerializeField, HideInInspector]
	
	public readonly PointCloud PointCloud;

	[SerializeField, HideInInspector]
	
	public List<Tuple<Plane, float>> PlanesWithScore;

	public AbstractPlaneFinder(PointCloud pointCloud) {
		this.PointCloud = pointCloud;
	}

	public abstract void Classify();

	public enum Type {
		Hough,
		Ransac
	}

	public static AbstractPlaneFinder Instantiate(Type type, PointCloud pointCloud) {
		switch (type) {
			case Type.Hough: return new HoughPlaneFinder(pointCloud);
			case Type.Ransac: return new RansacPlaneFinder(pointCloud);
			default: throw new NotImplementedException();
		}
	}

	private bool isGroundPlane(Plane plane) {
		return Mathf.Abs(plane.GetDistanceToPoint(this.PointCloud.GroundPoint)) < 2.0f
 			&& Vector3.Angle(Vector3.up, plane.normal) < 10.0f;
	}

	private bool isVerticalPlane(Plane plane) {
		var horizontalNormal = new Vector3(plane.normal.x, 0, plane.normal.z).normalized;
		return Vector3.Angle(horizontalNormal, plane.normal) < 10.0f;
	}	

	public void RemoveGroundPlanesAndVerticalPlanes() {
		this.PlanesWithScore = this.PlanesWithScore.Where(tuple => !this.isGroundPlane(tuple.Value1) && !this.isVerticalPlane(tuple.Value1)).ToList();
	}
}
