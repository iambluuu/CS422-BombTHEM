using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shared;

namespace Client {
    public class NetworkManager {
        private static NetworkManager _instance;
        public static NetworkManager Instance => _instance ??= new NetworkManager();

        private int _clientId = -1;
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _listenThread;
        private CancellationTokenSource _listenCts;
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

            _listenCts = new CancellationTokenSource();
            _listenThread = new Thread(() => StartListening(_listenCts.Token)) {
                IsBackground = true,
            };
            _listenThread.Start();
        }

        private async void StartListening(CancellationToken ct) {
            byte[] buffer = new byte[1024];

            try {
                while (!ct.IsCancellationRequested) {
                    int bytesRead = 0;

                    try {
                        var readTask = _stream.ReadAsync(buffer, 0, buffer.Length, ct);
                        bytesRead = await readTask;
                    } catch {
                        break;
                    }

                    if (bytesRead <= 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string[] messages = message.Split(['\n', '\r', '|'], StringSplitOptions.RemoveEmptyEntries);

                    foreach (var msg in messages) {
                        if (string.IsNullOrEmpty(msg)) continue;

                        try {
                            NetworkMessage messageObj = NetworkMessage.FromJson(msg);

                            if (messageObj.Type.Direction != MessageDirection.Server) {
                                Console.WriteLine("The received message must be from the server side");
                                continue;
                            }

                            Handlers?.Invoke(messageObj);
                        } catch (Exception ex) {
                            Console.WriteLine($"Error invoking handler: {ex.StackTrace}");
                        }
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Unexpected error in listener: {ex.Message}");
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

            if (_listenThread.IsAlive == true) {
                _listenCts.Cancel();
                _listenThread.Join();
            }

            _stream?.Close();
            _client?.Close();
            _client = null;
            _connected = false;

            Console.WriteLine("Disconnected from server");
        }
    }
}