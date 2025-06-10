using System;
using Microsoft.Xna.Framework;

using Client.Screen;
using Client.ContentHolder;
using Client.Audio;
using Client.Network;

namespace Client {
    public class Client : Game {
        private GraphicsDeviceManager _graphics;
        private ScreenManager _screenManager;

        public static Client Instance { get; private set; }
        public static Vector2 VirtualScreenSize => new(960, 720);
        public static Vector2 CurrentScreenSize => new(Instance.Window.ClientBounds.Width, Instance.Window.ClientBounds.Height);
        public static float ScreenScaleFactor => Math.Min(CurrentScreenSize.X / VirtualScreenSize.X, CurrentScreenSize.Y / VirtualScreenSize.Y);

        public Client() {
            _graphics = new GraphicsDeviceManager(this) {
                PreferredBackBufferWidth = (int)VirtualScreenSize.X,
                PreferredBackBufferHeight = (int)VirtualScreenSize.Y,
                PreferMultiSampling = false
            };
            _graphics.ApplyChanges();
            Window.AllowUserResizing = true;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize() {
            base.Initialize();

            Instance = this;

            MusicPlayer.Initialize();
            _screenManager = new ScreenManager(this);
            _screenManager.NavigateTo(ScreenName.MainMenu);
        }

        protected override void Update(GameTime gameTime) {
            _screenManager.IsFocused = IsActive;
            _screenManager.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            _screenManager.Draw(gameTime);
            base.Draw(gameTime);
        }

        protected override void LoadContent() {
            TextureHolder.SetContentManager(Content);
            FontHolder.SetContentManager(Content);
            AudioHolder.SetContentManager(Content);
        }

        protected override void UnloadContent() {
            TextureHolder.UnloadAll();
            FontHolder.UnloadAll();
            AudioHolder.UnloadAll();
            NetworkManager.PrintMessageSize();
            NetworkManager.Instance.Disconnect();
        }
    }
}
