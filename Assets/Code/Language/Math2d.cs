using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Math2d {
	// http://www.wyrmtale.com/blog/2013/115/2d-line-intersection-in-c
	public static Vector2 LineLineIntersection(Ray2D ray1, Ray2D ray2) {
		Vector2 line1a = ray1.origin;
		Vector2 line1b = ray1.origin + ray1.direction;
		Vector2 line2a = ray2.origin;
		Vector2 line2b = ray2.origin + ray2.direction;

		return Math2d.LineLineIntersection(line1a, line1b, line2a, line2b);
	}

	public static Vector2 ProjectTo2DRay(Vector2 point, Ray2D ray) {
		Vector2 direction = ray.direction.normalized;
		var v = point - direction;
		var d = Vector2.Dot(v, direction);
		return point + direction * d;
	}

	private class ParallelLinesException : System.Exception {}

	public static Vector2 LineLineIntersection(Vector2 line1a, Vector2 line1b, Vector2 line2a, Vector2 line2b) {
		// Get A,B,C of first line - points : ps1 to pe1
		float A1 = line1a.y - line1b.y;
		float B1 = line1b.x - line1a.x;
		float C1 = A1 * line1b.x + B1 * line1b.y;

		// Get A,B,C of second line - points : ps2 to pe2
		float A2 = line2a.y - line2b.y;
		float B2 = line2b.x - line2a.x;
		float C2 = A2 * line2b.x + B2 * line2b.y;

		// Get delta and check if the lines are parallel
		float delta = A1 * B2 - A2 * B1;
		if (delta == 0) {
			throw new ParallelLinesException();
		}

		// now return the Vector2 intersection point
		return new Vector2(
			(B2 * C1 - B1 * C2) / delta,
			(A1 * C2 - A2 * C1) / delta
		);
	}

	public static bool CheckLinesIntersect(Vector2 line1a, Vector2 line1b, Vector2 line2a, Vector2 line2b) {
		Vector2 dir1 = line1b - line1a;
		Vector2 dir2 = line2b - line2a;

		Vector2 intersect;
		try {
			intersect = Math2d.LineLineIntersection(line1a, line1b, line2a, line2b);
		}
		catch (ParallelLinesException) {
			return false;
		}
		

		float progress1 = Mathf.Abs(dir1.x) > Mathf.Abs(dir1.y) ? (intersect.x - line1a.x) / dir1.x : (intersect.y - line1a.y) / dir1.y;

		if (progress1 <= 0.0f || progress1 >= 1.0f) {
			return false;
		}

		float progress2 = Mathf.Abs(dir2.x) > Mathf.Abs(dir2.y) ? (intersect.x - line2a.x) / dir2.x : (intersect.y - line2a.y) / dir2.y;
		
		if (progress2 <= 0.0f || progress2 >= 1.0f) {
			return false;
		}

		return true;
    }
}
