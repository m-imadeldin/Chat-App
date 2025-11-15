using System;
using System.Threading.Tasks;

namespace ChatClientApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Chat Client";
            Console.WriteLine("== Chat Client with Socket.IO ===");
            Console.Write("Enter username: ");
            string? username = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(username)) username = "Anonymous";

            var user = new User(username);
            user.PrintUsername();

            var history = new MessageHistory();
            history.Load();

            // Sätt server-URL här (ändra till er server). Exempel:
            // http://localhost:3000/  eller  https://api.example.com/
            const string ServerUrl = "http://localhost:3000/";

            var client = new ChatClient(user, history, ServerUrl);
            var handler = new CommandHandler(client, history);

            await client.ConnectAsync();

            Console.WriteLine("Type /help for commands.");
            while (true)
            {
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;

                if (input.StartsWith("/"))
                {
                    await handler.HandleAsync(input);
                }
                else
                {
                    await client.SendMessageAsync(input);
                }
            }
        }
    }
}