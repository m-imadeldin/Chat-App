using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ChatClientApp
{
    public class MessageHistory
    {
        private const string HistoryFile = "chat_history.json";
        private List<Message> _messages;

        public MessageHistory()
        {
            _messages = new List<Message>();
        }

        public void Add(Message msg)
        {
            _messages.Add(msg);
            Save();
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(_messages);
                File.WriteAllText(HistoryFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving message history: " + ex.Message);
            }
        }

        public void Load()
        {
            if (!File.Exists(HistoryFile))
            {
                Console.WriteLine("No previous chat history found.");
                return;
            }

            try
            {
                string json = File.ReadAllText(HistoryFile);
                List<Message>? loadedMessages = JsonSerializer.Deserialize<List<Message>>(json);
                if (loadedMessages != null)
                    _messages.AddRange(loadedMessages);

                Console.WriteLine("Chat history loaded (" + _messages.Count + " messages).");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading message history: " + ex.Message);
            }
        }

        public void ShowLast(int count = 20)
        {
            Console.WriteLine("--- Last " + count + " messages ---");
            int start = Math.Max(_messages.Count - count, 0);
            for (int i = start; i < _messages.Count; i++)
            {
                Console.WriteLine(_messages[i].ToString());
            }
            Console.WriteLine("-----------------------------");
        }
    }
}