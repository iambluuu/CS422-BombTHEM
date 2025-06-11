namespace Server {
    class Program {
        static void Main(string[] args) {
            Server gameServer = new();
            Console.CancelKeyPress += (sender, e) => {
                Console.WriteLine("Shutting down server...");
                // gameServer.PrintNetworkStats();
            };
            gameServer.Start();
        }
    }
}
