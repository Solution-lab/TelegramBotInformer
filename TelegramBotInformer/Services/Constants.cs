namespace TelegramBotInformer.Services
{
    internal static class Constants
    {
        #region Commands
        /// <summary>
        /// Команда старт
        /// </summary>
        public static readonly string COMMAND_START = "/start";
        #endregion

        #region KeyboardActions
        public static readonly string KEYBOARD_ACTION_ADD_FILTER = "Добавить фильтр";

        public static readonly string KEYBOARD_ACTION_DELETE_FILTER = "Удалить фильтр";

        public static readonly string KEYBOARD_ACTION_SHOW_FILTERS = "Мои фильтры";

        public static readonly string KEYBOARD_ACTION_ENABLE_NOTIFICATIONS = "Включить уведомления";

        public static readonly string KEYBOARD_ACTION_DISABLE_NOTIFICATIONS = "Отключить уведомления";

        public static readonly string KEYBOARD_ACTION_BACK = "Назад";
        #endregion

        public static readonly byte UPDATE_FREQUENCY = 10;
    }
}
