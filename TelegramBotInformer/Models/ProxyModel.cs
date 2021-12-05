namespace TelegramBotInformer.Models
{
    internal class ProxyModel
    {
        public string Host { get; init; }

        public int Port { get; init; }
        
        public string UserName { get; init; }

        public string Password { get; init; }

        public ProxyModel(string host, int port, string userName, string password)
        {
            Host = host;
            Port = port;
            UserName = userName;
            Password = password;
        }
    }
}
