using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PointMeshCreator : MeshCreator {
	public PointMeshCreator(PointCloud pointCloud) : base(pointCloud) {
	}

	public void CreateMesh() {
		foreach (var plane in this.Planes.Take(1)) {
			var onPlane = this.PointCloud.CenteredPoints.Where(p => Mathf.Abs(plane.GetDistanceToPoint(p)) < HoughPlaneFinder.MaxDistance);

			var planeCoordinates = new PlaneCoordinates(plane, onPlane.First());

			var planePoints = onPlane.Select(p => planeCoordinates.ToPlane(p)).ToList();

			var indices = this.triangulate(planePoints).ToArray();

			var triangles = new List<Triangle>();

			for (int i = 0; i < indices.Length; i += 3) {
				triangles.Add(new Triangle(planeCoordinates.ToWorld(planePoints[indices[i]]), planeCoordinates.ToWorld(planePoints[indices[i + 1]]), planeCoordinates.ToWorld(planePoints[indices[i + 2]])));
			}
			this.Mesh = Triangle.CreateMesh(triangles, true);
		}
	}

	private IEnumerable<int> triangulate(List<Vector2> points) {
		var triangulator = new Delaunay.Voronoi(points.ToList(), null, getBounds(points));
		var lineSegments = triangulator.DelaunayTriangulation();

		var tuples = new HashSet<Tuple<int, int>>(lineSegments.Select(segment => new Tuple<int, int>(points.IndexOf(segment.p0.Value), points.IndexOf(segment.p1.Value))));

		var triangles = new List<int[]>();

		foreach (var baseTuple in tuples) {
			var thirdPoints = tuples
				.Where(t => t.Value1 == baseTuple.Value1 || t.Value2 == baseTuple.Value1)
				.Select(t => t.Value1 == baseTuple.Value1 ? t.Value2 : t.Value1)
				.Where(i => tuples.Contains(new Tuple<int, int>(i, baseTuple.Value2)) || tuples.Contains(new Tuple<int, int>(baseTuple.Value2, i)));
			foreach (var thirdPoint in thirdPoints) {
				if (!triangles.Any(t => t.Contains(thirdPoint) && t.Contains(baseTuple.Value1) && t.Contains(baseTuple.Value2))) {
					triangles.Add(new int[] { thirdPoint, baseTuple.Value1, baseTuple.Value2 });
					yield return thirdPoint;
					yield return baseTuple.Value1;
					yield return baseTuple.Value2;
				}
			}
		}
	}

	private Rect getBounds(IEnumerable<Vector2> points) {
		var first = points.First();
		float minX = first.x;
		float maxX = first.x;
		float minY = first.y;
		float maxY = first.y;

		foreach (var point in points) {
			if (point.x > maxX) maxX = point.x;
			if (point.x < minX) minX = point.x;
			if (point.y > maxY) maxY = point.y;
			if (point.y < minY) minY = point.y;
		}

		return new Rect(minX, minY, maxX - minX, maxY - minY);
	}
}
