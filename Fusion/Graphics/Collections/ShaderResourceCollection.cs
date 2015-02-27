﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;

namespace Fusion.Graphics {

	/// <summary>
	/// The texture collection.
	/// </summary>
	public sealed class ShaderResourceCollection {

		readonly ShaderResource[]	resources;	
		readonly CommonShaderStage	stage;
		readonly GraphicsDevice		device;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="device"></param>
		internal ShaderResourceCollection ( GraphicsDevice device, CommonShaderStage stage )
		{
			resources	=	new ShaderResource[ Count ];
			this.stage	=	stage;
			this.device	=	device;
		}



		/// <summary>
		/// Total count of sampler states that can be simultaniously bound to pipeline.
		/// </summary>
		public int Count { 
			get { 
				return CommonShaderStage.InputResourceRegisterCount;
			}
		}


		

		
		/// <summary>
		/// Sets and gets shader resources bound to given shader stage.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public ShaderResource this[int index] {
			set {
				resources[ index ] = value;
				stage.SetShaderResource( index, (value==null) ? null : value.SRV );
			}
			get {
				return resources[ index ];
			}
		}
	}
}