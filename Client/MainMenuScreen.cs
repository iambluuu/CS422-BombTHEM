using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Client.Component;
using System;

namespace Client {
    public class MainMenuScreen : GameScreen {
        public override void Initialize() {
            // Initialize your main menu components here
            var layout = new LinearLayout(LinearLayout.Orientation.Vertical, new List<IComponent>(), spacing: 30) {
                Position = new Vector2(50, 50),
                Size = new Vector2(300, 400),
                Padding = 30,
            };
            layout.Center(new Rectangle(0, 0, MainGame.Instance.GraphicsDevice.Viewport.Width, MainGame.Instance.GraphicsDevice.Viewport.Height));
            var createGameButton = new Button(position: new Vector2(0, 0), size: new Vector2(100, 200), onClick: CreateGame) {
                Text = "Create Game",
                Font = content.Load<SpriteFont>("Font/NormalFont"),
            };
            var exitButton = new Button(position: new Vector2(0, 0), size: new Vector2(0, 0), onClick: ExitGame) {
                Text = "Exit",
                Font = content.Load<SpriteFont>("Font/NormalFont"),
            };
            var joinGameButton = new Button(position: new Vector2(0, 0), size: new Vector2(100, 200), onClick: JoinGame) {
                Text = "Join Game",
                Font = content.Load<SpriteFont>("Font/NormalFont"),
            };

            layout.AddComponent(createGameButton);
            layout.AddComponent(joinGameButton);
            layout.AddComponent(exitButton);
            uiManager.AddComponent(layout, 0);
        }

        private void CreateGame() {
            // Logic to start the game
            Console.WriteLine("Creating game...");
        }

        private void ExitGame() {
            // Logic to exit the game
            Console.WriteLine("Exiting game...");
        }

        private void JoinGame() {
            // Logic to join a game
            Console.WriteLine("Joining game...");
            ScreenManager.Instance.NavigateTo(ScreenName.JoinGameScreen);
        }

        public override void Update(GameTime gameTime) {
            // Handle input and update UI components
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch) {
            // Draw the main menu components
            base.Draw(gameTime, spriteBatch);
        }

        public override void LoadContent() {
            base.LoadContent();
            TextureHolder.SetContentManager(content);
        }
    }
}