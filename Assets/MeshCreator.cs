using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	public void CreateMesh() {
		var verts = new List<Vector3>();
		var triangles = new List<int>();

		this.shape = this.pointCloud.GetShape();
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

	public void DisplayMesh() {
		var material = Resources.Load("MeshMaterial", typeof(Material)) as Material;
		var gameObject = new GameObject();
		gameObject.transform.parent = this.pointCloud.transform;
		gameObject.tag = "RoofMesh";
		gameObject.AddComponent<MeshFilter>().sharedMesh = this.Mesh;
		gameObject.AddComponent<MeshRenderer>().material = material;
		gameObject.transform.localPosition = Vector3.down * this.pointCloud.Center.y;
		gameObject.name = "Shape";
	}
}
