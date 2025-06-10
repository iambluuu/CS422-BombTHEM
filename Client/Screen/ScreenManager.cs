using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Shared;
using Client.Component;
using Client.ContentHolder;
using Client.Network;

namespace Client.Screen {
    public enum ScreenName {
        MainMenu,
        LobbyScreen,
        JoinGameScreen,
        MainGameScreen,
        EndGameScreen,
        PauseMenu,
        SettingsMenu
    }

    public class ScreenManager : DrawableGameComponent {
        private readonly Stack<GameScreen> screenStack = new Stack<GameScreen>();
        private RenderTarget2D _snapshotScreen, _virtualScreen;
        private readonly SpriteBatch spriteBatch;
        private LoadingScreen _loadingScreen;
        private ImageView _backgroundImage;

        public bool IsFocused { get; set; } = true;
        public bool IsLoading => _loadingScreen.IsLoading;

        public static ScreenManager Instance { get; private set; }

        public ScreenManager(Game game) : base(game) {
            Instance = this;
            spriteBatch = new SpriteBatch(game.GraphicsDevice);

            _loadingScreen = new LoadingScreen();
            _loadingScreen.LoadContent();
            _loadingScreen.Initialize();

            _backgroundImage = new ImageView() {
                Texture = TextureHolder.Get("Theme/Background"),
                Gravity = Gravity.Center,
                ScaleType = ScaleType.CenterCrop,
            };

            _snapshotScreen = new RenderTarget2D(GraphicsDevice, (int)Client.VirtualScreenSize.X, (int)Client.VirtualScreenSize.Y);
            _virtualScreen = new RenderTarget2D(GraphicsDevice, (int)Client.VirtualScreenSize.X, (int)Client.VirtualScreenSize.Y);
        }

        public override void Update(GameTime gameTime) {
            var screens = screenStack.ToArray();
            int index = 0;
            for (int i = 0; i < screens.Length; i++) {
                var screen = screens[i];
                if (screen.IsExclusive) {
                    index = i;
                    break;
                }
            }

            for (int i = index; i >= 0; i--) {
                var screen = screens[i];
                screen.IsFocused = IsFocused && !IsLoading;
                if (screen.IsActive) {
                    screen.Update(gameTime);
                }
            }

            ToastManager.Instance.Update(gameTime);

            _loadingScreen.Update(gameTime);

            _backgroundImage.Width = Client.CurrentScreenSize.X;
            _backgroundImage.Height = Client.CurrentScreenSize.Y;
            _backgroundImage.Update(gameTime);
        }

        public override void Draw(GameTime gameTime) {
            if (!IsLoading) {
                GraphicsDevice.SetRenderTarget(_snapshotScreen);
                GraphicsDevice.Clear(new Color(255, 173, 93, 255));

                spriteBatch.Begin(samplerState: SamplerState.PointClamp);

                var screens = screenStack.ToArray();
                int index = 0;
                for (int i = 0; i < screens.Length; i++) {
                    var screen = screens[i];
                    if (screen.IsExclusive) {
                        index = i;
                        break;
                    }
                }

                for (int i = index; i >= 0; i--) {
                    var screen = screens[i];
                    if (screen.IsVisible) {
                        screen.Draw(gameTime, spriteBatch);
                    }
                }

                ToastManager.Instance.Draw(spriteBatch);

                spriteBatch.End();
            }

            GraphicsDevice.SetRenderTarget(_virtualScreen);
            GraphicsDevice.Clear(new Color(255, 173, 93, 255));
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(_snapshotScreen, Vector2.Zero, Color.White);
            _loadingScreen.Draw(gameTime, spriteBatch);
            spriteBatch.End();

            int scaledWidth = (int)(Client.VirtualScreenSize.X * Client.ScreenScaleFactor);
            int scaledHeight = (int)(Client.VirtualScreenSize.Y * Client.ScreenScaleFactor);

            int xOffset = ((int)Client.CurrentScreenSize.X - scaledWidth) / 2;
            int yOffset = ((int)Client.CurrentScreenSize.Y - scaledHeight) / 2;

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(new Color(255, 173, 93, 255));
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _backgroundImage.Draw(spriteBatch);
            spriteBatch.Draw(_virtualScreen, new Rectangle(xOffset, yOffset, scaledWidth, scaledHeight), Color.White);
            spriteBatch.End();
        }

        public void NavigateTo(ScreenName screenType, bool isOverlay = false, Dictionary<string, object> parameters = null) {
            GameScreen screen;
            screen = CreateScreen(screenType);
            screen.IsExclusive = !isOverlay;
            PushScreen(screen, parameters);
        }

        private GameScreen CreateScreen(ScreenName screenType) {
            return screenType switch {
                ScreenName.MainMenu => new MainMenuScreen(),
                ScreenName.LobbyScreen => new LobbyScreen(),
                ScreenName.JoinGameScreen => new JoinGameScreen(),
                ScreenName.MainGameScreen => new MainGameScreen(),
                ScreenName.EndGameScreen => new EndGameScreen(),
                _ => throw new ArgumentException($"Unknown screen type: {screenType}"),
            };
        }

