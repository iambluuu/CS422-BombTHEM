using Client.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Shared;

namespace Client {
    public enum ScreenName {
        MainMenu,
        LobbyScreen,
        JoinGameScreen,
        MainGameScreen,
        PauseMenu,
        SettingsMenu
    }

    public class ScreenManager : DrawableGameComponent {
        private Stack<GameScreen> screenStack = new Stack<GameScreen>();
        private List<GameScreen> screensToUpdate = new List<GameScreen>();
        private Dictionary<ScreenName, GameScreen> screenCache = new Dictionary<ScreenName, GameScreen>();

        private ContentManager content;
        private SpriteBatch spriteBatch;

        public bool IsFocused { get; set; } = true;

        public static ScreenManager Instance { get; private set; }

        public ScreenManager(Game game) : base(game) {
            Instance = this;
            content = new ContentManager(game.Services, "Content");
            spriteBatch = new SpriteBatch(game.GraphicsDevice);
        }

        public override void Update(GameTime gameTime) {
            // Copy screens to a temporary list to avoid modification issues during update
            screensToUpdate.Clear();
            foreach (var screen in screenStack)
                screensToUpdate.Add(screen);

            // Update screens in order (from bottom to top of stack)
            // Reversed because stack has most recent screen at index 0
            for (int i = screensToUpdate.Count - 1; i >= 0; i--) {
                var screen = screensToUpdate[i];
                screen.IsFocused = IsFocused;
                screen.Update(gameTime);
            }
        }

        public override void Draw(GameTime gameTime) {
            var screens = screenStack.ToArray();
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            for (int i = screens.Length - 1; i >= 0; i--) {
                var screen = screens[i];
                if (screen.IsVisible)
                    screen.Draw(gameTime, spriteBatch);
            }
            spriteBatch.End();
        }

        // Navigate to a screen using an enum
        public void NavigateTo(ScreenName screenType, bool isOverlay = false) {
            GameScreen screen;

            // Check if screen exists in cache
            // if (screenCache.ContainsKey(screenType)) {
            //     screen = screenCache[screenType];
            // } else {
            // Create new screen based on enum type
            screen = CreateScreen(screenType);
            screenCache[screenType] = screen;
            // }

            // Set whether this screen should overlay or replace current screens
            screen.IsExclusive = !isOverlay;

            PushScreen(screen);
        }

        // Create screen instance based on enum type
        private GameScreen CreateScreen(ScreenName screenType) {
            switch (screenType) {
                case ScreenName.MainMenu:
                    return new MainMenuScreen();
                case ScreenName.LobbyScreen:
                    return new LobbyScreen();
                case ScreenName.JoinGameScreen:
                    return new JoinGameScreen();
                case ScreenName.MainGameScreen:
                    return new MainGameScreen();
                default:
                    throw new ArgumentException($"Unknown screen type: {screenType}");
            }
        }

        public void NavigateBack() {
            if (screenStack.Count > 1) {
                PopScreen();
            }
        }

        public void NavigateToRoot() {
            while (screenStack.Count > 1) {
                PopScreen();
            }
        }

        private void PushScreen(GameScreen screen) {
            // Optionally deactivate the current top screen
            if (screenStack.Count > 0) {
                var currentScreen = screenStack.Peek();
                if (screen.IsExclusive) {
                    currentScreen.IsVisible = false;
                    currentScreen.Deactivate();
                }
            }

            // Only initialize if it's a new screen
            if (!screen.IsInitialized) {
                screen.ScreenManager = this;
                screen.LoadContent();
                screen.Initialize();
                screen.IsInitialized = true;
            }
            screen.Activate();
            screenStack.Push(screen);
        }

        private void PopScreen() {
            if (screenStack.Count > 0) {
                var screen = screenStack.Pop();
                screen.Deactivate();
                screen.IsVisible = false;


                // Don't unload content if the screen is cached
                // Content will be unloaded when the game exits or explicitly

                // Reactivate the new top screen if there is one
                if (screenStack.Count > 0) {
                    var currentScreen = screenStack.Peek();
                    currentScreen.Activate();
                }
            }
        }

        // Call this when exiting the game or when you need to free up memory
        public void UnloadScreen(ScreenName screenType) {
            if (screenCache.TryGetValue(screenType, out GameScreen screen)) {
                screen.UnloadContent();
                screenCache.Remove(screenType);
            }
        }

        public void UnloadAllScreens() {
            foreach (var screen in screenCache.Values) {
                screen.UnloadContent();
            }
            screenCache.Clear();
            screenStack.Clear();
        }

        public ContentManager Content => content;
        public SpriteBatch SpriteBatch => spriteBatch;
    }

    public abstract class GameScreen {
        protected UIManager uiManager = new();

        private KeyboardState previousKeyboardState;
        private MouseState previousMouseState;

        public ScreenManager ScreenManager { get; set; }
        public bool IsExclusive { get; set; } = false; // If true, screens below won't be visible/active
        public bool IsVisible { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public bool IsFocused { get; set; } = true;
        public bool IsInitialized { get; set; } = false;

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

        public virtual void HandleResponse(NetworkMessage message) { }

        public virtual void LoadContent() { }

        public virtual void UnloadContent() { }

        public virtual void Update(GameTime gameTime) {
            if (!IsActive || !IsFocused) return;

            MouseState mouseState = Mouse.GetState();
            KeyboardState keyboardState = Keyboard.GetState();

            if (mouseState.Position != previousMouseState.Position) {
                // Handle mouse movement event
                UIEvent mouseMoveEvent = new UIEvent(UIEventType.MouseMove, mousePosition: mouseState.Position);
                uiManager.DispatchEvent(mouseMoveEvent);
            }

            // Handle Click
            if (mouseState.LeftButton == ButtonState.Released) {
                // Handle click event
                UIEvent clickEvent = new UIEvent(UIEventType.MouseUp, mousePosition: mouseState.Position);
                uiManager.DispatchEvent(clickEvent);
            }

            // Handle Mouse Down
            if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released) {
                // Handle mouse down event
                UIEvent mouseDownEvent = new UIEvent(UIEventType.MouseDown, mousePosition: mouseState.Position);
                uiManager.DispatchEvent(mouseDownEvent);
            }

            // Handle Key Press
            if (keyboardState.GetPressedKeys().Length > 0 && previousKeyboardState.GetPressedKeys().Length == 0) {
                // Handle key press event
                UIEvent keyPressEvent = new UIEvent(UIEventType.KeyPress, key: keyboardState.GetPressedKeys()[0]);
                uiManager.DispatchEvent(keyPressEvent);
            }

            // Update UI components
            uiManager.Update(gameTime);

            // Update previous states
            previousMouseState = mouseState;
            previousKeyboardState = keyboardState;
        }

        private void DispatchTextInput(object sender, TextInputEventArgs e) {
            // Handle text input event
            UIEvent textInputEvent = new(UIEventType.TextInput, character: e.Character);
            uiManager.DispatchEvent(textInputEvent);
        }

        public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch) {
            if (!IsVisible) return;
            uiManager.Draw(spriteBatch);
        }
    }
}