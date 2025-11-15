using System;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using SocketIOClient;

namespace ChatClientApp
{
    public class ChatClient
    {
        private readonly SocketIOClient.SocketIO _client;
        private readonly User _user;
        private readonly MessageHistory _history;

        public ChatClient(User user, MessageHistory history, string serverUrl)
        {
            _user = user;
            _history = history;
            
            _client = new SocketIOClient.SocketIO(serverUrl, new SocketIOClient.SocketIOOptions
            {
                Path = "/socket.io/",
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
            });


            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            _client.OnConnected += async (sender, e) =>
            {
                Console.WriteLine("Connected to server.");
                // Valfritt: meddela server att vi joinar (om servern förväntar sig detta)
                try
                {
                    await _client.EmitAsync("join", new { username = _user.Username });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error emitting join: " + ex.Message);
                }
            };

            _client.OnDisconnected += (sender, e) =>
            {
                Console.WriteLine("Disconnected from server.");
            };

            // Lyssna på ett enda konsekvent event: "chat_message"
            _client.On("chat_message", response =>
            {
                try
                {
                    // försöker tolka som JSON-objekt
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

            // Logga alla inkommande event - bra för felsökning
            _client.OnAny((eventName, response) =>
            {
                try
                {
                    Console.WriteLine($"ON_ANY -> Event: {eventName}, Payload: {response.ToString()}");
                }
                catch { }
            });
        }

        public async Task ConnectAsync()
        {
            try
            {
                Console.WriteLine("Connecting to server...");
                await _client.ConnectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ConnectAsync failed: " + ex);
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
                // Emit 'chat_message' — server förväntas broadcasta detta till andra klienter
                await _client.EmitAsync("chat_message", payload);
                Console.WriteLine("You: " + text);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error emitting chat_message: " + ex.Message);
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
                // Vi skickar private_message men servern måste route:a till mottagaren.
                await _client.EmitAsync("private_message", new { from = _user.Username, to = recipient, message = text, time = DateTime.Now.ToString("HH:mm") });
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
                await _client.DisconnectAsync();
            }
            catch { }

            Console.WriteLine("You have left the chat.");
        }
    }
}
