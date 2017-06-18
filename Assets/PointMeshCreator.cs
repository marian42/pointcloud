using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

public class PointMeshCreator : MeshCreator {
	public PointMeshCreator(PointCloud pointCloud) : base(pointCloud) {
	}

	public void CreateMesh() {
		Timekeeping.Reset();
		var result = new List<Triangle>();
		foreach (var plane in this.Planes.Take(1)) {
			var onPlane = this.PointCloud.CenteredPoints.Where(p => Mathf.Abs(plane.GetDistanceToPoint(p)) < HoughPlaneFinder.MaxDistance);
			Timekeeping.CompleteTask("Select points");

			var planeCoordinates = new PlaneCoordinates(plane, onPlane.First());
			var planePoints = onPlane.Select(p => planeCoordinates.ToPlane(p)).ToList();

			var triangles = this.triangluate(planePoints).ToArray();
			Timekeeping.CompleteTask("Triangulate");

			const float maxDistance = 1.5f;

			var edges = new List<Tuple<int, int>>();
			for (int i = 0; i < triangles.Count(); i += 3) {
				float dst1 = (planePoints[triangles[i]] - planePoints[triangles[i + 1]]).magnitude;
				float dst2 = (planePoints[triangles[i + 1]] - planePoints[triangles[i + 2]]).magnitude;
				float dst3 = (planePoints[triangles[i + 2]] - planePoints[triangles[i]]).magnitude;
				if (dst1 < maxDistance && dst2 < maxDistance && dst2 < maxDistance) {
					edges.Add(new Tuple<int,int>(triangles[i], triangles[i + 1]));
					edges.Add(new Tuple<int,int>(triangles[i + 1], triangles[i + 2]));
					edges.Add(new Tuple<int,int>(triangles[i + 2], triangles[i]));
					//result.Add(new Triangle(planeCoordinates.ToWorld(planePoints[triangles[i]]), planeCoordinates.ToWorld(planePoints[triangles[i + 1]]), planeCoordinates.ToWorld(planePoints[triangles[i + 2]])));
				}
			}
			Timekeeping.CompleteTask("Discard bad triangles");

			var neighbours = new Dictionary<int, HashSet<int>>();
			foreach (var edge in edges) {
				if (!neighbours.ContainsKey(edge.Value1)) {
					neighbours[edge.Value1] = new HashSet<int>();
				}
				neighbours[edge.Value1].Add(edge.Value2);
				if (!neighbours.ContainsKey(edge.Value2)) {
					neighbours[edge.Value2] = new HashSet<int>();
				}
				neighbours[edge.Value2].Add(edge.Value1);
			}

			var outsideEdges = new List<Tuple<int, int>>();

			foreach (var edge in edges) {
				var rayBase = planePoints[edge.Value1];
				var rayDirection = planePoints[edge.Value2] - planePoints[edge.Value1];
				var orthogonal = new Vector2(-rayDirection.y, rayDirection.x);

				var candidates = neighbours[edge.Value1].Where(i => neighbours[edge.Value2].Contains(i)).Select(i => planePoints[i]);

				bool left = false;
				bool right = false;

				foreach (var point in candidates) {
					if (Vector2.Angle(point - rayBase, orthogonal) < 90.0f) {
						left = true;
					} else {
						right = true;
					}
					if (left && right) {
						break;
					}
				}
				if (!left || !right) {
					outsideEdges.Add(edge);
				}
			}
			
			foreach (var edge in outsideEdges) {
				Debug.DrawLine(this.PointCloud.Center + planeCoordinates.ToWorld(planePoints[edge.Value1]),
					this.PointCloud.Center + planeCoordinates.ToWorld(planePoints[edge.Value2]),
					Color.green, 20.0f, false);
			}
			Timekeeping.CompleteTask("Find outside edges");

			var nextOutsideEdge = new Dictionary<int, HashSet<Tuple<int, int>>>();
			foreach (var edge in outsideEdges) {
				if (!nextOutsideEdge.ContainsKey(edge.Value1)) {
					nextOutsideEdge[edge.Value1] = new HashSet<Tuple<int, int>>();
				}
				nextOutsideEdge[edge.Value1].Add(edge);
				if (!nextOutsideEdge.ContainsKey(edge.Value2)) {
					nextOutsideEdge[edge.Value2] = new HashSet<Tuple<int, int>>();
				}
				nextOutsideEdge[edge.Value2].Add(edge);
			}

			var visitedEdges = new HashSet<Tuple<int, int>>();
			var polygons = new List<List<int>>();

			foreach (var edge in outsideEdges) {
				if (visitedEdges.Contains(edge)) {
					continue;
				}
				var polygon = new List<int>();
				var currentPoint = edge.Value1;
				polygon.Add(currentPoint);
				visitedEdges.Add(edge);
				
				while (true) {
					var nextEdge = nextOutsideEdge[currentPoint].FirstOrDefault(e => !visitedEdges.Contains(e));
					if (nextEdge == null) {
						break;
					}
					visitedEdges.Add(nextEdge);
					if (nextEdge.Value1 == currentPoint) {
						currentPoint = nextEdge.Value2;
					} else {
						currentPoint = nextEdge.Value1;
					}
					polygon.Add(currentPoint);
				}

				if (polygon.Count > 10) {
					polygons.Add(polygon);
					result.AddRange(this.polygonToTriangle(polygon.Select(i => planePoints[i]).ToList(), planeCoordinates));
				}
			}
			Timekeeping.CompleteTask("Find polygons");
			Debug.Log(polygons.Count());
		}
		this.Mesh = Triangle.CreateMesh(result, true);
	}

	private IEnumerable<Triangle> polygonToTriangle(IEnumerable<Vector2> points, PlaneCoordinates planeCoordinates) {
		var triangulator = new Triangulator(points.ToArray());
		var triangles = triangulator.Triangulate();

		return Triangle.GetTriangles(points.Select(p => planeCoordinates.ToWorld(p)).ToArray(), triangles);
	}

	private IEnumerable<int> triangluate(List<Vector2> points) {
		var triangulator = new Delaunay.Voronoi(points.ToList(), null, getBounds(points));
		var lineSegments = triangulator.DelaunayTriangulation();
		Timekeeping.CompleteTask("Triangulate");

		var edges = new HashSet<Tuple<int, int>>(lineSegments.Select(segment => new Tuple<int, int>(points.IndexOf(segment.p0.Value), points.IndexOf(segment.p1.Value))));
			
		var neighbours = new Dictionary<int, HashSet<int>>();
		foreach (var edge in edges) {
			if (!neighbours.ContainsKey(edge.Value1)) {
				neighbours[edge.Value1] = new HashSet<int>();
			}
			neighbours[edge.Value1].Add(edge.Value2);
			if (!neighbours.ContainsKey(edge.Value2)) {
				neighbours[edge.Value2] = new HashSet<int>();
			}
			neighbours[edge.Value2].Add(edge.Value1);
		}
			
		var foundTriangles = new HashSet<int>();

		foreach (int p1 in neighbours.Keys) {
			foreach (int p2 in neighbours[p1]) {
				foreach (int p3 in neighbours[p1]) {
					if (p2 == p3) {
						continue;
					}
					if (!neighbours[p2].Contains(p3)) {
						continue;
					}
					var triangles = new int[] { p1, p2, p3 }.OrderBy(i => i).ToArray();
					var intValue = triangles[0] + triangles[1] * points.Count + triangles[2] * points.Count + points.Count;
					if (foundTriangles.Contains(intValue)) {
						continue;
					}
					foundTriangles.Add(intValue);
					yield return p1;
					yield return p2;
					yield return p3;
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
