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
	public class PuzzleShader : GameLib.Shader
	{
		public PuzzleShader () : base(
			"Opuz2015.shaders.puzzle.vert",
			"Opuz2015.shaders.puzzle.frag")
		{
		}
		protected int   imgSize;

		public Vector2 ImgSize {
			set { GL.Uniform2 (imgSize, value); }
		}
		protected override void GetUniformLocations ()
		{
			base.GetUniformLocations ();
			imgSize = GL.GetUniformLocation (pgmId, "ImgSize");
		}
	}
}

