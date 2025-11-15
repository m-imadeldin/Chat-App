using System;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using SocketIOClient;
using SocketIOClient.Transport;

namespace ChatClientApp
{
    public class ChatClient
    {
        private readonly SocketIO _socket;
        private readonly User _user;
        private readonly MessageHistory _history;

        public ChatClient(User user, MessageHistory history, string serverUrl)
        {
            _user = user;
            _history = history;

            _socket = new SocketIO(serverUrl, new SocketIOOptions
            {
                Path = "/socket.io/",
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
            });

            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            _socket.OnConnected += async (_, _) =>
            {
                Console.WriteLine("Connected to server.");

                try
                {
                    await _socket.EmitAsync("join", new { username = _user.Username });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error sending join event: " + ex.Message);
                }
            };

            _socket.OnDisconnected += (_, _) =>
            {
                Console.WriteLine("Disconnected from server.");
            };

            _socket.On("chat_message", response =>
            {
                try
                {
                    var obj = response.GetValue<JsonObject>();
                    string sender = obj["username"]?.ToString() ?? "Unknown";
                    string text = obj["message"]?.ToString() ?? "";
                    string time = obj["time"]?.ToString() ?? DateTime.Now.ToString("HH:mm");

                    Console.WriteLine($"[{time}] {sender}: {text}");

                    _history.Add(new Message
                    {
                        Sender = sender,
                        Text = text,
                        Timestamp = DateTime.Now
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("chat_message handler failed: " + ex.Message);
                }
            });

            _socket.OnAny((eventName, response) =>
            {
                try
                {
                    Console.WriteLine($"ON_ANY -> {eventName}: {response}");
                }
                catch { }
            });

            _socket.OnError += (_, error) =>
            {
                Console.WriteLine("Socket.IO error: " + error);
            };
        }

        public async Task ConnectAsync()
        {
            try
            {
                Console.WriteLine("Connecting...");
                await _socket.ConnectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ConnectAsync failed: " + ex.Message);
            }
        }

        public async Task SendMessageAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var payload = new
            {
                username = _user.Username,
                message = text,
                time = DateTime.Now.ToString("HH:mm")
            };

            try
            {
                await _socket.EmitAsync("chat_message", payload);
                Console.WriteLine("You: " + text);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending chat_message: " + ex.Message);
            }

            _history.Add(new Message
            {
                Sender = _user.Username,
                Text = text,
                Timestamp = DateTime.Now
            });
        }

        public async Task SendPrivateMessageAsync(string recipient, string text)
        {
            if (string.IsNullOrWhiteSpace(recipient) || string.IsNullOrWhiteSpace(text)) return;

            var payload = new
            {
                from = _user.Username,
                to = recipient,
                message = text,
                time = DateTime.Now.ToString("HH:mm")
            };

            try
            {
                await _socket.EmitAsync("private_message", payload);
                Console.WriteLine($"(DM to {recipient}) {text}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending private_message: " + ex.Message);
            }

            _history.Add(new Message
            {
                Sender = _user.Username,
                Recipient = recipient,
                Text = text,
                IsPrivate = true,
                Timestamp = DateTime.Now
            });
        }

        public async Task DisconnectAsync()
        {
            try
            {
                await _socket.DisconnectAsync();
            }
            catch { }

            Console.WriteLine("You have left the chat.");
        }
    }
}
