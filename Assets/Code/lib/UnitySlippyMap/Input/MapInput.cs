// 
//  Inputs.cs
//  
//  Author:
//       Jonathan Derrough <jonathan.derrough@gmail.com>
//  
//  Copyright (c) 2012 Jonathan Derrough
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using UnityEngine;

using UnitySlippyMap.Map;

namespace UnitySlippyMap
{

	/// <summary>
	/// Input delegate.
	/// </summary>
	public delegate void InputDelegate(MapBehaviour map, bool wasInputInterceptedByGUI);

	/// <summary>
	/// A class defining a basic set of user inputs.
	/// </summary>
	public static class MapInput
	{
		/// <summary>
		/// The last raycast hit position.
		/// </summary>
		private static Vector3	lastHitPosition = Vector3.zero;
		
		private static float zoomLeft = 0.0f;

		/// <summary>
		/// Handles inputs on touch devices and desktop.
		/// The <see cref="UnitySlippyMap.Map.MapBehaviour"/> instance is told to update its layers and markers once a movement is complete.
		/// When panning the map, the map's root GameObject is moved. Once the panning is done, all the children are offseted and the root's position is reset.
		/// </summary>
		/// <param name="map">Map.</param>
		/// <param name="wasInputInterceptedByGUI">If set to <c>true</c> was input intercepted by GU.</param>
		public static void BasicTouchAndKeyboard (MapBehaviour map, bool wasInputInterceptedByGUI) {
			bool panning = false;
			bool panningStopped = false;
			Vector3 screenPosition = Vector3.zero;

			if (wasInputInterceptedByGUI == false) {
				// movements
				if (UnityEngine.Input.GetMouseButton (0)) {
					panning = true;
					screenPosition = UnityEngine.Input.mousePosition;
				} else if (UnityEngine.Input.GetMouseButtonUp (0)) {
					panningStopped = true;
				}
			}
			
			if (panning) {
				// disable the centerWGS84 update with the last location
				map.UpdatesCenterWithLocation = false;
    			
				// apply the movements
				Ray ray = map.CurrentCamera.ScreenPointToRay (screenPosition);
				RaycastHit hitInfo;
				if (Physics.Raycast (ray, out hitInfo)) {
					Vector3 displacement = Vector3.zero;
					if (lastHitPosition != Vector3.zero) {
						displacement = hitInfo.point - lastHitPosition;
					}
					lastHitPosition = new Vector3 (hitInfo.point.x, hitInfo.point.y, hitInfo.point.z);
    				
					if (displacement != Vector3.zero) {
						// update the centerWGS84 property to the new centerWGS84 wgs84 coordinates of the map
						double[] displacementMeters = new double[2] {
							displacement.x / map.RoundedScaleMultiplier,
							displacement.z / map.RoundedScaleMultiplier
						};
						double[] centerMeters = new double[2] {
							map.CenterEPSG900913 [0],
							map.CenterEPSG900913 [1]
						};
						centerMeters [0] -= displacementMeters [0];
						centerMeters [1] -= displacementMeters [1];
						map.CenterEPSG900913 = centerMeters;
    					
						#if DEBUG_LOG
    					Debug.Log("DEBUG: Map.Update: new centerWGS84 wgs84: " + centerWGS84[0] + ", " + centerWGS84[1]);
						#endif
					}
    
					map.HasMoved = true;
				}
			} else if (panningStopped) {
				// reset the last hit position
				lastHitPosition = Vector3.zero;
    			
				// trigger a tile update
				map.IsDirty = true;
			}

			// Zoom
			float scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");

			if (UnityEngine.Input.GetKey(KeyCode.Equals)) {
				map.Zoom(0.8f);
			}
			if (UnityEngine.Input.GetKey(KeyCode.Minus)) {
				map.Zoom(-0.8f);
			}

			if (scroll != 0) {
				zoomLeft += scroll;
			}
			if (Mathf.Abs(zoomLeft) > 0.1) {
				map.Zoom(Mathf.Sign(zoomLeft) * 3.0f);
				zoomLeft -= Mathf.Sign(zoomLeft) * 3.0f * Time.deltaTime;
			}

			// Rotate
			bool updateCamera = false;

			if (UnityEngine.Input.GetMouseButton(1)) {
				map.CameraPitch = Mathf.Clamp(map.CameraPitch + 10.0f * UnityEngine.Input.GetAxis("Mouse Y"), 1.0f, 50.0f);
				map.CameraYaw -= 10.0f * UnityEngine.Input.GetAxis("Mouse X");
				updateCamera = true;
			} else if (UnityEngine.Input.GetMouseButtonDown(1)) {
				map.IsDirty = true;
			}

			if (UnityEngine.Input.GetKey(KeyCode.Q)) {
				map.CameraYaw -= 90.0f * Time.deltaTime;
				updateCamera = true;
			}

			if (UnityEngine.Input.GetKey(KeyCode.E)) {
				map.CameraYaw += 90.0f * Time.deltaTime;
				updateCamera = true;
			}

			if (UnityEngine.Input.GetKey(KeyCode.F)) {
				map.CameraPitch  = Mathf.Clamp(map.CameraPitch + 60.0f * Time.deltaTime, 1.0f, 50.0f);
				updateCamera = true;
			}

			if (UnityEngine.Input.GetKey(KeyCode.R)) {
				map.CameraPitch = Mathf.Clamp(map.CameraPitch - 60.0f * Time.deltaTime, 1.0f, 50.0f);
				updateCamera = true;
			}

			if (updateCamera) {
				map.UpdateCamera();
			}

			// Move
			if (UnityEngine.Input.GetKey(KeyCode.W)) {
				move(map, Vector3.forward);
			}

			if (UnityEngine.Input.GetKey(KeyCode.A)) {
				move(map, Vector3.right);
			}

			if (UnityEngine.Input.GetKey(KeyCode.S)) {
				move(map, Vector3.back);
			}

			if (UnityEngine.Input.GetKey(KeyCode.D)) {
				move(map, Vector3.left);
			}
		}

		private static void move(MapBehaviour map, Vector3 direction) {
			var displacement = Quaternion.Euler(Vector3.up * map.CameraYaw) * direction * 400.0f * Time.deltaTime * map.MetersPerPixel;
			double[] newMapCenter = new double[2] {
					map.CenterEPSG900913[0] - displacement.x,
					map.CenterEPSG900913[1] + displacement.z
				};
			map.CenterEPSG900913 = newMapCenter;
		}
	}
}

