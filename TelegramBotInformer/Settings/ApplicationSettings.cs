using System.IO;
using System.Text;
using System.Text.Json;
using TelegramBotInformer.Models;

namespace TelegramBotInformer.Settings
{
	internal class ApplicationSettings 
	{
		private static ApplicationSettings _instance;

		public DatabaseSettings DatabaseSettings { get; init; }

		public string TelegramBotToken { get; init; }

		public ProxyModel TelegramBotProxy { get; init; }

		private ApplicationSettings()
		{
			string pathToFile = Path.Combine(Directory.GetCurrentDirectory(), "applicationSettings.json");

			if (File.Exists(pathToFile))
			{
				using (JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(pathToFile, Encoding.UTF8)))
				{
					DatabaseSettings = JsonSerializer.Deserialize<DatabaseSettings>(jsonDocument.RootElement.GetProperty(nameof(DatabaseSettings)).GetRawText());
					TelegramBotToken = JsonSerializer.Deserialize<string>(jsonDocument.RootElement.GetProperty(nameof(TelegramBotToken)).GetRawText());
					TelegramBotProxy = JsonSerializer.Deserialize<ProxyModel>(jsonDocument.RootElement.GetProperty(nameof(TelegramBotProxy)).GetRawText());
				}
			}
		}

		public static ApplicationSettings GetInstance()
		{
			if (_instance == null)
			{
				_instance = new ApplicationSettings();
			}

			return _instance;
		}
	}
}