        public void NavigateBack() {
            if (screenStack.Count > 1) {
                PopScreen();
            }
        }

        public void NavigateBackTo(ScreenName screenType) {
            while (screenStack.Count > 0) {
                var currentScreen = screenStack.Peek();
                if (currentScreen.GetType() == CreateScreen(screenType).GetType()) {
                    break;
                }
                PopScreen();
            }
        }

        public void NavigateToRoot() {
            while (screenStack.Count > 1) {
                PopScreen();
            }
        }

        private void PushScreen(GameScreen screen, Dictionary<string, object> parameters = null) {
            if (screenStack.Count > 0) {
                var currentScreen = screenStack.Peek();
                currentScreen.Deactivate();
            }

            if (!screen.IsInitialized) {
                screen.ScreenManager = this;
                screen.LoadContent();
                screen.Initialize();
                screen.IsInitialized = true;
            }

            screen.Activate();
            screen.LoadParameters(parameters);
            screenStack.Push(screen);
        }

        private void PopScreen() {
            if (screenStack.Count > 0) {
                var screen = screenStack.Pop();
                screen.Deactivate();
                screen.IsVisible = false;
                if (screenStack.Count > 0) {
                    var currentScreen = screenStack.Peek();
                    currentScreen.Activate();
                }
            }
        }

        public void StartLoading() {
            _loadingScreen.RequestClose();
        }

        public void StopLoading() {
            _loadingScreen.RequestOpen();
        }

        public SpriteBatch SpriteBatch => spriteBatch;
    }

    public abstract class GameScreen {
        protected UIManager uiManager = new();

        private KeyboardState previousKeyboardState;
        private MouseState previousMouseState;

        public ScreenManager ScreenManager { get; set; }
        public bool IsExclusive { get; set; } = false;
        public bool IsVisible { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public bool IsFocused { get; set; } = true;
        public bool IsInitialized { get; set; } = false;

        public static Vector2 ScreenSize => Client.VirtualScreenSize;

        public virtual void Initialize() { }

        public virtual void Activate() {
            IsActive = true;
            IsVisible = true;
            Client.Instance.Window.TextInput += DispatchTextInput;
            NetworkManager.Instance.InsertHandler(HandleResponse);
        }

        public virtual void Deactivate() {
            IsActive = false;
            Client.Instance.Window.TextInput -= DispatchTextInput;
            NetworkManager.Instance.RemoveHandler(HandleResponse);
        }

        public virtual void LoadParameters(Dictionary<string, object> parameters) { }

        public virtual void HandleResponse(NetworkMessage message) {
            switch ((ServerMessageType)message.Type.Name) {
                case ServerMessageType.NotConnected: {
                        ScreenManager.Instance.NavigateToRoot();
                    }
                    break;
            }
        }

        public virtual void LoadContent() { }

        public virtual void UnloadContent() { }

        public virtual void Update(GameTime gameTime) {
            if (!IsActive) return;

            MouseState mouseState = Mouse.GetState();
            KeyboardState keyboardState = Keyboard.GetState();

            if (IsFocused) {
                bool ctrlHeld = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
                if (mouseState.Position != previousMouseState.Position) {
                    UIEvent mouseMoveEvent = new UIEvent(UIEventType.MouseMove, mousePosition: mouseState.Position);
                    uiManager.DispatchEvent(mouseMoveEvent);
                }
                if (mouseState.LeftButton == ButtonState.Released && previousMouseState.LeftButton == ButtonState.Pressed) {
                    UIEvent clickEvent = new UIEvent(UIEventType.MouseUp, mousePosition: mouseState.Position);
                    uiManager.DispatchEvent(clickEvent);
                }
                if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released) {
                    UIEvent mouseDownEvent = new UIEvent(UIEventType.MouseDown, mousePosition: mouseState.Position);
                    uiManager.DispatchEvent(mouseDownEvent);
                }
                Keys[] currentKeys = keyboardState.GetPressedKeys();
                Keys[] previousKeys = previousKeyboardState.GetPressedKeys();

                foreach (Keys key in currentKeys) {
                    if (!previousKeys.Contains(key)) {
                        UIEvent keyPressEvent = new UIEvent(UIEventType.KeyPress, key: key, ctrlDown: ctrlHeld);
                        uiManager.DispatchEvent(keyPressEvent);
                    }
                }
            }

            uiManager.Update(gameTime);
            previousMouseState = mouseState;
            previousKeyboardState = keyboardState;
        }

        private void DispatchTextInput(object sender, TextInputEventArgs e) {
            UIEvent textInputEvent = new(UIEventType.TextInput, character: e.Character);
            uiManager.DispatchEvent(textInputEvent);
        }

        public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch) {
            if (!IsVisible) return;
            uiManager.Draw(spriteBatch);
        }
    }
}