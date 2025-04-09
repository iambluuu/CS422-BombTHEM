namespace Server {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Starting Server...");
            Server gameServer = new Server();
            gameServer.Start();
        }
    }
}
