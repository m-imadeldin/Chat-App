using System;

namespace ChatClientApp
{
    public class Message
    {
        public string Sender { get; set; } = "";
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsPrivate { get; set; } = false;
        public string? Recipient { get; set; }

        public override string ToString()
        {
            if (IsPrivate && !string.IsNullOrWhiteSpace(Recipient))
            {
                return $"[{Timestamp:HH:mm}] (DM to {Recipient}) {Sender}: {Text}";
            }
            return $"[{Timestamp:HH:mm}] {Sender}: {Text}";
        }
    }
}