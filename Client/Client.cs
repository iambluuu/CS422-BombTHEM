using Client.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;
using SharpDX.DXGI;

namespace Client {
    public class Client : Game {
        private GraphicsDeviceManager _graphics;
        private ScreenManager _screenManager;

        public static Client Instance { get; private set; }

        public Client() {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.PreferredBackBufferWidth = 960;
            _graphics.PreferMultiSampling = false;
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
            _screenManager.IsFocused = IsActive;
            _screenManager.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(new Color(255, 173, 93, 255));
            _screenManager.Draw(gameTime);
            base.Draw(gameTime);
        }

        protected override void LoadContent() {
            TextureHolder.SetContentManager(Content);
            FontHolder.SetContentManager(Content);
            NetworkManager.Instance.Connect("localhost", 5000);
        }

        protected override void UnloadContent() {
            TextureHolder.UnloadAll();
            FontHolder.UnloadAll();
            NetworkManager.Instance.Disconnect();
        }
    }
}
