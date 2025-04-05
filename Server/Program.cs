using System;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            // Start the server
            Console.WriteLine("Starting Server...");
            Server gameServer = new Server();
            gameServer.Start();
        }
    }
}
