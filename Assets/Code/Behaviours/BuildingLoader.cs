using UnityEngine;
using System;
using UnitySlippyMap.Map;
using UnitySlippyMap.Markers;
using UnitySlippyMap.Layers;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using UnityEditor;

public class BuildingLoader : MonoBehaviour {
	public static BuildingLoader Instance {
		get;
		private set;
	}

	private MapBehaviour map;

	private BuildingHashSet buildings;

	private Dictionary<string, PointCloudBehaviour> activeBuildings;

	private LineRenderer selectionRenderer;
	private LocationMarkerBehaviour selectionMarker;
	private PointCloudBehaviour selectedBuilding;

	private Queue<PointCloud> pointCloudsToDisplay;

	private bool doubleClick;

	public bool MetadataLoadingComplete {
		get;
		private set;
	}

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

		public IEnumerable<BuildingMetadata> Values {
			get {
				return this.dict.Values.SelectMany(list => list);
			}
		}

		private Tuple<int, int> getBucket(BuildingMetadata building) {
			return new Tuple<int, int>((int)(Math.Floor(building.center[0] / bucketSize)), (int)(Math.Floor(building.center[1] / bucketSize)));
		}

		public BuildingHashSet() {
			this.dict = new Dictionary<Tuple<int, int>, List<BuildingMetadata>>();			
		}

		public void Add(IEnumerable<BuildingMetadata> data) {
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
						if (Math.Pow(coordinates[0] - item.center[0], 2) + Math.Pow(coordinates[1] - item.center[1], 2) < radiusSquared) {
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

		map.CenterWGS84 = this.GetComponent<Bookmarks>().Items.First().Coordinates;
		map.UsesLocation = true;
		map.InputsEnabled = true;

		OSMTileLayer osmLayer = map.CreateLayer<OSMTileLayer>("OSM");
		osmLayer.BaseURL = "http://cartodb-basemaps-b.global.ssl.fastly.net/light_all/";
		osmLayer.MaxDisplayZoom = 18;

		Selection.activeTransform = this.transform;
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
		this.MetadataLoadingComplete = false;
		foreach (var file in new DirectoryInfo(Options.CleanPath(Options.Instance.MetadataFolder)).GetFiles()) {
			if (file.Extension != ".json") {
				continue;
			}
			var data = JsonUtility.FromJson<MetadataList>(File.ReadAllText(file.FullName));
			var buildingList = data.buildings;
			this.buildings.Add(buildingList);
			Debug.Log("Loaded metadata for " + buildingList.Count + " buildings (" + file.Name + ").");
		}
		this.MetadataLoadingComplete = true;
	}

	private void Start() {
		BuildingLoader.Instance = this;
		this.setupMap();
		this.pointCloudsToDisplay = new Queue<PointCloud>();
		this.buildings = new BuildingHashSet();
		this.activeBuildings = new Dictionary<string, PointCloudBehaviour>();

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
		foreach (var building in this.buildings.GetBuildings(center, this.LoadRadius).OrderBy(b => getDistance(b.center, center))) {
			if (this.activeBuildings.ContainsKey(building.filename)) {
				continue;
			}
			var pointCloud = new PointCloud(building);
			pointCloud.Load();
			this.pointCloudsToDisplay.Enqueue(pointCloud);
		}
	}

	private static double getDistance(double[] a, double[] b) {
		return Math.Sqrt(Math.Pow(a[0] - b[0], 2.0) + Math.Pow(a[1] - b[1], 2.0));	
	}

	public void UnloadBuildings(float radius) {
		var center = BuildingLoader.latLonToMeters(this.map.CenterWGS84);
		var removed = new List<string>();
		foreach (var building in this.activeBuildings.Values) {
			if (getDistance(center, building.PointCloud.Metadata.center) > radius) {
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
				this.selectedBuilding.CreateMesh(AbstractMeshCreator.CurrentType, ShapeMeshCreator.CleanMeshDefault);
			} else {
				this.selectFromMap();
			}
		}

		while (this.pointCloudsToDisplay.Any()) {
			this.createGameObject(this.pointCloudsToDisplay.Dequeue());
		}
	}

	private PointCloudBehaviour createGameObject(PointCloud pointCloud) {
		GameObject gameObject = new GameObject();
		var pointCloudBehaviour = gameObject.AddComponent<PointCloudBehaviour>();
		pointCloudBehaviour.Initialize(pointCloud);
		gameObject.transform.position = Vector3.up * gameObject.transform.position.y;
		var marker = gameObject.AddComponent<LocationMarkerBehaviour>();
		marker.Map = this.map;
		marker.CoordinatesWGS84 = BuildingLoader.metersToLatLon(pointCloud.Metadata.center);
		this.activeBuildings[pointCloud.Metadata.filename] = pointCloudBehaviour;
		return pointCloudBehaviour;
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
		this.select(result.First());
		UnityEditor.Selection.objects = new GameObject[] { this.selectedBuilding.gameObject };
	}

	private void select(PointCloudBehaviour building) {
		this.selectedBuilding = building;		
		this.selectionMarker.CoordinatesWGS84 = this.selectedBuilding.GetComponent<LocationMarkerBehaviour>().CoordinatesWGS84;
		var shape = this.selectedBuilding.GetComponent<PointCloudBehaviour>().PointCloud.Shape;
		this.selectionRenderer.positionCount = shape.Length;
		this.selectionRenderer.SetPositions(shape.Select(p => new Vector3(p.x, 0.25f, p.y)).ToArray());
	}

	public IEnumerable<PointCloudBehaviour> GetLoadedPointClouds() {
		return this.activeBuildings.Values;
	}

	public PointCloudBehaviour LoadRandom(Func<PointCloud, bool> condition = null) {
		var buildings = this.buildings.Values.ToList();

		int maxTries = 100;
		while (maxTries-- > 0) {
			var building = buildings.TakeRandom();

			if (this.activeBuildings.ContainsKey(building.filename)) {
				continue;
			}
			var pointCloud = new PointCloud(building);
			pointCloud.Load();

			if (condition != null && !condition.Invoke(pointCloud)) {
				continue;
			}
			var result = this.createGameObject(pointCloud);
			this.select(result);
			return result;
		}
		throw new Exception("Couldn't find a good building.");
	}	
}