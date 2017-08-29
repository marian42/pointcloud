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
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

public class BuildingLoader : MonoBehaviour {
	public static BuildingLoader Instance {
		get;
		private set;
	}

	private MapBehaviour map;

	private const string metadataFilename = "/data/metadata.json";

	private BuildingHashSet buildings;

	private Dictionary<string, PointCloudBehaviour> activeBuildings;

	private LineRenderer selectionRenderer;
	private LocationMarkerBehaviour selectionMarker;
	private PointCloudBehaviour selectedBuilding;

	private string dataPath;

	private Queue<PointCloud> pointCloudsToDisplay;

	private bool doubleClick;

	private class MetadataList {
		public List<BuildingMetadata> buildings;
	}

	[Range(100.0f, 600.0f)]
	public float LoadRadius = 200.0f;

	[Range(100.0f, 2000.0f)]
	public float UnloadRadius = 1000.0f;

	private static IMathTransform latLonToMetersTransform;
	private static IMathTransform metersToLatLonTransform;

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
		map.InputDelegate += UnitySlippyMap.MapInput.BasicTouchAndKeyboard;
		map.MaxZoom = 40.0f;
		map.CurrentZoom = 18.0f;

		map.CenterWGS84 = new double[2] { 7.4402747, 51.5638601 };
		map.UsesLocation = true;
		map.InputsEnabled = true;

