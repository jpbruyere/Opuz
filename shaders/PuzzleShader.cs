//
//  PuzzleShader.cs
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
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace Opuz2015
{
	public class PuzzleShader : Tetra.Shader
	{
		public PuzzleShader () : base(
			"#Opuz2015.shaders.puzzle.vert",
			"#Opuz2015.shaders.puzzle.frag")
		{
		}
		protected int   imgSize, colMult, colorLoc, modelLoc;//, kernel, offsets;

		public Vector2 ImgSize {
			set { GL.Uniform2 (imgSize, value); }
		}
		public float ColorMultiplier {
			set { GL.Uniform1 (colMult, value); }
		}
		public Vector4 Color {
			set { GL.Uniform4 (colorLoc, value); }
		}
		public Matrix4 Model {
			set { GL.UniformMatrix4(modelLoc, false, ref value); }
		}

		protected override void GetUniformLocations ()
		{
			base.GetUniformLocations ();

			colorLoc = GL.GetUniformLocation (pgmId, "color");
			modelLoc = GL.GetUniformLocation (pgmId, "model");

			imgSize = GL.GetUniformLocation (pgmId, "imgSize");
			colMult = GL.GetUniformLocation (pgmId, "colMult");
		}
		protected override void BindSamplesSlots ()
		{
			base.BindSamplesSlots ();
			GL.Uniform1(GL.GetUniformLocation (pgmId, "texBords"), 1);
		}
	}
}

