using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Triangle2D {
	public readonly Vector2 V1;
	public readonly Vector2 V2;
	public readonly Vector2 V3;

	public Triangle2D(Vector2 v1, Vector2 v2, Vector2 v3) {
		if ((v2.y - v1.y) * (v3.x - v1.x) - (v2.x - v1.x) * (v3.y - v1.y) > 0) {
			this.V1 = v1;
			this.V1 = v2;
			this.V3 = v3;
		} else {
			this.V1 = v3;
			this.V2 = v2;
			this.V3 = v1;
		}
	}

	public bool ContainsXZ(Vector2 point) {
		float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
		float cCROSSap, bCROSScp, aCROSSbp;

		ax = this.V1.x - this.V2.x; ay = this.V1.y - this.V2.y;
		bx = this.V3.x - this.V1.x; by = this.V3.y - this.V1.y;
		cx = this.V2.x - this.V3.x; cy = this.V2.y - this.V3.y;
		apx = point.x - this.V3.x; apy = point.y - this.V3.y;
		bpx = point.x - this.V2.x; bpy = point.y - this.V2.y;
		cpx = point.x - this.V1.x; cpy = point.y - this.V1.y;

		aCROSSbp = ax * bpy - ay * bpx;
		cCROSSap = cx * apy - cy * apx;
		bCROSScp = bx * cpy - by * cpx;

		return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
	}

	public bool Intersects(Triangle2D otherTriangle) {
		return
			   Math2d.CheckLinesIntersect(this.V1, this.V2, otherTriangle.V1, otherTriangle.V2)
			|| Math2d.CheckLinesIntersect(this.V1, this.V2, otherTriangle.V2, otherTriangle.V3)
			|| Math2d.CheckLinesIntersect(this.V1, this.V2, otherTriangle.V3, otherTriangle.V1)
			|| Math2d.CheckLinesIntersect(this.V2, this.V3, otherTriangle.V1, otherTriangle.V2)
			|| Math2d.CheckLinesIntersect(this.V2, this.V3, otherTriangle.V2, otherTriangle.V3)
			|| Math2d.CheckLinesIntersect(this.V2, this.V3, otherTriangle.V3, otherTriangle.V1)
			|| Math2d.CheckLinesIntersect(this.V3, this.V1, otherTriangle.V1, otherTriangle.V2)
			|| Math2d.CheckLinesIntersect(this.V3, this.V1, otherTriangle.V2, otherTriangle.V3)
			|| Math2d.CheckLinesIntersect(this.V3, this.V1, otherTriangle.V3, otherTriangle.V1);
	}

	public Triangle ProjectFromGroundToPlane(Plane plane) {
		return new Triangle(Math3d.ProjectFromGroundToPlane(this.V1, plane), Math3d.ProjectFromGroundToPlane(this.V2, plane), Math3d.ProjectFromGroundToPlane(this.V3, plane));
	}

	public Vector2[] ToArray() {
		return new Vector2[] { this.V1, this.V2, this.V3 };
	}

	public IEnumerable<Triangle2D> Without(Triangle2D triangle) {
		var arr1 = this.ToArray();
		var arr2 = triangle.ToArray();

		// TODO
		
		yield break;
	}
}
