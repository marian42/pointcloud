using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
			yield return new Triangle(vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
		}
	}
}
