﻿#define USE_DYNAMIC_CB

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DXGI = SharpDX.DXGI;
using SharpDX;
using SharpDX.Direct3D11;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;


namespace Fusion.Graphics {

	/// <summary>
	/// Wrapper for constant data and buffer
	/// </summary>
	/// <typeparam name="ConstDataT"></typeparam>
	public class ConstantBuffer : DisposableBase {
			
		readonly	GraphicsDevice	device;
		internal	D3D11.Buffer	buffer;

		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rs"></param>
		public ConstantBuffer( GraphicsDevice device, int sizeInBytes ) 
		{
			this.device	=	device;
			Create( sizeInBytes );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="device"></param>
		/// <param name="underlayingType"></param>
		public ConstantBuffer ( GraphicsDevice device, Type dataType )
		{
			this.device	=	device;
			Create( Marshal.SizeOf( dataType ) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="device"></param>
		/// <param name="underlayingType"></param>
		public ConstantBuffer ( GraphicsDevice device, Type dataType, int count )
		{
			if (count<1) {
				throw new ArgumentOutOfRangeException("count must be greater than zero");
			}
			this.device	=	device;
			Create( Marshal.SizeOf( dataType ) * count );
		}



		void CheckStructLayout ()
		{
			
		}



		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="data"></param>
		/// <returns></returns>
		public static ConstantBuffer Create<T>( GraphicsDevice device, T data ) where T: struct
		{
			var buffer = new ConstantBuffer( device, Marshal.SizeOf( data ) );
			buffer.SetData( data );
			return buffer;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="sizeInBytes"></param>
		void Create ( int sizeInBytes )
		{
			int size 	=	sizeInBytes;
			size		=	size % 16 == 0 ? size : (size/16 * 16) + 16;

			#if USE_DYNAMIC_CB
				buffer = new D3D11.Buffer( device.Device, size, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0 );
			#else
				buffer = D3D11.Buffer( device.Device, Marshal.SizeOf(type), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0 );
			#endif
		}



		/// <summary>
		/// Disposes buffer
		/// </summary>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref buffer );
			}
			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="?"></param>
		public void SetData<T> ( T value ) where T: struct
		{
			#if USE_DYNAMIC_CB
				var db = device.DeviceContext.MapSubresource( buffer, 0, MapMode.WriteDiscard, D3D11.MapFlags.None );
				Marshal.StructureToPtr( value, db.DataPointer, false );
				device.DeviceContext.UnmapSubresource( buffer, 0 );
			#else
				device.DeviceContext.UpdateSubresource( ref data, buffer );
			#endif
		}

		
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="?"></param>
		public void SetData<T> ( T[] data, int offset, int count ) where T: struct
		{
			#if USE_DYNAMIC_CB
				var db = device.DeviceContext.MapSubresource( buffer, 0, MapMode.WriteDiscard, D3D11.MapFlags.None );
				SharpDX.Utilities.Write( db.DataPointer, data, offset, count );
				device.DeviceContext.UnmapSubresource( buffer, 0 );
			#else
				device.DeviceContext.UpdateSubresource( ref data, buffer );
			#endif
		}



		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="?"></param>
		public void SetData<T> ( T[] data ) where T: struct
		{
			SetData<T>( data, 0, data.Length );
		}

	}
}