using UnityEngine;
using System;
using UnitySlippyMap.Map;
using UnitySlippyMap.Markers;
using UnitySlippyMap.Layers;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using SimpleJson;
using System.Linq;

public class BuildingLoader : MonoBehaviour {
	private MapBehaviour map;

	private const string metadataFilename = "/data/metadata.json";

	private BuildingHashSet buildings;

	private Dictionary<string, PointCloud> activeBuildings;

	private class MetadataList {
		public List<BuildingMetadata> buildings;
	}

	[Range(100.0f, 600.0f)]
	public float LoadRadius = 200.0f;

	[Range(100.0f, 2000.0f)]
	public float UnloadRadius = 1000.0f;

	private class BuildingHashSet {
		private const double bucketSize = 100.0f;

		private Dictionary<Tuple<int, int>, List<BuildingMetadata>> dict;

		private Tuple<int, int> getBucket(BuildingMetadata building) {
			return new Tuple<int, int>((int)(Math.Floor(building.Coordinates[0] / bucketSize)), (int)(Math.Floor(building.Coordinates[1] / bucketSize)));
		}

		public BuildingHashSet(IEnumerable<BuildingMetadata> data) {
			this.dict = new Dictionary<Tuple<int, int>, List<BuildingMetadata>>();
			foreach (var item in data) {
				var bucket = this.getBucket(item);
				if (!this.dict.ContainsKey(bucket)) {
					this.dict[bucket] = new List<BuildingMetadata>();
				}
				this.dict[bucket].Add(item);
			}
		}

		public IEnumerable<BuildingMetadata> GetBuildings(double[] coordinates, double radius) {
			double radiusSquared = Math.Pow(radius, 2.0);
			for (int x = (int)Math.Floor((coordinates[0] - radius) / bucketSize); x <= (int)Math.Ceiling((coordinates[0] + radius) / bucketSize); x++) {
				for (int y = (int)Math.Floor((coordinates[1] - radius) / bucketSize); y <= (int)Math.Ceiling((coordinates[1] + radius) / bucketSize); y++) {
					var bucket = new Tuple<int, int>(x, y);
					if (!dict.ContainsKey(bucket)) {
						continue;
					}
					
					foreach (var item in this.dict[bucket]) {
						if (Math.Pow(coordinates[0] - item.Coordinates[0], 2) + Math.Pow(coordinates[1] - item.Coordinates[1], 2) < radiusSquared) {
							yield return item;
						}
					}
				}
			}
		}
	}

	private void setupMap() {
		map = MapBehaviour.Instance;
		map.CurrentCamera = Camera.main;
		map.InputDelegate += UnitySlippyMap.Input.MapInput.BasicTouchAndKeyboard;
		map.MaxZoom = 40.0f;
		map.CurrentZoom = 15.0f;

		map.CenterWGS84 = new double[2] { 7.4639796, 51.5135063 };
		map.UsesLocation = true;
		map.InputsEnabled = true;

		OSMTileLayer osmLayer = map.CreateLayer<OSMTileLayer>("OSM");
		osmLayer.BaseURL = "http://cartodb-basemaps-b.global.ssl.fastly.net/light_all/";
		Debug.Log(Application.temporaryCachePath);
	}

	private void loadMetadata() {
		var data = JsonUtility.FromJson<MetadataList>(File.ReadAllText(Application.dataPath + metadataFilename));
		var buildingList = data.buildings;
		Debug.Log("Loaded metadata for " + buildingList.Count + " buildings.");
		Debug.Log(buildingList[0].Coordinates[0] + ", " + buildingList[0].Coordinates[1]);
		this.buildings = new BuildingHashSet(buildingList);
	}

	private void Start() {
		this.setupMap();
		this.loadMetadata();
		this.activeBuildings = new Dictionary<string, PointCloud>();
	}

	private static double[] metersToLatLon(double[] coordinates) {
		return new double[] {
			0.0000146113 * coordinates[0] + 1.7137570370,
			0.0000089185 * coordinates[1] + 0.6062153869
		};
	}

	private static double[] latLonToMeters(double[] coordinates) {
		return new double[] {
			(coordinates[0] - 1.7137570370) / 0.0000146113,
			(coordinates[1] - 0.6062153869) / 0.0000089185
		};
	}

	public void UpdateBuildings() {
		this.UnloadBuildings(this.UnloadRadius);

		var center = latLonToMeters(this.map.CenterWGS84);

		foreach (var building in this.buildings.GetBuildings(center, this.LoadRadius)) {
			if (this.activeBuildings.ContainsKey(building.filename)) {
				continue;
			}
			GameObject gameObject = new GameObject();
			var newPointCloud = gameObject.AddComponent<PointCloud>();
			newPointCloud.Load("C:/output/" + building.filename + ".points");
			newPointCloud.Show();
			gameObject.transform.position = Vector3.up * gameObject.transform.position.y;
			var marker = gameObject.AddComponent<LocationMarkerBehaviour>();
			marker.Map = this.map;
			marker.CoordinatesWGS84 = metersToLatLon(building.Coordinates);
			this.activeBuildings[building.filename] = newPointCloud;
		}
	}

	private static double getDistance(double[] a, double[] b) {
		return Math.Sqrt(Math.Pow(a[0] - b[0], 2.0) + Math.Pow(a[1] - b[1], 2.0));	
	}

	public void UnloadBuildings(float radius) {
		var center = latLonToMeters(this.map.CenterWGS84);
		var removed = new List<string>();
		foreach (var building in this.activeBuildings.Values) {
			if (getDistance(center, building.Metadata.Coordinates) > radius) {
				removed.Add(building.Metadata.filename);
				GameObject.Destroy(building.gameObject);
			}
		}
		foreach (var address in removed) {
			this.activeBuildings.Remove(address);
		}
	}
	
	void OnApplicationQuit() {
		map = null;
	}
}

