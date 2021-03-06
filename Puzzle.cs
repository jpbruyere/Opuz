﻿//
//  Puzzle.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
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
using Crow;
using GGL;
using OpenTK;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL;

namespace Opuz2015
{
	public class Puzzle: IValueChange, IDisposable
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public void NotifyValueChanged(string name, object value)
		{
			ValueChanged.Raise (this, new ValueChangeEventArgs (name, value));
		}
		#endregion

		int _nbPieceX = 5;
		int _nbPieceY = 3;
		int _nbSides = 4;

		Piece selectedPiece = null;


		public volatile bool Ready = false;
		public Piece[,] Pieces {get;set;}
		public List<Piece> ZOrderedPieces;
		public List<Piece> Selection = new List<Piece>();

		public object Mutex = new object();

		public GGL.Texture Image;
		public Cutter cutter = null;

		public int nbSides {
			get { return _nbSides; }
			set { _nbSides = value; }
		}
		public int nbPieceX {
			get { return _nbPieceX; }
			set {
				_nbPieceX = value;
				NotifyValueChanged("nbPieceX", _nbPieceX);
				NotifyValueChanged("nbPieces", nbPieces);
			}
		}
		public int nbPieceY {
			get { return _nbPieceY; }
			set {
				_nbPieceY = value;
				NotifyValueChanged("nbPieceY", _nbPieceY);
				NotifyValueChanged("nbPieces", nbPieces);
			}
		}
		public int nbPieces {
			get { return nbPieceX * nbPieceY; }
		}
		public float largP {
			get {
				return (float)Image.Width / nbPieceX;
			}
		}
		public float hautP {
			get {
				return (float)Image.Height / nbPieceY;
			}
		}
		public float PieceThickness { get { return (hautP + largP) * 0.02f; }}
		public double TolerancePlacementPieces {
			get { return (largP + hautP) / 30; }
		}
		internal uint BorderOffset = 0;


		public Piece SelectedPiece{
			get { return selectedPiece; }
			set {
				if (selectedPiece == value)
					return;

				Selection.Clear ();
				selectedPiece = value;
				MainWin.RebuildCache = true;

				if (selectedPiece == null)					
					return;

				selectedPiece.ResetVisitedStatus ();
				selectedPiece.UpdateSelection ();
			}
		}

		#region VAO
		int vaoHandle,
		positionVboHandle,
		normalsVboHandle,
		eboHandle,
		texVboHandle;

		public Vector3[] positions;
		public Vector3[] normals;
		public Vector2[] texCoords;
		public uint[] indices;

		void CreateVBOs()
		{
			positionVboHandle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
				new IntPtr(positions.Length * Vector3.SizeInBytes),
				positions, BufferUsageHint.StaticDraw);

			if (normals != null) {
				normalsVboHandle = GL.GenBuffer ();
				GL.BindBuffer (BufferTarget.ArrayBuffer, normalsVboHandle);
				GL.BufferData<Vector3> (BufferTarget.ArrayBuffer,
					new IntPtr (normals.Length * Vector3.SizeInBytes),
					normals, BufferUsageHint.StaticDraw);
			}

