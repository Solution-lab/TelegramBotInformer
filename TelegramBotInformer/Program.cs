using TelegramBotInformer.Handler;
using MihaZupan;
using System;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using System.Collections.Generic;
using TelegramBotInformer.Models;
using System.IO;
using System.Text;
using System.Text.Json;
using TelegramBotInformer.Settings;

namespace TelegramBotInformer
{
    internal class Program
    {
        private static ITelegramBotClient _telegramBotClient;
        private static BotHandler _botHandler;

        static void Main(string[] args)
        {
            Initialization();

            new AutoResetEvent(false).WaitOne();
        }

        #region TelegramBot
        private static void TelegramBotClient_OnMessage(object sender, MessageEventArgs e)
        {
            string text = e?.Message?.Text;

            if (text != null)
            {
                if (text.Length > 0)
                {
                    if (text[0] == '/')
                    {
                        _botHandler.OnCommandAsync(e);
                    }
                    else if (_botHandler.IsKeyboardAction(text))
                    {
                        _botHandler.OnKeyboardActionAsync(e);
                    }
                    else
                    {
                        _botHandler.OnMessageAsync(e);
                    }
                }
            }
        }

        #endregion TelegramBot

        private static void Initialization()
		{
            List<ProxyModel> proxyModels = GetProxyModels();

            if (proxyModels.Count == 0)
            {
                Console.WriteLine("Не удалось найти json с proxy");
                Environment.Exit(-1);
            }

            ApplicationSettings applicationSettings = ApplicationSettings.GetInstance();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            var proxy = new HttpToSocks5Proxy(applicationSettings.TelegramBotProxy.Host, applicationSettings.TelegramBotProxy.Port, applicationSettings.TelegramBotProxy.UserName, applicationSettings.TelegramBotProxy.Password);

            _telegramBotClient = new TelegramBotClient(applicationSettings.TelegramBotToken, proxy) { Timeout = TimeSpan.FromSeconds(10) };
            _telegramBotClient.DeleteWebhookAsync();
            _telegramBotClient.OnMessage += TelegramBotClient_OnMessage;

            _botHandler = new BotHandler(_telegramBotClient, proxyModels);

            User user = _telegramBotClient.GetMeAsync().Result;

            Console.WriteLine("-----------------------------------");
            Console.WriteLine(user.FirstName + " запущен");
            Console.WriteLine("-----------------------------------");

            _telegramBotClient.StartReceiving();
        }

        private static List<ProxyModel> GetProxyModels()
		{
            string pathToFile = Path.Combine(Directory.GetCurrentDirectory(), "proxies.json");

            if (System.IO.File.Exists(pathToFile))
            {
                return JsonSerializer.Deserialize<List<ProxyModel>>(System.IO.File.ReadAllText(pathToFile, Encoding.UTF8));
            }

            return new List<ProxyModel>();
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (_telegramBotClient != null)
            {
                _telegramBotClient.StopReceiving();
            }
        }
    }
}
