using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using SocketIOClient;

namespace ChatClientApp
{
    public class ChatClient
    {
        private readonly SocketIO _client;
        private readonly User _user;
        private readonly MessageHistory _history;

        public ChatClient(User user, MessageHistory history)
        {
            _user = user;
            _history = history;

            _client = new SocketIO("wss://api.leetcode.se", new SocketIOOptions
            {
                Path = "/sys25d",
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
            });
        }

        public async Task ConnectAsync()
        {
            _client.OnConnected += async (sender, e) =>
            {
                Console.WriteLine("Connected to chat!");
                await _client.EmitAsync("join", new { username = _user.Username });
                await _client.EmitAsync("chat_join", new { username = _user.Username });
                Console.WriteLine("Join emitted (join + chat_join)");
            };

            _client.On("message", response =>
            {
                try
                {
                    var obj = response.GetValue<JsonObject>();
                    Console.WriteLine("RECEIVED event 'message' raw: " + obj.ToJsonString());
                    string sender = obj["username"]?.ToString() ?? "Unknown";
                    string text = obj["message"]?.ToString() ?? "";
                    string time = obj["time"]?.ToString() ?? DateTime.Now.ToString("HH:mm");
                    Console.WriteLine($"[{time}] {sender}: {text}");
                    _history.Add(new Message { Sender = sender, Text = text, Timestamp = DateTime.Now });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("message handler exception: " + ex.Message);
                    Console.WriteLine("message raw: " + response.ToString());
                }
            });

            _client.On("chat_message", response =>
            {
                try
                {
                    var obj = response.GetValue<JsonObject>();
                    Console.WriteLine("RECEIVED event 'chat_message' raw: " + obj.ToJsonString());
                    string sender = obj["username"]?.ToString() ?? "Unknown";
                    string text = obj["message"]?.ToString() ?? "";
                    string time = obj["time"]?.ToString() ?? DateTime.Now.ToString("HH:mm");
                    Console.WriteLine($"[{time}] {sender}: {text}");
                    _history.Add(new Message { Sender = sender, Text = text, Timestamp = DateTime.Now });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("chat_message handler exception: " + ex.Message);
                    Console.WriteLine("chat_message raw: " + response.ToString());
                }
            });

            _client.On("user_joined", res =>
            {
                Console.WriteLine("RECEIVED event 'user_joined': " + res.ToString());
                try
                {
                    var name = res.GetValue<string>();
                    Console.WriteLine($"*** {name} has joined ***");
                }
                catch { }
            });

            _client.On("user_left", res =>
            {
                Console.WriteLine("RECEIVED event 'user_left': " + res.ToString());
                try
                {
                    var name = res.GetValue<string>();
                    Console.WriteLine($"*** {name} has left ***");
                }
                catch { }
            });

            // Generic fallback - log unknown events if any library supports them (best-effort)
            _client.OnAny((eventName, response) =>
            {
                try
                {
                    Console.WriteLine($"ON_ANY -> Event: {eventName}, Payload: {response.ToString()}");
                }
                catch { }
            });

            _client.OnDisconnected += (s, e) =>
            {
                Console.WriteLine("Disconnected from server.");
            };

            Console.WriteLine("Connecting to server...");
            await _client.ConnectAsync();
        }

        public async Task SendMessageAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                await _client.EmitAsync("message", new { username = _user.Username, message = text });
                await _client.EmitAsync("chat_message", new { username = _user.Username, message = text });
                Console.WriteLine("Sent (message + chat_message): " + text);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error emitting message: " + ex.Message);
            }

            var localMsg = new Message
            {
                Sender = _user.Username,
                Text = text,
                Timestamp = DateTime.Now
            };

            _history.Add(localMsg);
        }

        public async Task SendPrivateMessageAsync(string recipient, string text)
        {
            if (string.IsNullOrWhiteSpace(recipient) || string.IsNullOrWhiteSpace(text)) return;

            var msg = new Message
            {
                Sender = _user.Username,
                Text = text,
                IsPrivate = true,
                Recipient = recipient,
                Timestamp = DateTime.Now
            };

            try
            {
                await _client.EmitAsync("private_message", new { from = _user.Username, to = recipient, message = text });
                Console.WriteLine($"(DM to {recipient}) {text}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error emitting private_message: " + ex.Message);
            }

            _history.Add(msg);
        }

        public async Task DisconnectAsync()
        {
            try
            {
                await _client.EmitAsync("leave", new { username = _user.Username });
                await _client.EmitAsync("chat_leave", new { username = _user.Username });
                Console.WriteLine("Leave emitted (leave + chat_leave)");
            }
            catch { }

            try
            {
                await _client.DisconnectAsync();
            }
            catch { }

            Console.WriteLine("You have left the chat.");
        }
    }
}
