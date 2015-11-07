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
	public enum CutType{
		Simple,
		Curvy,
		Diamond,
	}
	public class Cutter
	{
		static Random rnd = new Random();

		public bool TutIsInside = false;


		int resolution = 20;
		float tutWidth = 0.10f;
		float tutWidthVariance = 0.3f;
		float tutHeight = 0.3f;
		float tutHeightVariance = 0.5f;
		float tutPosVariance = 0.3f;

		public int NbPoints { 
			get {
				switch (CutType) {
				case CutType.Simple:
					return resolution + 1; 
				case CutType.Curvy:
					return (resolution-1) * 3; 
				case CutType.Diamond:
					return resolution + 1; 
				}
				return -1;
			}
		}
		CutType CutType = CutType.Simple;

		public Cutter(CutType _type = CutType.Simple){
			CutType = _type;

		}

		public Vector3[] Cut (float startPos, float length)
		{
			float tw = length * tutWidth;
			float th = length * tutHeight;
			float tp = length / 2f;


			tw = tw + (float)(0.5 - rnd.NextDouble())*tw*tutWidthVariance;
			th = th + (float)(0.5 - rnd.NextDouble())*th*tutHeightVariance;
			tp = tp + (float)(0.5 - rnd.NextDouble())*length*tutPosVariance;

			if (rnd.NextDouble () < 0.5) {
				th = -th;
				TutIsInside = true;
			}
			List<Vector3> points = new List<Vector3> ();
			Vector3[] p = null;

			switch (CutType) {
			case CutType.Simple:
				p = new Vector3[] {
					new Vector3 (startPos, 0, 0),
					new Vector3 (startPos + tp - tw, 		0, 0),
					new Vector3 (startPos + tp - tw * 3.0f, 	th, 0),
					new Vector3 (startPos + tp + tw * 3.0f,	th, 0),
					new Vector3 (startPos + tp + tw, 		0, 0),
					new Vector3 (startPos + length, 0, 0)
				};


				points.Add (p [0]);
				for (int i = 0; i < resolution; i++) {
					float t = i / (float)(resolution-1);
					points.Add(CalculateBezierPoint(t,p[1],p[2],p[3],p[4]));
				}
				break;
			case CutType.Curvy:
				p = new Vector3[] {
					new Vector3 (startPos, 0, 0),
					new Vector3 (startPos + tp * 0.3f, 0, 0),
					new Vector3 (startPos + tp - tw * 0.3f, -th * 0.3f, 0),

					new Vector3 (startPos + tp - tw, 		0, 0),

					new Vector3 (startPos + tp - tw * 3.0f, 	th, 0),
					new Vector3 (startPos + tp + tw * 3.0f,		th, 0),

					new Vector3 (startPos + tp + tw, 		0, 0),

					new Vector3 (startPos + tp + tw * 0.3f, -th * 0.3f, 0),
					new Vector3 (startPos + length - (length - tp) * 0.3f, 0, 0),
					new Vector3 (startPos + length, 0, 0)
				};
					
				for (int i = 0; i < resolution-1; i++) {
					float t = i / (float)(resolution-1);
					points.Add(CalculateBezierPoint(t,p[0],p[1],p[2],p[3]));

				}
				for (int i = 0; i < resolution-1; i++) {
					float t = i / (float)(resolution-1);
					points.Add(CalculateBezierPoint(t,p[3],p[4],p[5],p[6]));

				}
				for (int i = 0; i < resolution-1; i++) {
					float t = i / (float)(resolution-1);
					points.Add(CalculateBezierPoint(t,p[6],p[7],p[8],p[9]));
				}

				break;			
			case CutType.Diamond:
				break;
			}
				

			return points.ToArray ();
		}

		static Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
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

