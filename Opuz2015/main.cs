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
		public void NotifyValueChanged(string name, object value)
		{
			ValueChanged.Raise (this, new ValueChangeEventArgs (name, value));
		}
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
		public static Vector3 vEye = new Vector3(0.0f, -200.0f, 1400.0f);    // Camera Position
		public static Vector3 vEyeTarget = new Vector3(0, 100, 0f);
		public static Vector3 vLook = new Vector3(0.5f, 0.5f, -0.5f);  // Camera vLook Vector
		public static Vector4 vLight = new Vector4 (-5.0f, -5.0f, 25.0f, 0.0f);
		public static Vector3 vMouse = Vector3.Zero;

		float _zFar = 6000.0f;

		public float zFar {
			get { return _zFar; }
			set {
				_zFar = value;
			}
		}

		public float zNear = 1.0f;
		public float fovY = (float)Math.PI / 4;

		float MoveSpeed = 100.0f;
		float RotationSpeed = 0.02f;
		#endregion

		public static PuzzleShader mainShader;
		public static GameLib.EffectShader redShader;
		public static GameLib.EffectShader testShader;


		int nbPceX = 5;
		int nbPceY = 3;
		string imagePath = @"Images/0.jpg";

		public int NbPceX {
			get {
				return nbPceX;
			}
			set {
				nbPceX = value;
				NotifyValueChanged ("NbPceX", nbPceX);
			}
		}
		public int NbPceY {
			get { return nbPceY; }
			set { 
				nbPceY = value;
				NotifyValueChanged ("NbPceY", nbPceY);
			}
		}
		public string ImagePath {
			get { return imagePath; }
			set {
				imagePath = value;
				NotifyValueChanged ("ImagePath", imagePath);
			}
		}

		public void ActivateMainShader()
		{
			mainShader.Enable ();
			mainShader.Color = Color.White;
			mainShader.ColorMultiplier = 1f;
			mainShader.ImgSize = new Vector2 (puzzle.Image.Width, puzzle.Image.Height);
			mainShader.ProjectionMatrix = projection;
			mainShader.ModelViewMatrix = modelview;
			mainShader.ModelMatrix = Matrix4.Identity;

		}
		public static void ActivateRedShader(){
			redShader.Enable ();
			redShader.ProjectionMatrix = projection;
			redShader.ModelViewMatrix = modelview;
		}

		void initOpenGL()
		{
			vLook = vEyeTarget - vEye;

			GL.ClearColor(0.0f, 0.0f, 0.2f, 1.0f);
			//GL.Enable(EnableCap.DepthTest);
			GL.DepthFunc(DepthFunction.Less);
			GL.Enable(EnableCap.CullFace);
			GL.CullFace (CullFaceMode.Front);

			GL.PrimitiveRestartIndex (int.MaxValue);
			GL.Enable (EnableCap.PrimitiveRestart);
			GL.Enable (EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			mainShader = new PuzzleShader();
			
			redShader = new GameLib.EffectShader ("Opuz2015.shaders.Border");
			testShader = new GameLib.EffectShader ("Opuz2015.shaders.red");

			GL.ActiveTexture (TextureUnit.Texture0);

			ErrorCode err = GL.GetError ();
			Debug.Assert (err == ErrorCode.NoError, "OpenGL Error");	

		}

		void onNbPceXChanged (object sender, ValueChangeEventArgs e)
		{
			if (e.MemberName != "Value")
				return;
			nbPceX = Convert.ToInt32(e.NewValue);
		}
		void onNbPceYChanged (object sender, ValueChangeEventArgs e)
		{
			if (e.MemberName != "Value")
				return;
			nbPceY = Convert.ToInt32(e.NewValue);
		}

		void onCutPuzzle (object sender, MouseButtonEventArgs e)
		{
			mainMenu.Visible = false;
			if (puzzle != null)
				puzzle.Dispose();
			puzzle = new Puzzle (NbPceX, NbPceY, ImagePath);
			puzzle.Shuffle();
		}
		void onButQuitClick (object sender, MouseButtonEventArgs e){
			closeGame ();

		}
		void onBackToMainMenu (object sender, MouseButtonEventArgs e)
		{
			closeCurrentPuzzle ();
		}
		void closeGame(){
			if (puzzle != null)
				puzzle.Dispose();
			this.Quit ();
		}
		void closeCurrentPuzzle(){
			finishedMessage.Visible = false;
			mainMenu.Visible = true;
			if (puzzle != null)
				puzzle.Dispose();
			puzzle = null;
		}

		Puzzle puzzle;
		GraphicObject mainMenu = null;
		GraphicObject finishedMessage = null;

		void draw()
		{
			if (puzzle == null)
				return;
			if (!puzzle.Ready)
				return;
			
			ActivateMainShader ();

			puzzle.Render ();

			if (selMesh == null)
				return;

			redShader.Enable ();
			redShader.ProjectionMatrix = projection;
			redShader.ModelViewMatrix = modelview;
			redShader.ModelMatrix = puzzle.SelectedPiece.Transformations;

			GL.PointSize (2f);
			//GL.Disable (EnableCap.DepthTest);
			selMesh.Render (PrimitiveType.TriangleStrip);
			//GL.Enable (EnableCap.DepthTest);
			GL.UseProgram (0);
		}
			
		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			//special event handlers fired only if mouse not in interface objects
			//for scene mouse handling
			this.MouseWheelChanged += new EventHandler<MouseWheelEventArgs>(Mouse_WheelChanged);
			this.MouseMove += new EventHandler<MouseMoveEventArgs>(Mouse_Move);
			this.MouseButtonDown += new EventHandler<MouseButtonEventArgs>(Mouse_ButtonDown);
			this.MouseButtonUp += new EventHandler<MouseButtonEventArgs>(Mouse_ButtonUp);

			LoadInterface("#Opuz2015.ui.fps.goml").DataSource = this;
			mainMenu = LoadInterface("#Opuz2015.ui.MainMenu.goml");
			mainMenu.DataSource = this;
			finishedMessage = LoadInterface ("#Opuz2015.ui.Finished.goml");
			finishedMessage.DataSource = this;
			finishedMessage.Visible = false;


			initOpenGL ();
		}
			
		public override void GLClear ()
		{
			GL.ClearColor(0.1f, 0.1f, 0.3f, 1.0f);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}
		public override void OnRender (FrameEventArgs e)
		{
			GL.CullFace (CullFaceMode.Front);
			draw ();
			GL.CullFace (CullFaceMode.Back);
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

			GGL.Animation.ProcessAnimations ();
		}
		protected override void OnKeyDown (KeyboardKeyEventArgs e)
		{
			base.OnKeyDown (e);
			switch (e.Key) {
			case Key.Space:
				if (puzzle != null)
					puzzle.resolve ();
				break;
			case Key.Escape:
				if (puzzleIsReady)
					closeCurrentPuzzle ();
				else
					closeGame ();
				break;
			}
		}
		vaoMesh selMesh;
		const float zSelPce = 8.0f;

		bool puzzleIsReady { get { return puzzle == null ? false : puzzle.Ready; } }

		#region vLookCalculations
		public void UpdateViewMatrix()
		{
			Rectangle r = this.ClientRectangle;
			GL.Viewport( r.X, r.Y, r.Width, r.Height);
			projection = Matrix4.CreatePerspectiveFieldOfView (fovY, r.Width / (float)r.Height, zNear, zFar);
				//*Matrix4.CreateRotationZ(MathHelper.Pi);
			vLook = Vector3.NormalizeFast (vEyeTarget - vEye);
			modelview = Matrix4.LookAt(vEye, vEyeTarget, Vector3.UnitZ);
			GL.GetInteger(GetPName.Viewport, viewport);

			try {
				mainShader.ProjectionMatrix = projection;
				mainShader.ModelViewMatrix = modelview;
				mainShader.ModelMatrix = Matrix4.Identity;
			} catch (Exception ex) {
				Debug.WriteLine ("UpdateViewMatrices: failed to set shader matrices: " + ex.Message);
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
		void Mouse_ButtonDown (object sender, MouseButtonEventArgs e)
		{
			if (!puzzleIsReady)
				return;

			CursorVisible = true;

			if (e.Button == MouseButton.Left) {
				Point<float> mPos = new Point<float> (e.X, e.Y);
				mPos.Y = viewport [3] - mPos.Y;
				Piece[] tmp = null;
				lock (puzzle.Mutex) {
					//Piece[] tmp = new Piece[ZOrderedPieces.Count];
					tmp = puzzle.ZOrderedPieces.ToArray();
					//Array.Copy (ZOrderedPieces, tmp, tmp.Length);
				}
				for (int i = tmp.Length-1; i >= 0; i--) {					
					Piece p = tmp [i];
					if (p.MouseIsIn (mPos)) {
						//this.CursorVisible = false;
						puzzle.SelectedPiece = p;
						p.ResetVisitedStatus ();
						p.PutOnTop ();
						Point<float> c = p.Bounds.Center;
						//selMesh = new vaoMesh (c.X, c.Y, 15f, 50f, 50f);
						selMesh = new vaoMesh (c.X, c.Y, 0f, p.Bounds.Width, p.Bounds.Height);
						p.ResetVisitedStatus ();
						p.Move (0f, 0f, zSelPce);
						break;
					}
				}
			} else if (e.Button == MouseButton.Right) {
				if (puzzle.SelectedPiece == null)
					return;
				puzzle.SelectedPiece.ResetVisitedStatus ();
				puzzle.SelectedPiece.Rotate (puzzle.SelectedPiece);
			}

		}
		void Mouse_ButtonUp (object sender, MouseButtonEventArgs e)
		{	
			this.CursorVisible = true;	
			if (!puzzleIsReady)
				return;	
			if (puzzle.SelectedPiece == null || e.Button != MouseButton.Left)
				return;
			
			puzzle.SelectedPiece.ResetVisitedStatus ();
			puzzle.SelectedPiece.Move (0f, 0f, -zSelPce);
			selMesh.Dispose ();
			selMesh = null;
			puzzle.SelectedPiece.ResetVisitedStatus ();
			puzzle.SomePiecesAreNotLinked = false;
			puzzle.SelectedPiece.Test ();
			if (!puzzle.SomePiecesAreNotLinked) {
				finishedMessage.Visible = true;
			}
			//ensure newly linked pce are on top of others
			puzzle.SelectedPiece.ResetVisitedStatus ();
			puzzle.SelectedPiece.PutOnTop ();
			puzzle.SelectedPiece = null;
		}

		void Mouse_Move(object sender, MouseMoveEventArgs e)
		{
			if (!puzzleIsReady)
				return;
			if (e.XDelta != 0 || e.YDelta != 0)
			{
				if (e.Mouse.MiddleButton == OpenTK.Input.ButtonState.Pressed) {
					Vector3 tmp = Vector3.Transform (vEye, 
						Matrix4.CreateTranslation (-vEyeTarget) *
						Matrix4.CreateRotationX (-e.YDelta * RotationSpeed) *
						Matrix4.CreateTranslation (vEyeTarget));
					if (tmp.Y >= vEyeTarget.Y || tmp.Z < 0f)
						return;
					vEye = tmp;
					UpdateViewMatrix ();
					return;
				}
				if (e.Mouse.LeftButton == ButtonState.Pressed) {
					if (puzzle.SelectedPiece != null) {
						
						Piece p = puzzle.SelectedPiece;
						p.ResetVisitedStatus ();
						p.Move ((float)e.XDelta * vEye.Z*0.003f, -(float)e.YDelta* vEye.Z*0.003f);
						return;
					}
				}
				if (e.Mouse.RightButton == ButtonState.Pressed) {
					Matrix4 m = Matrix4.CreateTranslation (-e.XDelta, e.YDelta, 0);

					vEye = Vector3.Transform (vEye, m);
					vEyeTarget = Vector3.Transform (vEyeTarget, m);
					//vLook = Vector3.Transform (vLook, m);

					//vLook = Vector3.Transform(vLook, m2);
					UpdateViewMatrix ();
					return;
				}

			}

		}			
		void Mouse_WheelChanged(object sender, MouseWheelEventArgs e)
		{
			if (!puzzleIsReady)
				return;
			float speed = MoveSpeed;
			if (Keyboard[Key.ShiftLeft])
				speed *= 0.1f;
			else if (Keyboard[Key.ControlLeft])
				speed *= 20.0f;

			Vector3 tmp = vEye + vLook * e.Delta * speed;
			if (tmp.Z < 0)
				return;
			vEye = tmp;
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