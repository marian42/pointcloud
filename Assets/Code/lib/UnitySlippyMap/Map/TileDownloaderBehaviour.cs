// 
//  TileDownloader.cs
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
using System.IO;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace UnitySlippyMap.Map
{
	/// <summary>
	/// A singleton class in charge of downloading, caching and serving tiles.
	/// </summary>
	public class TileDownloaderBehaviour : MonoBehaviour
	{
	#region Singleton implementation
	
		/// <summary>
		/// The instance.
		/// </summary>
		private static TileDownloaderBehaviour instance = null;

		/// <summary>
		/// Gets the instance.
		/// </summary>
		/// <value>The instance.</value>
		public static TileDownloaderBehaviour Instance
		{
			get
			{
	            if (null == (object)instance)
	            {
					instance = FindObjectOfType(typeof (TileDownloaderBehaviour)) as TileDownloaderBehaviour;
	                if (null == (object)instance)
	                {
	                    var go = new GameObject("[TileDownloader]");
	                    go.hideFlags = HideFlags.HideAndDontSave;
						instance = go.AddComponent<TileDownloaderBehaviour>();
	                }
				}

				return instance;
			}
		}
	
		/// <summary>
		/// Initializes a new instance of the <see cref="UnitySlippyMap.Map.TileDownloader"/> class.
		/// </summary>
		private TileDownloaderBehaviour()
		{
		}
	
		/// <summary>
		/// Raises the application quit event.
		/// </summary>
		private void OnApplicationQuit()
		{
			DestroyImmediate(this.gameObject);
		}
	
	#endregion
	
	#region Tile download subclasses
	
		/// <summary>
		/// A helper class for asynchronous IO operations.
		/// </summary>
	    private class AsyncInfo
	    {
			/// <summary>
			/// The tile entry.
			/// </summary>
	        private TileEntry entry;

			/// <summary>
			/// Gets the tile entry.
			/// </summary>
			/// <value>The tile entry.</value>
	        public TileEntry Entry { get { return entry;  } }

			/// <summary>
			/// The filestream.
			/// </summary>
	        private FileStream fs;

			/// <summary>
			/// Gets the FileStream instance.
			/// </summary>
			/// <value>The filestream.</value>
	        public FileStream FS { get { return fs; } }

			/// <summary>
			/// Initializes a new instance of the <see cref="UnitySlippyMap.Map.TileDownloaderBehaviour+AsyncInfo"/> class.
			/// </summary>
			/// <param name="entry">Entry.</param>
			/// <param name="fs">Fs.</param>
	        public AsyncInfo(TileEntry entry, FileStream fs)
	        {
	            this.entry = entry;
	            this.fs = fs;
	        }
	    }

		/// <summary>
		/// The TileEntry class holds the information necessary to the TileDownloader to manage the tiles.
		/// It also handles the (down)loading/caching of the concerned tile, taking advantage of Prime31's JobManager
		/// </summary>
		public class TileEntry
		{

			public readonly int x;
			public readonly int y;
			public readonly int zoom;

			public readonly string url;

			public readonly string cacheFileName;

			public TileBehaviour     tile;

			public Texture2D texture;


			/// <summary>
			/// The error flag.
			/// </summary>
			public bool		error = false;
		
			/// <summary>
			/// The job.
			/// </summary>
			public Job		job;

			/// <summary>
			/// The job complete handler.
			/// </summary>
	        public Job.JobCompleteHandler jobCompleteHandler;

			/// <summary>
			/// Initializes a new instance of the <see cref="UnitySlippyMap.Map.TileDownloader+TileEntry"/> class.
			/// </summary>
			public TileEntry(int x, int y, int zoom, string url)
			{
				this.x = x;
				this.y = y;
				this.zoom = zoom;
				this.url = url;
				string extension = Path.GetExtension(url);

				if (extension.Contains("?")) {
	                extension = extension.Substring(0, extension.IndexOf('?'));
				}
				
				this.cacheFileName = TileDownloaderBehaviour.tilePath + "/" + zoom + "-" + x + "-" + y + extension;
				this.jobCompleteHandler = new Job.JobCompleteHandler(TileDownloaderBehaviour.Instance.JobTerminationEvent);
			}

			public bool Cached {
				get {
					return File.Exists(this.cacheFileName);
				}
			}
		
			/// <summary>
			/// Initializes a new instance of the <see cref="UnitySlippyMap.Map.TileDownloader+TileEntry"/> class.
			/// </summary>
			/// <param name="url">URL.</param>
			/// <param name="tile">Tile.</param>
			public TileEntry(int x, int y, int zoom, string url, TileBehaviour tile) : this(x,y,zoom, url)
			{
				this.tile = tile;
			}
		
			/// <summary>
			/// Starts the download.
			/// </summary>
			public void StartDownload()
			{
#if DEBUG_LOG
				Debug.Log("DEBUG: TileEntry.StartDownload: " + url);
#endif
				job = new Job(DownloadCoroutine(), this);
				job.JobComplete += jobCompleteHandler;
			}
		
			/// <summary>
			/// Stops the download.
			/// </summary>
			public void StopDownload()
			{
#if DEBUG_LOG
				Debug.Log("DEBUG: TileEntry.StopDownload: " + url);
#endif
	            job.JobComplete -= jobCompleteHandler;
				job.Kill();
			}
			
			/// <summary>
			/// The download the coroutine.
			/// </summary>
			/// <returns>The coroutine.</returns>
			private IEnumerator DownloadCoroutine()
			{
				WWW www = null;

				bool cacheHit = this.Cached;

				if (cacheHit)
	            {
					www = new WWW("file:///" + this.cacheFileName);
            	}
            	else {
                	www = new WWW(url);
	            }

	            yield return www;
			
				if (String.IsNullOrEmpty(www.error) && www.text.Contains("404 Not Found") == false)
				{

	                Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, true);
					www.LoadImageIntoTexture(texture);

					if (cacheHit == false) {

		                byte[] bytes = www.bytes;
						
						FileStream fs = new FileStream(this.cacheFileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
						fs.BeginWrite(bytes, 0, bytes.Length, new AsyncCallback(EndWriteCallback), new AsyncInfo(this, fs));
				
					}
					
					tile.SetTexture(texture);
				}
				else {
					this.error = true;
				}
			}
		
#if !UNITY_WEBPLAYER
			/// <summary>
			/// The callback called at the end of the writing operation.
			/// </summary>
			/// <param name="result">Result.</param>
			private static void EndWriteCallback(IAsyncResult result)
			{
				AsyncInfo info = result.AsyncState as AsyncInfo;
				
	            info.FS.EndWrite(result);
	            info.FS.Flush();

	            info.FS.Close();

#if DEBUG_LOG
				Debug.Log("DEBUG: TileEntry.EndWriteCallback: done writing: " + info.Entry.url + " [" + info.Entry.guid + "]");
#endif
			}
#endif
		}
	
	#endregion
	
	#region Private members & properties

		/// <summary>
		/// The tiles to load.
		/// </summary>
		private List<TileEntry>	tilesToLoad = new List<TileEntry>();

		/// <summary>
		/// The tiles loading.
		/// </summary>
		private List<TileEntry>	tilesLoading = new List<TileEntry>();

#if !UNITY_WEBPLAYER
		/// <summary>
		/// The tiles.
		/// </summary>
		private List<TileEntry>	tiles = new List<TileEntry>();

		/// <summary>
		/// The tile path.
		/// </summary>
		private static string tilePath {
			get {
				return Application.temporaryCachePath;
			}
		}
#endif
	
		/// <summary>
		/// The max simultaneous downloads.
		/// </summary>
		private int maxSimultaneousDownloads = 2;

		/// <summary>
		/// Gets or sets the max simultaneous downloads.
		/// </summary>
		/// <value>The max simultaneous downloads.</value>
		public int MaxSimultaneousDownloads { get { return maxSimultaneousDownloads; } set { maxSimultaneousDownloads = value; } }
	
#if !UNITY_WEBPLAYER
		/// <summary>
		/// The size of the max cache.
		/// </summary>
		private int maxCacheSize = 1000000000; // 1 GB

		/// <summary>
		/// Gets or sets the size of the max cache.
		/// </summary>
		/// <value>The size of the max cache.</value>
		public int MaxCacheSize { get { return maxCacheSize; } set { maxCacheSize = value; } }
#endif
	
	#endregion
		
	#region Public methods

		/// <summary>
		/// Gets a tile by its URL, the main texture of the material is assigned if successful.
		/// </summary>
		/// <param name="url">URL.</param>
		/// <param name="tile">Tile.</param>
		public void Get(int x, int y, int zoom, string url, TileBehaviour tile)
		{
	        
			if (tilesToLoad.Exists(t => t.url == url) || tilesLoading.Exists(t => t.url == url))
			{
				return ;
			}

			TileEntry cachedEntry = tiles.Find(t => t.url == url);

			if (cachedEntry == null)
	        {
				tilesToLoad.Add(new TileEntry(x, y, zoom, url, tile));
	        }
			else
			{
				cachedEntry.tile = tile;
				tilesToLoad.Add(cachedEntry);
			}
	    }

		/// <summary>
		/// Cancels the request for a tile by its URL.
		/// </summary>
		/// <returns><c>true</c> if this instance cancel url; otherwise, <c>false</c>.</returns>
		/// <param name="url">URL.</param>
		public void Cancel(string url)
		{
			TileEntry entry = tilesToLoad.Find(t => t.url == url);
			if (entry != null)
			{
	#if DEBUG_LOG
				Debug.Log("DEBUG: TileDownloader.Cancel: remove download from schedule: " + url);
	#endif
				tilesToLoad.Remove(entry);
				return ;
			}
			
			entry = tilesLoading.Find(t => t.url == url);
			if (entry != null)
			{
	#if DEBUG_LOG
				Debug.Log("DEBUG: TileDownloader.Cancel: stop downloading: " + url);
	#endif
	            tilesLoading.Remove(entry);
				entry.StopDownload();
				return ;
			}

	#if DEBUG_LOG
			Debug.LogWarning("WARNING: TileDownloader.Cancel: url not scheduled to be downloaded nor downloading: " + url);
	#endif
		}

		/// <summary>
		/// A method called when the job is done, successfully or not.
		/// </summary>
		/// <param name="job">Job.</param>
		/// <param name="e">E.</param>
		public void JobTerminationEvent(object job, JobEventArgs e) {
			TileEntry entry = e.Owner as TileEntry;
			tilesLoading.Remove(entry);
		}

		/// <summary>
		/// Pauses all.
		/// </summary>
		public void PauseAll()
		{
	        foreach (TileEntry entry in tilesLoading)
	        {
	            entry.job.Pause();
	        }
		}

		/// <summary>
		/// Unpauses all.
		/// </summary>
		public void UnpauseAll()
		{
	        foreach (TileEntry entry in tilesLoading)
	        {
	            entry.job.Unpause();
	        }
		}
		
	#endregion
	
	#region Private methods
 
		/// <summary>
		/// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.Start().
		/// </summary>
	    private void Start()
	    {
			TextureBogusExtension.Init(this);
	    }
    
		/// <summary>
		/// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.Update().
		/// </summary>
		private void Update()
		{
			while (tilesToLoad.Count > 0
				&& tilesLoading.Count < MaxSimultaneousDownloads)
			{
				DownloadNextTile();
			}
	        
	#if DEBUG_LOG
	        /*
	        if (tilesLoading.Count >= MaxSimultaneousDownloads)
	        {
	            Debug.Log("DEBUG: TileDownload.Update: tilesLoading.Count (" + tilesLoading.Count + ") > MaxSimultaneousDownloads (" + MaxSimultaneousDownloads + ")");
	            string dbg = "DEBUG: tilesLoading entries:\n";
	            foreach (TileEntry entry in tilesLoading)
	            {
	                dbg += entry.url + "\n";
	            }
	            Debug.Log(dbg);
	        }
	         */
	  
	        /*
	        {
	            string dbg = "DEBUG: tilesToLoad entries:\n";
	            foreach (TileEntry entry in tilesToLoad)
	            {
	                dbg += entry.url + "\n";
	            }
	            Debug.Log(dbg);
	        }
	        */
	#endif
		}
		
		/// <summary>
		/// Downloads the next tile.
		/// </summary>
		private void DownloadNextTile()
		{
			TileEntry entry = tilesToLoad[0];
			tilesToLoad.RemoveAt(0);
			tilesLoading.Add(entry);
			
	#if DEBUG_LOG
	        Debug.Log("DEBUG: TileDownloader.DownloadNextTile: entry.url: " + entry.url);
	#endif
	        
			entry.StartDownload();		
		}
	 
		/// <summary>
		/// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.OnDestroy().
		/// </summary>
		private void OnDestroy()
		{
	        KillAll();		
			instance = null;
		}
    
		/// <summary>
		/// Kills all.
		/// </summary>
	    private void KillAll()
	    {
	        foreach (TileEntry entry in tilesLoading)
	        {
	            entry.job.Kill();
	        }
	    }
	#endregion
	}


}