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
		foreach (var plane in this.Planes.Take(8)) {
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
			var resultPoints = new List<Vector3>();
			
			foreach (var edge in outsideEdges) {
				if (visitedEdges.Contains(edge)) {
					continue;
				}
				var polygonIndices = new List<int>();
				var currentPoint = edge.Value1;
				polygonIndices.Add(currentPoint);
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
					polygonIndices.Add(currentPoint);
				}

				if (polygonIndices.Count > 10) {
					IEnumerable<Vector2> polygon = this.simplifyPolygon(polygonIndices.Select(i => planePoints[i]), 2.0f, 20.0f, 160.0f, 5);
					polygon = this.snapPoints(polygon, planeCoordinates, resultPoints, 2f);
					resultPoints.AddRange(polygon.Select(p => planeCoordinates.ToWorld(p)));
					result.AddRange(this.polygonToTriangle(polygon, planeCoordinates));
				}
			}
			Timekeeping.CompleteTask("Find polygons");
		}
		this.Mesh = Triangle.CreateMesh(result, true);
	}

	private IEnumerable<Vector2> snapPoints(IEnumerable<Vector2> points, PlaneCoordinates planeCoordinates, IEnumerable<Vector3> previousPoints, float snapDistance) {
		var snapPoints2D = previousPoints.Where(p => Mathf.Abs(planeCoordinates.Plane.GetDistanceToPoint(p)) < snapDistance).Select(p => planeCoordinates.ToPlane(p)).ToList();
		var otherPlanes = new List<Ray2D>();
		foreach (var plane in this.PointCloud.Planes.Take(8)) {
			if (plane.Equals(planeCoordinates.Plane)) {
				continue;
			}

			var intersect = Math3d.PlanePlaneIntersection(plane, planeCoordinates.Plane);
			if (intersect.HasValue) {
				var p1 = planeCoordinates.ToPlane(intersect.Value.origin);
				var p2 = planeCoordinates.ToPlane(intersect.Value.origin + intersect.Value.direction);
				otherPlanes.Add(new Ray2D(p1, p2 - p1));
			}
		}

		foreach (var ray1 in otherPlanes) {
			foreach (var ray2 in otherPlanes) {
				if (ray1.Equals(ray2)) {
					continue;
				}
				snapPoints2D.Add(Math3d.LineLineIntersection2D(ray1, ray2));
			}
		}

		foreach (var point in points) {
			var snapToPoint = snapPoints2D
				.Where(p => (point - p).magnitude < snapDistance)
				.Select(p => new Tuple<Vector2, float>(p, (point - p).magnitude))
				.OrderBy(tuple => tuple.Value2)
				.FirstOrDefault();
			if (snapToPoint != null) {
				yield return snapToPoint.Value1;
			} else {
				var snapToPlane = otherPlanes
					.Select(ray => Math3d.ProjectTo2DRay(point, ray))
					.Select(p => new Tuple<Vector2, float>(p, (point - p).magnitude))
					.Where(tuple => tuple.Value2 < snapDistance)
					.OrderBy(tuple => tuple.Value2)
					.FirstOrDefault();
				if (snapToPlane != null) {
					Debug.Log("snaptoplane");
					yield return snapToPlane.Value1;
				} else {
					yield return point;
				}
			}
		}
	}

	private List<Vector2> simplifyPolygon(IEnumerable<Vector2> points, float minDistanceBetweetnPoints, float minAngle, float maxAngle, int runs) {
		var result = points.ToList();

		for (int run = 0; run < runs; run++) {
			for (int i = 0; i < result.Count; i++) {
				int prev = (i - 1 + result.Count) % result.Count;
				int next = (i + 1 + result.Count) % result.Count;
				if ((result[i] - result[prev]).magnitude < minDistanceBetweetnPoints) {
					result.RemoveAt(i);
					i--;
					continue;
				}
				float angle = Vector2.Angle(result[prev] - result[i], result[next] - result[i]);
				if (angle < minAngle || angle > maxAngle) {
					result.RemoveAt(i);
					i--;
					continue;
				}
			}
		}			

		return result;
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
