﻿//
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
using Crow;

namespace Opuz2015
{
	public class Piece 
	{
		#region CTOR
		public Piece (Puzzle _puzzle, uint _x, uint _y, List<uint> _indices)
		{
			puzzle = _puzzle;

			uint[] ind = _indices.ToArray ();
			computeBounds (ind);
			ComputeBorderIndices (_x, _y, ind);

			IndFill = earTriangulation(_indices);

			IsLinked = Enumerable.Repeat(false, puzzle.nbSides).ToArray();
			Neighbours = new Piece[puzzle.nbSides];
		}
		#endregion

		Puzzle puzzle = null;
		float 	angle = 0f,
				dx = 0f,
				dy = 0f,
				dz = 0f;

		bool transformationsAreUpToDate = false;
		Matrix4 transformations = Matrix4.Identity;
		Matrix4 transformationsInverse = Matrix4.Identity;

		float colorMultiplier = 1f;

		#region Pubilc Properties
		public int IndFillPtr, indFillLength;
		public int[] IndBorderPtr, indBorderLength;

		public Rectangle<float> Bounds;
		public uint[][] IndBorder;
		public uint[] IndFill;
		public Matrix4 Transformations{
			get {
				if (!transformationsAreUpToDate) {
					Point<float> c = Bounds.Center;
					transformations = 
						Matrix4.CreateTranslation (-c.X, -c.Y, 0) *
						Matrix4.CreateRotationZ (angle) *					
						Matrix4.CreateTranslation(dx, dy, dz);
					transformationsInverse = transformations.Inverted ();
					transformationsAreUpToDate = true;
				}
				return transformations;
			}
		}
		public float Angle {
			get { return angle; }
			set {
				if (angle == value)
					return;
				//rotate dx and dy by difference
				float deltaAngle = value - angle;
				if (rotationRef != this && rotationRef != null) {
					Vector3 rotatedDxDy = Vector4.Transform (new Vector4 (Dx, Dy, 0,1),
						Matrix4.CreateTranslation (-rotationRef.Dx, -rotationRef.Dy, 0) *
						Matrix4.CreateRotationZ (deltaAngle) *
						Matrix4.CreateTranslation (rotationRef.Dx, rotationRef.Dy, 0)).Xyz;
					Dx = rotatedDxDy.X ;
					Dy = rotatedDxDy.Y ;
				}

				if (value == -MathHelper.TwoPi)
					angle = 0f;
				else
					angle = value;
				
				if (!puzzle.Selection.Contains (this))
					MainWin.RebuildCache = true;
				
				transformationsAreUpToDate = false;
			}
		}
		public bool[] IsLinked;
		public Piece[] Neighbours;
		public bool Visited = false;

		public float Dx 
		{ 
			get { return dx; }
			set {
				if (dx == value)
					return;
				dx = value;
//				if (puzzle.Selection.Contains (this))
//					MainWin.RebuildCache = true;
				transformationsAreUpToDate = false;
			}
		}
		public float Dy 
		{ 
			get { return dy; }
			set {
				dy = value;
//				if (puzzle.Selection.Contains (this))
//					MainWin.RebuildCache = true;
				transformationsAreUpToDate = false;
			}
		}
		public float Dz 
		{ 
			get { return dz; }
			set {
				if (dz == value)
					return;
				dz = value;
//				if (puzzle.Selection.Contains (this))
//					MainWin.RebuildCache = true;
				transformationsAreUpToDate = false;
			}
		}
		public float ColorMultiplier {
			get {
				return colorMultiplier;
			}
			set {
				colorMultiplier = value;
				MainWin.RebuildCache = true;
			}
		}
		#endregion

