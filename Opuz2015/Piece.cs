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

namespace Opuz2015
{
	public class Piece : IDisposable
	{
		#region IDisposable implementation

		public void Dispose ()
		{
			mesh.Dispose ();
		}

		#endregion

		vaoMesh mesh;

		public Piece ()
		{
			Side[] sides = new Side[] {
				new Side (),
				new Side (),
				new Side (),
				new Side ()
			};

			int totalPoints = sides [0].Positions.Length - 1;

			for (int s = 1; s < sides.Length; s++) {
				totalPoints += sides [s].Positions.Length - 1;

				Matrix4 back = Matrix4.CreateTranslation (-sides [s].Positions [0]);
				Matrix4 forth = Matrix4.CreateTranslation (-sides [s].Positions [0]);
				Matrix4 disp = Matrix4.CreateTranslation 
					(sides [s - 1].Positions.LastOrDefault());
				Matrix4 m = Matrix4.CreateRotationZ (-MathHelper.TwoPi / sides.Length * s);

				for (int i = 0; i < sides[s].Positions.Length; i++) {
					
					sides [s].Positions [i] = Vector3.Transform 
						(sides [s].Positions [i],back * m * forth * disp);

				}
			}

			Vector3[] positions = new Vector3[totalPoints];
			int offset = 0;

			for (int i = 0; i < sides.Length; i++) {
				
				Array.Copy(sides [i].Positions, 0, positions, offset, sides [i].Positions.Length - 1);
				offset += sides [i].Positions.Length - 1;
			}
				
			mesh = new vaoMesh (positions, null, earTriangulation(positions));
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

		int[] earTriangulation(Vector3[] positions)
		{
			//indices to triangulate
			List<int> tril = new List<int> (Enumerable.Range(0, positions.Length));
			//triangles list
			List<int> indices = new List<int> ();

			int p0 = 0, p1, p2;

			while (tril.Count > 2) {				
				p1 = cyclingIncrementer (p0, tril.Count-1);
				p2 = cyclingIncrementer (p1, tril.Count-1);
				
				if (Vector3.Cross (
					    positions [tril [p1]] - positions [tril [p0]], 
					    positions [tril [p2]] - positions [tril [p0]]).Z < 0) {

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

		public void Render(){
			mesh.Render (PrimitiveType.Triangles);
			//mesh.Render (PrimitiveType.LineStrip);
		}


	}
}

