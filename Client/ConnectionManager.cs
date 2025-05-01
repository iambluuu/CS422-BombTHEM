using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Shared;

namespace Client {
    public class ConnectionManager {
        private static ConnectionManager _instance;
        public static ConnectionManager Instance => _instance ??= new ConnectionManager();

        private TcpClient _client;
        private NetworkStream _stream;
        private bool _connected = false;

        public event Action<NetworkMessage> MessageReceived;

        private ConnectionManager() { }

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

            while (_connected) {
                try {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0) {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        string[] messages = message.Split(['\n', '\r', '|'], StringSplitOptions.RemoveEmptyEntries);

                        foreach (var msg in messages) {
                            if (!string.IsNullOrEmpty(msg)) {
                                MessageReceived?.Invoke(NetworkMessage.FromJson(msg));
                            }
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    _connected = false;
                    break;
                }
            }
        }

        public void InsertHandler(Action<NetworkMessage> handler) {
            if (MessageReceived == null || !MessageReceived.GetInvocationList().Contains(handler)) {
                MessageReceived += handler;
                Console.WriteLine($"Handler registered: {handler.Method.Name}");
            } else {
                Console.WriteLine("Handler already registered");
            }
        }

        public void RemoveHandler(Action<NetworkMessage> handler) {
            if (MessageReceived != null && MessageReceived.GetInvocationList().Contains(handler)) {
                MessageReceived -= handler;
                Console.WriteLine($"Handler unregistered: {handler.Method.Name}");
            } else {
                Console.WriteLine("Handler not found");
            }
        }

        public void Send(NetworkMessage message) {
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