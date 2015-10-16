#define MONO_CAIRO_DEBUG_DISPOSE


using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Diagnostics;

//using GGL;
using go;
using System.Threading;
using GGL;
using System.Collections.Generic;


namespace Opuz2015
{
	class MainWin : OpenTKGameWindow, IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		#endregion

		#region FPS
		int _fps = 0;

		public int fps {
			get { return _fps; }
			set {
				if (_fps == value)
					return;

				_fps = value;

				if (_fps > fpsMax) {
					fpsMax = _fps;
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fpsMax", fpsMax));
				} else if (_fps < fpsMin) {
					fpsMin = _fps;
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fpsMin", fpsMin));
				}

				ValueChanged.Raise(this, new ValueChangeEventArgs ("fps", _fps));
				ValueChanged.Raise (this, new ValueChangeEventArgs ("update",
					this.updateTime.ElapsedMilliseconds.ToString () + " ms"));
			}
		}

		public int fpsMin = 1000;
		public int fpsMax = 0;
		public string update = "";

		void resetFps ()
		{
			fpsMin = int.MaxValue;
			fpsMax = 0;
			_fps = 0;
		}
		#endregion

		#region  scene matrix and vectors
		public static Matrix4 modelview;
		public static Matrix4 projection;
		public static int[] viewport = new int[4];

		//public static Vector3 vEye = new Vector3(150.0f, 50.0f, 1.5f);    // Camera Position
		public static Vector3 vEye = new Vector3(-5.0f, -5.0f, 5.0f);    // Camera Position
		public static Vector3 vEyeTarget = new Vector3(0f, 0f, 0.0f);
		public static Vector3 vLook = new Vector3(0.5f, 0.5f, -0.5f);  // Camera vLook Vector
		public static Vector4 vLight = new Vector4 (-5.0f, -5.0f, 25.0f, 0.0f);
		public static Vector3 vMouse = Vector3.Zero;

		float _zFar = 2000.0f;

		public float zFar {
			get { return _zFar; }
			set {
				_zFar = value;
			}
		}

		public float zNear = 0.001f;
		public float fovY = (float)Math.PI / 4;

		float MoveSpeed = 1.0f;
		float RotationSpeed = 0.02f;
		#endregion

		public static GameLib.Shader mainShader;
		public static GameLib.EffectShader redShader;

		public void ActivateMainShader()
		{
			mainShader.Enable ();
			//shader.LineWidth = lineWidth;
			mainShader.Color = Color.White;

			//mainShader.LightPos = vLight;
			mainShader.ProjectionMatrix = projection;
			mainShader.ModelViewMatrix = modelview;
			mainShader.ModelMatrix = Matrix4.Identity;

		}

		Piece test;
		Texture tex;

		void renderArrow(){
			redShader.Enable ();
			redShader.ProjectionMatrix = projection;
			redShader.ModelViewMatrix = modelview;
			redShader.ModelMatrix = Matrix4.Identity;
			GL.PointSize (2f);
			GL.Disable (EnableCap.CullFace);
			test.Render ();
			GL.Enable (EnableCap.CullFace);
			redShader.Disable ();
		}

