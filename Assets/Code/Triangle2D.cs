using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Triangle2D {
	public readonly Vector2 V1;
	public readonly Vector2 V2;
	public readonly Vector2 V3;

	public Triangle2D(Vector2 v1, Vector2 v2, Vector2 v3) {
		if ((v2.y - v1.y) * (v3.x - v1.x) - (v2.x - v1.x) * (v3.y - v1.y) > 0) {
			this.V1 = v1;
			this.V2 = v2;
			this.V3 = v3;
		} else {
			this.V1 = v3;
			this.V2 = v2;
			this.V3 = v1;
		}

		if (v1 == v2 || v2 == v3 || v3 == v1) {
			throw new Exception("Triangle vertices must be distinct. " + this);
		}
	}

	public bool Contains(Vector2 point) {
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

	public IEnumerable<Vector2> ToEnumerable() {
		yield return this.V1;
		yield return this.V2;
		yield return this.V2;
	}

	// !!! Does not work currently !!!
	public IEnumerable<Triangle2D> Without(Triangle2D triangle) {
		var points1 = this.ToEnumerable().ToArray();
		var points2 = triangle.ToEnumerable().ToArray();

		var points = this.ToEnumerable().Concat(triangle.ToEnumerable()).ToList();

		var indices1 = new List<int>(new int[] { 0, 1, 2 });
		var indices2 = new List<int>(new int[] { 3, 4, 5 });

		// Find all intersections between edges
		for (int i = 0; i < 3; i++) {
			for (int j = 0; j < 3; j++) {
				if (Math2d.CheckLinesIntersect(points1[i], points2[(i + 1) % 3], points2[j], points2[(j + 1) % 3])) {
					var intersect = Math2d.LineLineIntersection(points1[i], points2[(i + 1) % 3], points2[j], points2[(j + 1) % 3]);
					points.Add(intersect);
					indices1.Add(points.Count - 1);
					indices2.Add(points.Count - 1);
				}
			}
		}

		if (points.Count == 6) { // No intersections found
			if (triangle.Contains(points1[0])) {
				// This triangle is contained in the other one, result is empty
				yield break;
			} else if (this.Contains(points2[0])) {
				// Other triangle is contained in this one
				// TODO
				yield break;

			} else {
				// Triangles don't touch each other
				yield return this;
				yield break;
			}
		}

		// Make indices clockwise
		var center1 = (points1[0] + points1[1] + points1[2]) / 3.0f;
		var center2 = (points2[0] + points2[1] + points2[2]) / 3.0f;

		indices1.Sort((i, j) => getAngle(center1, points[i]).CompareTo(getAngle(center1, points[j])));
		indices2.Sort((i, j) => getAngle(center2, points[i]).CompareTo(getAngle(center2, points[j])));
		
		// Find polygons and triangulate
		var visited = new bool[3];
		for (int i = 0; i < 3; i++) {
			if (triangle.Contains(points[i]) || visited[i]) {
				continue;
			}
			int indices1position = indices1.IndexOf(i);
			var polygon = new List<int>();
			while (true) {
				indices1position = (indices1position + 1) % indices1.Count;
				int polygonIndex = indices1[indices1position];

				polygon.Add(polygonIndex);

				if (polygonIndex == i) {
					break;
				}
				if (polygonIndex < 3) {
					visited[polygonIndex] = true;
				}

				if (indices2.Contains(polygonIndex)) {
					int indices2position = indices2.IndexOf(polygonIndex);
					while (true) {
						indices2position = (indices2position - 1 + indices2.Count) % indices2.Count;
						polygonIndex = indices2[indices2position];
						polygon.Add(polygonIndex);

						if (indices1.Contains(polygonIndex)) {
							indices1position = indices1.IndexOf(polygonIndex);
							break;
						}
					}
				}
			}

			foreach (var t in Triangle2D.Triangulate(polygon.Select(p => points[p]).ToArray())) {
				yield return t;
			}
		}
	}

	private static float getAngle(Vector2 center, Vector2 vertex) {
		Vector2 relative = vertex - center;

		return Vector2.Angle(relative, Vector2.up) * (relative.x < 0 ? -1 : 1);
	}

	public static IEnumerable<Triangle2D> Triangulate(Vector2[] shape) {
		var triangulator = new Triangulator(shape);
		var triangles = triangulator.Triangulate();

		for (int i = 0; i < triangles.Length; i += 3) {
			yield return new Triangle2D(shape[triangles[i]], shape[triangles[i + 1]], shape[triangles[i + 2]]);
		}
	}

	public override string ToString() {
		return "Triangle2D: " + this.V1 + ", " + this.V2 + ", " + this.V3;
	}
}