		OSMTileLayer osmLayer = map.CreateLayer<OSMTileLayer>("OSM");
		osmLayer.BaseURL = "http://cartodb-basemaps-b.global.ssl.fastly.net/light_all/";
	}

	private static void initiailizeTransforms() {
		ICoordinateSystem wgs84geo = ProjNet.CoordinateSystems.GeographicCoordinateSystem.WGS84;
		ICoordinateSystem utm = ProjNet.CoordinateSystems.ProjectedCoordinateSystem.WGS84_UTM(32, true);
		var factory = new CoordinateTransformationFactory();
		var wgsToUtm = factory.CreateFromCoordinateSystems(wgs84geo, utm);
		latLonToMetersTransform = wgsToUtm.MathTransform;
		metersToLatLonTransform = wgsToUtm.MathTransform.Inverse();
	}

	public static double[] latLonToMeters(double[] value) {
		if (BuildingLoader.latLonToMetersTransform == null) {
			initiailizeTransforms();
		}

		return latLonToMetersTransform.Transform(value);
	}

	public static double[] metersToLatLon(double[] value) {
		if (BuildingLoader.metersToLatLonTransform == null) {
			initiailizeTransforms();
		}

		return metersToLatLonTransform.Transform(value);
	}

	private void loadMetadata() {
		var data = JsonUtility.FromJson<MetadataList>(File.ReadAllText(this.dataPath + metadataFilename));
		var buildingList = data.buildings;
		this.buildings = new BuildingHashSet(buildingList);
		Debug.Log("Loaded metadata for " + buildingList.Count + " buildings.");
	}

	private void Start() {
		BuildingLoader.Instance = this;
		this.setupMap();
		this.pointCloudsToDisplay = new Queue<PointCloud>();
		this.buildings = new BuildingHashSet(Enumerable.Empty<BuildingMetadata>());
		this.activeBuildings = new Dictionary<string, PointCloudBehaviour>();

		this.dataPath = Application.dataPath;
		var thread = new System.Threading.Thread(this.loadMetadata);
		thread.Start();

		GameObject selectionGO = GameObject.Find("Selection");
		this.selectionRenderer = selectionGO.GetComponent<LineRenderer>();
		this.selectionMarker = selectionGO.AddComponent<LocationMarkerBehaviour>();
		this.selectionMarker.Map = this.map;
	}

	public void UpdateBuildings() {
		this.UnloadBuildings(this.UnloadRadius);

		var thread = new System.Threading.Thread(this.updateBuildingsThread);
		thread.Start();
	}

	private void updateBuildingsThread() {
		var center = BuildingLoader.latLonToMeters(this.map.CenterWGS84);
		foreach (var building in this.buildings.GetBuildings(center, this.LoadRadius).OrderBy(b => getDistance(b.Coordinates, center))) {
			if (this.activeBuildings.ContainsKey(building.filename)) {
				continue;
			}
			this.pointCloudsToDisplay.Enqueue((new PointCloud("C:/output/" + building.filename + ".points")));
		}
	}

	private static double getDistance(double[] a, double[] b) {
		return Math.Sqrt(Math.Pow(a[0] - b[0], 2.0) + Math.Pow(a[1] - b[1], 2.0));	
	}

	public void UnloadBuildings(float radius) {
		var center = BuildingLoader.latLonToMeters(this.map.CenterWGS84);
		var removed = new List<string>();
		foreach (var building in this.activeBuildings.Values) {
			if (getDistance(center, building.PointCloud.Metadata.Coordinates) > radius) {
				removed.Add(building.PointCloud.Metadata.filename);
				GameObject.Destroy(building.gameObject);
			}
		}
		foreach (var address in removed) {
			this.activeBuildings.Remove(address);
		}
	}

	public void Isolate(PointCloud pointCloud) {
		foreach (var building in this.activeBuildings.Values.ToList()) {
			if (building.PointCloud == pointCloud) {
				continue;
			}
			this.activeBuildings.Remove(building.PointCloud.Metadata.filename);
			GameObject.Destroy(building.gameObject);
		}
	}
	
	void OnApplicationQuit() {
		map = null;
	}

	private float lastMouseDown;

	public void Update() {
		if (Input.GetMouseButtonDown(2) || Input.GetKeyDown(KeyCode.Space)) {
			this.UpdateBuildings();
		}

		if (Input.GetMouseButtonDown(0)) {
			this.doubleClick = Time.time - this.lastMouseDown < 0.3;
			this.lastMouseDown = Time.time;
		}

		if (Input.GetMouseButtonUp(0) && Time.time - this.lastMouseDown < 0.2) {
			if (this.selectedBuilding != null && this.doubleClick) {
				this.selectedBuilding.CreateMesh(AbstractMeshCreator.CurrentType, true);
			} else {
				this.selectFromMap();
			}
		}

		while (this.pointCloudsToDisplay.Any()) {
			var pointCloud = this.pointCloudsToDisplay.Dequeue();
			GameObject gameObject = new GameObject();
			var pointCloudBehaviour = gameObject.AddComponent<PointCloudBehaviour>();
			pointCloudBehaviour.Initialize(pointCloud);
			gameObject.transform.position = Vector3.up * gameObject.transform.position.y;
			var marker = gameObject.AddComponent<LocationMarkerBehaviour>();
			marker.Map = this.map;
			marker.CoordinatesWGS84 = BuildingLoader.metersToLatLon(pointCloud.Metadata.Coordinates);
			this.activeBuildings[pointCloud.Metadata.filename] = pointCloudBehaviour;
		}
	}

	private void selectFromMap() {
		Ray ray = map.CurrentCamera.ScreenPointToRay(Input.mousePosition);
		Plane plane = new Plane(Vector3.up, Vector3.zero);
		float dist;
		if (!plane.Raycast(ray, out dist)) {
			return;
		}
		Vector3 displacement = ray.GetPoint(dist);

		double[] displacementMeters = new double[2] {
				displacement.x / map.RoundedScaleMultiplier,
				displacement.z / map.RoundedScaleMultiplier
			};
		double[] coordinateMeters = new double[2] {
				map.CenterEPSG900913[0] + displacementMeters[0],
				map.CenterEPSG900913[1] + displacementMeters[1]
			};

		var coordinates = BuildingLoader.latLonToMeters(this.map.EPSG900913ToWGS84Transform.Transform(coordinateMeters));

		var result = this.activeBuildings.Values.OrderBy(p => getDistance(p.PointCloud.Center, coordinates));
		if (!result.Any()) {
			return;
		}
		selectedBuilding = result.First();
		UnityEditor.Selection.objects = new GameObject[] { selectedBuilding.gameObject };

		this.selectionMarker.CoordinatesWGS84 = selectedBuilding.GetComponent<LocationMarkerBehaviour>().CoordinatesWGS84;
		var shape = selectedBuilding.GetComponent<PointCloudBehaviour>().PointCloud.GetShape();
		this.selectionRenderer.positionCount = shape.Length;
		this.selectionRenderer.SetPositions(shape.Select(p => new Vector3(p.x, 0.25f, p.y)).ToArray());
	}

	public IEnumerable<PointCloudBehaviour> GetLoadedPointClouds() {
		return this.activeBuildings.Values;
	}
}