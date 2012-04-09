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
using Utils;
using FarseerPhysics.Factories;
using Camera;
using FarseerPhysics.DebugViews;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Collision;
using ProjectMercury.Renderers;
using ProjectMercury;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics.Joints;


namespace BoxExample.Objects
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class Box : Microsoft.Xna.Framework.DrawableGameComponent
    {
        #region Private Members
        private GraphicsDeviceManager mGraphics;
        private SpriteBatch mSpriteBatch;
        private ContentManager mContentManager;
        private World mWorld;

        private Body mBody;
        private Texture2D mTexture;
        private Color mColor = Color.White;
        private Vector2 mScreenCenter;

        private Vector2 mPosition = Vector2.Zero;
        private float mWidth;
        private float mHeight;
        private Vector2 mOrigin;

        private Matrix mView;
        private Matrix mProj;
        private DebugViewXNA mDebugViewXNA;

        Renderer particleRenderer;
        ParticleEffect particleEffect; 

        #endregion

        #region Properties
        public PlayerIndex playerIndex { get; set; }

        public DebugViewXNA DebugViewXNA
        {
            get { return mDebugViewXNA; }
            set { this.mDebugViewXNA = value; }
        }

        public Matrix View
        {
            get { return mView; }
            set { this.mView = value; }
        }

        public Matrix Projection
        {
            get { return mProj; }
            set { this.mProj = value; }
        }

        public Vector2 Position
        {
            get { return mOrigin; }
        }

        public Body Body
        {
            get { return mBody; }
        }
        #endregion

        public Box(Game game, Vector2 position)
            : base(game)
        {
            mContentManager = new ContentManager(game.Services);
            mWorld = (World)game.Services.GetService(typeof(World));
            mGraphics = (GraphicsDeviceManager)game.Services.GetService(typeof(GraphicsDeviceManager));
            mSpriteBatch = (SpriteBatch)game.Services.GetService(typeof(SpriteBatch));

            particleRenderer = new SpriteBatchRenderer
            {
                GraphicsDeviceService = mGraphics
            };
            particleEffect = new ParticleEffect();
        }

        /// <summary>
        /// Called when graphics resources need to be loaded. Override this method to load any component-specific graphics resources.
        /// </summary>
        protected override void LoadContent()
        {
            mScreenCenter = new Vector2(mGraphics.GraphicsDevice.Viewport.Width / 2f,
                                        mGraphics.GraphicsDevice.Viewport.Height / 2f);

            mTexture = mContentManager.Load<Texture2D>(@"Content\Textures\boxSprite");
            mWidth = ConvertUnits.ToSimUnits(mTexture.Width);
            mHeight = ConvertUnits.ToSimUnits(mTexture.Height);
            mOrigin = new Vector2(mWidth / 2, mHeight / 2);

            mPosition = ConvertUnits.ToSimUnits(mScreenCenter.X, mScreenCenter.Y);
            mBody = BodyFactory.CreateRectangle(mWorld, mWidth, mHeight,1f, mPosition);
            //mBody.LinearDamping = f;
            //mBody.Restitution = 1f;

            if (playerIndex == PlayerIndex.One)
                mBody.BodyType = BodyType.Dynamic;
            else
                mBody.BodyType = BodyType.Dynamic;

            mBody.OnCollision += new OnCollisionEventHandler(OnCollision);
            mBody.OnSeparation += new OnSeparationEventHandler(OnSeparation);
            mWorld.ContactManager.OnBroadphaseCollision += OnBroadphaseCollision;
            mBody.FixtureList[0].AfterCollision += new AfterCollisionEventHandler(OnAfterCollision);

            //FixedPrismaticJoint fixedPrismJoint = new FixedPrismaticJoint(mBody, mBody.Position, new Vector2(0, 1f));
            //fixedPrismJoint.LimitEnabled = true;
            //fixedPrismJoint.LowerLimit = -3f;
            //fixedPrismJoint.UpperLimit = 3f;
            //fixedPrismJoint.Enabled = true;
            //mWorld.AddJoint(fixedPrismJoint);

            particleRenderer.LoadContent(mContentManager);
            particleEffect = mContentManager.Load<ParticleEffect>(@"Content\basicExplosion");
            particleEffect.LoadContent(mContentManager);
            particleEffect.Initialise(); 

            base.LoadContent();
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            HandleInput(gameTime);

            float SecondsPassed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            particleEffect.Update(SecondsPassed);

            particleEffect.Trigger(mScreenCenter);

            base.Update(gameTime);
        }

        /// <summary>
        /// Called when the DrawableGameComponent needs to be drawn. Override this method with component-specific drawing code. Reference page contains links to related conceptual articles.
        /// </summary>
        /// <param name="gameTime">Time passed since the last call to Draw.</param>
        public override void Draw(GameTime gameTime)
        {
            Vector2 boxPosition = mBody.Position * 64f;
            Vector2 boxOrigin = new Vector2(mTexture.Width / 2f, mTexture.Height / 2f);
            float boxRotation = mBody.Rotation;

            // Draw mTexture first so it is underneath mDebugViewXNA
            mSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend); //, null, null, null, null, mProj);
            mSpriteBatch.Draw(mTexture, 
                boxPosition, 
                null, 
                mColor, 
                boxRotation, 
                boxOrigin, 
                1f, 
                SpriteEffects.None, 
                0f);
            mSpriteBatch.End();

            mDebugViewXNA.RenderDebugData(ref mProj);

            particleRenderer.RenderEffect(particleEffect); 

            base.Draw(gameTime);
        }

        #region Methods
        float mForce = 20f;

        private void HandleInput(GameTime gameTime)
        {
            KeyboardState keyState = Keyboard.GetState();

            Vector2 force = Vector2.Zero;
            float forceAmount = mForce * 0.6f;

            if (playerIndex == PlayerIndex.Two)
            {
                if (keyState.IsKeyDown(Keys.Left))
                {
                    force += new Vector2(-forceAmount, 0);
                }
                if (keyState.IsKeyDown(Keys.Right))
                {
                    force += new Vector2(forceAmount, 0);
                }
                if (keyState.IsKeyDown(Keys.Up))
                {
                    force += new Vector2(0, -forceAmount);
                }
                if (keyState.IsKeyDown(Keys.Down))
                {
                    force += new Vector2(0, forceAmount);
                }
            }
            else
            {
                if (keyState.IsKeyDown(Keys.A))
                {
                    force += new Vector2(-forceAmount, 0);
                }
                if (keyState.IsKeyDown(Keys.D))
                {
                    force += new Vector2(forceAmount, 0);
                }
                if (keyState.IsKeyDown(Keys.W))
                {
                    force += new Vector2(0, -forceAmount);
                }
                if (keyState.IsKeyDown(Keys.S))
                {
                    force += new Vector2(0, forceAmount);
                }

                if (keyState.IsKeyDown(Keys.Q))
                    mBody.ApplyAngularImpulse(-5);

                if (keyState.IsKeyDown(Keys.E))
                    mBody.ApplyAngularImpulse(5);
            }

            mBody.LinearVelocity = force;
            //mBody.ApplyForce(ref force);
        }
        #endregion

        #region Collision
        public void OnAfterCollision(Fixture fixtureA, Fixture fixtureB, Contact contact)
        {
            Vector2 vec = Vector2.Zero;
            FixedArray2<Vector2> fx;
            contact.GetWorldManifold(out vec, out fx);

            for (int i = 0; i < contact.Manifold.PointCount; i++)
            {
                fx[i].Normalize();
                particleEffect.Trigger(new Vector2(fx[i].X * 64, fx[i].Y * 64));
            }
        }

        public void OnBroadphaseCollision(ref FixtureProxy fixtureProxyA, ref FixtureProxy fixtureProxyB)
        {
           
        }
        public bool OnCollision(Fixture fixtureA, Fixture fixtureB, Contact contact)
        {
            if (fixtureA.Body == mBody)
            {
                mColor = Color.Red;
            }

            Vector2 vec = Vector2.Zero;
            FixedArray2<Vector2> fx;
            contact.GetWorldManifold(out vec, out fx);

            for (int i = 0; i < contact.Manifold.PointCount; i++)
            {
                fx[i].Normalize();
                //particleEffect.Trigger(new Vector2(fx[i].X * 64, fx[i].Y * 64));
            }

            return true; // return true to actually do the collision
        }

        private void OnSeparation(Fixture fixtureA, Fixture fixtureB)
        {
            if (fixtureA.Body == mBody)
            {
                mColor = Color.White;
            }
        }
        #endregion
    }
}
