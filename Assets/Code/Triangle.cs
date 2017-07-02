using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Triangle {
	public readonly Vector3 V1;
	public readonly Vector3 V2;
	public readonly Vector3 V3;

	public Vector3 Normal {
		get {
			var normal = Vector3.Cross(this.V2 - this.V1, this.V3 - this.V1);
			if (normal.y < 0) {
				return normal * -1f;
			} else {
				return normal;
			}
		}
	}

	public Plane Plane {
		get {
			return new Plane(this.Normal.normalized, this.V1);
		}
	}

	public Triangle(Vector3 v1, Vector3 v2, Vector3 v3) {
		if (Vector3.Cross(v2 - v1, v3 - v1).y > 0) {
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

	public IEnumerable<Vector3> ToEnumerable() {
		yield return this.V1;
		yield return this.V2;
		yield return this.V3;
	}

	public IEnumerable<Vector2> GetUV() {
		var up = Vector3.Cross(this.Normal, this.Normal - Vector3.up * this.Normal.y).normalized;
		if (up.y < 0) {
			up = up * -1;
		}
		var right = Vector3.Cross(this.Normal, up).normalized;

		var planeCoordinates = new PlaneCoordinates(this.Plane);

		var uvs = this.ToEnumerable().Select(v => planeCoordinates.ToPlane(v));
		const float scale = 5f;
		return uvs.Select(uv => uv / scale);
	}

	public static Mesh CreateMesh(IEnumerable<Triangle> triangles, bool twoSided = false) {
		var vertices = new List<Vector3>();
		var indices = new List<int>();
		var uvs = new List<Vector2>();

		int count = 0;
		foreach (var triangle in triangles) {
			vertices.AddRange(triangle.ToEnumerable());
			indices.AddRange(new[] { count, count + 1, count + 2 });
			uvs.AddRange(triangle.GetUV());

			count += 3;
		}

		if (twoSided) {
			foreach (var triangle in triangles) {
				vertices.AddRange(triangle.ToEnumerable());
				indices.AddRange(new[] { count + 2, count + 1, count });
				uvs.AddRange(triangle.GetUV());
				count += 3;
			}
		}

		Mesh result = new Mesh();
		result.vertices = vertices.ToArray();
		result.triangles = indices.ToArray();

		result.uv = uvs.ToArray();
		result.RecalculateNormals();
		result.RecalculateBounds();
		return result;
	}

	public static IEnumerable<Triangle> GetTriangles(Vector3[] vertices, int[] triangles) {
		for (int i = 0; i < triangles.Length; i += 3) {
			if (triangles[i] == triangles[i + 1] || triangles[i + 1] == triangles[i + 2] || triangles[i + 2] == triangles[i]) {
				continue;
			}
			var triangle = Triangle.TryCreate(vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
			if (triangle != null) {
				yield return triangle;
			}
		}
	}

	private const float smallDistance = 0.01f;

	public Tuple<IEnumerable<Triangle>, IEnumerable<Triangle>> Split(Plane plane) {
		float[] distance = new float[] {
			plane.GetDistanceToPoint(this.V1),
			plane.GetDistanceToPoint(this.V2),
			plane.GetDistanceToPoint(this.V3)
		};

		int onPlane = distance.Count(d => Mathf.Abs(d) <= smallDistance);
		int abovePlane = distance.Count(d => Mathf.Abs(d) > smallDistance && d > 0);
		int belowPlane = distance.Count(d => Mathf.Abs(d) > smallDistance && d < 0);

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

			var intersect1 = Math3d.LinePlaneIntersection(plane, single, double1);
			var intersect2 = Math3d.LinePlaneIntersection(plane, single, double2);

			if (abovePlane == 1) {
				return new Tuple<IEnumerable<Triangle>, IEnumerable<Triangle>>(
					Triangle.TryCreateEnum(single, intersect1, intersect2),
					Triangle.TryCreateEnum(double1, double2, intersect1)
					.Concat(Triangle.TryCreateEnum(double2, intersect1, intersect2)));
			} else {
				return new Tuple<IEnumerable<Triangle>, IEnumerable<Triangle>>(
					Triangle.TryCreateEnum(double1, double2, intersect1)
					.Concat(Triangle.TryCreateEnum(double2, intersect1, intersect2)),
					Triangle.TryCreateEnum(single, intersect1, intersect2));
			}
			
		}
		if (onPlane == 1) {
			var vertexOn = this.ToEnumerable().First(v => Mathf.Abs(plane.GetDistanceToPoint(v)) <= smallDistance);
			var vertexBelow = this.ToEnumerable().First(v => plane.GetDistanceToPoint(v) < 0 && Mathf.Abs(plane.GetDistanceToPoint(v)) > smallDistance);
			var vertexAbove = this.ToEnumerable().First(v => plane.GetDistanceToPoint(v) > 0 && Mathf.Abs(plane.GetDistanceToPoint(v)) > smallDistance);
			var ray = new Ray(vertexAbove, vertexBelow - vertexAbove);
			float dst;
			plane.Raycast(ray, out dst);
			var intersect = ray.GetPoint(dst);

			return new Tuple<IEnumerable<Triangle>, IEnumerable<Triangle>>(Triangle.TryCreateEnum(vertexBelow, vertexOn, intersect), Triangle.TryCreateEnum(vertexAbove, vertexOn, intersect));
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

	public bool ContainsXZ(Vector3 point) {
		float ax, az, bx, bz, cx, cz, apx, apz, bpx, bpz, cpx, cpz;
		float cCROSSap, bCROSScp, aCROSSbp;

		ax = this.V1.x - this.V2.x; az = this.V1.z - this.V2.z;
		bx = this.V3.x - this.V1.x; bz = this.V3.z - this.V1.z;
		cx = this.V2.x - this.V3.x; cz = this.V2.z - this.V3.z;
		apx = point.x - this.V3.x; apz = point.z - this.V3.z;
		bpx = point.x - this.V2.x; bpz = point.z - this.V2.z;
		cpx = point.x - this.V1.x; cpz = point.z - this.V1.z;

		aCROSSbp = ax * bpz - az * bpx;
		cCROSSap = cx * apz - cz * apx;
		bCROSScp = bx * cpz - bz * cpx;

		return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
	}

	public float GetScore(PointCloud pointCloud) {
		float result = 0;

		var plane = this.Plane;

		for (int i = 0; i < pointCloud.CenteredPoints.Length; i++) {
			if (!this.ContainsXZ(pointCloud.CenteredPoints[i])) {
				continue;
			}
			result += pointCloud.GetScore(i, plane);
		}

		return result;
	}

	public float GetScore(PointCloud pointCloud, IEnumerable<int> indices) {
		float result = 0;

		var plane = this.Plane;

		foreach (int i in indices) {
			if (!this.ContainsXZ(pointCloud.CenteredPoints[i])) {
				continue;
			}
			result += pointCloud.GetScore(i, plane);
		}

		return result;
	}

	public bool Contains(Vector3 point) {
		if (!this.ContainsXZ(point)) {
			return false;
		}

		return Mathf.Abs(this.Plane.GetDistanceToPoint(point)) < HoughPlaneFinder.MaxDistance;
	}

	public int GetPointCount(PointCloud pointCloud, IEnumerable<int> indices) {
		int result = 0;
		var plane = this.Plane;

		foreach (int i in indices) {
			if (!this.ContainsXZ(pointCloud.CenteredPoints[i])) {
				continue;
			}
			if (Mathf.Abs(plane.GetDistanceToPoint(pointCloud.CenteredPoints[i])) < HoughPlaneFinder.MaxDistance) {
				result++;
			}
		}

		return result;
	}

	public float GetArea() {
		return Vector3.Cross(this.V2 - this.V1, this.V3 - this.V1).magnitude / 2.0f;
	}

	public Triangle2D ProjectToGround() {
		return new Triangle2D(new Vector2(this.V1.x, this.V1.z), new Vector2(this.V2.x, this.V2.z), new Vector2(this.V3.x, this.V3.z));
	}

	public static IEnumerable<Triangle> TryCreateEnum(Vector3 v1, Vector3 v2, Vector3 v3) {
		if (v1 == v2 || v2 == v3 || v3 == v1) {
			yield break;
		}

		var result = new Triangle(v1, v2, v3);

		if (result.GetArea() > 0.01f) {
			yield return result;
		}
	}

	public static Triangle TryCreate(Vector3 v1, Vector3 v2, Vector3 v3) {
		if (v1 == v2 || v2 == v3 || v3 == v1) {
			return null;
		}

		var result = new Triangle(v1, v2, v3);

		if (result.GetArea() > 0.01f) {
			return result;
		} else {
			return null;
		}
	}
}
