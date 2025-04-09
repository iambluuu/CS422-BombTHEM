using System;

namespace Client {
    public static class Program {
        [STAThread]
        static void Main() {
            using var game = new ClientGame();
            game.Run();
        }
    }
}
