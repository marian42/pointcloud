using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MeshCreator {
	private readonly PlaneClassifier planeClassifier;
	private readonly PointCloud pointCloud;
	private Vector2[] shape;

	public Mesh Mesh {
		get;
		private set;
	}

	public MeshCreator(PlaneClassifier planeClassifier) {
		this.planeClassifier = planeClassifier;
		this.pointCloud = planeClassifier.PointCloud;
	}

	private void createLayoutMesh() {
		var verts = new List<Vector3>();
		var triangles = new List<int>();

		for (int i = 0; i < this.shape.Length; i++) {
			var v1 = this.shape[i];
			var v2 = this.shape[(i + 1) % this.shape.Length];

			verts.Add(new Vector3(v1.x, 0, v1.y));
			verts.Add(new Vector3(v2.x, 0, v2.y));
			verts.Add(new Vector3(v2.x, 5, v2.y));
			verts.Add(new Vector3(v1.x, 5, v1.y));
			verts.Add(new Vector3(v1.x, 0, v1.y));
			verts.Add(new Vector3(v2.x, 0, v2.y));
			verts.Add(new Vector3(v2.x, 5, v2.y));
			verts.Add(new Vector3(v1.x, 5, v1.y));
			int p = i * 8;
			triangles.AddRange(new int[] { p + 0, p + 1, p + 2 });
			triangles.AddRange(new int[] { p + 0, p + 2, p + 3 });
			triangles.AddRange(new int[] { p + 6, p + 5, p + 4 });
			triangles.AddRange(new int[] { p + 7, p + 6, p + 4 });
		}

		this.Mesh = new Mesh();
		this.Mesh.vertices = verts.ToArray();
		this.Mesh.triangles = triangles.ToArray();
		this.Mesh.RecalculateNormals();
	}

	private Mesh createMeshFromPolygon(Plane plane, Vector2[] shape) {
		Triangulator tr = new Triangulator(shape);
		int[] indices = tr.Triangulate().Reverse().ToArray();

		Vector3[] vertices = new Vector3[shape.Length];
		for (int i = 0; i < vertices.Length; i++) {
			var ray = new Ray(new Vector3(shape[i].x, 0, shape[i].y), Vector3.up);
			float hit;
			if (!plane.Raycast(ray, out hit)) {
				Debug.LogError("Ray didn't hit plane.");
			}
			vertices[i] = ray.GetPoint(hit);
		}

		Mesh result = new Mesh();
		result.vertices = vertices;
		result.triangles = indices;
		result.RecalculateNormals();
		result.RecalculateBounds();

		return result;
	}

	public void CreateMesh() {
		this.shape = this.pointCloud.GetShape();
		this.Mesh = this.createMeshFromPolygon(this.planeClassifier.PlanesWithScore.First().Value1, this.shape);
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
	}
}
