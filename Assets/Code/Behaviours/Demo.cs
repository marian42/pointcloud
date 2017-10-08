using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnitySlippyMap;

public class Demo : MonoBehaviour {
	private bool running;

	public bool Active;

	[Range(2, 20)]
	public float Interval;

	[Range(0, 60)]
	public float RoatationSpeed;

	void Start () {
		
	}

	private void showNextBuilding() {
		var currentPointcloudBehaviour = BuildingLoader.Instance.LoadRandom(pointcloud => pointcloud.Points.Length > 2000 && pointcloud.Points.Length < 6000);
		var pointCloud = currentPointcloudBehaviour.PointCloud;
		var localCenter = new Vector2((pointCloud.Shape.Max(v => v.x) + pointCloud.Shape.Min(v => v.x)) * 0.5f, (pointCloud.Shape.Max(v => v.y) + pointCloud.Shape.Min(v => v.y)) * 0.5f);
		UnitySlippyMap.Map.MapBehaviour.Instance.CenterWGS84 = BuildingLoader.metersToLatLon(new double[] { pointCloud.Metadata.center[0] + localCenter.x, pointCloud.Metadata.center[1] + localCenter.y });
		currentPointcloudBehaviour.CreateMesh(AbstractMeshCreator.CurrentType, ShapeMeshCreator.CleanMeshDefault);
		BuildingLoader.Instance.UpdateBuildings();
	}

	private IEnumerator demoCoroutine() {
		this.running = true;

		while (this.Active) {
			this.showNextBuilding();
			yield return new WaitForSeconds(this.Interval);
		}

		this.running = false;
	}

	void Update () {
		if (this.Active && !this.running && BuildingLoader.Instance.MetadataLoadingComplete) {
			StartCoroutine(this.demoCoroutine());
		}
		if (this.Active && this.running) {
			UnitySlippyMap.Map.MapBehaviour.Instance.CameraYaw += Time.deltaTime * this.RoatationSpeed;
			UnitySlippyMap.Map.MapBehaviour.Instance.UpdateCamera();
		}
	}
}
