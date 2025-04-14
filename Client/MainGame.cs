using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client {
    public class MainGame : Game {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private ScreenManager _screenManager;

        public static MainGame Instance { get; private set; }

        public const int ScreenWidth = 1280;
        public const int ScreenHeight = 720;
        public const int ScreenWidthHalf = ScreenWidth / 2;
        public const int ScreenHeightHalf = ScreenHeight / 2;

        public MainGame() {
            _graphics = new GraphicsDeviceManager(this);
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
            // Load your game content here
        }
    }
}