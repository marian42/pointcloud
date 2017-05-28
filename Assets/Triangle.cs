using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Triangle {
	public readonly Vector3 V1;
	public readonly Vector3 V2;
	public readonly Vector3 V3;

	public Triangle(Vector3 v1, Vector3 v2, Vector3 v3) {
		this.V1 = v1;
		this.V2 = v2;
		this.V3 = v3;
	}

	public IEnumerable<Vector3> ToEnumerable() {
		yield return this.V1;
		yield return this.V2;
		yield return this.V3;
	}

	public static Mesh CreateMesh(IEnumerable<Triangle> triangles, bool twoSided = false) {
		var vertices = new List<Vector3>();
		var indices = new List<int>();

		int count = 0;
		foreach (var triangle in triangles) {
			vertices.AddRange(triangle.ToEnumerable());
			indices.AddRange(new[] { count, count + 1, count + 2 });
			count += 3;
		}

		if (twoSided) {
			foreach (var triangle in triangles) {
				vertices.AddRange(triangle.ToEnumerable());
				indices.AddRange(new[] { count + 2, count + 1, count });
				count += 3;
			}
		}

		Mesh result = new Mesh();
		result.vertices = vertices.ToArray();
		result.triangles = indices.ToArray();
		result.RecalculateNormals();
		result.RecalculateBounds();
		return result;
	}

	public static IEnumerable<Triangle> GetTriangles(Vector3[] vertices, int[] triangles) {
		for (int i = 0; i < triangles.Length; i += 3) {
			if (triangles[i] == triangles[i + 1] || triangles[i + 1] == triangles[i + 2] || triangles[i + 2] == triangles[i]) {
				continue;
			}
			yield return new Triangle(vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
		}
	}

	public Tuple<IEnumerable<Triangle>, IEnumerable<Triangle>> Split(Plane plane) {
		float[] distance = new float[] {
			plane.GetDistanceToPoint(this.V1),
			plane.GetDistanceToPoint(this.V2),
			plane.GetDistanceToPoint(this.V3)
		};

		int onPlane = distance.Count(d => d == 0);
		int abovePlane = distance.Count(d => d > 0);
		int belowPlane = distance.Count(d => d < 0);

		if (belowPlane == 0) {
			return new Tuple<IEnumerable<Triangle>,IEnumerable<Triangle>>(this.Yield(), Enumerable.Empty<Triangle>());
		}
		if (abovePlane == 0) {
			return new Tuple<IEnumerable<Triangle>,IEnumerable<Triangle>>(Enumerable.Empty<Triangle>(), this.Yield());
		}
		if (onPlane == 0) {
			var single = abovePlane == 1 ? this.ToEnumerable().First(v => plane.GetDistanceToPoint(v) > 0) : this.ToEnumerable().First(v => plane.GetDistanceToPoint(v) < 0);
			var double1 = abovePlane == 1 ? this.ToEnumerable().First(v => plane.GetDistanceToPoint(v) < 0) : this.ToEnumerable().First(v => plane.GetDistanceToPoint(v) > 0);
			var double2 = abovePlane == 1 ? this.ToEnumerable().Where(v => plane.GetDistanceToPoint(v) < 0).Skip(1).First() : this.ToEnumerable().Where(v => plane.GetDistanceToPoint(v) > 0).Skip(1).First();

			var ray1 = new Ray(single, double1 - single);
			float dst1;
			if (!plane.Raycast(ray1, out dst1)) {
				throw new Exception("Ray didn't hit plane");
			}
			var intersect1 = ray1.GetPoint(dst1);
			var ray2 = new Ray(single, double2 - single);
			float dst2;
			if (!plane.Raycast(ray2, out dst2)) {
					throw new Exception("Ray didn't hit plane");
			}
			var intersect2 = ray2.GetPoint(dst2);

			if (abovePlane == 1) {
				return new Tuple<IEnumerable<Triangle>, IEnumerable<Triangle>>(new Triangle(single, intersect1, intersect2).Yield(), new Triangle(double1, double2, intersect1).Yield().Concat(new Triangle(double2, intersect1, intersect2).Yield()));
			} else {
				return new Tuple<IEnumerable<Triangle>, IEnumerable<Triangle>>(new Triangle(double1, double2, intersect1).Yield().Concat(new Triangle(double2, intersect1, intersect2).Yield()), new Triangle(single, intersect1, intersect2).Yield());
			}
			
		}
		if (onPlane == 1) {
			var vertexOn = this.ToEnumerable().First(v => plane.GetDistanceToPoint(v) == 0);
			var vertexBelow = this.ToEnumerable().First(v => plane.GetDistanceToPoint(v) < 0);
			var vertexAbove = this.ToEnumerable().First(v => plane.GetDistanceToPoint(v) > 0);
			var ray = new Ray(vertexAbove, vertexBelow - vertexAbove);
			float dst;
			plane.Raycast(ray, out dst);
			var intersect = ray.GetPoint(dst);

			return new Tuple<IEnumerable<Triangle>,IEnumerable<Triangle>>(new Triangle(vertexBelow, vertexOn, intersect).Yield(), new Triangle(vertexAbove, vertexOn, intersect).Yield());
		}
		return new Tuple<IEnumerable<Triangle>, IEnumerable<Triangle>>(Enumerable.Empty<Triangle>(), Enumerable.Empty<Triangle>());
	}

	public static Tuple<IEnumerable<Triangle>, IEnumerable<Triangle>> SplitMesh(IEnumerable<Triangle> triangles, Plane plane) {
		var tuples = triangles.Select(t => t.Split(plane));
		return new Tuple<IEnumerable<Triangle>, IEnumerable<Triangle>>(tuples.SelectMany(t => t.Value1).NonNull(), tuples.SelectMany(t => t.Value2).NonNull());
	}

	public static IEnumerable<Triangle> CutMesh(IEnumerable<Triangle> triangles, Plane plane, bool keepAbove) {
		return triangles.SelectMany(t => keepAbove ? t.Split(plane).Value1 : t.Split(plane).Value2).NonNull();
	}

	public override string ToString() {
		return "Triangle: " + this.V1 + ", " + this.V2 + ", " + this.V3;
	}
}
