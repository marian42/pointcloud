// 
//  WebTileLayer.cs
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

using UnitySlippyMap.Map;

namespace UnitySlippyMap.Layers {
	public abstract class WebTileLayerBehaviour : TileLayerBehaviour {
		public string BaseURL;

		protected override void RequestTile (int tileX, int tileY, int roundedZoom, TileBehaviour tile)	{
			TileDownloaderBehaviour.Instance.Get (tileX, tileY, roundedZoom, GetTileURL (tileX, tileY, roundedZoom), tile);
		}

		protected override void CancelTileRequest (int tileX, int tileY, int roundedZoom) {
			TileDownloaderBehaviour.Instance.Cancel (GetTileURL (tileX, tileY, roundedZoom));
		}

		protected abstract string GetTileURL (int tileX, int tileY, int roundedZoom);
	}

}