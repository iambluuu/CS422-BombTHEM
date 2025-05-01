using Client.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;
using SharpDX.DXGI;

namespace Client {
    public class MainGame : Game {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private ScreenManager _screenManager;

        public static MainGame Instance { get; private set; }

        public MainGame() {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferHeight = 800;
            _graphics.PreferredBackBufferWidth = 1200;
            _graphics.ApplyChanges();
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize() {
            base.Initialize();
            Instance = this;
            _screenManager = new ScreenManager(this);
            _screenManager.NavigateTo(ScreenName.MainMenu);

        }

        protected override void Update(GameTime gameTime) {
            _screenManager.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _screenManager.Draw(gameTime);
            base.Draw(gameTime);
        }

        protected override void LoadContent() {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            TextureHolder.SetContentManager(Content);
            FontHolder.SetContentManager(Content);
            ConnectionManager.Instance.Connect("localhost", 5000);
        }

        protected override void UnloadContent() {
            TextureHolder.UnloadAll();
            FontHolder.UnloadAll();
            ConnectionManager.Instance.Disconnect();
        }
    }
}