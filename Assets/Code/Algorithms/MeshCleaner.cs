using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MeshCleaner : MonoBehaviour {

	public static IEnumerable<Triangle> CleanMesh(IEnumerable<Triangle> triangles) {
		List<Vector2> samplePoints = new List<Vector2>();

		foreach (var triangle in triangles) {
			foreach (var point in triangle.ToEnumerable()) {
				var projected = new Vector2(point.x, point.z);
				if (!samplePoints.Any(p => (p - projected).magnitude < 0.3f)) {
					samplePoints.Add(projected);
				}
			}
		}

		return MeshCleaner.SampleMesh(triangles, samplePoints);
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

		var indices = PointMeshCreator.Triangulate(samplePoints).ToArray();

		for (int i = 0; i < indices.Length; i += 3) {
			var triangle = new Triangle(points3d[indices[i]], points3d[indices[i+1]], points3d[indices[i+2]]);
			var center = triangle.Center;
			if (!triangles.Any(t => t.ContainsXZ(center)) || Mathf.Abs(Vector3.Angle(triangle.Normal, Vector3.up) - 90.0f) < 5.0f) {
				continue;
			}
			yield return triangle;
		}
	}
}
