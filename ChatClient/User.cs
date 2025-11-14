namespace ChatClientApp
{
    public class User
    {
        public string Username;

        public User(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                Username = "Anonymous";
            else
                Username = username;
        }

        public void PrintUsername()
        {
            System.Console.WriteLine("User: " + Username);
        }
    }
}