using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using FarseerPhysics.DebugViews;
using FarseerPhysics;
using Camera;
using BoxExample.Objects;
using Utils;
using FarseerPhysics.Common;

namespace BoxExample
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager mGraphics;
        private SpriteBatch mSpriteBatch;
        private ContentManager mContentManager;

        private World mWorld;
        private DebugViewXNA mDebugViewXNA;

        public Camera.Camera2D mCamera;
        private Vector2 mScreenCenter;
        private Matrix mProjection;
        private Matrix mView;

        private KeyboardState mOldKeyState;

        private const float MeterInPixels = 64f;

        private Box mBox;
        private Box mBox2;
        private Body mBorderBody;
        private Texture2D mBorderTexture;

        public Game1()
        {
            mGraphics = new GraphicsDeviceManager(this);
            mGraphics.PreferredBackBufferWidth = 800;
            mGraphics.PreferredBackBufferHeight = 480;

            Services.AddService(typeof(GraphicsDeviceManager), mGraphics);
            Content.RootDirectory = "Content";

            mContentManager = new ContentManager(this.Services);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            IsFixedTimeStep = true;
            TargetElapsedTime = new TimeSpan(0, 0, 0, 0, 10);

            mWorld = new World(new Vector2(0, 0));
            Services.AddService(typeof(World), mWorld);

            mSpriteBatch = new SpriteBatch(GraphicsDevice);
            Services.AddService(typeof(SpriteBatch), mSpriteBatch);

            mDebugViewXNA = new DebugViewXNA(mWorld);
            mDebugViewXNA.LoadContent(mGraphics.GraphicsDevice, Content);

            mDebugViewXNA.AppendFlags(DebugViewFlags.Shape);
            mDebugViewXNA.AppendFlags(DebugViewFlags.CenterOfMass);
            mDebugViewXNA.AppendFlags(DebugViewFlags.AABB);
            mDebugViewXNA.AppendFlags(DebugViewFlags.ContactPoints);
            mDebugViewXNA.AppendFlags(DebugViewFlags.ContactNormals);
            mDebugViewXNA.DefaultShapeColor = Color.LightGray;
            mDebugViewXNA.SleepingShapeColor = Color.LightGray;
            mDebugViewXNA.DebugPanelPosition = new Vector2(0, 0);
            mDebugViewXNA.StaticShapeColor = Color.Orange;

            mBox = new Box(this, Vector2.Zero);
            mBox.playerIndex = PlayerIndex.One;
            mBox.DebugViewXNA = mDebugViewXNA;
            Components.Add(mBox);

            mBox2 = new Box(this, Vector2.Zero);
            mBox2.playerIndex = PlayerIndex.Two;
            mBox2.DebugViewXNA = mDebugViewXNA;
            Components.Add(mBox2);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            mCamera = new Camera.Camera2D(mGraphics.GraphicsDevice);

            mScreenCenter = new Vector2(mGraphics.GraphicsDevice.Viewport.Width / 2,
                mGraphics.GraphicsDevice.Viewport.Height / 2);

            mBorderTexture = Content.Load<Texture2D>(@"Textures\pavement");

            DrawBorderLoop();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            Content.Unload();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Setting the Box object's View and Project properties so the
            // Box's DebugViewXNA draws on top
            mBox.View = mView;
            mBox.Projection = mProjection;
            mBox2.View = mView;
            mBox2.Projection = mProjection;

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            if (Keyboard.GetState().IsKeyDown(Keys.Home))
            {
                mDebugViewXNA.RemoveFlags(DebugViewFlags.Shape);
                mDebugViewXNA.RemoveFlags(DebugViewFlags.Joint);
                mDebugViewXNA.RemoveFlags(DebugViewFlags.DebugPanel);
                mDebugViewXNA.RemoveFlags(DebugViewFlags.ContactPoints);
                mDebugViewXNA.RemoveFlags(DebugViewFlags.AABB);
                mDebugViewXNA.RemoveFlags(DebugViewFlags.PerformanceGraph);
                mDebugViewXNA.RemoveFlags(DebugViewFlags.PolygonPoints);
                mDebugViewXNA.RemoveFlags(DebugViewFlags.CenterOfMass);
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.End))
            {
                mDebugViewXNA.AppendFlags(DebugViewFlags.Shape);
                mDebugViewXNA.AppendFlags(DebugViewFlags.Joint);
                mDebugViewXNA.AppendFlags(DebugViewFlags.DebugPanel);
                mDebugViewXNA.AppendFlags(DebugViewFlags.ContactPoints);
                mDebugViewXNA.AppendFlags(DebugViewFlags.AABB);
                mDebugViewXNA.AppendFlags(DebugViewFlags.PerformanceGraph);
                mDebugViewXNA.AppendFlags(DebugViewFlags.PolygonPoints);
                mDebugViewXNA.AppendFlags(DebugViewFlags.CenterOfMass);
            }

            mWorld.Step((float)gameTime.ElapsedGameTime.TotalMilliseconds * 0.001f);

            Console.WriteLine(string.Format("BoxA {0},{1}", mBox2.Body.Position.X, mBox2.Body.Position.Y));

            HandleKeyboard(gameTime);
            mCamera.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            mProjection = mCamera.SimProjection;
            mView = mCamera.SimView;

            mDebugViewXNA.RenderDebugData(ref mProjection);

            base.Draw(gameTime);
        }

        #region Methods
        float mForce = 5f;
        const float borderWidth = 0.2f;
        const float width = 40f;
        const float height = 25f;

        public void DrawBorderLoop()
        {
            Vertices vertices = new Vertices(4);

            float mWidth = ConvertUnits.ToSimUnits(mGraphics.GraphicsDevice.Viewport.Width);
            float mHeight = ConvertUnits.ToSimUnits(mGraphics.GraphicsDevice.Viewport.Height);

            vertices = new Vertices(4);
            vertices.Add(new Vector2(0, 0));
            vertices.Add(new Vector2(mWidth, 0));
            vertices.Add(new Vector2(mWidth, mHeight));
            vertices.Add(new Vector2(0, mHeight));

            mBorderBody = BodyFactory.CreateLoopShape(mWorld, vertices, new Vector2(0,0));
        }

        private void HandleKeyboard(GameTime gameTime)
        {
            KeyboardState state = Keyboard.GetState();

            // Move camera
            Vector2 camMove = Vector2.Zero;

            if (state.IsKeyDown(Keys.U))
            {
                camMove.Y -= 100f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            if (state.IsKeyDown(Keys.N))
            {
                camMove.Y += 100f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            if (state.IsKeyDown(Keys.H))
            {
                camMove.X -= 100f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            if (state.IsKeyDown(Keys.J))
            {
                camMove.X += 100f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            if (state.IsKeyDown(Keys.PageUp))
            {
                mCamera.Zoom += 5f * (float)gameTime.ElapsedGameTime.TotalSeconds * mCamera.Zoom / 20f;
            }
            if (state.IsKeyDown(Keys.PageDown))
            {
                mCamera.Zoom -= 5f * (float)gameTime.ElapsedGameTime.TotalSeconds * mCamera.Zoom / 20f;
            }

            if (camMove != Vector2.Zero)
            {
                mCamera.MoveCamera(camMove);
            }

            if (state.IsKeyDown(Keys.OemTilde))
            {
                mCamera.ResetCamera();
            }

            if (state.IsKeyDown(Keys.Escape))
                Exit();

            mOldKeyState = state;
        }
        #endregion
    }
}
