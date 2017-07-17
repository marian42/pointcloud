using UnityEngine;
using System;
using UnitySlippyMap.Map;
using UnitySlippyMap.Markers;
using UnitySlippyMap.Layers;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class TestMap : MonoBehaviour {
	private MapBehaviour map;
	
	private void Start() {
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
	
	void OnApplicationQuit() {
		map = null;
	}
}

