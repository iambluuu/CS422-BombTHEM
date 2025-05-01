using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Shared;

namespace Client {
    public class NetworkManager {
        private static NetworkManager _instance;
        public static NetworkManager Instance => _instance ??= new NetworkManager();

        private int _clientId = -1;
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _connected = false;
        private event Action<NetworkMessage> Handlers;

        public int ClientId {
            get {
                return _clientId;
            }
            set {
                if (_clientId == -1) {
                    _clientId = value;
                } else {
                    Console.WriteLine("Client ID already set");
                }
            }
        }

        private NetworkManager() { }

        public void Connect(string ip, int port) {
            try {
                Console.WriteLine($"Connecting to {ip}:{port}");
                _client = new TcpClient();
                _client.Connect(ip, port);
                _stream = _client.GetStream();
                _connected = true;
                Console.WriteLine("Connected to server");
            } catch (Exception ex) {
                Console.WriteLine($"Error connecting to server: {ex.Message}");
                return;
            }

            new Thread(StartListening) { IsBackground = true }.Start();
        }

        private void StartListening() {
            byte[] buffer = new byte[1024];

            try {
                while (true) {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0) {
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string[] messages = message.Split(['\n', '\r', '|'], StringSplitOptions.RemoveEmptyEntries);

                    foreach (var msg in messages) {
                        if (string.IsNullOrEmpty(msg)) {
                            continue;
                        }

                        // Console.WriteLine($"Received message from server: {msg}");

                        NetworkMessage messageObj = NetworkMessage.FromJson(msg);
                        if (messageObj.Type.Direction != MessageDirection.Server) {
                            Console.WriteLine("The received message must be from the server side");
                            continue;
                        }

                        try {
                            Handlers?.Invoke(messageObj);
                        } catch (Exception ex) {
                            Console.WriteLine($"Error invoking handler: {ex.Message}");
                        }
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error receiving message: {ex.Message}");
            } finally {
                Disconnect();
            }
        }

        public void InsertHandler(Action<NetworkMessage> handler) {
            if (Handlers == null || !Handlers.GetInvocationList().Contains(handler)) {
                Handlers += handler;
            } else {
                Console.WriteLine("Handler already registered");
            }
        }

        public void RemoveHandler(Action<NetworkMessage> handler) {
            if (Handlers != null && Handlers.GetInvocationList().Contains(handler)) {
                Handlers -= handler;
            } else {
                Console.WriteLine("Handler not found");
            }
        }

        public void Send(NetworkMessage message) {
            if (message.Type.Direction != MessageDirection.Client) {
                Console.WriteLine("The sent message must be from the client side");
                return;
            }

            if (_connected) {
                try {
                    byte[] data = Encoding.UTF8.GetBytes(message.ToJson() + "|");
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush();
                } catch (Exception ex) {
                    Console.WriteLine($"Error sending message: {ex.Message}");
                }
            } else {
                Console.WriteLine("Not connected to server");
            }
        }

        public void Disconnect() {
            if (!_connected) {
                return;
            }

            _client = null;
            _stream?.Close();
            _client?.Close();
            _connected = false;

            Console.WriteLine("Disconnected from server");
        }
    }
}