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
			IndProfile = _indicesBorder.ToArray ();
			IndFill = earTriangulation(_indicesBorder);
			computeBounds ();

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

		float colorMultiplier = 1f;

		#region Pubilc Properties
		public Rectangle<float> Bounds;
		public int[][] IndBorder;
		public int[] IndProfile;
		public int[] IndFill;
		public Matrix4 Transformations{
			get {
				if (!transformationsAreUpToDate) {
					Point<float> c = Bounds.Center;
					transformations = 
						Matrix4.CreateTranslation (-c.X, -c.Y, 0) *
						Matrix4.CreateRotationZ (angle) *					
						Matrix4.CreateTranslation(dx, dy, dz);
					transformationsAreUpToDate = true;
				}
				return transformations;
			}
		}
		public float Angle {
			get { return angle; }
			set {
				//rotate dx and dy by difference
				float deltaAngle = value - angle;
				if (rotationRef != this && rotationRef != null) {
					Vector3 rotatedDxDy = Vector3.Transform (new Vector3 (Dx, Dy, 0),
						Matrix4.CreateTranslation (-rotationRef.Dx, -rotationRef.Dy, 0) *
						Matrix4.CreateRotationZ (deltaAngle) *
						Matrix4.CreateTranslation (rotationRef.Dx, rotationRef.Dy, 0));
					Dx = rotatedDxDy.X ;
					Dy = rotatedDxDy.Y ;
				}

				if (value == -MathHelper.TwoPi)
					angle = 0f;
				else
					angle = value;
				
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
				dx = value;
				transformationsAreUpToDate = false;
			}
		}
		public float Dy 
		{ 
			get { return dy; }
			set {
				dy = value;
				transformationsAreUpToDate = false;
			}
		}
		public float Dz 
		{ 
			get { return dz; }
		}
		public float ColorMultiplier {
			get {
				return colorMultiplier;
			}
			set {
				colorMultiplier = value;
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
			Animation.StartAnimation(new Animation<float> (this, "Angle", target, 0.3f));

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

		public void Bind(int i)
		{			
			IsLinked [i] = true;
			Animation.StartAnimation (new Animation<float> (this, "ColorMultiplier", 2f,0.5f), 0, onColorMultAnimEnd);
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
			cDelta = Vector3.Transform (cDelta, Matrix4.CreateRotationZ (Angle));
			if (
				Math.Abs (Dx - p.Dx-cDelta.X) < puzzle.TolerancePlacementPieces &&
				Math.Abs (Dy - p.Dy-cDelta.Y) < puzzle.TolerancePlacementPieces)
				return true;
			return false;
		}
		public void RenderShadow(){

			MainWin.mainShader.ModelMatrix = Transformations * Matrix4.CreateTranslation(-puzzle.PieceThickness,-puzzle.PieceThickness,0);
			MainWin.mainShader.ColorMultiplier = 1.0f;
			MainWin.mainShader.Color = Color.Black;
			GL.DrawElements (PrimitiveType.Triangles, IndFill.Length,
				DrawElementsType.UnsignedInt, IndFill);
		}
		public void Render(){
			MainWin.mainShader.ModelMatrix = Transformations;
			MainWin.mainShader.ColorMultiplier = colorMultiplier;

			//border, only when not linked
			for (int i = 0; i < puzzle.nbSides; i++) {
				if (IsLinked [i])
					continue;
				MainWin.mainShader.Color = Color.DimGray;
				GL.DrawElements (PrimitiveType.TriangleStrip, IndBorder[i].Length,
					DrawElementsType.UnsignedInt, IndBorder[i]);				
			}
			//face
			MainWin.mainShader.Color = Color.White;
			GL.DrawElements (PrimitiveType.Triangles, IndFill.Length,
				DrawElementsType.UnsignedInt, IndFill);
		}
		public void RenderProfile(){
			GL.DrawElements (PrimitiveType.LineLoop, IndProfile.Length,
				DrawElementsType.UnsignedInt, IndProfile);
		}
		#endregion

		void  onColorMultAnimEnd(Animation a)
		{
			Animation.StartAnimation (new FloatAnimation (this, "ColorMultiplier", 1f));
		}

		#region Mouse handling
		public bool MouseIsIn(Point<float> m)
		{
			Rectangle<float> r = getProjectedBounds ();
			return r.ContainsOrIsEqual (m);
		}

		Rectangle<float> getProjectedBounds()
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
		#endregion

		#region triangulation and bounds calculations
		public void ComputeBorderIndices()
		{
			IndBorder = new int[puzzle.nbSides][];

			int ptr = 0;
			for (int c = 0; c < puzzle.nbSides; c++) {
				int nbp = puzzle.cutter.NbPoints;
				if (Neighbours [c] == null)
					nbp = 1;
				IndBorder [c] = new int[nbp*2+2];
				for (int i = 0; i < nbp; i++) {
					IndBorder [c] [i * 2] = IndProfile [ptr+i] + puzzle.BorderOffset;
					IndBorder [c] [i * 2 + 1] = IndProfile [ptr+i];
				}
				if (c < puzzle.nbSides - 1)
					ptr += nbp;
				else
					ptr = 0;
				IndBorder [c][nbp*2] = IndProfile [ptr] + puzzle.BorderOffset;
				IndBorder [c][nbp*2 + 1] = IndProfile [ptr] ;
			}			
		}
		void computeBounds()
		{
			float minX = float.MaxValue,
			maxX = float.MinValue,
			minY = float.MaxValue,
			maxY = float.MinValue;

			for (int i = 0; i < IndProfile.Length; i++) {
				Vector3 p = puzzle.positions [IndProfile [i]];
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

