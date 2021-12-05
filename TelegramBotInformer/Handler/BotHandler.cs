using TelegramBotInformer.Models;
using TelegramBotInformer.Services;
using System.Collections.Generic;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Linq;
using NLog;
using System;
using System.Threading.Tasks;

namespace TelegramBotInformer.Handler
{
    internal class BotHandler
    {
        private readonly ITelegramBotClient _telegramBotClient;
        private readonly DatabaseProvider _databaseProvider;
        private readonly Logger _logger;
        private readonly Parser _parser;
        private readonly Queue<ProxyModel> _proxyModels;

        public BotHandler(ITelegramBotClient telegramBotClient, IEnumerable<ProxyModel> proxyModels)
        {
            _logger = LogManager.GetCurrentClassLogger();
            _telegramBotClient = telegramBotClient;
            _databaseProvider = DatabaseProvider.GetInstance();
            _parser = Parser.GetInstance();

            _proxyModels = new Queue<ProxyModel>(proxyModels);

            Thread workerThread = new Thread(() =>
            {
                while (true)
                {
                    SendUpdatesAsync();

                    Thread.Sleep(TimeSpan.FromSeconds(Constants.UPDATE_FREQUENCY));
                }
            });

            workerThread.Start();
        }

        #region Handlers
        public async void OnCommandAsync(MessageEventArgs e)
        {
            if (e.Message.Text == Constants.COMMAND_START)
            {
                await _databaseProvider.AddUserIfNotExistsAsync(e.Message.Chat.Id);
                await SendTextMessageAsync(e.Message.Chat.Id, text: "Выберите пункт меню \u2935\ufe0f", replyMarkup: new ReplyKeyboardMarkup(GetMainUserKeyboard(), true));
            }
        }

        public async void OnMessageAsync(MessageEventArgs e)
        {
            UserAction userAction = await _databaseProvider.GetUserLastActionAsync(e.Message.Chat.Id);

            if (userAction == UserAction.AddFilter)
            {
                await _databaseProvider.AddNewFilterAsync(e.Message.Chat.Id, e.Message.Text);
                await SendTextMessageAsync(e.Message.Chat.Id, "Фильтр успешно добавлен");
            }
            else if (userAction == UserAction.DeleteFilter)
            {
                if (int.TryParse(e.Message.Text, out int filterId))
                {
                    if (await _databaseProvider.IsFilterExistsAsync(e.Message.Chat.Id, filterId))
                    {
                        await _databaseProvider.DeleteFilterAsync(filterId);
                        await SendTextMessageAsync(e.Message.Chat.Id, "Фильтр успешно удалён");
                        return;
                    }
                }

                await SendTextMessageAsync(e.Message.Chat.Id, "У вас нет фильтра с таким id");
            }
        }

        public async void OnKeyboardActionAsync(MessageEventArgs e)
        {
            if (e.Message.Text == Constants.KEYBOARD_ACTION_ADD_FILTER)
            {
                await SendTextMessageAsync(e.Message.Chat.Id, "Отправьте фильтр сообщением", new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>() { new KeyboardButton(Constants.KEYBOARD_ACTION_BACK) },
                    }, true));

