using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

public abstract class MeshCreator {
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

	public MeshCreator(PointCloud pointCloud) {
		this.Planes = pointCloud.Planes.ToList();
		this.PointCloud = pointCloud;
	}	

	protected float GetScore(IEnumerable<Triangle> mesh) {
		return mesh.Sum(triangle => triangle.GetScore(this.PointCloud));
	}

	
	public static MeshCreator CreateMesh(PointCloud pointCloud, Type type) {
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
		var material = Resources.Load("MeshMaterial", typeof(Material)) as Material;
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
		System.IO.File.WriteAllText(PointCloud.GetDataPath() + this.PointCloud.Name + ".obj", sb.ToString());
	}
}
