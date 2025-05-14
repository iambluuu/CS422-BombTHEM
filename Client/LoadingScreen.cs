using Microsoft.Xna.Framework;

using Client.Component;

namespace Client {
    class LoadingScreen : GameScreen {
        private string _message;
        private ImageView _catImage;
        private TextView _loadingText;

        private readonly float _dotTime = 0.2f;
        private float _currentDotTime = float.MaxValue;
        private int _dotIndex = 0;
        private readonly int _totalDots = 3;

        private readonly float _frameTime = 0.1f;
        private float _currentFrameTime = float.MaxValue;
        private int _currentFrame = 0;
        private readonly int _totalFrames = 2;

        public override void Initialize() {
            var layout = new LinearLayout() {
                LayoutOrientation = Orientation.Vertical,
                Width = ScreenSize.X,
                Height = ScreenSize.Y,
                Gravity = Gravity.Center,
            };
            uiManager.AddComponent(layout, 0);

            var mainLayout = new LinearLayout() {
                LayoutOrientation = Orientation.Vertical,
                HeightMode = SizeMode.WrapContent,
                WidthMode = SizeMode.WrapContent,
                Gravity = Gravity.Center,
            };
            layout.AddComponent(mainLayout);

            _catImage = new ImageView() {
                Texture = TextureHolder.Get("Animal/Cat", new Rectangle(0, 0, 16, 16)),
                Width = 100,
                Height = 100,
                Gravity = Gravity.Center,
                ScaleType = ScaleType.FitCenter,
            };
            // mainLayout.AddComponent(_catImage);

            _loadingText = new TextView() {
                WidthMode = SizeMode.WrapContent,
                HeightMode = SizeMode.WrapContent,
                Text = "",
                TextColor = Color.White,
                Gravity = Gravity.Center,
            };
            mainLayout.AddComponent(_loadingText);
        }

        public void SetMessage(string message) {
            _message = message;
        }

        public override void Update(GameTime gameTime) {
            _currentDotTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_currentDotTime >= _dotTime) {
                _dotIndex = (_dotIndex + 1) % (_totalDots + 1);
                _loadingText.Text = _message + new string('.', _dotIndex) + new string(' ', _totalDots - _dotIndex);
                _currentDotTime = 0f;
            }

            _currentFrameTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_currentFrameTime >= _frameTime) {
                _catImage.Texture = TextureHolder.Get("Animal/Cat", new Rectangle(_currentFrame * 16, 0, 16, 16));
                _currentFrame = (_currentFrame + 1) % _totalFrames;
                _currentFrameTime = 0f;
            }
        }
    }
}