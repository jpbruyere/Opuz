//
//  Side.cs
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
using OpenTK;
using System.Collections.Generic;
using GGL;
using OpenTK.Graphics.OpenGL;
using System.Linq;
using System.Diagnostics;

namespace Opuz2015
{
	public class Side
	{
		static Random rnd = new Random();
		public float Length = 5f;
		public bool TutIsInside = false;

		public Vector3[] Positions;
		Vector2[] texcoords;

		const int resolution = 20;
		const float tutWidth = 0.10f;
		const float tutWidthVariance = 0.2f;
		const float tutHeight = 0.3f;
		const float tutHeightVariance = 0.2f;
		const float tutPosVariance = 0.2f;

		vaoMesh mesh;

		public Side ()
		{
			float tw = Length * tutWidth;
			float th = Length * tutHeight;
			float tp = Length / 2f;


			tw = tw + (float)(0.5 - rnd.NextDouble())*tw*tutWidthVariance;
			th = th + (float)(0.5 - rnd.NextDouble())*th*tutHeightVariance;
			tp = tp + (float)(0.5 - rnd.NextDouble())*Length*tutPosVariance;

			if (rnd.NextDouble () < 0.5) {
				th = -th;
				TutIsInside = true;
			}
				

			Vector3[] p = new Vector3[] {
				new Vector3 (0, 0, 0),
				new Vector3 (tp - tw, 		0, 0),
				new Vector3 (tp - tw * 3.0f, 	th, 0),
				new Vector3 (tp + tw * 3.0f,	th, 0),
				new Vector3 (tp + tw, 		0, 0),
				new Vector3 (Length, 0, 0)
			};

			List<Vector3> points = new List<Vector3> ();
			List<int> indices = new List<int> ();
			points.Add (p [0]);
			//points.Add (p [1]);
			for (int i = 0; i < resolution; i++) {
				float t = i / (float)(resolution-1);
				points.Add(CalculateBezierPoint(t,p[1],p[2],p[3],p[4]));
			}

			//points.Add (p [4]);
			points.Add (p [5]);

//			for (int i = 0; i < points.Count; i++)
//				indices.Add (i);			

			Positions = points.ToArray ();
			Debug.WriteLine(Vector3.Cross (Positions [1] - Positions [0], Positions [2] - Positions [1]).ToString ());
			//mesh = new vaoMesh (Positions.ToArray (), null, Enumerable.Range(0,Positions.Length-1).ToArray());

		}

		public void Render(){
			//mesh.Render (PrimitiveType.LineStrip);
		}

		public static Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
		{
			float u = 1 - t;
			float tt = t * t;
			float uu = u * u;
			float uuu = uu * u;
			float ttt = tt * t;

			Vector3 p = uuu * p0;
			p += 3 * uu * t * p1;
			p += 3 * u * tt * p2;
			p += ttt * p3;

			return p;
		}
	}
}

