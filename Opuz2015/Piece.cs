//
//  Piece.cs
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
using GGL;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using go;

namespace Opuz2015
{
	public class Piece 
	{
		#region CTOR
		public Piece (Puzzle _puzzle, List<int> _indicesBorder)
		{
			puzzle = _puzzle;
			IndBorder = _indicesBorder.ToArray ();
			IndFill = earTriangulation(_indicesBorder);
			computeBounds ();
		}
		#endregion

		Puzzle puzzle = null;
		float 	angle = 0f,
				dx = 0f,
				dy = 0f,
				dz = 0f;

		bool transformationsAreUpToDate = false;
		Matrix4 transformations = Matrix4.Identity;

		#region Pubilc Properties
		public Rectangle<float> Bounds;
		public int[] IndBorder;
		public int[] IndFill;
		public Matrix4 Transformations{
			get {
				if (!transformationsAreUpToDate) {
					Point<float> c = Bounds.Center;
					transformations = 
						Matrix4.CreateTranslation (-c.X, -c.Y, 0) *
					Matrix4.CreateRotationZ (angle) *
					Matrix4.CreateTranslation (c.X, c.Y, 0) *
					Matrix4.CreateTranslation(dx, dy, dz);
					transformationsAreUpToDate = true;
				}
				return transformations;
			}
		}
		public float Angle {
			get { return angle; }
			set {				
				angle = value;
				transformationsAreUpToDate = false;
			}
		}

		public bool MouseIsIn(Point<float> m)
		{
			Rectangle<float> r = getProjectedBounds ();
			return r.ContainsOrIsEqual (m);
		}
		#endregion

		#region Public Functions
		public void Rotate()
		{
			if (Angle == MathHelper.ThreePiOver2)
				Angle = 0f;
			else
				Angle += MathHelper.PiOver2;
		}
		public void Move(float _dispX, float _dispY, float _dispZ = 0f){
			dx += _dispX;
			dy += _dispY;
			dz += _dispZ;

			transformationsAreUpToDate = false;
		}

		public void Render(){
			MainWin.mainShader.Color = Color.White;
			MainWin.mainShader.ModelMatrix = Transformations;
			GL.DrawElements (PrimitiveType.Triangles, IndFill.Length,
				DrawElementsType.UnsignedInt, IndFill);
//			GL.Disable (EnableCap.DepthTest);
//			MainWin.mainShader.Color = Color.Red;
//			GL.DrawElements (PrimitiveType.LineLoop, IndBorder.Length,
//				DrawElementsType.UnsignedInt, IndBorder);			
//
//			GL.Enable (EnableCap.DepthTest);
		}
		#endregion

		public Rectangle<float> getProjectedBounds()
		{
			Matrix4 M = Transformations *
			            MainWin.mainShader.ModelViewMatrix *
			            MainWin.mainShader.ProjectionMatrix;
			Rectangle<float> projR = Rectangle<float>.Zero;
			Point<float> topLeft, bottomRight;

			if (Angle % MathHelper.Pi == 0) {
				topLeft = Bounds.TopLeft;
				bottomRight = Bounds.BottomRight;
			} else {
				topLeft = Bounds.BottomLeft;
				bottomRight = Bounds.TopRight;
			}
		

			Point<float> pt1 = glHelper.Project (topLeft, M, MainWin.viewport [2], MainWin.viewport [3]);
			Point<float> pt2 = glHelper.Project (bottomRight, M, MainWin.viewport [2], MainWin.viewport [3]);
			if (pt1 < pt2) {
				projR.TopLeft = pt1;
				projR.BottomRight = pt2;
			} else {
				projR.TopLeft = pt2;
				projR.BottomRight = pt1;
			}
			return projR;
		}

		void computeBounds()
		{
			float minX = float.MaxValue,
					maxX = float.MinValue,
					minY = float.MaxValue,
					maxY = float.MinValue;

			for (int i = 0; i < IndBorder.Length; i++) {
				Vector3 p = puzzle.positions [IndBorder [i]];
				if (p.X < minX)
					minX = p.X;
				if (p.X > maxX)
					maxX = p.X;
				if (p.Y < minY)
					minY = p.Y;
				if (p.Y > maxY)
					maxY = p.Y;
			}
			Bounds = new Rectangle<float> (minX, minY, maxX - minX, maxY - minY);
		}

		#region triangulation
		int[] earTriangulation(List<int> tril)
		{
			Vector3[] positions = puzzle.positions;
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


	}
}

