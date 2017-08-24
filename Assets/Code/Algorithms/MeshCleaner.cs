using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MeshCleaner {
	private const float minDistance = 0.6f;

	public static List<Triangle> CleanMesh(IEnumerable<Triangle> triangles) {
		List<Vector2> samplePoints = new List<Vector2>();

		foreach (var triangle in triangles) {
			samplePoints.AddRange(getSamplePoints(triangle));
		}
		
		return MeshCleaner.SampleMesh(triangles, samplePoints).ToList();
	}

	private static IEnumerable<Vector2> getSamplePoints(Triangle triangle) {
		var verts = triangle.ToEnumerable().Select(p => new Vector2(p.x, p.z)).ToArray();
		foreach (var point in verts) {
			yield return point;
		}
		yield return triangle.Center;

		for (int i = 0; i < 3; i++) {
			var v1 = verts[i];
			var v2 = verts[(i + 1) % 3];
			float distance = (v1 - v2).magnitude;
			int steps = Mathf.FloorToInt(distance / minDistance * 2.0f);
			for (int p = 1; p < steps; p++) {
				yield return Vector2.Lerp(v1, v2, distance * p / steps);
			}
		}
	}

	public static IEnumerable<Triangle> SampleMesh(IEnumerable<Triangle> triangles, List<Vector2> samplePoints) {
		var points3d = new List<Vector3>();

		for (int i = 0; i < samplePoints.Count; i++) {
			var projected = triangles.Where(t => t.ContainsXZ(samplePoints[i])).Select(t => Math3d.ProjectFromGroundToPlane(samplePoints[i], t.Plane));
			if (!projected.Any()) {
				samplePoints.RemoveAt(i);
				i--;
			} else {
				points3d.Add(projected.OrderByDescending(p => p.y).First());
			}
		}

		var distinctPoints = new List<Vector3>();

		foreach (var point in points3d.OrderByDescending(p => p.y)) {
			if (!distinctPoints.Any(p => Mathf.Pow(p.x - point.x, 2.0f) + Mathf.Pow(p.z - point.z, 2.0f) < Mathf.Pow(minDistance, 2.0f))) {
				distinctPoints.Add(point);
			}
		}

		var indices = PointMeshCreator.Triangulate(distinctPoints.Select(p => new Vector2(p.x, p.z)).ToList()).ToArray();

		for (int i = 0; i < indices.Length; i += 3) {
			var triangle = new Triangle(distinctPoints[indices[i]], distinctPoints[indices[i + 1]], distinctPoints[indices[i + 2]]);
			var center = triangle.Center;
			if (!triangles.Any(t => t.ContainsXZ(center)) || Mathf.Abs(Vector3.Angle(triangle.Normal, Vector3.up) - 90.0f) < 5.0f) {
				continue;
			}
			yield return triangle;
		}
	}
}