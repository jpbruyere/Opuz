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

		public Piece[,] Pieces {get;set;}

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
			get { return (largP + hautP) / 20; }
		}


		private int _nbPieceX = 5;
		private int _nbPieceY = 3;

		int vaoHandle,
		positionVboHandle,
		normalsVboHandle,
		texVboHandle;

		public Vector3[] positions;
		public Vector3[] normals;
		public Vector2[] texCoords;

		public int[] indices;

		public Puzzle ()
		{
			Image = new Texture (@"/mnt/data/Images/00.jpg", true);

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

			positions = tmp.ToArray ();
			indices = Enumerable.Range (0, tmp.Count - 1).ToArray ();

			CreateVBOs ();
			CreateVAOs ();

			createPieces ();
		}

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
							ind.Add (positions.Length - 1);
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

					Pieces [x, y] = new Piece (earTriangulation(ind));
					//Pieces [x, y] = new Piece (ind.ToArray());

				}
			}
		}

		#region triangulation
		int[] earTriangulation(List<int> tril)
		{
			//triangles list
			List<int> indices = new List<int> ();

			int p0 = 0, p1, p2;

			while (tril.Count > 2){				
				p1 = cyclingIncrementer (p0, tril.Count-1);
				p2 = cyclingIncrementer (p1, tril.Count-1);

				if (Vector3.Cross (
					positions [tril [p1]] - positions [tril [p0]], 
					positions [tril [p2]] - positions [tril [p0]]).Z > 0) {

					bool theresPointsInsideTri = false;

					for (int i = 0; i < tril.Count; i++) {
						if (i == p0 || i == p1 || i == p2)
							continue;
						if (!PointIsInTriangle (
							positions [tril [i]],
							positions [tril [p0]],
							positions [tril [p1]],
							positions [tril [p2]]))
							continue;
						theresPointsInsideTri = true;
						break;
					}

					if (!theresPointsInsideTri) {
						//build triangle
						indices.Add (tril [p0]);
						indices.Add (tril [p2]);
						indices.Add (tril [p1]);

						tril.RemoveAt (p1);

						if (p0 == tril.Count)
							p0 = 0;

						continue;
					}
				} 

				p0 = cyclingIncrementer (p0, tril.Count-1);						
			}
			return indices.ToArray ();
		}			
		int cyclingIncrementer(int val, int max)
		{
			return val < max ? val + 1 : 0;
		}

		float sign (Vector3 p1, Vector3 p2, Vector3 p3)
		{
			return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
		}

		bool PointIsInTriangle (Vector3 pt, Vector3 v1, Vector3 v2, Vector3 v3)
		{
			bool b1, b2, b3;

			b1 = sign(pt, v1, v2) < 0.0f;
			b2 = sign(pt, v2, v3) < 0.0f;
			b3 = sign(pt, v3, v1) < 0.0f;

			return ((b1 == b2) && (b2 == b3));
		}
		#endregion

		public void Render(){
			GL.BindTexture(TextureTarget.Texture2D, Image);
			GL.BindVertexArray(vaoHandle);
			//Piece p = Pieces[4,2];
			foreach (Piece p in Pieces) {
				MainWin.mainShader.ModelMatrix = p.Transformations;
				GL.DrawElements (PrimitiveType.Triangles, p.Indices.Length,
					DrawElementsType.UnsignedInt, p.Indices);
			}
				//break;
			//}
			GL.BindVertexArray (0);
			GL.BindTexture(TextureTarget.Texture2D, 0);
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

		#region IDisposable implementation
		public void Dispose ()
		{
			GL.DeleteBuffer (positionVboHandle);
			GL.DeleteBuffer (normalsVboHandle);
			GL.DeleteBuffer (texVboHandle);
			GL.DeleteVertexArray (vaoHandle);
		}
		#endregion
	}
}