                await _databaseProvider.UpdateLastUserActionAsync(e.Message.Chat.Id, UserAction.AddFilter);
            }
            else if (e.Message.Text == Constants.KEYBOARD_ACTION_SHOW_FILTERS)
            {
                IList<FilterModel> filters = await _databaseProvider.GetUserFiltersAsync(e.Message.Chat.Id);
                IList<string> strs = new List<string>();

                foreach (FilterModel filterModel in filters)
                {
                    strs.Add($"{filterModel.Id}. {filterModel.Url}");
                }

                if (filters.Count > 0)
                {
                    await SendTextMessageAsync(e.Message.Chat.Id, string.Join('\n', strs));
                }
                else
                {
                    await SendTextMessageAsync(e.Message.Chat.Id, "У вас нет фильтров");
                }
            }
            else if (e.Message.Text == Constants.KEYBOARD_ACTION_DELETE_FILTER)
            {
                await SendTextMessageAsync(e.Message.Chat.Id, "Отправьте Id фильтра сообщением", new ReplyKeyboardMarkup(
                    new List<List<KeyboardButton>>
                    {
                        new List<KeyboardButton>() { new KeyboardButton(Constants.KEYBOARD_ACTION_BACK) },
                    }, true));

                await _databaseProvider.UpdateLastUserActionAsync(e.Message.Chat.Id, UserAction.DeleteFilter);
            }
            else if (e.Message.Text == Constants.KEYBOARD_ACTION_BACK)
            {
                await SendTextMessageAsync(e.Message.Chat.Id, text: "Выберите пункт меню \u2935\ufe0f", replyMarkup: new ReplyKeyboardMarkup(GetMainUserKeyboard(), true));
                await _databaseProvider.UpdateLastUserActionAsync(e.Message.Chat.Id, UserAction.Nothing);
            }
            else if (e.Message.Text == Constants.KEYBOARD_ACTION_ENABLE_NOTIFICATIONS)
            {
                await SendTextMessageAsync(e.Message.Chat.Id, "Уведомления включены");
                await _databaseProvider.SetNotificationsEnableAsync(e.Message.Chat.Id, true);
            }
            else if (e.Message.Text == Constants.KEYBOARD_ACTION_DISABLE_NOTIFICATIONS)
            {
                await SendTextMessageAsync(e.Message.Chat.Id, "Уведомления отключены");
                await _databaseProvider.SetNotificationsEnableAsync(e.Message.Chat.Id, false);
            }
        }

        public bool IsKeyboardAction(string action)
        {
            if (action == Constants.KEYBOARD_ACTION_ADD_FILTER || action == Constants.KEYBOARD_ACTION_BACK || action == Constants.KEYBOARD_ACTION_DELETE_FILTER || action == Constants.KEYBOARD_ACTION_SHOW_FILTERS
                || action == Constants.KEYBOARD_ACTION_ENABLE_NOTIFICATIONS || action == Constants.KEYBOARD_ACTION_DISABLE_NOTIFICATIONS)
            {
                return true;
            }

            return false;
        }
        #endregion

        private List<List<KeyboardButton>> GetMainUserKeyboard()
        {
            return new List<List<KeyboardButton>>
            {
                new List<KeyboardButton>() { new KeyboardButton(Constants.KEYBOARD_ACTION_SHOW_FILTERS) },
                new List<KeyboardButton>() { new KeyboardButton(Constants.KEYBOARD_ACTION_ADD_FILTER), new KeyboardButton(Constants.KEYBOARD_ACTION_DELETE_FILTER) },
                new List<KeyboardButton>() { new KeyboardButton(Constants.KEYBOARD_ACTION_ENABLE_NOTIFICATIONS), new KeyboardButton(Constants.KEYBOARD_ACTION_DISABLE_NOTIFICATIONS) }
            };
        }

        private async Task SendTextMessageAsync(long chatId, string text, IReplyMarkup replyMarkup = null)
        {
            try
            {
                await _telegramBotClient.SendTextMessageAsync(chatId, text: text, replyMarkup: replyMarkup, parseMode: ParseMode.Html);
            }
            catch (Exception ex)
            {
                _logger.Error("Ошибка при отправке сообщения : " + ex?.Message);
            }
        }

        private async void SendUpdatesAsync()
        {
            IList<long> ids = await _databaseProvider.GetAllChatIdsWithEnabledNotificationsAsync();

            if (ids.Count > 0)
            {
                ProxyModel proxyModel = _proxyModels.Dequeue();
                _proxyModels.Enqueue(proxyModel);

                Task[] tasks = new Task[ids.Count];

                for (int i = 0; i < ids.Count; i++)
                {
                    long id = ids[i];

                    tasks[i] = Task.Run(async () =>
                    {
                        IList<FilterModel> filterModels = await _databaseProvider.GetUserFiltersAsync(id);

                        IEnumerable<IGrouping<Site, FilterModel>> filterModelsGroup = filterModels.GroupBy(x => x.Site);
                        Task[] groupTasks = new Task[filterModelsGroup.Count()];

                        for (int j = 0; j < filterModelsGroup.Count(); j++)
                        {
                            IGrouping<Site, FilterModel> group = filterModelsGroup.ElementAt(j);

                            groupTasks[j] = Task.Run(async () =>
                            {                               
                                foreach (FilterModel filterModel in group)
                                {
                                    await Task.Delay(2000);

                                    IList<UpdateModel> updateModels = await _parser.GetUpdatesAsync(id, filterModel, proxyModel);

                                    if (updateModels.Count > 0)
                                    {
                                        foreach (UpdateModel updateModel in updateModels)
                                        {
                                            string price = string.Empty;

                                            if (filterModel.Site != Site.AutoRu)
											{
                                                price = "<b>Цена: " + updateModel.Price + " руб.</b>" + Environment.NewLine;
                                            }

                                            await SendTextMessageAsync(id, price + updateModel.Url);
                                        }

                                        await _databaseProvider.SaveSentAdsAsync(id, updateModels.Select(x => x.Url));
                                    }
                                }
                            });
                        }

                        Task.WaitAll(groupTasks);
                    });
                }

                Task.WaitAll(tasks);
            }
        }
    }
}
