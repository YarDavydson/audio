﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;


namespace Fusion.Content {

	public class AbstractAsset : Asset {


		/// <summary>
		/// 
		/// </summary>
		public override string[] Dependencies
		{
			get { return new string[0]; }
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="buildContext"></param>
		public override void Build ( BuildContext buildContext )
		{
			Misc.SaveObjectToXml( this, GetType(), buildContext.TargetStream( this ) );
		}
	}
}
