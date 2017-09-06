using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using System.Linq;
using System.IO;
using UnitySlippyMap.Markers;
using UnitySlippyMap.Map;

[SelectionBase]
public class PointCloudBehaviour : MonoBehaviour {
	private const int POINTS_PER_MESH = 60000;

	public PointCloud PointCloud;

	public void Initialize(PointCloud pointCloud) {
		this.PointCloud = pointCloud;
		this.gameObject.name = pointCloud.GetName();
		this.transform.position = Vector3.down * this.PointCloud.GroundPoint.y;
		
		for (int start = 0; start < this.PointCloud.Points.Length; start += POINTS_PER_MESH) {
			this.createMeshObject(start, Math.Min(start + POINTS_PER_MESH, this.PointCloud.Points.Length - 1));
		}
	}

	private void createMeshObject(int fromIndex, int toIndex) {
		var prefab = Resources.Load("Prefabs/PointMesh") as GameObject;
		var gameObject = GameObject.Instantiate(prefab) as GameObject;
		gameObject.layer = 8;
		gameObject.transform.parent = this.transform;
		gameObject.transform.localPosition = Vector3.zero;
		var mesh = new Mesh();
		gameObject.GetComponent<MeshFilter>().mesh = mesh;

		int[] indecies = new int[toIndex - fromIndex];
		Vector3[] meshPoints = new Vector3[toIndex - fromIndex];
		for (int i = fromIndex; i < toIndex; ++i) {
			indecies[i - fromIndex] = i - fromIndex;
			meshPoints[i - fromIndex] = this.PointCloud.Points[i];
		}

		mesh.vertices = meshPoints;
		mesh.SetIndices(indecies, MeshTopology.Points, 0);
	}

	public void DisplayNormals() {
		if (this.PointCloud.Normals == null) {
			this.PointCloud.EstimateNormals();
		}
		for (int i = 0; i < this.PointCloud.Normals.Length; i++) {
			var start = this.PointCloud.Points[i];
			Debug.DrawLine(this.transform.position + start, this.transform.position + start + this.PointCloud.Normals[i].normalized * 3.0f, Color.red, 10.0f);
		}
	}

	[MenuItem("File/Load pointcloud...")]
	public static void LoadSingle() {
		string selected = EditorUtility.OpenFilePanel("Load file", Application.dataPath + "/data/buildings/", null);
		if (selected.Any() && File.Exists(selected)) {
			GameObject gameObject = new GameObject();
			var pointcloud = new PointCloud(selected);
			pointcloud.Load();
			var pointCloudBehaviour = gameObject.AddComponent<PointCloudBehaviour>();
			pointCloudBehaviour.Initialize(pointcloud);
			gameObject.transform.position = Vector3.up * gameObject.transform.position.y;
			var marker = gameObject.AddComponent<LocationMarkerBehaviour>();
			marker.Map = MapBehaviour.Instance;
			marker.CoordinatesWGS84 = BuildingLoader.metersToLatLon(pointCloudBehaviour.PointCloud.Center);
			MapBehaviour.Instance.CenterEPSG900913 = marker.CoordinatesEPSG900913;
		}
	}

	public void DisplayMesh(Mesh mesh) {
		var material = Resources.Load("Materials/MeshMaterial", typeof(Material)) as Material;
		var gameObject = new GameObject();
		gameObject.transform.parent = this.transform;
		gameObject.tag = "RoofMesh";
		gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		gameObject.AddComponent<MeshRenderer>().material = material;
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.name = "Shape";
		gameObject.layer = 10;
	}

	public void CreateMesh(AbstractMeshCreator.Type type, bool cleanMesh) {
		this.transform.DeleteRoofMeshes();
		Timekeeping.Reset();
		var meshCreator = AbstractMeshCreator.CreateMesh(this.PointCloud, type, cleanMesh);
		this.DisplayMesh(meshCreator.GetMesh());
		meshCreator.SaveMesh();
		Debug.Log(Timekeeping.GetStatus());

		if (type == AbstractMeshCreator.Type.Cutoff || type == AbstractMeshCreator.Type.CutoffWithAttachments) {
			File.AppendAllText(Application.dataPath + "/data.txt", Timekeeping.GetDataLine(5)
				+ this.PointCloud.Points.Length + ";"
				+ this.PointCloud.Stats["attachments"] + ";"
				+ meshCreator.GetMesh().triangles.Length / 6 + "\n");
		}		
	}

	public void FindPlanes(AbstractPlaneFinder.Type type, bool showPlanes) {
		PlaneBehaviour.DeletePlanesIn(this.transform);
		this.transform.DeleteRoofMeshes();
		var planeClassifier = AbstractPlaneFinder.Instantiate(type, this.PointCloud);
		planeClassifier.Classify();
		planeClassifier.RemoveGroundPlanesAndVerticalPlanes();
		if (showPlanes) {
			foreach (var tuple in planeClassifier.PlanesWithScore.OrderByDescending(t => t.Value2).Take(6)) {
				var plane = tuple.Value1;
				PlaneBehaviour.DisplayPlane(plane, this);
			}
		}

		this.PointCloud.Planes = planeClassifier.GetPlanes();
	}
}
