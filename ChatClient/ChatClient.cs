using System;
using System.Threading.Tasks;
using SocketIOClient;

namespace ChatClientApp
{
    public class ChatClient
    {
        private readonly SocketIOClient.SocketIO _client;
        private readonly User _user;
        private readonly MessageHistory _history;

        public ChatClient(User user, MessageHistory history)
        {
            _user = user;
            _history = history;

            _client = new SocketIOClient.SocketIO("wss://api.leetcode.se", new SocketIOOptions
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
                await _client.EmitAsync("join", _user.Username);
            };

            _client.On("message", response =>
            {
                try
                {
                    var msg = response.GetValue<Message>();
                    Console.WriteLine(msg.ToString());
                    _history.Add(msg);
                }
                catch
                {
                    try
                    {
                        var raw = response.GetValue<string>();
                        Console.WriteLine(raw);
                    }
                    catch
                    {
                        Console.WriteLine("Received message event with unknown payload.");
                    }
                }
            });

            _client.On("user_joined", res =>
            {
                var name = res.GetValue<string>();
                Console.WriteLine($"*** {name} has joined ***");
            });

            _client.On("user_left", res =>
            {
                var name = res.GetValue<string>();
                Console.WriteLine($"*** {name} has left ***");
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
            var msg = new Message { Sender = _user.Username, Text = text, Timestamp = DateTime.Now };
            await _client.EmitAsync("message", msg);
            _history.Add(msg);
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
            await _client.EmitAsync("private_message", msg);
            _history.Add(msg);
            Console.WriteLine($"(DM to {recipient}) {text}");
        }

        public async Task DisconnectAsync()
        {
            try
            {
                await _client.EmitAsync("leave", _user.Username);
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
