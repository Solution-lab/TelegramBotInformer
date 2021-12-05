using TelegramBotInformer.Models;
using TelegramBotInformer.Services;
using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TelegramBotInformer.Settings;

namespace TelegramBotInformer
{
    internal class DatabaseProvider
    {
        private static DatabaseProvider _instance;
        private readonly string _connectionString;
        private readonly Logger _logger;

        private DatabaseProvider()
        {
            _logger = LogManager.GetCurrentClassLogger();

            ApplicationSettings applicationSettings = ApplicationSettings.GetInstance();

            _connectionString = $"SERVER={applicationSettings.DatabaseSettings.Address}; DATABASE={applicationSettings.DatabaseSettings.Name}; UID={applicationSettings.DatabaseSettings.User}; PASSWORD={applicationSettings.DatabaseSettings.Password};";
        }

        public static DatabaseProvider GetInstance()
        {
            if (_instance == null)
            {
                _instance = new DatabaseProvider();
            }

            return _instance;
        }

        public async Task AddUserIfNotExistsAsync(long chatId)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    MySqlCommand command = connection.CreateCommand();
                    await connection.OpenAsync();

                    command.CommandText = $"SELECT chat_id FROM users WHERE chat_id = {chatId}";

                    using (MySqlDataReader mySqlDataReader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        await mySqlDataReader.ReadAsync();

                        if (!mySqlDataReader.HasRows)
                        {
                            await mySqlDataReader.CloseAsync();
                            command.CommandText = $"INSERT INTO users(chat_id) VALUES({chatId})";
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Ошибка при добавлении пользователя : " + ex?.Message);
                }
            }
        }

        public async Task UpdateLastUserActionAsync(long chatId, UserAction userAction)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    MySqlCommand command = connection.CreateCommand();
                    await connection.OpenAsync();

                    command.CommandText = "UPDATE users SET last_action = " + (int)userAction + " WHERE chat_id = " + chatId.ToString();
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error("Ошибка при обновлении последнего действия пользователя : " + ex?.Message);
                }
            }
        }

        public async Task<UserAction> GetUserLastActionAsync(long chatId)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    MySqlCommand command = connection.CreateCommand();
                    await connection.OpenAsync();

                    command.CommandText = "SELECT last_action FROM users WHERE chat_id = " + chatId.ToString();

                    using (MySqlDataReader mySqlDataReader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        await mySqlDataReader.ReadAsync();

                        if (mySqlDataReader.HasRows)
                        {
                            return (UserAction)mySqlDataReader.GetInt16("last_action");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Ошибка при получении последнего действия пользователя : " + ex?.Message);
                }
            }

            return UserAction.Undefined;
        }

        public async Task AddNewFilterAsync(long chatId, string url)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    MySqlCommand command = connection.CreateCommand();
                    await connection.OpenAsync();

                    command.CommandText = $"INSERT INTO user_filters(chat_id, url) VALUES({chatId},'{url}')";
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error("Ошибка при добавлении фильтра : " + ex?.Message);
                }
            }
        }

        public async Task<IList<FilterModel>> GetUserFiltersAsync(long chatId)
        {
            IList<FilterModel> filters = new List<FilterModel>();

            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    MySqlCommand command = connection.CreateCommand();
                    await connection.OpenAsync();

                    command.CommandText = "SELECT id, url FROM user_filters WHERE chat_id = " + chatId.ToString();

                    using (MySqlDataReader mySqlDataReader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await mySqlDataReader.ReadAsync())
                        {
                            filters.Add(new FilterModel(mySqlDataReader.GetInt32("id"), mySqlDataReader.GetString("url")));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Ошибка при получении фильтров пользователя : " + ex?.Message);
                }
            }

            return filters;
        }

        public async Task<bool> IsFilterExistsAsync(long chatId, int filterId)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    MySqlCommand command = connection.CreateCommand();
                    await connection.OpenAsync();

                    command.CommandText = "SELECT 1 FROM user_filters WHERE id = " + filterId.ToString() + " AND chat_id = " + chatId.ToString();

                    using (MySqlDataReader mySqlDataReader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        await mySqlDataReader.ReadAsync();

                        return mySqlDataReader.HasRows;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Ошибка при получении информации о существовании фильтра : " + ex?.Message);
                }
            }

            return false;
        }

        public async Task DeleteFilterAsync(int filterId)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    MySqlCommand command = connection.CreateCommand();
                    await connection.OpenAsync();

                    command.CommandText = "DELETE FROM user_filters WHERE id = " + filterId.ToString();
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error("Ошибка при удалении фильтра : " + ex?.Message);
                }
            }
        }

        public async Task<IList<long>> GetAllChatIdsWithEnabledNotificationsAsync()
        {
            IList<long> ids = new List<long>();

            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    MySqlCommand command = connection.CreateCommand();
                    await connection.OpenAsync();

                    command.CommandText = "SELECT chat_id FROM users WHERE is_notifications_enable = 1";

                    using (MySqlDataReader mySqlDataReader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await mySqlDataReader.ReadAsync())
                        {
                            ids.Add(mySqlDataReader.GetInt64("chat_id"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Ошибка при получении фильтров пользователя : " + ex?.Message);
                }
            }

            return ids;
        }

        public async Task SaveSentAdsAsync(long chatId, IEnumerable<string> urls)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    IList<string> rows = new List<string>();

                    foreach (string url in urls)
                    {
                        rows.Add($"({chatId},'{url}')");
                    }

                    MySqlCommand command = connection.CreateCommand();
                    await connection.OpenAsync();

                    command.CommandText = "INSERT INTO history (chat_id, url) VALUES " + string.Join(",", rows);
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error("Ошибка при сохранении id отправленных объявлений : " + ex?.Message);
                }
            }
        }

        public async Task<bool> IsAdAlreadyBeenSentAsync(long chatId, string url)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    MySqlCommand command = connection.CreateCommand();
                    await connection.OpenAsync();

                    command.CommandText = "SELECT 1 FROM history WHERE chat_Id = " + chatId.ToString() + " AND url = '" + url + "'";

                    using (MySqlDataReader mySqlDataReader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        await mySqlDataReader.ReadAsync();

                        return mySqlDataReader.HasRows;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Ошибка при получении информации о статусе отправки объявления : " + ex?.Message);
                }
            }

            return false;
        }

        public async Task SetNotificationsEnableAsync(long chatId, bool status)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    MySqlCommand command = connection.CreateCommand();
                    await connection.OpenAsync();

                    command.CommandText = $"UPDATE users SET is_notifications_enable = {(status == true ? 1 : 0)} WHERE chat_id = {chatId}";
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error("Ошибка при включении или отключении уведомлений : " + ex?.Message);
                }
            }
        }
    }
}
