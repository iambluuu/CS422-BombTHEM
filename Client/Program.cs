using System;
using System.Windows.Forms;

namespace Client {
    public static class Program {
        [STAThread]
        static void Main() {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            using var game = new Client();
            game.Run();
        }
    }
}
