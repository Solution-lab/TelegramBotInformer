using TelegramBotInformer.Services;

namespace TelegramBotInformer.Models
{
    internal class FilterModel
    {
        public int Id { get; init; }
        
        public string Url { get; init; }

        public Site Site { get; init; }

        public FilterModel(int id, string url)
        {
            Id = id;
            Url = url;
            Site = GetSiteFromUrl(url);
        }

        private Site GetSiteFromUrl(string url)
        {
            if (url.Contains("avito.ru"))
            {
                return Site.Avito;
            }
            else if (url.Contains("auto.ru"))
            {
                return Site.AutoRu;
            }
            else if (url.Contains("drom.ru"))
            {
                return Site.DromRu;
            }

            return Site.Undefined;
        }
    }
}
