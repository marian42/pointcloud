// 
//  TileLayer.cs
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

using System;
using System.Collections.Generic;

using UnityEngine;

using UnitySlippyMap.Map;

namespace UnitySlippyMap.Layers {

	public abstract class TileLayerBehaviour : LayerBehaviour {

		protected int tileCacheSizeLimit = 100;

		protected static TileBehaviour tileTemplate;

		protected static int tileTemplateUseCount = 0;

		protected Dictionary<string, TileBehaviour> tiles = new Dictionary<string, TileBehaviour> ();

		protected List<TileBehaviour> tileCache = new List<TileBehaviour> ();

		protected List<string> visitedTiles = new List<string> ();

		protected bool isReadyToBeQueried = false;

		protected bool needsToBeUpdatedWhenReady = false;

		public int MaxDisplayZoom = 40;

		protected int DisplayZoom {
			get {
				return Math.Min(this.MaxDisplayZoom, Map.RoundedZoom);
			}
		}

		protected float RoundedHalfMapScale {
			get {
				return Map.GetRoundedHalfMapScale(this.DisplayZoom);
			}
		}
    	
		protected enum NeighbourTileDirection {
			North,
			South,
			East,
			West
		}

		protected void Awake ()	{
			if (tileTemplate == null) {
				tileTemplate = TileBehaviour.CreateTileTemplate ();
				tileTemplate.hideFlags = HideFlags.HideAndDontSave;
				tileTemplate.GetComponent<Renderer>().enabled = false;
			}
			++tileTemplateUseCount;
		}

		private void Start () {		
			if (tileTemplate.transform.localScale.x != this.RoundedHalfMapScale)
				tileTemplate.transform.localScale = new Vector3(this.RoundedHalfMapScale, 1.0f, this.RoundedHalfMapScale);
		}

		private void OnDestroy () {
			--tileTemplateUseCount;
		
			if (tileTemplate != null && tileTemplateUseCount == 0)
				DestroyImmediate(tileTemplate);
		}	
	
		public override void UpdateContent () {
			if (tileTemplate.transform.localScale.x != this.RoundedHalfMapScale)
				tileTemplate.transform.localScale = new Vector3 (this.RoundedHalfMapScale, 1.0f, this.RoundedHalfMapScale);

			if (Map.CurrentCamera != null && isReadyToBeQueried) {
				Plane[] frustum = GeometryUtility.CalculateFrustumPlanes (Map.CurrentCamera);

				visitedTiles.Clear ();

				UpdateTiles (frustum);

				CleanUpTiles(frustum, this.DisplayZoom);
			} else
				needsToBeUpdatedWhenReady = true;
		
			// move the tiles by the map's root translation
			Vector3 displacement = Map.gameObject.transform.position;
			if (displacement != Vector3.zero) {
				foreach (KeyValuePair<string, TileBehaviour> tile in tiles) {
					tile.Value.transform.position += displacement;
				}
			}
		}
	
		protected static string	tileAddressLookedFor;

		protected static bool visitedTilesMatchPredicate (string tileAddress) {
			if (tileAddress == tileAddressLookedFor)
				return true;
			return false;
		}

		private bool CheckTileExistence (int tileRoundedZoom, int tileX, int tileY)	{
			string key = TileBehaviour.GetTileKey (tileRoundedZoom, tileX, tileY);
			if (!tiles.ContainsKey (key))
				return true; // the tile is out of the frustum
			TileBehaviour tile = tiles [key];
			Renderer r = tile.GetComponent<Renderer>();
			return r.enabled && r.material.mainTexture != null && !tile.Showing;
		}

		private bool CheckTileOutExistence (int roundedZoom, int tileRoundedZoom, int tileX, int tileY)	{
			if (roundedZoom == tileRoundedZoom)
				return CheckTileExistence (tileRoundedZoom, tileX, tileY);
			return CheckTileOutExistence (roundedZoom, tileRoundedZoom - 1, tileX / 2, tileY / 2); 
		}

		private bool CheckTileInExistence (int roundedZoom, int tileRoundedZoom, int tileX, int tileY) {
			if (roundedZoom == tileRoundedZoom)
				return CheckTileExistence (tileRoundedZoom, tileX, tileY);
			int currentRoundedZoom = tileRoundedZoom + 1;
			int currentTileX = tileX * 2;
			int currentTileY = tileY * 2;
			return CheckTileInExistence (roundedZoom, currentRoundedZoom, currentTileX, currentTileY)
				&& CheckTileInExistence (roundedZoom, currentRoundedZoom, currentTileX + 1, currentTileY)
				&& CheckTileInExistence (roundedZoom, currentRoundedZoom, currentTileX, currentTileY + 1)
				&& CheckTileInExistence (roundedZoom, currentRoundedZoom, currentTileX + 1, currentTileY + 1);
		}

