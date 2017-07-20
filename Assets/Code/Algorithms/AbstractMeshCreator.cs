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

	protected void CheckForPlanes() {
		if (this.PointCloud.Planes == null || !this.PointCloud.Planes.Any()) {
			throw new System.Exception("Can't create mesh. Find planes first.");
		}
	}
}
