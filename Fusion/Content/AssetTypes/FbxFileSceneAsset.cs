﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Fusion.Mathematics;


namespace Fusion.Content {

	[Asset("Content", "FBX File Scene", "*.fbx")]
	public class FbxFileSceneAsset : Asset {

		/// <summary>
		/// 
		/// </summary>
		[Category("Import Parameters")]
		[Description("Source FBX file.")]
		public string SourceFile { get; set; }

		/// <summary>
		/// Vertex merge tolerance
		/// </summary>
		[Category("Import Parameters")]
		[Description("Vertex merge tolerance.")]
		public float MergeTolerance { get; set; }

		/// <summary>
		/// Evaluates transform
		/// </summary>
		[Category("Import Parameters")]
		[Description("Import animation.")]
		public bool ImportAnimation { get; set; }

		/// <summary>
		/// Evaluates transform
		/// </summary>
		[Category("Import Parameters")]
		[Description("Import geometry.")]
		public bool ImportGeometry { get; set; }



		/// <summary>
		/// Dependencies
		/// </summary>
		public override string[] Dependencies
		{
			get { return new[]{ SourceFile }; }
		}



		/// <summary>
		/// Ctor
		/// </summary>
		public FbxFileSceneAsset()
		{
			ImportGeometry	=	true;
		}


		/// <summary>
		/// Builds asset
		/// </summary>
		/// <param name="buildContext"></param>
		public override void Build ( BuildContext buildContext )
		{
			var resolvedPath	=	buildContext.Resolve( SourceFile );
			var destPath		=	buildContext.GetTempFileName( Hash, ".scene", true );
			var cmdLine			=	string.Format("\"{0}\" /out:\"{1}\" /merge:{2} /base:\"{3}\" {4} {5}", 
				resolvedPath, destPath, 
				MergeTolerance, 
				buildContext.ContentDirectory, 
				ImportAnimation ? "/anim":"", 
				ImportGeometry ? "/geom":"" 
			);

			buildContext.RunTool( "Fusion.Native.Fbx.exe", cmdLine );

			using ( var target = buildContext.TargetStream( this ) ) {
				buildContext.CopyTo( destPath, target );
			}
		}
	}
}