		#region Public Functions
		Piece rotationRef = null;
		public void Rotate(Piece pcr)
		{
			if (Visited)
				return;
			Visited = true;

			float target = 0f;
			float res = Angle / MathHelper.PiOver2;
			int nbPi = (int)Math.Floor(-res);

			if (res % 1 != 0)
				nbPi++;
			
			switch (nbPi) {
			case 0:
				target = -MathHelper.PiOver2;
				break;
			case 1:
				target = -MathHelper.Pi;
				break;
			case 2:
				target = -MathHelper.ThreePiOver2;
				break;
			case 3:
				target = -MathHelper.TwoPi;
				break;
			}

			rotationRef = pcr;
			Animation.StartAnimation(new Animation<float> (this, "Angle", target, 0.2f));

			for (int i = 0; i < IsLinked.Length; i++) {
				if (!IsLinked [i])
					continue;
				Neighbours [i].Rotate(pcr);
			}
		}
		public void Move(float _dispX, float _dispY, float _dispZ = 0f){
			if (Visited)
				return;
			Visited = true;

			dx += _dispX;
			dy += _dispY;
			dz += _dispZ;
			
			for (int i = 0; i < IsLinked.Length; i++) {
				if (!IsLinked [i])
					continue;
				Neighbours [i].Move (_dispX, _dispY, _dispZ);
			}

			transformationsAreUpToDate = false;
		}
		public void PutOnTop()
		{
			if (Visited)
				return;
			Visited = true;
			MainWin.RebuildCache = true;
			lock (puzzle.Mutex) {
				puzzle.ZOrderedPieces.Remove (this);
				puzzle.ZOrderedPieces.Add(this);
			}
			for (int i = 0; i < IsLinked.Length; i++) {
				if (!IsLinked [i])
					continue;
				Neighbours [i].PutOnTop();
			}
		}
		/// <summary>
		/// Build selected pces list recursively
		/// </summary>
		public void UpdateSelection()
		{
			if (Visited)
				return;
			Visited = true;

			puzzle.Selection.Add (this);
			for (int i = 0; i < IsLinked.Length; i++) {
				if (!IsLinked [i])
					continue;
				Neighbours [i].UpdateSelection ();
			}
		}
		/// <summary>
		/// rotated delta between centers of tested pce, kept global in pce class to compute it only once.
		/// </summary>
		Vector3 cDelta = Vector3.Zero;

		public void Test(){
			if (Visited)
				return;
			Visited = true;

			for (int i = 0; i < IsLinked.Length; i++) {
				if (Neighbours == null)
					continue;
				if (IsLinked [i]) {
					Neighbours [i].Test ();
					continue;
				}
				if (testProximity (i))
					Bind (i);
			}
		}
		public bool PuzzleIsFinished
		{
			get { 
				if (Visited)
					return true;
				Visited = true;

				for (int i = 0; i < IsLinked.Length; i++) {
					if (Neighbours [i] == null)
						continue;
					if (!IsLinked [i])
						return false;
					if (!Neighbours [i].PuzzleIsFinished)
						return false;					
				}
				return true;
			}
		}
		public void Highlight(float _colorMultiplier, float _step = 0.5f){
			Animation.StartAnimation (new Animation<float> (this, "ColorMultiplier", _colorMultiplier,_step), 0, onColorMultAnimEnd);
		}
		public void Bind(int i)
		{			
			IsLinked [i] = true;
			Highlight (2f, 0.5f);
			Piece p = Neighbours [i];
			p.ResetVisitedStatus ();
			p.Move (Dx - p.Dx -cDelta.X, Dy - p.Dy- cDelta.Y);
			p.IsLinked[opositePce(i)] = true;
			p.Visited = false;
			p.Test ();
		}		
		public void ResetVisitedStatus()
		{			
			if (!Visited)
				return;
			Visited = false;
			for (int i = 0; i < IsLinked.Length; i++) {
				if (IsLinked [i])
					Neighbours [i].ResetVisitedStatus();
			}
		}

