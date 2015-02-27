﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Mathematics;
using Fusion.Audio;
using Fusion.Content;
using Fusion.Graphics;
using Fusion.Input;
using Fusion.Development;
using System.Runtime.InteropServices;

namespace DeferredDemo {
	public class HdrFilter : GameService {

		[Config]
		public HdrFilterConfig	Config { get; set; }


		Ubershader	shader;
		ConstantBuffer	paramsCB;
		RenderTarget2D	averageLum;
		RenderTarget2D	measuredOld;
		RenderTarget2D	measuredNew;

		RenderTarget2D	bloom0;
		RenderTarget2D	bloom1;


		Texture2D		bloomMask;


		struct Params {
			public	float	AdaptationRate;
			public	float 	LuminanceLowBound;
			public	float	LuminanceHighBound;
			public	float	KeyValue;
			public	float	BloomAmount;
		}


		enum Flags {	
			TONEMAPPING		=	0x001,
			MEASURE_ADAPT	=	0x002,
			LINEAR			=	0x004, 
			REINHARD		=	0x008,
			FILMIC			=	0x010,
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public HdrFilter ( Game game ) : base(game)
		{
			Config	=	new HdrFilterConfig();
		}



		/// <summary>
		/// /
		/// </summary>
		public override void Initialize ()
		{
			base.Initialize();

			averageLum	=	new RenderTarget2D( Game.GraphicsDevice, ColorFormat.Rgba16F, 256,256, true, false );
			measuredOld	=	new RenderTarget2D( Game.GraphicsDevice, ColorFormat.Rgba32F,   1,  1 );
			measuredNew	=	new RenderTarget2D( Game.GraphicsDevice, ColorFormat.Rgba32F,   1,  1 );
			paramsCB	=	new ConstantBuffer( Game.GraphicsDevice, typeof(Params) );

			CreateTargets();
			LoadContent();

			Game.GraphicsDevice.DisplayBoundsChanged += (s,e) => CreateTargets();
			Game.Reloading += (s,e) => LoadContent();
		}



		/// <summary>
		/// 
		/// </summary>
		void CreateTargets ()
		{
			var disp	=	Game.GraphicsDevice.DisplayBounds;

			SafeDispose( ref bloom0 );
			SafeDispose( ref bloom1 );

			bloom0	=	new RenderTarget2D( Game.GraphicsDevice, ColorFormat.Rgba16F, disp.Width / 2, disp.Height / 2, true, false );
			bloom1	=	new RenderTarget2D( Game.GraphicsDevice, ColorFormat.Rgba16F, disp.Width / 2, disp.Height / 2, true, false );
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shader	=	Game.Content.Load<Ubershader>("hdr");
			shader.Map( typeof(Flags) );

			bloomMask	=	Game.Content.Load<Texture2D>("bloomMask");
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref bloom0 );
				SafeDispose( ref bloom1 );
				SafeDispose( ref averageLum	 );
				SafeDispose( ref measuredOld );
				SafeDispose( ref measuredNew );
				SafeDispose( ref paramsCB	 );
			}

			base.Dispose( disposing );
		}



		/// <summary>
		/// Performs luminance measurement, tonemapping, applies bloom.
		/// </summary>
		/// <param name="target">LDR target.</param>
		/// <param name="hdrImage">HDR source image.</param>
		public void Render ( GameTime gameTime, RenderTargetSurface target, ShaderResource hdrImage )
		{
			var device	=	Game.GraphicsDevice;
			var filter	=	Game.GetService<Filter>();
			var ds		=	Game.GetService<DebugStrings>();


			//
			//	Rough downsampling of source HDR-image :
			//
			filter.StretchRect( averageLum.Surface, hdrImage, BlendState.Opaque );
			averageLum.BuildMipmaps();

			//
			//	Make bloom :
			//
			filter.StretchRect( bloom0.Surface, hdrImage );
			bloom0.BuildMipmaps();

			filter.GaussBlur( bloom0, bloom1, Config.GaussBlurSigma, 0 );
			filter.GaussBlur( bloom0, bloom1, Config.GaussBlurSigma, 1 );
			filter.GaussBlur( bloom0, bloom1, Config.GaussBlurSigma, 2 );
			filter.GaussBlur( bloom0, bloom1, Config.GaussBlurSigma, 3 );


			//
			//	Setup parameters :
			//
			var paramsData	=	new Params();
			paramsData.AdaptationRate		=	1 - (float)Math.Pow( 0.5f, gameTime.ElapsedSec / Config.AdaptationHalfLife );
			paramsData.LuminanceLowBound	=	Config.LuminanceLowBound;
			paramsData.LuminanceHighBound	=	Config.LuminanceHighBound;
			paramsData.KeyValue				=	Config.KeyValue;
			paramsData.BloomAmount			=	Config.BloomAmount;

			paramsCB.SetData( paramsData );
			device.PixelShaderConstants[0]	=	paramsCB;


			//
			//	Measure and adapt :
			//
			device.SetTargets( null, measuredNew );

			device.PixelShaderResources[0]	=	averageLum;
			device.PixelShaderResources[1]	=	measuredOld;
			device.DepthStencilState	=	DepthStencilState.None;

			shader.SetPixelShader ( (int)Flags.MEASURE_ADAPT );
			shader.SetVertexShader( (int)Flags.MEASURE_ADAPT );
				
			device.Draw( Primitive.TriangleList, 3, 0 );


			//
			//	Tonemap and compose :
			//
			device.SetTargets( null, target );

			device.PixelShaderResources[0]	=	hdrImage;// averageLum;
			device.PixelShaderResources[1]	=	measuredNew;// averageLum;
			device.PixelShaderResources[2]	=	bloom0;// averageLum;
			device.PixelShaderResources[3]	=	bloomMask;// averageLum;
			device.DepthStencilState	=	DepthStencilState.None;
			device.PixelShaderSamplers[0]	=	SamplerState.LinearClamp;

			Flags op = Flags.LINEAR;
			if (Config.TonemappingOperator==TonemappingOperator.Filmic)   { op = Flags.FILMIC;   }
			if (Config.TonemappingOperator==TonemappingOperator.Linear)   { op = Flags.LINEAR;	 }
			if (Config.TonemappingOperator==TonemappingOperator.Reinhard) { op = Flags.REINHARD; }

			shader.SetPixelShader ( (int)(Flags.TONEMAPPING|op) );
			shader.SetVertexShader( (int)(Flags.TONEMAPPING|op) );
				
			device.Draw( Primitive.TriangleList, 3, 0 );
			
			device.ResetStates();


			//	swap luminanice buffers :
			Misc.Swap( ref measuredNew, ref measuredOld );
		}



	}
}