			if (texCoords != null) {
				texVboHandle = GL.GenBuffer ();
				GL.BindBuffer (BufferTarget.ArrayBuffer, texVboHandle);
				GL.BufferData<Vector2> (BufferTarget.ArrayBuffer,
					new IntPtr (texCoords.Length * Vector2.SizeInBytes),
					texCoords, BufferUsageHint.StaticDraw);
			}

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}
		void CreateVAOs()
		{
			vaoHandle = GL.GenVertexArray();
			GL.BindVertexArray(vaoHandle);

			GL.EnableVertexAttribArray(0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

			if (texCoords != null) {
				GL.EnableVertexAttribArray (1);
				GL.BindBuffer (BufferTarget.ArrayBuffer, texVboHandle);
				GL.VertexAttribPointer (1, 2, VertexAttribPointerType.Float, true, Vector2.SizeInBytes, 0);
			}
			if (normals != null) {
				GL.EnableVertexAttribArray (2);
				GL.BindBuffer (BufferTarget.ArrayBuffer, normalsVboHandle);
				GL.VertexAttribPointer (2, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);
			}
			GL.BindVertexArray(0);
		}
		#endregion

		#region CTOR
		public Puzzle (int _nbx, int _nby, string _imgPath, int _Sides = 4)
		{
			Image = new GGL.Texture (_imgPath);

			nbPieceX = _nbx;
			nbPieceY = _nby;
			nbSides = _Sides;

			List<Vector3> tmp = new List<Vector3>();

			cutter = new Cutter (CutType.Curvy);

			Vector3[] HBorder = MakeHorizontalBorder (nbPieceX, largP);
			Vector3[] VBorder = MakeHorizontalBorder (nbPieceY, hautP);

			tmp.AddRange (HBorder);

			for (int i = 1; i < nbPieceY; i++) {
				Matrix4 mat = Matrix4.CreateTranslation(0, i * hautP, 0);
				tmp.AddRange( transform (MakeHorizontalCut (nbPieceX, largP), mat));
			}

			tmp.AddRange (transform (HBorder, Matrix4.CreateTranslation (0, Image.Height, 0)));

			Matrix4 rot = Matrix4.CreateRotationZ (MathHelper.PiOver2);
				
			tmp.AddRange ( transform (VBorder, rot));

			for (int i = 1; i < nbPieceX; i++) {
				Matrix4 mat = rot * Matrix4.CreateTranslation(i * largP, 0, 0);
				tmp.AddRange( transform (MakeHorizontalCut (nbPieceY, hautP), mat));
			}

			tmp.AddRange ( transform (VBorder, 
				rot *
				Matrix4.CreateTranslation (Image.Width, 0, 0)));

			tmp.Add(new Vector3(Image.Width,Image.Height,0));

			positions = new Vector3[tmp.Count*2];

			Vector3[] bord = tmp.ToArray ();
			transform (ref bord, Matrix4.CreateTranslation (0, 0, -PieceThickness));
			BorderOffset = (uint)tmp.Count;

			Array.Copy (tmp.ToArray (), 0, positions, 0, tmp.Count);
			Array.Copy (bord, 0, positions, BorderOffset, tmp.Count);

			CreateVBOs ();
			CreateVAOs ();

			createPieces ();

			List<uint> indicesList = new List<uint>();
			foreach (Piece p in ZOrderedPieces) {
				p.IndFillPtr = indicesList.Count * sizeof(uint);
				p.indFillLength = p.IndFill.Length + 1;

				indicesList.AddRange (p.IndFill);
				indicesList.Add (uint.MaxValue);

				p.IndBorderPtr = new int[nbSides];
				p.indBorderLength = new int[nbSides];
				for (int i = 0; i < nbSides; i++) {
					p.IndBorderPtr[i] = indicesList.Count * sizeof(uint);
					p.indBorderLength [i] = p.IndBorder [i].Length;
					indicesList.AddRange (p.IndBorder [i]);
					indicesList.Add (uint.MaxValue);
				}
			}
			indices = indicesList.ToArray ();
			eboHandle = GL.GenBuffer ();
			GL.BindVertexArray(vaoHandle);
			GL.BindBuffer (BufferTarget.ElementArrayBuffer, eboHandle);
			GL.BufferData (BufferTarget.ElementArrayBuffer,
				new IntPtr (sizeof(uint) * indices.Length),
				indices, BufferUsageHint.StaticDraw);			
			GL.BindVertexArray(0);
			Ready = true;
		}
		#endregion




		static Random rnd = new Random();
		public void Shuffle()
		{
			double hw = (double)Image.Width / 2.0;
			double hh = (double)Image.Height / 2.0;

			foreach (Piece p in Pieces) {
				p.IsLinked = Enumerable.Repeat(false,4).ToArray();
				p.Dx = (float)(rnd.NextDouble () * (double)Image.Width - hw);
				p.Dy = (float)(rnd.NextDouble () * (double)Image.Height - hh);
				p.Angle = (float)-rnd.Next (0, 3) * MathHelper.PiOver2;
			}
			lock(Mutex)
				ZOrderedPieces.Shuffle ();
		}

		public void resolve()
		{
			foreach (Piece p in Pieces ) {
				p.Angle = 0f;
				Point<float> c = p.Bounds.Center;
				p.Dx = c.X;
				p.Dy = c.Y;
			}
		}
		public void Render(Piece[] pces){
			GL.CullFace (CullFaceMode.Front);

			GL.BindVertexArray(vaoHandle);

			GL.ActiveTexture (TextureUnit.Texture1);
			GL.BindTexture(TextureTarget.Texture2D, profileTexture);
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, Image);

			foreach (Piece p in pces) {
				MainWin.mainShader.Color = new Vector4 (0.4f, 0.4f, 0.4f, 1);
				MainWin.mainShader.Model = p.Transformations;
				MainWin.mainShader.ColorMultiplier = p.ColorMultiplier;

				//border, only when not linked
				for (int i = 0; i < nbSides; i++) {
					if (p.IsLinked [i])
						continue;
					GL.DrawElements (BeginMode.TriangleStrip, p.indBorderLength[i],
						DrawElementsType.UnsignedInt, p.IndBorderPtr[i]);
				}

				MainWin.mainShader.Color = new Vector4 (1, 1, 1, 1);

				GL.DrawElements (BeginMode.Triangles, p.indFillLength,
					DrawElementsType.UnsignedInt, p.IndFillPtr);
			}

			GL.BindVertexArray (0);
			GL.BindTexture(TextureTarget.Texture2D, 0);

			GL.CullFace (CullFaceMode.Back);
		}
		public void Render(){
			Piece[] tmp = null;
			lock (Mutex) {
				tmp = ZOrderedPieces.Except(Selection).ToArray();
			}
			Render (tmp);
		}

		#region Cutting
		void createPieces()
		{						
			Pieces = new Piece[nbPieceX,nbPieceY];

			int ptr0 = 0;
			int ptr2 = nbPieceX;

			int ptrV0 = (nbPieceY - 1) * nbPieceX * cutter.NbPoints + 2 * nbPieceX;

			//used to build profile texture
			List<uint> indProfile = new List<uint> ();

			for (int y = 0; y < nbPieceY; y++) {
				int ptr1 = ptrV0 + nbPieceY;

				int ptr3 = ptrV0;
				for (int x = 0; x < nbPieceX; x++) {
					List<uint> ind = new List<uint>();

					#region Compute border indices
					//indice du bord sup
					if (y == 0) {
						ind.Add ((uint)ptr0);
						ptr0++;
					} else {						
						ind.AddRange (Enumerable.Range(ptr0, cutter.NbPoints).Select(i => (uint)i).ToArray());
						ptr0 += cutter.NbPoints;
					}
					//indice du bord droit
					if (x == nbPieceX - 1) {
						ind.Add ((uint)(ptr1 + y));
						ptr1++;
					} else {						
						ind.AddRange (Enumerable.Range(ptr1 + y * cutter.NbPoints, cutter.NbPoints).Select(i => (uint)i).ToArray());
						ptr1 += cutter.NbPoints * nbPieceY ;
					}
					//
					//indice du bord inférieur
					if (y == nbPieceY - 1) {
						if (x == nbPieceX - 1)
							ind.Add (BorderOffset - 1);
						else
							ind.Add ((uint)(ptr2+1));
						ind.Add ((uint)ptr2);
						ptr2++;
					} else {
						if (x == nbPieceX - 1)
							ind.Add ((uint)(ptr1 + y));
						else
							ind.Add ((uint)(ptr2 + cutter.NbPoints));

						ind.AddRange (Enumerable.Range(ptr2+1, cutter.NbPoints - 1).Reverse().Select(i => (uint)i).ToArray());
						ind.Add ((uint)ptr2);//1st point of left border
						ptr2 += cutter.NbPoints;
					}

					//indice du bord gauche
					if (x == 0)
						ptr3 += nbPieceY ;
					else if (x > 0 ) {
						//						ind.Add (ptr3 + Cut.NbPoints);
						ind.AddRange (Enumerable.Range(ptr3+y*cutter.NbPoints+1, cutter.NbPoints-1).Reverse().Select(i => (uint)i).ToArray());
						ptr3 += cutter.NbPoints * nbPieceY ;
					}
					#endregion

					indProfile.AddRange (ind);
					indProfile.Add (uint.MaxValue);

					Pieces [x, y] = new Piece (this, (uint)x, (uint)y, ind);

				}
			}

			createProfileTexture (indProfile.ToArray());

			//init neighbourhoud, borders indices, and zordered list
			ZOrderedPieces = new List<Piece>();
			for (int y = 0; y < nbPieceY; y++) {
				for (int x = 0; x < nbPieceX; x++) {
					Piece p = Pieces [x, y];
					lock(Mutex)
						ZOrderedPieces.Add (p);
					if (y < nbPieceY - 1)
						p.Neighbours [2] = Pieces [x, y + 1];
					if (x < nbPieceX - 1)
						p.Neighbours [1] = Pieces [x + 1, y];
					if (y > 0)
						p.Neighbours [0] = Pieces [x, y - 1];
					if (x > 0)
						p.Neighbours [3] = Pieces [x - 1, y];
				}
			}
		}
		//maybe those functions should be in Cutter class
		Vector3[] MakeHorizontalBorder (int nbCut, float cutSize){
			Vector3[] tmp = new Vector3[nbCut];
			for (int i = 0; i < nbCut; i++) 
				tmp [i] = new Vector3 (i * cutSize, 0, 0);
			return tmp;
		}
		Vector3[] MakeHorizontalCut (int nbCut, float cutSize){
			List<Vector3> tmp = new List<Vector3>();

			for (int i = 0; i < nbCut; i++) {
				tmp.AddRange (cutter.Cut(i * cutSize, cutSize));
			}
			//tmp.Add (new Vector3 (nbCut * cutSize, 0, 0));
			return tmp.ToArray ();
		}
		Vector3[] transform(Vector3[] points, Matrix4 mat){
			Vector3[] tmp = new Vector3[points.Length];
			for (int i = 0; i < points.Length; i++) 
				tmp [i] = points [i].Transform (mat);
			return tmp;
		}
		void transform(ref Vector3[] points, Matrix4 mat){
			for (int i = 0; i < points.Length; i++) 
				points [i] = points [i].Transform (mat);
		}
		#endregion

		#region PROFILE FBO
		int profileTexture;
		void createProfileTexture(uint[] indProfile)
		{
			int fbo;
			profileTexture = new GGL.Texture (Image.Width, Image.Height);

			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, profileTexture);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 
				Image.Width, Image.Height, 0,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			GL.BindTexture(TextureTarget.Texture2D, 0);

			GL.GenFramebuffers(1, out fbo);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
				TextureTarget.Texture2D, profileTexture, 0);

			if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
			{
				throw new Exception(GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer).ToString());
			}

			GL.Disable (EnableCap.CullFace);

			GameLib.EffectShader RedFillShader = new GameLib.EffectShader ("Opuz2015.shaders.red");
			RedFillShader.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter 
				(0, Image.Width, 0, Image.Height, 0, 1);
			RedFillShader.ModelViewMatrix = Matrix4.Identity;
			RedFillShader.ModelMatrix = Matrix4.Identity;
			RedFillShader.Enable ();

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
			float[] cc = new float[4];
			int[] viewport = new int[4];
			GL.GetInteger (GetPName.Viewport, viewport);
			GL.Viewport (0, 0, Image.Width, Image.Height);
			GL.GetFloat (GetPName.ColorClearValue, cc);
			GL.ClearColor (0, 0, 0, 0);
			GL.Clear (ClearBufferMask.ColorBufferBit);

			GL.BindVertexArray(vaoHandle);
			GL.LineWidth(3);
			GL.DrawElements (BeginMode.LineLoop, indProfile.Length,
				DrawElementsType.UnsignedInt, indProfile);
			GL.BindVertexArray (0);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GL.Enable (EnableCap.CullFace);
			GL.UseProgram (0);
			GL.ClearColor (cc[0],cc[1],cc[2],cc[3]);
			GL.Viewport (viewport [0], viewport [1], viewport [2], viewport [3]);			

			GL.DeleteFramebuffer(fbo);
			RedFillShader.Dispose ();
		}
		#endregion


		#region IDisposable implementation
		public void Dispose ()
		{
			GL.DeleteBuffer (positionVboHandle);
			GL.DeleteBuffer (normalsVboHandle);
			GL.DeleteBuffer (texVboHandle);
			GL.DeleteVertexArray (vaoHandle);
			GL.DeleteTexture (Image);
		}
		#endregion
	}
}