		int opositePce(int i)
		{
			return (i + puzzle.nbSides / 2) % puzzle.nbSides;	
		}
		bool testProximity(int n)
		{
			Piece p = Neighbours [n];
			if (p == null)
				return false;
			if (Angle != p.Angle)
				return false;
			cDelta = Bounds.Center - p.Bounds.Center;
			cDelta = cDelta.Transform (Matrix4.CreateRotationZ (Angle));
			if (
				Math.Abs (Dx - p.Dx-cDelta.X) < puzzle.TolerancePlacementPieces &&
				Math.Abs (Dy - p.Dy-cDelta.Y) < puzzle.TolerancePlacementPieces)
				return true;
			return false;
		}
//		public void RenderShadow(){
//
//			MainWin.mainShader.Model = Transformations * Matrix4.CreateTranslation(-puzzle.PieceThickness,-puzzle.PieceThickness,0);
//			MainWin.mainShader.ColorMultiplier = 1.0f;
//			MainWin.mainShader.Color = new Vector4 (0, 0, 0, 1);
//
//			GL.DrawElements (BeginMode.Triangles, IndFill.Length,
//				DrawElementsType.UnsignedInt, IndFill);
//		}
		public void Render(){
			MainWin.mainShader.Color = new Vector4 (0.4f, 0.4f, 0.4f, 1);
			MainWin.mainShader.Model = Transformations;
			MainWin.mainShader.ColorMultiplier = colorMultiplier;

			//border, only when not linked
			for (int i = 0; i < puzzle.nbSides; i++) {
				if (IsLinked [i])
					continue;
				GL.DrawElements (BeginMode.TriangleStrip, IndBorder[i].Length,
					DrawElementsType.UnsignedInt, IndBorder[i]);
			}
			//face
//			MainWin.mainShader.Color = new Vector4 (1, 1, 1, 1);
//			GL.DrawElements (BeginMode.Triangles, IndFill.Length,
//				DrawElementsType.UnsignedInt, IndFill);
		}
		#endregion

		void  onColorMultAnimEnd(Animation a)
		{
			Animation.StartAnimation (new FloatAnimation (this, "ColorMultiplier", 1f));
		}

		#region Mouse handling
		public bool MouseIsIn(Vector3 m)
		{			
			Vector3 mm = m.Transform (transformationsInverse);
			return Bounds.ContainsOrIsEqual (new Point<float> (mm.X, mm.Y));
		}
		#endregion

		#region triangulation and bounds calculations
		public void ComputeBorderIndices(uint x, uint y, uint[] indProfile)
		{
			IndBorder = new uint[puzzle.nbSides][];

			uint ptr = 0;
			for (uint c = 0; c < puzzle.nbSides; c++) {
				int nbp = puzzle.cutter.NbPoints;

				switch (c) {
				case 0:
					if (y == 0)
						nbp = 1;
					break;
				case 1:
					if (x == puzzle.nbPieceX - 1)
						nbp = 1;
					break;
				case 2:
					if (y == puzzle.nbPieceY - 1)
						nbp = 1;
					break;
				case 3:
					if (x == 0)
						nbp = 1;
					break;
				}									
				
				IndBorder [c] = new uint[nbp*2+2];
				for (uint i = 0; i < nbp; i++) {
					IndBorder [c] [i * 2] = indProfile [ptr+i] + puzzle.BorderOffset;
					IndBorder [c] [i * 2 + 1] = indProfile [ptr+i];
				}
				if (c < puzzle.nbSides - 1)
					ptr += (uint)nbp;
				else
					ptr = 0;
				IndBorder [c][nbp*2] = indProfile [ptr] + puzzle.BorderOffset;
				IndBorder [c][nbp*2 + 1] = indProfile [ptr] ;
			}			
		}
		void computeBounds(uint[] indProfile)
		{
			float minX = float.MaxValue,
			maxX = float.MinValue,
			minY = float.MaxValue,
			maxY = float.MinValue;

			for (int i = 0; i < indProfile.Length; i++) {
				Vector3 p = puzzle.positions [indProfile [i]];
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

		uint[] earTriangulation(List<uint> tril)
		{
			Vector3[] positions = puzzle.positions;
			//triangles list
			List<uint> indices = new List<uint> ();

			int p0 = 0, p1, p2;

			while (tril.Count > 2){				
				p1 = cyclingIncrementer (p0, tril.Count-1);
				p2 = cyclingIncrementer (p1, tril.Count-1);

				if (Vector3.Cross (
					positions [tril [p1]] - positions [(int)tril [p0]], 
					positions [(int)tril [p2]] - positions [(int)tril [p0]]).Z > 0) {

					bool theresPointsInsideTri = false;

					for (int i = 0; i < tril.Count; i++) {
						if (i == p0 || i == p1 || i == p2)
							continue;
						if (!PointIsInTriangle (
							positions [(int)tril [i]],
							positions [(int)tril [p0]],
							positions [(int)tril [p1]],
							positions [(int)tril [p2]]))
							continue;
						theresPointsInsideTri = true;
						break;
					}

					if (!theresPointsInsideTri) {
						//build triangle
						indices.Add (tril [p0]);
						indices.Add (tril [p2]);
						indices.Add (tril [p1]);

						tril.RemoveAt ((int)p1);

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

