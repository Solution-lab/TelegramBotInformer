namespace TelegramBotInformer.Models
{
    internal class UpdateModel
    {
        public string Url { get; init; }

        public string Date { get; init; }

        public string Price { get; init; }

        public UpdateModel(string url, string date, string price)
        {
            Url = url;
            Date = date;
            Price = price;
        }
    }
}
