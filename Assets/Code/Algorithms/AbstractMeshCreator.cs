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

	protected readonly List<Plane> Planes;
	protected readonly PointCloud PointCloud;
	
	public Mesh Mesh {
		get;
		protected set;
	}

	public AbstractMeshCreator(PointCloud pointCloud) {
		this.Planes = pointCloud.Planes.ToList();
		this.PointCloud = pointCloud;
	}	

	protected float GetScore(IEnumerable<Triangle> mesh) {
		return mesh.Sum(triangle => triangle.GetScore(this.PointCloud));
	}

	
	public static AbstractMeshCreator CreateMesh(PointCloud pointCloud, Type type) {
		switch (type) {
			case Type.Cutoff: {
					var creator = new ShapeMeshCreator(pointCloud);
					creator.CreateMeshCutoff(false);
					return creator;
				}
			case Type.CutoffWithAttachments: {
					var creator = new ShapeMeshCreator(pointCloud);
					creator.CreateMeshCutoff(true);
					return creator;
				}
			case Type.Permutations: {
					var creator = new ShapeMeshCreator(pointCloud);
					creator.CreateMeshWithPermutations();
					return creator;
				}
			case Type.Layout: {
					var creator = new ShapeMeshCreator(pointCloud);
					creator.CreateLayoutMesh();
					return creator;
				}
			case Type.FromPoints: {
					var creator = new PointMeshCreator(pointCloud);
					creator.CreateMesh();
					return creator;
				}
			default: throw new System.NotImplementedException();
		}
	}

	public void DisplayMesh() {
		var material = Resources.Load("Materials/MeshMaterial", typeof(Material)) as Material;
		var gameObject = new GameObject();
		gameObject.transform.parent = this.PointCloud.transform;
		gameObject.tag = "RoofMesh";
		gameObject.AddComponent<MeshFilter>().sharedMesh = this.Mesh;
		gameObject.AddComponent<MeshRenderer>().material = material;
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.name = "Shape";
		gameObject.layer = 10;
	}

	public void SaveMesh() {
        StringBuilder sb = new StringBuilder();
 
        sb.Append("g ").Append(this.PointCloud.Name).Append("\n");
        foreach(Vector3 v in this.Mesh.vertices) {
			sb.Append(string.Format("v {0} {1} {2}\n", v.x + XYZLoader.ReferenceX, v.z + XYZLoader.ReferenceY, v.y +XYZLoader.ReferenceZ));
        }
        sb.Append("\n");
        foreach(Vector3 v in this.Mesh.normals) {
			sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
        }
		sb.Append("\n");
        for (int i=0;i<this.Mesh.triangles.Length;i+=3) {
			sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
				this.Mesh.triangles[i] + 1, this.Mesh.triangles[i + 1] + 1, this.Mesh.triangles[i + 2] + 1));
        }
		System.IO.File.WriteAllText(this.PointCloud.Folder + this.PointCloud.Name + ".obj", sb.ToString());
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
