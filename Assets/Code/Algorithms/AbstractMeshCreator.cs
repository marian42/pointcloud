using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

public abstract class AbstractMeshCreator {
	public enum Type {
		Convex,
		ConvexWithAttachments,
		SplitLayout,
		TriangulatePoints,
		Layout
	}

	public static AbstractMeshCreator.Type CurrentType = AbstractMeshCreator.Type.ConvexWithAttachments;	

	public bool CleanMesh;

	protected List<Plane> Planes { get; private set; }
	protected readonly PointCloud PointCloud;
	
	public List<Triangle> Triangles {
		get;
		protected set;
	}

	private Mesh mesh;

	public AbstractMeshCreator(PointCloud pointCloud, bool cleanMesh) {
		this.PointCloud = pointCloud;
		this.CleanMesh = cleanMesh;
	}	

	protected float GetScore(IEnumerable<Triangle> mesh) {
		return mesh.Sum(triangle => triangle.GetScore(this.PointCloud));
	}
	
	public static AbstractMeshCreator CreateMesh(PointCloud pointCloud, Type type, bool cleanMesh) {
		switch (type) {
			case Type.Convex: {
				var creator = new ShapeMeshCreator(pointCloud, cleanMesh);
				creator.CreateMeshCutoff(false);
				return creator;
			}
			case Type.ConvexWithAttachments: {
				var creator = new ShapeMeshCreator(pointCloud, cleanMesh);
				creator.CreateMeshCutoff(true);
				return creator;
				}
			case Type.SplitLayout: {
				var creator = new ShapeMeshCreator(pointCloud, cleanMesh);
				creator.CreateMeshWithPermutations();
				return creator;
			}
			case Type.Layout: {
				var creator = new ShapeMeshCreator(pointCloud, cleanMesh);
				creator.CreateLayoutMesh();
				return creator;
			}
			case Type.TriangulatePoints: {
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
				Timekeeping.CompleteTask("Clean mesh");
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
			sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y - this.PointCloud.GroundPoint.y, v.z));
        }
        sb.Append("\n");
		foreach (Vector3 v in this.mesh.normals) {
			sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.z, v.y));
        }
		sb.Append("\n");
		for (int i = 0; i < this.mesh.triangles.Length; i += 3) {
			sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
				this.mesh.triangles[i] + 1, this.mesh.triangles[i + 1] + 1, this.mesh.triangles[i + 2] + 1));
        }
		System.IO.File.WriteAllText(Options.CleanPath(Options.Instance.MeshOutputFolder) + "/" + this.PointCloud.Name + ".obj", sb.ToString());
	}	

	protected void CheckForPlanes() {
		if (this.PointCloud.Planes == null || !this.PointCloud.Planes.Any()) {
			var planeClassifier = AbstractPlaneFinder.Instantiate(AbstractPlaneFinder.CurrentType, this.PointCloud);
			planeClassifier.Classify();
			planeClassifier.RemoveGroundPlanesAndVerticalPlanes();
			PointCloud.Planes = planeClassifier.GetPlanes();
		}
		this.Planes = this.PointCloud.Planes;
	}
}
