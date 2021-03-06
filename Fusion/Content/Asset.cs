﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Fusion.Mathematics;


namespace Fusion.Content {

	/// <summary>
	/// Asset represent of designers' or artists' product (like scene, texture or game object) and its description in suitable for game form.
	/// </summary>
	/// 
	/// <remarks>
	/// </remarks>
	public abstract class Asset {

		/// <summary>
		/// Path to this path.
		/// Extension will be ignored.
		/// </summary>
		[Category("General")]
		[ReadOnly(true)]
		public string AssetPath { get; set; }


		/// <summary>
		/// Path to this path
		/// </summary>
		[Category("General")]
		[ReadOnly(true)]
		[XmlIgnore]
		public string Hash { 
			get { 
				return ContentUtils.GetHashedFileName( AssetPath, "" ); 
				} 
			}


		/// <summary>
		/// Asset's build order. Default value 0.
		/// </summary>
		[Category("General")]
		[ReadOnly(true)]
		public int BuildOrder { get; set; }


		/// <summary>
		/// Asset's type assembly update will be ignored.
		/// </summary>
		[Category("General")]
		[Description("Asset's type assembly update will be ignored.")]
		public bool IgnoreToolChanges { get; set; }



		/// <summary>
		/// Creates asset's instance
		/// </summary>
		public Asset ()
		{
			BuildOrder	=	0;
		}



		/// <summary>
		/// Performs asset build.
		/// This method can add new assets to asset collection while building.
		/// </summary>
		/// <param name="buildContext"></param>
		public abstract void Build ( BuildContext buildContext );
		


		/// <summary>
		/// Gets the list of file which asset depends on.
		/// Both direct file names as well as wildcard patterns are acceptable.
		/// </summary>
		[Category("General")]
		[XmlIgnore]
		public abstract string[] Dependencies {
			get;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		internal static Type[] GatherAssetTypes ()
		{
			return Misc.GetAllSubclassedOf( typeof(Asset) )
				.Where( t => t.HasAttribute<AssetAttribute>() )
				.ToArray();
		}
	}
}