		private void CleanUpTiles (Plane[] frustum, int roundedZoom) {
			List<string> tilesToRemove = new List<string> ();
			foreach (KeyValuePair<string, TileBehaviour> pair in tiles) {
				TileBehaviour tile = pair.Value;
				string tileKey = pair.Key;

				string[] tileAddressTokens = tileKey.Split ('_');
				int tileRoundedZoom = Int32.Parse (tileAddressTokens [0]);
				int tileX = Int32.Parse (tileAddressTokens [1]);
				int tileY = Int32.Parse (tileAddressTokens [2]);

				int roundedZoomDif = tileRoundedZoom - roundedZoom;
				bool inFrustum = GeometryUtility.TestPlanesAABB (frustum, tile.GetComponent<Collider>().bounds);

				if (!inFrustum || roundedZoomDif != 0) {
					CancelTileRequest (tileX, tileY, tileRoundedZoom);

					if (!inFrustum
						|| (roundedZoomDif > 0 && CheckTileOutExistence (roundedZoom, tileRoundedZoom, tileX, tileY))
						|| (roundedZoomDif < 0 && CheckTileInExistence (roundedZoom, tileRoundedZoom, tileX, tileY))) {
						tilesToRemove.Add (tileKey);
					}
				}
			}

			foreach (string tileAddress in tilesToRemove) {
				TileBehaviour tile = tiles [tileAddress];

				Renderer renderer = tile.GetComponent<Renderer>();
				if (renderer != null) {
					GameObject.DestroyImmediate (renderer.material.mainTexture);
					//TextureAtlasManager.Instance.RemoveTexture(pair.Value.TextureId);
					renderer.material.mainTexture = null;

					renderer.enabled = false;
				}

				tiles.Remove (tileAddress);
				tileCache.Add (tile);
			}
		}

		private void UpdateTiles (Plane[] frustum) {
			int tileX, tileY;
			int tileCountOnX, tileCountOnY;
			float offsetX, offsetZ;
		
			GetTileCountPerAxis (out tileCountOnX, out tileCountOnY);
			GetCenterTile (tileCountOnX, tileCountOnY, out tileX, out tileY, out offsetX, out offsetZ);
			GrowTiles (frustum, tileX, tileY, tileCountOnX, tileCountOnY, offsetX, offsetZ);
		}

		void GrowTiles (Plane[] frustum, int tileX, int tileY, int tileCountOnX, int tileCountOnY, float offsetX, float offsetZ) {
			tileTemplate.transform.position = new Vector3 (offsetX, tileTemplate.transform.position.y, offsetZ);
			if (GeometryUtility.TestPlanesAABB (frustum, tileTemplate.GetComponent<Collider>().bounds) == true) {
				if (tileX < 0)
					tileX += tileCountOnX;
				else if (tileX >= tileCountOnX)
					tileX -= tileCountOnX;

				string tileAddress = TileBehaviour.GetTileKey(this.DisplayZoom, tileX, tileY);
				if (tiles.ContainsKey (tileAddress) == false) {
					TileBehaviour tile = null;
					if (tileCache.Count > 0) {
						tile = tileCache [0];
						tileCache.Remove (tile);
						tile.transform.position = tileTemplate.transform.position;
						tile.transform.localScale = new Vector3(this.RoundedHalfMapScale, 1.0f, this.RoundedHalfMapScale);
						//tile.gameObject.active = this.gameObject.active;
					} else {
						tile = (GameObject.Instantiate (tileTemplate.gameObject) as GameObject).GetComponent<TileBehaviour> ();
						tile.transform.parent = this.gameObject.transform;
					}
				
					tile.name = "tile_" + tileAddress;
					tiles.Add (tileAddress, tile);

					RequestTile(tileX, tileY, this.DisplayZoom, tile);
				}
			
				tileAddressLookedFor = tileAddress;
				if (visitedTiles.Exists (visitedTilesMatchPredicate) == false) {
					visitedTiles.Add (tileAddress);

					// grow tiles in the four directions without getting outside of the coordinate range of the zoom level
					int nTileX, nTileY;
					float nOffsetX, nOffsetZ;

					if (GetNeighbourTile (tileX, tileY, offsetX, offsetZ, tileCountOnX, tileCountOnY, NeighbourTileDirection.South, out nTileX, out nTileY, out nOffsetX, out nOffsetZ))
						GrowTiles (frustum, nTileX, nTileY, tileCountOnX, tileCountOnY, nOffsetX, nOffsetZ);

					if (GetNeighbourTile (tileX, tileY, offsetX, offsetZ, tileCountOnX, tileCountOnY, NeighbourTileDirection.North, out nTileX, out nTileY, out nOffsetX, out nOffsetZ))
						GrowTiles (frustum, nTileX, nTileY, tileCountOnX, tileCountOnY, nOffsetX, nOffsetZ);

					if (GetNeighbourTile (tileX, tileY, offsetX, offsetZ, tileCountOnX, tileCountOnY, NeighbourTileDirection.East, out nTileX, out nTileY, out nOffsetX, out nOffsetZ))
						GrowTiles (frustum, nTileX, nTileY, tileCountOnX, tileCountOnY, nOffsetX, nOffsetZ);

					if (GetNeighbourTile (tileX, tileY, offsetX, offsetZ, tileCountOnX, tileCountOnY, NeighbourTileDirection.West, out nTileX, out nTileY, out nOffsetX, out nOffsetZ))
						GrowTiles (frustum, nTileX, nTileY, tileCountOnX, tileCountOnY, nOffsetX, nOffsetZ);
				}
			}
		}
	
		protected abstract void GetTileCountPerAxis (out int tileCountOnX, out int tileCountOnY);

		protected abstract void GetCenterTile (int tileCountOnX, int tileCountOnY, out int tileX, out int tileY, out float offsetX, out float offsetZ);

		protected abstract bool GetNeighbourTile (int tileX, int tileY, float offsetX, float offsetY, int tileCountOnX, int tileCountOnY, NeighbourTileDirection dir, out int nTileX, out int nTileY, out float nOffsetX, out float nOffsetZ);
	
		
		protected abstract void RequestTile (int tileX, int tileY, int roundedZoom, TileBehaviour tile);
	
		
		protected abstract void CancelTileRequest (int tileX, int tileY, int roundedZoom);
	}

}