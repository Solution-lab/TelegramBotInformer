namespace TelegramBotInformer.Settings
{
	internal class DatabaseSettings
	{
		public string Address { get; init; }

		public string Name { get; init; }

		public string User { get; init; }

		public string Password { get; init; }

		public DatabaseSettings(string address, string name, string user, string password)
		{
			Address = address;
			Name = name;
			User = user;
			Password = password;
		}
	}
}
