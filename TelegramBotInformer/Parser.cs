using TelegramBotInformer.Models;
using System.Collections.Generic;
using HtmlAgilityPack;
using NLog;
using System;
using System.Threading.Tasks;
using TelegramBotInformer.Services;
using System.Text;

namespace TelegramBotInformer
{
    internal class Parser
    {
        private static Parser _instance;
        private readonly Logger _logger;
        private readonly DatabaseProvider _databaseProvider;

        private Parser()
        {
            _logger = LogManager.GetCurrentClassLogger();
            _databaseProvider = DatabaseProvider.GetInstance();
        }

        public static Parser GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Parser();
            }

            return _instance;
        }

        public async Task<IList<UpdateModel>> GetUpdatesAsync(long chatId, FilterModel filterModel, ProxyModel proxyModel)
        {
            IList<UpdateModel> updateModels = new List<UpdateModel>();

            Site site = GetSiteFromUrl(filterModel.Url);

            if (site != Site.Undefined)
            {
                HtmlDocument htmlDocument = GetHtmlDocument(filterModel.Url, site, proxyModel);

                if (htmlDocument != null)
                {
                    IEnumerable<HtmlNode> htmlNodes = GetHtmlNodes(htmlDocument, site);

                    if (htmlNodes != null)
                    {
                        foreach (HtmlNode node in htmlNodes)
                        {
                            if (CheckHtmlNode(node, site))
                            {
                                try
                                {
                                    UpdateModel updateModel = GetUpdateModel(node, site);

                                    if (updateModel.Date.Contains("минут") || updateModel.Date.Contains("секунд"))
                                    {
                                        if (await _databaseProvider.IsAdAlreadyBeenSentAsync(chatId, updateModel.Url) == false)
                                        {
                                            if (updateModel.Date.Contains("секунд") || updateModel.Date.Replace((char)160, ' ') == "минуту назад")
                                            {
                                                updateModels.Add(updateModel);
                                            }
                                            else if (int.TryParse(updateModel.Date.Replace((char)160, ' ').Split(' ')[0], out int minutes))
                                            {
                                                if (minutes <= 15)
                                                {
                                                    updateModels.Add(updateModel);
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.Error($"Ошибка при получении обновления : {ex?.Message}, InnerException : {ex?.InnerException}");
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.Error($"Не найдены htmlNodes {site}");
                    }
                }
            }
            
            return updateModels;
        }

        private IEnumerable<HtmlNode> GetHtmlNodes(HtmlDocument htmlDocument, Site site)
        {
            if (site == Site.Avito)
            {
                return htmlDocument.DocumentNode.SelectNodes("//div[@data-marker='item']");
            }
            else if (site == Site.AutoRu)
            {
                return htmlDocument.DocumentNode.SelectNodes("//div[@class='ListingItem__main']");
            }
            else if (site == Site.DromRu)
            {
                return htmlDocument.DocumentNode.SelectNodes("//a[@data-ftid='bulls-list_bull']");
            }

            return new List<HtmlNode>();
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

        private HtmlDocument GetHtmlDocument(string url, Site site, ProxyModel proxyModel)
        {
            try
            {
                Encoding encoding = Encoding.UTF8;

                if (site == Site.DromRu)
                {
                    encoding = Encoding.GetEncoding(1251);
                }

                HtmlWeb web = new HtmlWeb
                {
                    OverrideEncoding = encoding
                };

                return web.Load(url, proxyModel.Host, proxyModel.Port, proxyModel.UserName, proxyModel.Password);
            }
            catch (Exception ex)
            {
                _logger.Error("Ошибка при получении HTML документа : " + ex?.Message);
            }

            return null;
        }

        private UpdateModel GetUpdateModel(HtmlNode htmlNode, Site site)
        {
            if (site == Site.Avito)
            {
                string url = "www.avito.ru" + htmlNode.SelectSingleNode(htmlNode.XPath + "//a[@data-marker='item-title']").Attributes["href"].Value;
                string date = htmlNode.SelectSingleNode(htmlNode.XPath + "//div[@data-marker='item-date']").InnerText;
                string price = htmlNode.SelectSingleNode(htmlNode.XPath + "//meta[@itemprop='price']").Attributes["content"].Value;

                return new UpdateModel(url, date, price);
            }
            else if (site == Site.AutoRu)
            {
                string url = htmlNode.SelectSingleNode(htmlNode.XPath + "//a[@class='Link ListingItemTitle__link']").Attributes["href"].Value;
                string date = htmlNode.SelectSingleNode(htmlNode.XPath + "//span[@class='MetroListPlace__content MetroListPlace_nbsp']").InnerText;

                return new UpdateModel(url, date, string.Empty);
            }
            else if (site == Site.DromRu)
            {
                string url = htmlNode.Attributes["href"].Value;
                string date = htmlNode.SelectSingleNode(htmlNode.XPath + "//div[@data-ftid='bull_date']").InnerText;

                return new UpdateModel(url, date, string.Empty);
            }

            return null;
        }

        private bool CheckHtmlNode(HtmlNode htmlNode, Site site)
        {
            try
            {
                if (site == Site.Avito)
                {
                    if (htmlNode.Attributes.Contains("data-marker"))
                    {
                        if (htmlNode.Attributes["data-marker"].Value == "item" && htmlNode.ParentNode.Attributes["data-marker"]?.Value == "catalog-serp")
                        {
                            return true;
                        }
                    }
                }
                else if (site == Site.AutoRu)
                {
                    return htmlNode.SelectSingleNode(htmlNode.XPath + "//span[@class='MetroListPlace__content MetroListPlace_nbsp']") != null;
                }
                else if (site == Site.DromRu)
                {
                    return htmlNode.SelectSingleNode(htmlNode.XPath + "//div[@data-ftid='bull_date']") != null;
                }
            }
            catch(Exception ex)
            {
                _logger.Error("Ошибка при проверке HtmlNode : " + ex?.Message);
            }

            return false;
        }
    }
}
