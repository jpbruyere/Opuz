//
//  RenderCache.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace Opuz2015
{
	public class RenderCache : IDisposable
	{
		static Tetra.Shader cacheShader;

		public static void EnableCacheShader(){
			cacheShader.Enable();
		}
		#region CTOR
		static RenderCache(){
			cacheShader = new Tetra.Shader(null,"#Opuz2015.shaders.cache.frag");
			cacheShader.Enable ();
			cacheShader.SetMVP (OpenTK.Matrix4.CreateOrthographicOffCenter (-0.5f, 0.5f, -0.5f, 0.5f, 1, -1));
			cacheShader.Disable ();
		}
		public RenderCache(System.Drawing.Size _cacheSize){
			cacheSize = _cacheSize;

			createCache ();
		}
		public RenderCache(System.Drawing.Size _cacheSize, DrawBuffersEnum[] _dbe)
			: this (_cacheSize)
		{
			dbe = _dbe;

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboId);
			GL.DrawBuffers(dbe.Length, dbe);
			if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
				throw new Exception(GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer).ToString());
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		}
		#endregion

		protected System.Drawing.Size cacheSize;
		protected bool wireframe = false;
		protected bool colorCacheIsUpToDate = false;
		protected bool selectionCacheIsUpToDate = false;

		protected int fboId;
		DrawBuffersEnum[] dbe = new DrawBuffersEnum[]
		{
			DrawBuffersEnum.ColorAttachment0,

		};

		public Tetra.Texture	colorTex, depthTex;

		public System.Drawing.Size CacheSize {
			get { return cacheSize; }
			set {
				if (value == cacheSize)
					return;
				cacheSize = value;
				createCache ();
			}
		}

		protected virtual void createCache(){
			this.Dispose();
			initFbo ();
		}
		void initFbo()
		{
			GL.GenFramebuffers(1, out fboId);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboId);

			configureFbo ();

			GL.DrawBuffers(dbe.Length, dbe);

			if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
			{
				throw new Exception(GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer).ToString());
			}

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		}
		protected virtual void configureFbo(){

			Tetra.Texture.DefaultTarget = TextureTarget.Texture2DMultisample;
			Tetra.Texture.GenerateMipMaps = false;
			colorTex = new Tetra.Texture()
			{
				Width = CacheSize.Width,
				Height = CacheSize.Height,
				InternalFormat = PixelInternalFormat.Rgba,
				PixelFormat = PixelFormat.Bgra,
				PixelType = PixelType.UnsignedByte,
				Samples = 4
			}; colorTex.Create ();
					

			// Create Depth Renderbuffer
//			depthTex = new Tetra.Texture()
//			{
//				Width = CacheSize.Width,
//				Height = CacheSize.Height,
//				InternalFormat = PixelInternalFormat.DepthComponent32f,
//				PixelFormat = PixelFormat.DepthComponent,
//				PixelType = PixelType.Float,
//			}; depthTex.Create ();
//
//			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
//				TextureTarget.Texture2D, depthTex, 0);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
				TextureTarget.Texture2DMultisample, colorTex, 0);
			
			Tetra.Texture.ResetToDefaultLoadingParams ();
		}
		public virtual void UpdateFbo()
		{						
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboId);

			onDraw ();

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			colorCacheIsUpToDate = true;
		}
		public void Bind(bool clear = true){
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboId);
			if (!clear)
				return;			
			GL.Clear (ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);
		}
		protected virtual void onDraw(){}

		public void PaintCache(){
			//			GL.ActiveTexture (TextureUnit.Texture1);
			//			GL.BindTexture (TextureTarget.Texture2D, depthTex);
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2DMultisample, colorTex);
			Tetra.ShadedTexture.quad.Render (BeginMode.TriangleStrip);
		}

		#region IDisposable implementation
		public virtual void Dispose ()
		{
			if (GL.IsTexture (colorTex))
				GL.DeleteTexture (colorTex);
			if (GL.IsTexture (depthTex))
				GL.DeleteTexture (depthTex);
			if (GL.IsFramebuffer (fboId))
				GL.DeleteFramebuffer (fboId);			
		}
		#endregion
	}
}

