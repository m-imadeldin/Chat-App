using System;
using System.Threading.Tasks;

namespace ChatClientApp
{
    public class CommandHandler
    {
        private ChatClient _client;
        private MessageHistory _history;

        public CommandHandler(ChatClient client, MessageHistory history)
        {
            _client = client;
            _history = history;
        }

        public async Task HandleAsync(string input)
        {
            if (!input.StartsWith("/")) return;

            string[] parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            string command = parts.Length > 0 ? parts[0].ToLower() : "";

            if (command == "/help")
            {
                ShowHelp();
            }
            else if (command == "/quit")
            {
                Console.WriteLine("Exiting program...");
                await _client.DisconnectAsync();
                Environment.Exit(0);
            }
            else if (command == "/history")
            {
                int count = 20;
                if (parts.Length > 1 && int.TryParse(parts[1], out var n))
                    count = n;
                _history.ShowLast(count);
            }
            else if (command == "/dm")
            {
                if (parts.Length < 3)
                {
                    Console.WriteLine("Usage: /dm <user> <text>");
                    return;
                }
                string recipient = parts[1];
                string text = parts[2];
                await _client.SendPrivateMessageAsync(recipient, text);
            }
            else
            {
                Console.WriteLine("Unknown command. Type /help for list.");
            }
        }

        private void ShowHelp()
        {
            Console.WriteLine("--- Commands ---");
            Console.WriteLine("/help        - show this help");
            Console.WriteLine("/quit        - exit the program");
            Console.WriteLine("/history [n] - show last n messages");
            Console.WriteLine("/dm <user> <text> - send direct message");
            Console.WriteLine("----------------");
        }
    }
}