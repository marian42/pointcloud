using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneCoordinates {
	public readonly Plane Plane;
	public readonly Vector3 Pivot;
	public readonly Vector3 Up;
	public readonly Vector3 Right;

	public PlaneCoordinates(Plane plane) {
		this.Plane = plane;
		this.Pivot = Math3d.PointOnPlane(plane);

		this.Up = Vector3.Cross(this.Plane.normal, this.Plane.normal - Vector3.up * this.Plane.normal.y).normalized;
		if (this.Up.y < 0) {
			this.Up = this.Up * -1;
		}
		this.Right = Vector3.Cross(this.Plane.normal, this.Up).normalized;
	}

	private Vector3 projectToPlane(Vector3 point) {
		float distance = this.Plane.GetDistanceToPoint(point);
		return point - this.Plane.normal * distance;
	}

	public Vector2 ToPlane(Vector3 point) {
		var local = this.projectToPlane(point) - this.Pivot;
		return new Vector2(Vector3.Dot(this.Up, local), Vector3.Dot(this.Right, local));
	}

	public Vector3 ToWorld(Vector2 point) {
		return this.Pivot + this.Up * point.x + this.Right * point.y;
	}
}
