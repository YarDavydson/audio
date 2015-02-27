﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Fusion.Mathematics;

namespace Fusion.Content {
	
	public class ContentManager : DisposableBase {

		class Item {
			public DateTime	LoadTime;
			public object	Object;
		}

		object lockObject =  new object();

		public readonly Game Game;
		Dictionary<string, Item> content;
		List<object> toDispose = new List<object>();
		List<ContentLoader> loaders;
		readonly string contentDirectory;



		/// <summary>
		/// Overloaded. Initializes a new instance of ContentManager. 
		/// </summary>
		/// <param name="game"></param>
		public ContentManager ( Game game, string contentDirectory = "Content" )
		{
			this.Game = game;

			this.contentDirectory = contentDirectory;

			content	=	new Dictionary<string,Item>();
			loaders	=	ContentLoader.GatherContentLoaders()
						.Select( clt => (ContentLoader)Activator.CreateInstance( clt ) )
						.ToList();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				Unload();
			}
			base.Dispose(disposing);
		}



		/// <summary>
		/// Gets path to cpecified content item. 
		/// Returns null if given object was not loaded by this content manager.
		/// </summary>
		/// <param name="contentItem"></param>
		/// <returns></returns>
		public string GetPathTo ( object contentItem )
		{
			return content
				.Where( c1 => c1.Value.Object == contentItem )
				.Select( c2 => c2.Key )
				.FirstOrDefault();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="abstractAsset"></param>
		/// <returns></returns>
		public void PatchAbstractAsset ( string name, AbstractAsset abstractAsset )
		{
			var targetAsset = content[ name ].Object;

			if (targetAsset.GetType()!=abstractAsset.GetType()) {
				throw new ContentException(string.Format("Could not patch abstract asset object '{0}' because diferrent types. Target type: {1}. Source type: {2}", name, targetAsset.GetType(), abstractAsset.GetType() ));
			}

			Misc.CopyPropertyValues( abstractAsset, targetAsset );
		}



		/// <summary>
		/// Determines whether the specified asset exists.
		/// </summary>
		/// <param name="assetPath"></param>
		/// <returns></returns>
		public bool Exists ( string assetPath )
		{
			return File.Exists( GetFileName( assetPath ) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		string GetFileName ( string assetPath )
		{
			return Path.Combine(contentDirectory, ContentUtils.GetHashedFileName( assetPath, ".asset" ) );
		}



		/// <summary>
		/// Opens a stream for reading the specified asset.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public Stream OpenStream ( string path )
		{
			var hashedName =  ContentUtils.GetHashedFileName( path, ".asset" );

			try {
				var fileStream	=	File.OpenRead( GetFileName( path ) );
				var zipStream	=	new DeflateStream( fileStream, CompressionMode.Decompress, false );

				return zipStream;

			} catch ( IOException ioex ) {
				throw new IOException( string.Format("Could not open file: '{0}'\r\nHashed file name: '({1})'", path, hashedName ), ioex );
			}
		}



		/// <summary>
		/// Gets loader for given type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		ContentLoader GetLoader( Type type )
		{
			// try get loader that absolutly meets desired type
			foreach ( var loader in loaders ) {
				if (loader.TargetType==type) {
					return loader;
				}
			}

			foreach ( var loader in loaders ) {
				if (type.IsSubclassOf( loader.TargetType ) ) {
					return loader;
				}
			}

			throw new ContentException( string.Format("Loader for type {0} not found", type ) );
		}



		/// <summary>
		/// Loads an asset that has been processed by the Content Pipeline.
		/// ContentManager.Unload will dispose all objects loaded by this method.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="path"></param>
		/// <returns></returns>
		public T Load<T> ( string path )
		{
			Item item;

			//
			//	search for already loaded object :
			//
			lock (lockObject) {
				//	try to find object in dictionary :
				if ( content.TryGetValue( path, out item ) ) {
				
					if (item.Object is T) {

						var time = File.GetLastWriteTime( GetFileName( path ) );

						if ( time > item.LoadTime ) {
							content.Remove(	path );
						} else {
							return (T)item.Object;
						}
				
					} else {
						throw new ContentException( string.Format("'{0}' is not '{1}'", path, typeof(T) ) );
					}
				}
			}


			ContentLoader loader	=	GetLoader( typeof(T) );


			//
			//	load content and add to dictionary :
			//
			Log.Message("Loading : {0}", path );
			using (var stream = OpenStream(path) ) {
				item = new Item() {
					Object		= loader.Load( Game, stream, typeof(T), path ),
					LoadTime	= File.GetLastWriteTime( GetFileName( path ) ),
				};
			}


			//
			//	check for content again and add it.
			//
			lock (lockObject) {
				if (!content.ContainsKey( path )) {
					content.Add( path, item );
					toDispose.Add( item.Object );
				}
			}

			return (T)item.Object;
		}



		/// <summary>
		/// Safe version of ContentManager.Load. If any exception occurs default object will be returned.
		/// ContentManager.Unload will not dispose default object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="path"></param>
		/// <param name="fallbackObject"></param>
		/// <returns></returns>
		public T Load<T>( string path, T defaultObject )
		{
			try {
				return Load<T>(path);
			} catch ( Exception e ) {
				Log.Warning("Could not load {0} '{1}' : {2}", typeof(T).Name, path, e.Message);
				return defaultObject;
			}
		}




		/// <summary>
		/// Disposes all data that was loaded by this ContentManager.
		/// </summary>
		public void Unload()
		{
			lock (lockObject) {
				Log.Message("Unloading content");

				foreach ( var item in toDispose ) {
					if ( item is IDisposable ) {
						((IDisposable)item).Dispose();
					}
				}

				toDispose.Clear();
				content.Clear();
			}
		}
	}
}