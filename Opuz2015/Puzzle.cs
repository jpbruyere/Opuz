//
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
using go;
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

		public volatile bool Ready = false;
		public Piece[,] Pieces {get;set;}
		public List<Piece> ZOrderedPieces;

		public object Mutex = new object();

		public Texture Image;

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
		}					//read-only largeur piece
		public float hautP {
			get {
				return (float)Image.Height / nbPieceY;
			}
		}
		public double TolerancePlacementPieces {
			get { return (largP + hautP) / 30; }
		}


		private int _nbPieceX = 5;
		private int _nbPieceY = 3;

		#region VAO
		int vaoHandle,
		positionVboHandle,
		normalsVboHandle,
		texVboHandle;

		public Vector3[] positions;
		public Vector3[] normals;
		public Vector2[] texCoords;
		public int[] indices;

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
		public Puzzle (int _nbx, int _nby, string _imgPath)
		{
			Image = new Texture (_imgPath, true);
			nbPieceX = _nbx;
			nbPieceY = _nby;

			List<Vector3> tmp = new List<Vector3>();

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
			transform (ref bord, Matrix4.CreateTranslation (0, 0, -4f));
			BorderOffset = tmp.Count;

			Array.Copy (tmp.ToArray (), 0, positions, 0, tmp.Count);
			Array.Copy (bord, 0, positions, BorderOffset, tmp.Count);

			//indices = Enumerable.Range (0, tmp.Count - 1).ToArray ();

			CreateVBOs ();
			CreateVAOs ();

			createPieces ();

			Ready = true;
		}
		#endregion
		internal int BorderOffset = 0;

		public Piece SelectedPiece = null;

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

		void resetLinkedPce()
		{}

		public void Render(){
			GL.BindTexture(TextureTarget.Texture2D, Image);
			GL.BindVertexArray(vaoHandle);
			//Piece p = Pieces[4,2];
			Piece[] tmp = null;
			lock (Mutex) {
				//Piece[] tmp = new Piece[ZOrderedPieces.Count];
				tmp = ZOrderedPieces.ToArray();
				//Array.Copy (ZOrderedPieces, tmp, tmp.Length);
			}

			foreach (Piece p in tmp)
				p.Render ();			
				//break;
			//}
			GL.BindVertexArray (0);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		#region Cutting
		void createPieces()
		{			
			Pieces = new Piece[nbPieceX,nbPieceY];

			int ptr0 = 0;
			int ptr2 = nbPieceX;


			int ptrV0 = (nbPieceY - 1) * nbPieceX * Cut.NbPoints + 2 * nbPieceX;


			for (int y = 0; y < nbPieceY; y++) {
				int ptr1 = ptrV0 + nbPieceY;

				//int ptr1 = (nbPieceY-1) * (nbPieceX * Cut.NbPoints + 1) + 2 * (nbPieceX + 1) + nbPieceY + 1;
				int ptr3 = ptrV0;
				for (int x = 0; x < nbPieceX; x++) {
					List<int> ind = new List<int>();

					//indice du bord sup
					if (y == 0) {
						ind.Add (ptr0);
						ptr0++;
					} else {						
						ind.AddRange (Enumerable.Range(ptr0, Cut.NbPoints).ToArray());
						ptr0 += Cut.NbPoints;
					}
					//indice du bord droit
					if (x == nbPieceX - 1) {
						ind.Add (ptr1 + y);
						ptr1++;
					} else {						
						ind.AddRange (Enumerable.Range(ptr1 + y * Cut.NbPoints, Cut.NbPoints).ToArray());
						ptr1 += Cut.NbPoints * nbPieceY ;
					}
					//
					//indice du bord inférieur
					if (y == nbPieceY - 1) {
						if (x == nbPieceX - 1)
							ind.Add (BorderOffset - 1);
						else
							ind.Add (ptr2+1);
						ind.Add (ptr2);
						ptr2++;
					} else {
						if (x == nbPieceX - 1)
							ind.Add (ptr1 + y);
						else
							ind.Add (ptr2 + Cut.NbPoints);

						ind.AddRange (Enumerable.Range(ptr2+1, Cut.NbPoints - 1).Reverse().ToArray());
						ind.Add (ptr2);//1st point of left border
						ptr2 += Cut.NbPoints;
					}

					//indice du bord gauche
					if (x == 0)
						ptr3 += nbPieceY ;
					else if (x > 0 ) {
						//						ind.Add (ptr3 + Cut.NbPoints);
						ind.AddRange (Enumerable.Range(ptr3+y*Cut.NbPoints+1, Cut.NbPoints-1).Reverse().ToArray());
						ptr3 += Cut.NbPoints * nbPieceY ;
					}

					Pieces [x, y] = new Piece (this, ind);
				}
			}
			//init neighbourhoud and zordered list
			ZOrderedPieces = new List<Piece>();
			for (int y = 0; y < nbPieceY; y++) {
				for (int x = 0; x < nbPieceX; x++) {
					Piece p = Pieces [x, y];
					lock(Mutex)
						ZOrderedPieces.Add (p);
					if (y < nbPieceY - 1)
						p.Neighbours [0] = Pieces [x, y + 1];
					if (x < nbPieceX - 1)
						p.Neighbours [1] = Pieces [x + 1, y];
					if (y > 0)
						p.Neighbours [2] = Pieces [x, y - 1];
					if (x > 0)
						p.Neighbours [3] = Pieces [x - 1, y];
				}
			}
		}

		Vector3[] MakeHorizontalBorder (int nbCut, float cutSize){
			Vector3[] tmp = new Vector3[nbCut];
			for (int i = 0; i < nbCut; i++) 
				tmp [i] = new Vector3 (i * cutSize, 0, 0);
			return tmp;
		}
		Vector3[] MakeHorizontalCut (int nbCut, float cutSize){
			List<Vector3> tmp = new List<Vector3>();

			for (int i = 0; i < nbCut; i++) {
				tmp.AddRange (new Cut (i * cutSize, cutSize).Positions);
			}
			//tmp.Add (new Vector3 (nbCut * cutSize, 0, 0));
			return tmp.ToArray ();
		}
		Vector3[] transform(Vector3[] points, Matrix4 mat){
			Vector3[] tmp = new Vector3[points.Length];
			for (int i = 0; i < points.Length; i++) 
				tmp [i] = Vector3.Transform (points [i], mat);
			return tmp;
		}
		void transform(ref Vector3[] points, Matrix4 mat){
			for (int i = 0; i < points.Length; i++) 
				points [i] = Vector3.Transform (points [i], mat);			
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

