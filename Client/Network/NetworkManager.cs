using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using Shared;

namespace Client.Network {
    public class NetworkManager {
        private static NetworkManager _instance;
        public static NetworkManager Instance => _instance ??= new NetworkManager();
        private static readonly object _sendlock = new object();
        private static readonly object _receiveLock = new object();
        private static Dictionary<string, (int, float)> _sentMessageAverageSize = new Dictionary<string, (int, float)>();
        private static Dictionary<string, (int, float)> _receivedMessageAverageSize = new Dictionary<string, (int, float)>();

        private int _clientId = -1;
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _listenThread;
        private CancellationTokenSource _listenCts;
        private bool _connected = false;
        private event Action<NetworkMessage> Handlers;

        private DateTime _lastPing;
        private bool _pingReceived = true;
        private System.Timers.Timer _pingTimer;

        private byte[] _receiveBuffer = new byte[4096];
        private MemoryStream _messageBuffer = new MemoryStream();

        private const int MaxMessageSize = 1024 * 1024;

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

        public int Ping { get; private set; } = 0;
        public bool IsConnected => _connected;

        private NetworkManager() { }

        public void Connect(string ip, int port) {
            new Thread(() => {
                if (_connected) {
                    Console.WriteLine("Already connected to server");
                    return;
                }

                try {
                    Console.WriteLine($"Connecting to {ip}:{port}");
                    _client = new TcpClient();
                    _client.Connect(ip, port);
                    _stream = _client.GetStream();
                    _connected = true;
                    _messageBuffer = new MemoryStream();
                    Console.WriteLine("Connected to server");
                } catch (Exception ex) {
                    Handlers?.Invoke(NetworkMessage.From(ServerMessageType.NotConnected));
                    Console.WriteLine($"Error connecting to server: {ex.Message}");
                    return;
                }

                _listenCts = new CancellationTokenSource();
                _listenThread = new Thread(() => StartListening(_listenCts.Token)) {
                    IsBackground = true,
                };
                _listenThread.Start();

                _pingTimer = new System.Timers.Timer(5000) {
                    AutoReset = true,
                    Enabled = true
                };
                _pingTimer.Elapsed += (sender, e) => {
                    if (_pingReceived) {
                        _lastPing = DateTime.Now;
                        Send(NetworkMessage.From(ClientMessageType.Ping));
                        _pingReceived = false;
                    }
                };
                _pingTimer.Start();

                Handlers?.Invoke(NetworkMessage.From(ServerMessageType.Connected));
            }) {
                IsBackground = true,
            }.Start();
        }

        private async void StartListening(CancellationToken ct) {
            try {
                while (!ct.IsCancellationRequested && _connected) {
                    int bytesRead = 0;

                    try {
                        var readTask = _stream.ReadAsync(_receiveBuffer, 0, _receiveBuffer.Length, ct);
                        bytesRead = await readTask;
                    } catch (Exception ex) {
                        if (!ct.IsCancellationRequested && _connected) {
                            Console.WriteLine($"Error reading from stream: {ex.Message}");
                        }
                        break;
                    }

                    if (bytesRead <= 0) {
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

                // NetworkMessage messageObj = NetworkMessage.FromJson(messageString);
                NetworkMessage messageObj = NetworkMessage.FromBytes(messageData);
                lock (_receiveLock) {
                    string messageType = ((ServerMessageType)messageObj.Type.Name).ToString();
                    if (_receivedMessageAverageSize.TryGetValue(messageType, out var sizeInfo)) {
                        _receivedMessageAverageSize[messageType] = (sizeInfo.Item1 + 1, sizeInfo.Item2 + messageSize);
                    } else {
                        _receivedMessageAverageSize[messageType] = (1, messageSize);
                    }
                }
                // Console.WriteLine($"Received message from server: {messageString}");

                try {

                    if (messageObj.Type.Direction != MessageDirection.Server) {
                        Console.WriteLine("The received message must be from the server side");
                    } else {
                        if (messageObj.Type.Name == (byte)ServerMessageType.Pong) {
                            _pingReceived = true;
                            DateTime now = DateTime.Now;
                            Ping = (int)(now - _lastPing).TotalMilliseconds;
                            // Console.WriteLine($"Ping: {Ping} ms");
                        } else {
                            Handlers?.Invoke(messageObj);
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error processing message {messageObj.Type.Name}: {ex.Message}");
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
                    byte[] data = message.ToBytes();
                    lock (_sendlock) {
                        string messageType = ((ClientMessageType)message.Type.Name).ToString();
                        if (_sentMessageAverageSize.TryGetValue(messageType, out var sizeInfo)) {
                            _sentMessageAverageSize[messageType] = (sizeInfo.Item1 + 1, sizeInfo.Item2 + data.Length);
                        } else {
                            _sentMessageAverageSize[messageType] = (1, data.Length);
                        }
                    }

                    _stream.Write(data, 0, data.Length);
                    // Console.WriteLine($"Total packet size: {data.Length}");
                    _stream.Flush();
                } catch (Exception ex) {
                    Console.WriteLine($"Error sending message: {ex.Message}");
                    Disconnect();
                }
            } else {
                Handlers?.Invoke(NetworkMessage.From(ServerMessageType.NotConnected));
                Console.WriteLine("Not connected to server");
            }
        }

        public static void PrintMessageSize() {
            Console.WriteLine("Sent message average sizes:");
            foreach (var kvp in _sentMessageAverageSize) {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value.Item2 / kvp.Value.Item1} bytes (count: {kvp.Value.Item1})");
            }
            Console.WriteLine("Received message average sizes:");
            foreach (var kvp in _receivedMessageAverageSize) {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value.Item2 / kvp.Value.Item1} bytes (count: {kvp.Value.Item1})");
            }
        }

        public void Disconnect() {
            if (!_connected) {
                return;
            }

            _connected = false;
            _clientId = -1;

            _listenCts?.Cancel();
            _listenThread?.Join();

            _stream?.Close();
            _client?.Close();
            _client = null;
            _messageBuffer?.Dispose();

            _pingTimer?.Stop();
            _pingTimer?.Dispose();

            Console.WriteLine("Disconnected from server");
        }
    }
}