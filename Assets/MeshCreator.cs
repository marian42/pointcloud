using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

public class MeshCreator {
	public enum Type {
		Cutoff,
		Layout
	}

	private readonly List<Plane> planes;
	private readonly PointCloud pointCloud;
	private Vector2[] shape;

	public Mesh Mesh {
		get;
		private set;
	}

	public MeshCreator(PointCloud pointCloud) {
		this.planes = pointCloud.Planes.ToList();
		this.pointCloud = pointCloud;
		this.shape = this.pointCloud.GetShape();
		if (this.shape.First() == this.shape.Last()) {
			this.shape = this.shape.Skip(1).ToArray();
		}
	}

	public void createLayoutMesh() {
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
			var ray = new Ray(new Vector3(vertices[i].x, -1000, vertices[i].y), Vector3.up);
			float hit;
			if (!plane.Raycast(ray, out hit)) {
				Debug.LogError("Ray didn't hit plane. " + ray.origin + " -> " + plane.normal);
			}
			result[i] = ray.GetPoint(hit);
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

	private float getScore(IEnumerable<Triangle> mesh) {
		return mesh.Sum(triangle => triangle.GetScore(this.pointCloud));
	}

	public void createMeshCutoff() {
		float bestScore = -1.0f;
		IEnumerable<Triangle> bestMesh = null;

		foreach (var selectedPlanes in this.planes.Take(5).Subsets()) {
			var currentMesh = this.createFromPlanes(selectedPlanes);
			var currentScore = this.getScore(currentMesh);

			if (currentScore > bestScore) {
				bestScore = currentScore;
				bestMesh = currentMesh;
			}
		}

		this.Mesh = Triangle.CreateMesh(bestMesh, true);
	}

	public void CreateMesh(Type type) {
		switch (type) {
			case Type.Cutoff:
				this.createMeshCutoff();
				return;			
			case Type.Layout:
				this.createLayoutMesh();
				return;
			default: throw new System.NotImplementedException();
		}
	}

	public void DisplayMesh() {
		var material = Resources.Load("MeshMaterial", typeof(Material)) as Material;
		var gameObject = new GameObject();
		gameObject.transform.parent = this.pointCloud.transform;
		gameObject.tag = "RoofMesh";
		gameObject.AddComponent<MeshFilter>().sharedMesh = this.Mesh;
		gameObject.AddComponent<MeshRenderer>().material = material;
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.name = "Shape";
		gameObject.layer = 10;
	}

	public void SaveMesh() {
        StringBuilder sb = new StringBuilder();
 
        sb.Append("g ").Append(this.pointCloud.Name).Append("\n");
        foreach(Vector3 v in this.Mesh.vertices) {
            sb.Append(string.Format("v {0} {1} {2}\n",v.x,v.y,v.z));
        }
        sb.Append("\n");
        foreach(Vector3 v in this.Mesh.normals) {
            sb.Append(string.Format("vn {0} {1} {2}\n",v.x,v.y,v.z));
        }
		sb.Append("\n");
        for (int i=0;i<this.Mesh.triangles.Length;i+=3) {
            sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", 
                this.Mesh.triangles[i]+1, this.Mesh.triangles[i+1]+1, this.Mesh.triangles[i+2]+1));
        }
		System.IO.File.WriteAllText(PointCloud.GetDataPath() + this.pointCloud.Name + ".obj", sb.ToString());
	}
}
