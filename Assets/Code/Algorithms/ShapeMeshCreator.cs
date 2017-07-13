using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ShapeMeshCreator : AbstractMeshCreator {
	private Vector2[] shape;
	
	public ShapeMeshCreator(PointCloud pointCloud) : base(pointCloud) {
		this.shape = this.PointCloud.GetShape();
		if (this.shape.First() == this.shape.Last()) {
			this.shape = this.shape.Skip(1).ToArray();
		}
	}

	public void CreateLayoutMesh() {
		var triangles = new List<Triangle>();

		for (int i = 0; i < this.shape.Length; i++) {
			var v1 = this.shape[i];
			var v2 = this.shape[(i + 1) % this.shape.Length];

			triangles.Add(new Triangle(new Vector3(v1.x, 0, v1.y), new Vector3(v2.x, 0, v2.y), new Vector3(v2.x, 5, v2.y)));
			triangles.Add(new Triangle(new Vector3(v1.x, 0, v1.y), new Vector3(v2.x, 5, v2.y), new Vector3(v1.x, 5, v1.y)));
		}

		this.Mesh = Triangle.CreateMesh(triangles, true);
	}

	private Vector3[] projectToPlane(Vector2[] vertices, Plane plane) {
		Vector3[] result = new Vector3[vertices.Length];
		for (int i = 0; i < vertices.Length; i++) {
			result[i] = Math3d.ProjectFromGroundToPlane(vertices[i], plane);
		}
		return result;
	}

	private IEnumerable<Triangle> createMeshFromPolygon(Plane plane, Vector2[] shape) {
		var vertices = projectToPlane(shape, plane);

		var triangulator = new Triangulator(shape);
		var triangles = triangulator.Triangulate();

		return Triangle.GetTriangles(vertices, triangles);
	}

	private IEnumerable<Triangle> createFromPlanes(IEnumerable<Plane> planes) {
		var result = new List<Triangle>();
		foreach (var startingPlane in planes) {
			var mesh = this.createMeshFromPolygon(startingPlane, this.shape);
			foreach (var cutawayPlane in planes) {
				if (cutawayPlane.Equals(startingPlane)) {
					continue;
				}
				mesh = Triangle.CutMesh(mesh, cutawayPlane, false);
				if (!mesh.Any()) {
					break;
				}
			}
			if (mesh.Any()) {
				result.AddRange(mesh);
			}
		}

		return result;
	}

	private Plane checkForSimilarPlanes(Plane current, IEnumerable<Plane> usedPlanes) {
		var similarPlanes = usedPlanes.Where(plane => RansacPlaneFinder.Similar(plane, current));
		if (similarPlanes.Any()) {
			return similarPlanes.First();
		} else {
			return current;
		}
	}

	public void CreateMeshCutoff(bool createAttachments) {
		this.CheckForPlanes();

		float bestScore = -1.0f;
		IEnumerable<Triangle> bestMesh = null;
		IEnumerable<Plane> bestPlanes = null;

		foreach (var selectedPlanes in this.Planes.Take(5).Subsets()) {
			var currentMesh = this.createFromPlanes(selectedPlanes);
			var currentScore = this.GetScore(currentMesh);

			if (currentScore > bestScore) {
				bestScore = currentScore;
				bestMesh = currentMesh;
				bestPlanes = selectedPlanes;
			}
		}

		var resultMesh = bestMesh.ToList();
		var usedPlanes = bestPlanes.ToList();

		if (!createAttachments) {
			this.Mesh = Triangle.CreateMesh(resultMesh, true);
			return;
		}

		var planeFinder = new RansacPlaneFinder(this.PointCloud);

		foreach (var plane in bestPlanes) {
			var indices = new List<int>();
			for (int i = 0; i < this.PointCloud.Points.Length; i++) {
				if (plane.GetDistanceToPoint(this.PointCloud.CenteredPoints[i]) > HoughPlaneFinder.MaxDistance * 0.5f) {
					indices.Add(i);
				}
			}

			while (true) {
				planeFinder.Classify(indices);
				planeFinder.RemoveGroundPlanesAndVerticalPlanes();
				var outsidePlanes = planeFinder.PlanesWithScore.Where(tuple => tuple.Value2 > 5.0f).Select(tuple => tuple.Value1).Where(newPlane => !RansacPlaneFinder.Similar(plane, newPlane));
				outsidePlanes = outsidePlanes.Select(p => this.checkForSimilarPlanes(p, usedPlanes));

				if (outsidePlanes.Count() == 0) {
					break;
				}

				bestScore = 0.0f;
				IEnumerable<Plane> planesInAttachment = null;
				foreach (var selectedPlanes in outsidePlanes.Take(5).Subsets()) {
					var currentMesh = this.createFromPlanes(selectedPlanes);
					currentMesh = Triangle.CutMesh(currentMesh, plane, true);
					float pointDensity = currentMesh.Sum(t => t.GetPointCount(this.PointCloud, indices)) / currentMesh.Sum(t => t.GetArea());
					if (pointDensity < 4.0f) {
						continue;
					}
					var currentScore = currentMesh.Sum(triangle => triangle.GetScore(this.PointCloud, indices));

					if (currentScore > bestScore) {
						bestScore = currentScore;
						bestMesh = currentMesh;
						planesInAttachment = selectedPlanes;
					}
				}

				if (bestScore > 2.0f) {
					resultMesh.AddRange(bestMesh);
					Debug.Log("Attachment score: " + bestScore + ", pointdensity: " + bestMesh.Sum(t => (t.GetPointCount(this.PointCloud, indices))) / bestMesh.Sum(t => t.GetArea()) + ", area: " + bestMesh.Sum(t => t.GetArea()) + ", created from " + outsidePlanes.Count() + " planes");
					usedPlanes = usedPlanes.Union(planesInAttachment).ToList();
				} else {
					break;
				}

				indices = indices.Where(i => !bestMesh.Any(triangle => triangle.Contains(this.PointCloud.CenteredPoints[i]))).ToList();
			}
		}

		this.Mesh = Triangle.CreateMesh(MeshCleaner.CleanMesh(resultMesh), true);
	}

	private Vector3 intersectPlane(Plane plane, Vector3 pointOnLine, Vector3 lineDirection) {
		Ray ray1 = new Ray(pointOnLine, lineDirection);
		float ray1hit;
		if (plane.Raycast(ray1, out ray1hit)) {
			return ray1.GetPoint(ray1hit);
		}
		Ray ray2 = new Ray(pointOnLine, -lineDirection);
		float ray2hit;
		if (plane.Raycast(ray2, out ray2hit)) {
			return ray2.GetPoint(ray1hit);
		}
		throw new System.InvalidOperationException("Plane and line are coindident.");
	}

	private Plane getGroundSeparatingPlane(Plane a, Plane b) {
		var intersectingLineDirection = Vector3.Cross(a.normal, b.normal).normalized;
		var pointOnA = this.intersectPlane(a, Vector3.zero, Vector3.up);
		var pointOnBoth = this.intersectPlane(b, pointOnA, Vector3.Cross(a.normal, intersectingLineDirection));

		// TODO why the * -1 ???
		var result = new Plane(Vector3.Cross(Vector3.up, intersectingLineDirection) * -1.0f, pointOnBoth);
		return result;
	}

	private IEnumerable<Plane> getAllGroundSeparatingPlanes(IEnumerable<Plane> planes) {
		var planeArray = planes.ToArray();
		var groundMesh = this.createMeshFromPolygon(new Plane(Vector3.up, 0), this.shape);

		for (int i = 1; i < planeArray.Length; i++) {
			for (int j = 0; j < i; j++) {
				var plane = this.getGroundSeparatingPlane(planeArray[i], planeArray[j]);

				var splitGroundMesh = Triangle.SplitMesh(groundMesh, plane);
				if (splitGroundMesh.Value1.Any() && splitGroundMesh.Value2.Any()) {
					yield return plane;
				}
			}
		}
	}

	private IEnumerable<Triangle> displayVerticalPlane(Plane plane) {
		var vectorLeft = Vector3.Cross(plane.normal, Vector3.up);
		var pointOnPlane = this.intersectPlane(plane, Vector3.zero, plane.normal);
		pointOnPlane -= Vector3.up * pointOnPlane.y;

		float scale = 10.0f * 0.5f;

		var topLeft = pointOnPlane + (vectorLeft + Vector3.up) * scale;
		var downLeft = pointOnPlane + (vectorLeft + Vector3.down) * scale;

		var topRight = pointOnPlane + (-vectorLeft + Vector3.up) * scale;
		var downRight = pointOnPlane + (-vectorLeft + Vector3.down) * scale;


		yield return new Triangle(topLeft, downLeft, downRight);
		yield return new Triangle(topLeft, topRight, downRight);
	}

	public void CreateMeshWithPermutations() {
		const int planeCount = 5;
		var result = new List<Triangle>();

		var groundPlanes = this.getAllGroundSeparatingPlanes(this.Planes.Take(planeCount)).ToList();

		for (int permutation = 0; permutation < Mathf.Pow(2.0f, groundPlanes.Count); permutation++) {
			var meshes = new List<IEnumerable<Triangle>>();
			foreach (var basePlane in this.Planes.Take(planeCount)) {
				var mesh = this.createMeshFromPolygon(basePlane, this.shape);

				for (int i = 0; i < groundPlanes.Count; i++) {
					mesh = Triangle.CutMesh(mesh, groundPlanes[i], (1 << i & permutation) == 0);
					if (!mesh.Any()) {
						break;
					}
				}
				if (mesh.Any()) {
					meshes.Add(mesh);
				}
			}
			var bestMesh = meshes.Select(mesh => new Tuple<IEnumerable<Triangle>, float>(mesh, this.GetScore(mesh)))
				.OrderByDescending(tuple => tuple.Value2)
				.Select(tuple => tuple.Value1)
				.FirstOrDefault();

			if (bestMesh != null) {
				result.AddRange(bestMesh);
			}
		}

		this.Mesh = Triangle.CreateMesh(result, true);
	}

}
