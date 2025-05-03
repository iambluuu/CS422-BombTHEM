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

        // Buffer for receiving data
        private byte[] _receiveBuffer = new byte[4096];
        private MemoryStream _messageBuffer = new MemoryStream();

        // Maximum allowed message size to prevent DoS attacks
        private const int MaxMessageSize = 1024 * 1024; // 1MB

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
                _messageBuffer = new MemoryStream();
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
            try {
                while (!ct.IsCancellationRequested && _connected) {
                    int bytesRead = 0;

                    try {
                        var readTask = _stream.ReadAsync(_receiveBuffer, 0, _receiveBuffer.Length, ct);
                        bytesRead = await readTask;
                    } catch (Exception ex) {
                        Console.WriteLine($"Error reading from stream: {ex.Message}");
                        break;
                    }

                    if (bytesRead <= 0) {
                        Console.WriteLine("Server closed connection");
                        break;
                    }

                    _messageBuffer.Write(_receiveBuffer, 0, bytesRead);

                    ProcessMessageBuffer();
                }
            } catch (Exception ex) {
                Console.WriteLine($"Unexpected error in listener: {ex.Message}");
            } finally {
                Disconnect();
            }
        }

        private void ProcessMessageBuffer() {
            _messageBuffer.Position = 0;

            long processedPosition = 0;
            byte[] bufferData = _messageBuffer.ToArray();

            while (true) {
                int delimiterIndex = -1;
                for (int i = (int)processedPosition; i < bufferData.Length; i++) {
                    if (bufferData[i] == '|') {
                        delimiterIndex = i;
                        break;
                    }
                }

                if (delimiterIndex == -1) break;

                int messageSize = delimiterIndex - (int)processedPosition;

                if (messageSize > MaxMessageSize) {
                    Console.WriteLine($"Message size exceeds maximum allowed ({messageSize} > {MaxMessageSize})");
                    _messageBuffer = new MemoryStream();
                    return;
                }

                byte[] messageData = new byte[messageSize];
                Array.Copy(bufferData, processedPosition, messageData, 0, messageSize);
                string messageString = Encoding.UTF8.GetString(messageData);

                try {
                    NetworkMessage messageObj = NetworkMessage.FromJson(messageString);

                    if (messageObj.Type.Direction != MessageDirection.Server) {
                        Console.WriteLine("The received message must be from the server side");
                    } else {
                        Handlers?.Invoke(messageObj);
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error processing message: {ex.Message}");
                }
                processedPosition = delimiterIndex + 1;
            }

            if (processedPosition >= bufferData.Length) {
                _messageBuffer = new MemoryStream();
            } else if (processedPosition > 0) {
                byte[] remainingData = new byte[bufferData.Length - processedPosition];
                Array.Copy(bufferData, processedPosition, remainingData, 0, remainingData.Length);
                _messageBuffer = new MemoryStream(remainingData);
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
                    Disconnect();
                }
            } else {
                Console.WriteLine("Not connected to server");
            }
        }

        public void Disconnect() {
            if (!_connected) {
                return;
            }

            _connected = false;

            if (_listenThread?.IsAlive == true) {
                _listenCts?.Cancel();
                _listenThread?.Join(1000);
            }

            _stream?.Close();
            _client?.Close();
            _client = null;
            _messageBuffer?.Dispose();

            Console.WriteLine("Disconnected from server");
        }
    }
}