		void initOpenGL()
		{
			vLook = vEyeTarget - vEye;

			GL.ClearColor(0.0f, 0.0f, 0.2f, 1.0f);
			GL.Enable(EnableCap.DepthTest);
			GL.DepthFunc(DepthFunction.Less);
			GL.Enable(EnableCap.CullFace);
			GL.PrimitiveRestartIndex (int.MaxValue);
			GL.Enable (EnableCap.PrimitiveRestart);
			GL.Enable (EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			mainShader = new GameLib.Shader (
				"Opuz2015.shaders.puzzle.vert",
				"Opuz2015.shaders.puzzle.frag");
			
			redShader = new GameLib.EffectShader ("GGL.Shaders.GameLib.red");

			GL.ActiveTexture (TextureUnit.Texture0);

			//mesh = vaoMesh.Load (@"/mnt/data/obj/spaceship.obj");
			Vector3[] positions = new Vector3[]
			{
				new Vector3(0,0,0),
				new Vector3(2,0,0),
				new Vector3(1,1,0),
			};
			Vector2[] texcoords = new Vector2[]
			{
				new Vector2(0,0),
				new Vector2(1,0),
				new Vector2(0.5f,1f),
			};
			Vector3[] normales = new Vector3[]
			{
				new Vector3(0,0,1),
				new Vector3(0,0,1),
				new Vector3(0,0,1),
			};
			int[] indices = new int[] { 0, 1, 2 };
			mesh = new vaoMesh (positions, texcoords, normales, indices);
			meshTex = new Texture(@"/mnt/data/obj/sf-01.jpg");

			test = new Piece ();

			System.Drawing.Bitmap bmp = new System.Drawing.Bitmap (@"/mnt/data/Images/00.jpg");


			tex = new Texture (@"/mnt/data/Images/00.jpg");




			ErrorCode err = GL.GetError ();
			Debug.Assert (err == ErrorCode.NoError, "OpenGL Error");			
		}

		vaoMesh mesh;
		int meshTex;

		void draw()
		{
			ActivateMainShader ();
			GL.BindTexture (TextureTarget.Texture2D, tex);
			test.Render ();
			GL.BindTexture (TextureTarget.Texture2D, 0);

			//renderArrow ();
		}
			
		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			Mouse.WheelChanged += new EventHandler<MouseWheelEventArgs>(Mouse_WheelChanged);
			Mouse.Move += new EventHandler<MouseMoveEventArgs>(Mouse_Move);

			initOpenGL ();
		}
		public override void GLClear ()
		{
			GL.ClearColor(0.1f, 0.1f, 0.3f, 1.0f);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}
		public override void OnRender (FrameEventArgs e)
		{
			draw ();
		}
		protected override void OnResize (EventArgs e)
		{
			base.OnResize (e);
			UpdateViewMatrix();
		}
		private int frameCpt = 0;
		protected override void OnUpdateFrame (FrameEventArgs e)
		{


			base.OnUpdateFrame (e);

			fps = (int)RenderFrequency;
			if (frameCpt > 200) {
				resetFps ();
				frameCpt = 0;

			}
			frameCpt++;

			UpdateViewMatrix ();
		}
		protected override void OnKeyDown (KeyboardKeyEventArgs e)
		{
			base.OnKeyDown (e);
			switch (e.Key) {
			case Key.Space:
				test.Dispose();
				test = new Piece ();
				break;
			}
		}
		#region vLookCalculations
		public void UpdateViewMatrix()
		{
			Rectangle r = this.ClientRectangle;
			GL.Viewport( r.X, r.Y, r.Width, r.Height);
			projection = Matrix4.CreatePerspectiveFieldOfView(fovY, r.Width / (float)r.Height, zNear, zFar);
			vEyeTarget = vEye + vLook;
			modelview = Matrix4.LookAt(vEye, vEyeTarget, Vector3.UnitZ);

			//GL.GetInteger(GetPName.Viewport, viewport);
			try {
				mainShader.ProjectionMatrix = projection;
				mainShader.ModelViewMatrix = modelview;
				mainShader.ModelMatrix = Matrix4.Identity;
			} catch (Exception ex) {

			}
		}
		Vector3 vLookDirOnXYPlane
		{
			get
			{
				return Vector3.NormalizeFast(new Vector3 (vLook.X, vLook.Y, 0));
			}
		}
		public Vector3 vLookPerpendicularOnXYPlane
		{
			get
			{
				Vector3 vHorizDir;
				if (vLook.X < 0)
					vHorizDir = Vector3.Cross(Vector3.UnitZ, Vector3.NormalizeFast(new Vector3 (vLook.X, vLook.Y, 0)));
				else
					vHorizDir = Vector3.Cross(Vector3.NormalizeFast(new Vector3 (vLook.X, vLook.Y, 0)), Vector3.UnitZ);
				Debug.WriteLine (vLook.ToString() + " - " + vHorizDir.ToString ());
				return vHorizDir;
			}
		}

		void moveCamera(Vector3 v)
		{
			vEye += v;
			vEyeTarget += v;
		}
		#endregion

		#region Mouse
		void Mouse_Move(object sender, MouseMoveEventArgs e)
		{
			if (e.XDelta != 0 || e.YDelta != 0)
			{
				if (e.Mouse.MiddleButton == OpenTK.Input.ButtonState.Pressed) {
					Matrix4 m = Matrix4.CreateRotationZ (e.XDelta * RotationSpeed);
					Matrix4 m2 = Matrix4.CreateFromAxisAngle (vLookPerpendicularOnXYPlane, e.YDelta * RotationSpeed);
					vEye = Vector3.Transform (vEye, m * m2);
					vEyeTarget = Vector3.Zero;
					vLook = vEyeTarget - vEye;
					UpdateViewMatrix ();
					return;
				}
				if (e.Mouse.RightButton == ButtonState.Pressed) {

					Matrix4 m = Matrix4.CreateRotationZ (-e.XDelta * RotationSpeed);
					Matrix4 m2 = Matrix4.Rotate (vLookPerpendicularOnXYPlane, -e.YDelta * RotationSpeed);

					vLook = Vector3.Transform (vLook, m * m2);

					//vLook = Vector3.Transform(vLook, m2);
					UpdateViewMatrix ();
					return;
				}

			}

		}			
		void Mouse_WheelChanged(object sender, MouseWheelEventArgs e)
		{
			float speed = MoveSpeed;
			if (Keyboard[Key.ShiftLeft])
				speed *= 0.1f;
			else if (Keyboard[Key.ControlLeft])
				speed *= 20.0f;

			vEye += vLook * e.Delta * speed;
			//vLook.Z += e.Delta * 0.1f;
			UpdateViewMatrix();
		}
		#endregion

		#region CTOR and Main
		public MainWin ()
			: base(1024, 800,"test")
		{}

		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (MainWin win = new MainWin( )) {
				win.Run (30.0);
			}
		}
		#endregion



	}
}