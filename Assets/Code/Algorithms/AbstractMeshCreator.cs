using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

public abstract class AbstractMeshCreator {
	public enum Type {
		Cutoff,
		CutoffWithAttachments,
		Permutations,
		Layout,
		FromPoints
	}

	public bool CleanMesh;

	protected readonly List<Plane> Planes;
	protected readonly PointCloud PointCloud;
	
	public IEnumerable<Triangle> Triangles {
		get;
		protected set;
	}

	private Mesh mesh;

	public AbstractMeshCreator(PointCloud pointCloud, bool cleanMesh) {
		this.Planes = pointCloud.Planes.ToList();
		this.PointCloud = pointCloud;
		this.CleanMesh = cleanMesh;
	}	

	protected float GetScore(IEnumerable<Triangle> mesh) {
		return mesh.Sum(triangle => triangle.GetScore(this.PointCloud));
	}

	
	public static AbstractMeshCreator CreateMesh(PointCloud pointCloud, Type type, bool cleanMesh) {
		switch (type) {
			case Type.Cutoff: {
				var creator = new ShapeMeshCreator(pointCloud, cleanMesh);
				creator.CreateMeshCutoff(false);
				return creator;
			}
			case Type.CutoffWithAttachments: {
				var creator = new ShapeMeshCreator(pointCloud, cleanMesh);
				creator.CreateMeshCutoff(true);
				return creator;
				}
			case Type.Permutations: {
				var creator = new ShapeMeshCreator(pointCloud, cleanMesh);
				creator.CreateMeshWithPermutations();
				return creator;
			}
			case Type.Layout: {
				var creator = new ShapeMeshCreator(pointCloud, cleanMesh);
				creator.CreateLayoutMesh();
				return creator;
			}
			case Type.FromPoints: {
				var creator = new PointMeshCreator(pointCloud, cleanMesh);
				creator.CreateMesh();
				return creator;
			}
			default: throw new System.NotImplementedException();
		}
	}

	public Mesh GetMesh() {
		if (this.mesh == null) {
			if (this.CleanMesh) {
				var cleanedTriangles = MeshCleaner.CleanMesh(this.Triangles);
				this.mesh = Triangle.CreateMesh(cleanedTriangles, true);
			} else {
				this.mesh = Triangle.CreateMesh(this.Triangles, true);
			}
		}

		return this.mesh;		
	}

	public void SaveMesh() {
		this.GetMesh();
        StringBuilder sb = new StringBuilder();
 
        sb.Append("g ").Append(this.PointCloud.Name).Append("\n");
        foreach(Vector3 v in this.mesh.vertices) {
			sb.Append(string.Format("v {0} {1} {2}\n", v.x + this.PointCloud.Center[0], v.z, v.y + this.PointCloud.Center[1]));
        }
        sb.Append("\n");
		foreach (Vector3 v in this.mesh.normals) {
			sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
        }
		sb.Append("\n");
		for (int i = 0; i < this.mesh.triangles.Length; i += 3) {
			sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
				this.mesh.triangles[i] + 1, this.mesh.triangles[i + 1] + 1, this.mesh.triangles[i + 2] + 1));
        }
		System.IO.File.WriteAllText(this.PointCloud.FileInfo.Directory + "/" + this.PointCloud.Name + ".obj", sb.ToString());
	}

	public IEnumerable<Triangle> MakeRoofSolid(IEnumerable<Triangle> triangles) {
		const float groundHeight = -10.0f;
		var solids = triangles.Select(t => triangleToSolid(t, groundHeight)).ToList();
		var result = solids.First();
		for (int i = 1; i < solids.Count; i++) {
			var next = solids[i];
			var modeller = new Net3dBool.BooleanModeller(result, next);
			result = modeller.getUnion();
			result = new Net3dBool.Solid(result.getVertices(), result.getIndices(), result.getColors());
		}
		return solidToTriangles(result, groundHeight);
	}

	private static Net3dBool.Solid triangleToSolid(Triangle triangle, float groundHeight) {
		var vertices = new Vector3[] {
			triangle.V1,
			triangle.V2,
			triangle.V3,
			new Vector3(triangle.V1.x, groundHeight, triangle.V1.y),
			new Vector3(triangle.V2.x, groundHeight, triangle.V2.y),
			new Vector3(triangle.V3.x, groundHeight, triangle.V3.y)
		};
		var indices = new int[] {
			0, 1, 2,
			3, 4, 5,
			0, 1, 3,
			1, 4, 3,
			1, 2, 4,
			2, 5, 4,
			2, 0, 5,
			5, 3, 0
		};

		var colors = Enumerable.Repeat(new Net3dBool.Color3f(0, 0, 0), vertices.Length).ToArray();
		return new Net3dBool.Solid(vertices.Select(p => new Net3dBool.Point3d(p.x, p.y, p.z)).ToArray(), indices, colors);			
	}

	private static IEnumerable<Triangle> solidToTriangles(Net3dBool.Solid solid, float groundHeight) {
		var vertices = solid.getVertices().Select(p => new Vector3((float)p.x, (float)p.y, (float)p.z)).ToArray();
		var indices = solid.getIndices();

		for (int i = 0; i < indices.Length; i += 3) {
			if (vertices[indices[i]].y == groundHeight
				|| vertices[indices[i] + 1].y == groundHeight
				|| vertices[indices[i] + 2].y == groundHeight) {
					continue;
			}

			yield return new Triangle(vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]]);
		}
	} 

	public IEnumerable<Triangle> MakeRoofSolid2(IEnumerable<Triangle> trianglesIn) {
		var triangles = trianglesIn.ToArray();
		var triangles2D = triangles.Select(t => t.ProjectToGround()).ToArray();

		for (int i = 0; i < triangles.Length; i++) {
			IEnumerable<Triangle> current = triangles[i].Yield();
			for (int j = 0; j < triangles.Length; j++) {
				if (j == i) {
					continue;
				}
				if (!triangles2D[i].Intersects(triangles2D[j])) {
					continue;
				}

				if (Math3d.SimilarPlanes(triangles[i].Plane, triangles[j].Plane)) {
					if (j < i) {
						current = current
							.SelectMany(t => t.ProjectToGround().Without(triangles2D[j]))
							.Select(t => t.ProjectFromGroundToPlane(triangles[i].Plane))
							.ToList();
					}
				} else {
					var cutMesh = Triangle.SplitMesh(current, triangles[j].Plane);
					current = cutMesh.Value1.Concat(removeGroundTriangle(cutMesh.Value2, triangles2D[j])).ToList();
				}
			}

			foreach (var triangle in current) {
				yield return triangle;
			}
		}
	}

	private IEnumerable<Triangle> removeGroundTriangle(IEnumerable<Triangle> coplanarTriangles, Triangle2D triangleToRemove) {
		if (!coplanarTriangles.Any()) {
			return Enumerable.Empty<Triangle>();
		}

		var plane = coplanarTriangles.First().Plane;
		return coplanarTriangles.SelectMany(t => t.ProjectToGround().Without(triangleToRemove)).Select(t => t.ProjectFromGroundToPlane(coplanarTriangles.First().Plane));
	}

	protected void CheckForPlanes() {
		if (this.PointCloud.Planes == null || !this.PointCloud.Planes.Any()) {
			throw new System.Exception("Can't create mesh. Find planes first.");
		}
	}
}
