using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using CertTechWatchBot.DL;
using CertTechWatchBot.Properties;
using Newtonsoft.Json;
using Document = Spire.Doc.Document;

namespace CertTechWatchBot.Control
{
    internal static class Helper
    {
        internal static readonly object LockHelper = new object();
        internal static bool IsNumeric(this string input)
        {
            long trueLong = 0;
            long.TryParse(input, out trueLong);

            return trueLong > 0;
        }

        public static void ErrorHandler(
                                             Message message,
                                             Exception exceptionMessage,
                                             [CallerMemberName] string methodName = null,
                                             [CallerFilePath] string sourceFile = null,
                                             [CallerLineNumber] int lineNumber = 0)
        {

            //LogEvent(exceptionMessage, methodName, sourceFile, lineNumber);

            //ClearUserFromCache();

        }

        public static bool IsFilled(this string input)
        {
            return !(string.IsNullOrWhiteSpace(input) && string.IsNullOrEmpty(input));
        }

        public static string Sanitize(this string rawUserInput)
        {
            return rawUserInput.Trim().Normalize().ToLower().TrimStart().TrimEnd();
        }

        public static XmlNodeList ReadRssContent(string url, string supplier)
        {
            try
            {
                string rssContent;
                using (var wc = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls |
                                       SecurityProtocolType.Tls11 |
                                       SecurityProtocolType.Tls12;
                    rssContent = wc.DownloadString(url);
                }
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(rssContent);
                var xmlElement = xmlDoc.DocumentElement;

                if (xmlElement == null)
                    throw new NoNullAllowedException("xml Parsing Failed!");

                var res = xmlElement.SelectNodes("channel/item");
                return res;
            }
            catch (Exception e)
            {
                ShowNotify(e.Message, e.StackTrace, supplier);
                return null;
            }

        }

        public static List<Model.News> CategorizeNewsByAttackType(List<Model.News> listOfNews)
        {
            // its okay to take only a day back's news. on production phase, its gonna be hourly.
            var groupedByList = listOfNews.OrderBy(news => news.AttackCategory).ThenBy(news => news.PublishDate).ToList();
            return groupedByList;
        }

        public static DateTime ToDate(this string stringDate)
        {
            try
            {
                if (!stringDate.IsFilled()) { return new DateTime(); }
                return Convert.ToDateTime(stringDate);
            }
            catch (Exception)
            {
                try { return DateTime.Parse(stringDate).ToUniversalTime(); }
                catch
                {
                    try
                    {
                        DateTime date = DateTime.ParseExact(stringDate.Substring(5, stringDate.Length - 9), "d MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        return date;
                    }
                    catch { return DateTime.Now; }
                }
            }
        }

        public static List<T> JsonToModel<T>(this string json)
        {
            if (!json.IsFilled()) throw new ArgumentNullException();
            return JsonConvert.DeserializeObject<List<T>>(json);
        }


        public static int ToInteger(this string integerValue)
        {
            try
            {
                return int.Parse(integerValue);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static string RemoveAttackName(this string newsTitle, Model.AttackCategory attackName, Model.NewsCategory newsCategory)
        {
            if (newsCategory != Model.NewsCategory.Exploit)
                return newsTitle;

            //TODO Command Injection not removed
            var indexOfAttackCategoryInNewsTitle = newsTitle.IndexOf(attackName.GetEnumDescription(),
                StringComparison.InvariantCultureIgnoreCase);

            return (indexOfAttackCategoryInNewsTitle < 1) ? newsTitle : newsTitle.Remove(indexOfAttackCategoryInNewsTitle);
        }

        public static string GetEnumDescription(this object enumValue)
        {
            var info =
                enumValue.GetType().GetMember(enumValue.ToString())[0].GetCustomAttributes(
                    typeof(DescriptionAttribute), false);

            return info.Length > 0 ? ((DescriptionAttribute)info[0]).Description : "";
        }


        /// <summary>
        /// Returns the number of steps required to transform the source string
        /// into the target string.
        /// </summary>
        public static int ComputeLevenshteinDistance(this string source, string target)
        {
            if (string.IsNullOrEmpty(source))
                return string.IsNullOrEmpty(target) ? 0 : target.Length;

            if (string.IsNullOrEmpty(target))
                return string.IsNullOrEmpty(source) ? 0 : source.Length;

            int sourceLength = source.Length;
            int targetLength = target.Length;

            int[,] distance = new int[sourceLength + 1, targetLength + 1];

            // Step 1
            for (int i = 0; i <= sourceLength; distance[i, 0] = i++) ;
            for (int j = 0; j <= targetLength; distance[0, j] = j++) ;

            for (int i = 1; i <= sourceLength; i++)
            {
                for (int j = 1; j <= targetLength; j++)
                {
                    // Step 2
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

                    // Step 3
                    distance[i, j] = Math.Min(
                                        Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                                        distance[i - 1, j - 1] + cost);
                }
            }

            return distance[sourceLength, targetLength];
        }

        /// <summary>
        /// Calculate percentage similarity of two strings
        /// <param name="source">Source String to Compare with</param>
        /// <param name="target">Targeted String to Compare</param>
        /// <returns>Return Similarity between two strings from 0 to 1.0</returns>
        /// </summary>
        public static bool DoesItMatch(this string source, string target)
        {
            double result;
            if (string.IsNullOrEmpty(source))
                result = string.IsNullOrEmpty(target) ? 1 : 0;

            if (string.IsNullOrEmpty(target))
                result = string.IsNullOrEmpty(source) ? 1 : 0;

            double stepsToSame = ComputeLevenshteinDistance(source, target);
            result = (1.0 - (stepsToSame / (double)Math.Max(source.Length, target.Length)));

            return result >= 0.8;
        }

        private static Model.NewsSupplier GetSupplier(string url)
        {
            switch (url)
            {
                case "https://tools.cisco.com/security/center/publicationService.x?criteria=exact&cves=&keyword=&last_published_date=&limit=20&offset=0&publicationTypeIDs=6,9&securityImpactRatings=&sort=-day_sir&title=":
                case "https://tools.cisco.com/security/center/publicationService.x?criteria=exact&cves=&keyword=&last_published_date=&limit=20&offset=0&publicationTypeIDs=1,3&securityImpactRatings=&sort=-day_sir&title=":
                    return Model.NewsSupplier.CiscoCert;

                case "https://searchsecurity.techtarget.com/rss/Security-Wire-Daily-News.xml":
                    return Model.NewsSupplier.TechTarget;
                case "https://rss.packetstormsecurity.com/files/":
                    return Model.NewsSupplier.PacketStorm;
                case "https://cxsecurity.com/wlb/rss/all/":
                    return Model.NewsSupplier.CxSecurity;

                case "https://www.us-cert.gov/ncas/current-activity.xml":
                case "https://ics-cert.us-cert.gov/alerts/alerts.xml":
                case "https://ics-cert.us-cert.gov/advisories/advisories.xml":
                case "https://www.us-cert.gov/ncas/alerts.xml":
                    return Model.NewsSupplier.UsCert;

                case "https://www.vulnerability-lab.com/rss/rss.php":
                    return Model.NewsSupplier.VulnerableLab;
                case "https://www.securityfocus.com/rss/vulnerabilities.xml":
                    return Model.NewsSupplier.SecFocus;
                case "http://securityaffairs.co/wordpress/feed":
                    return Model.NewsSupplier.SecAffair;
                case "https://vulners.com/rss.xml":
                    return Model.NewsSupplier.Vulners;
                case "http://seclists.org/rss/microsoft.rss":
                    return Model.NewsSupplier.Microsoft;
                case "https://support.hpe.com/portal/site/hpsc/public/kb/secBullArchive":
                    return Model.NewsSupplier.HP;
                case "https://exploit.kitploit.com/feeds/posts/default?alt=rss":
                    return Model.NewsSupplier.Kitploit;
                case "https://www.secnews24.com/feed/":
                    return Model.NewsSupplier.Sec24;
                case "https://www.huawei.com/en/rss-feeds/psirt/rss":
                    return Model.NewsSupplier.Huawei;
                case "https://www.novell.com/newsfeeds/rss/patches/security_notifications-daily.xml":
                    return Model.NewsSupplier.Novel;
                case "https://www.oracle.com/ocom/groups/public/@otn/documents/webcontent/rss-otn-sec.xml":
                    return Model.NewsSupplier.Oracle;
                default:
                    return Model.NewsSupplier.None;
            }
        }

        internal static List<Model.News> GetNewsOnline(string url)
        {
            return GetNewsOnline(url, GetSupplier(url));
        }
        internal static List<Model.News> GetNewsOnline(string url, Model.NewsSupplier supplier)
        {
            var newsModel = new List<Model.News>();
            try
            {
                var xmlListOfNews = ReadRssContent(url, supplier.ToString());
                if (xmlListOfNews == null) return new List<Model.News>();

                foreach (XmlNode news in xmlListOfNews)
                {
                    if (news.NextSibling != null)
                        newsModel.Add(new Model.News
                        {
                            Identifier = GetId(news),
                            Url = GetUrl(news),
                            Title = news.SelectSingleNode("title").InnerText,
                            Description = news.SelectSingleNode("description")?.InnerText ?? news.SelectSingleNode("title").InnerText,
                            PublishDate = news.SelectSingleNode("pubDate").InnerText.ToDate(),
                            Supplier = supplier
                        });
                }

                newsModel.RemoveAll(n => (n.PublishDate < DateTime.Today.AddDays(-4) && (n.PublishDate > DateTime.Today.AddDays(-4))));
                if (newsModel.Any()) Console.WriteLine($"[+] {supplier}: {newsModel.Count}");
                return newsModel;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"*Err* {supplier} = {exception.Message}");
                return newsModel;
            }
        }
        internal static List<Model.News> GetNewsOffline(int fromDay = -1)
        {
            try
            {
                return CategorizeNewsByAttackType(DbHelper.SelectNews(fromDay == 0 ? fromDay : ((DateTime.Today.ToString("dddd") == "Saturday") ? -3 : -1)));

            }
            catch (Exception exception)
            {
                ShowNotify(exception.Message, exception.StackTrace, "Get New offline");
                return new List<Model.News>();
            }
        }

        internal static List<Model.News> GetUnGeneratedNews()
        {
            try
            {
                return DbHelper.SelectNews((DateTime.Today.ToString("dddd") == "Saturday") ? -3 : -1).Where(n => n.Reported == false).ToList();

            }
            catch (Exception exception)
            {
                ShowNotify(exception.Message, exception.StackTrace, "Get Unread News");
                return new List<Model.News>();
            }
        }

        private static string GetId(XmlNode node)
        {
            var res = node.SelectSingleNode("guid")?.InnerText;
            return string.IsNullOrWhiteSpace(res) ? (node.SelectSingleNode("link").InnerText.Substring(node.SelectSingleNode("link").InnerText.LastIndexOf('/') + 1) ?? "") : res;
        }
        private static string GetUrl(XmlNode node)
        {
            return node.SelectSingleNode("comments")?.InnerText ?? node.SelectSingleNode("link")?.InnerText;
        }


        internal static void RemoveDuplicateNews(ref List<Model.News> uniqueNews, List<Model.News> allNews)
        {
            try
            {
                //because of junk xss items
                allNews.RemoveAll(n => n.AttackCategory == Model.AttackCategory.Xss && n.Supplier == Model.NewsSupplier.Vulners);

                foreach (var news in allNews)
                {
                    if (uniqueNews.Any(u => u.Title.DoesItMatch(news.Title)))
                        continue;
                    uniqueNews.Add(news);
                }
            }
            catch (Exception ex)
            {
                ShowNotify(ex.Message, ex.StackTrace, "Remove Duplicate");
            }
            //foreach (var news in newList)
            //{
            //    try
            //    {
            //        var isMatchFound = false;

            //        foreach (var uniqe in distinctNewsList.Where(d => (d.Title.DoesItMatch(news.Title)) && d.Supplier != news.Supplier))
            //        {
            //            isMatchFound = true;
            //            removed.Add(news);
            //        }
            //        //if (distinctNewsList.Any(d => (d.Supplier != news.Supplier && d.Title.DoesItMatch(news.Title)))) { isMatchFound = true; removed.Add(news); }
            //        //if (!isMatchFound) distinctNewsList.Add(news);
            //    }
            //    catch (Exception ex)
            //    {
            //        ShowNotify(ex.Message);
            //        continue;
            //    }
            //}
            //if (removed.Any())
            //    DocHelper.GenerateDocument(removed);
        }

        internal static List<Model.News> SendRequestToCisco(string urls)
        {
            try
            {
                string cookie = "utag_main=" +
                                "v_id:9991fa2ddd3600a531d0f5aaf9c804073002e06b00bd0$_sn:1$_ss:0$_st:9520325941541$ses_id:9520320765240%3Bexp-session$_pn:10%3Bexp-session$vapi_domain:cisco.com$dc_visit:1$dc_event:1%3Bexp-session$dc_region:eu-central-1%3Bexp-session";

                string ciscoJsonRaw;
                using (var wc = new WebClient())
                {
                    wc.Headers["User-Agent"] = "Mozilla/5.0 (Windows; U; MSIE 9.0; Windows NT 9.0; en-US)";
                    wc.Headers.Add(HttpRequestHeader.Cookie, cookie);
                    ciscoJsonRaw = wc.DownloadString(urls);
                }

                var news = ciscoJsonRaw.JsonToModel<Model.CiscoJsonModel>().Select(c => new Model.News(c)).ToList();
                news.RemoveAll(n => n.PublishDate < DateTime.Today.AddDays(-4));
                if (news.Any()) Console.WriteLine($"[+] Cisco: {news.Count}");

                return news;
            }
            catch (Exception exception)
            {
                ShowNotify(exception.Message, exception.StackTrace, "Cisco");
                return new List<Model.News>();
            }
        }


        internal static List<Model.News> SendRequestToExploitDb(string url)
        {
            try
            {
                string xmlListOfNews;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
                request.Headers[HttpRequestHeader.AcceptLanguage] = "en-US,en;q=0.9,fa;q=0.8";
                request.UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3325.181 Safari/537.36";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    xmlListOfNews = reader.ReadToEnd();
                }

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlListOfNews);
                var xmlElement = xmlDoc.DocumentElement;

                if (xmlElement == null)
                    throw new NoNullAllowedException("xml Parsing Failed!");

                var rawXml = xmlElement.SelectNodes("channel/item");


                List<Model.News> newsModel = (from XmlNode news in rawXml
                                              select new Model.News
                                              {
                                                  Identifier = GetId(news),
                                                  Url = GetUrl(news),
                                                  Title = news.SelectSingleNode("title").InnerText.Substring(news.SelectSingleNode("title").InnerText.IndexOf(']') + 2),
                                                  Description = news.SelectSingleNode("description").InnerText,
                                                  PublishDate = news.SelectSingleNode("pubDate").InnerText.ToDate(),
                                                  Supplier = Model.NewsSupplier.ExploitDb,
                                                  //NewsCategory = Model.NewsCategory.Exploit
                                              }).ToList();

                newsModel.RemoveAll(n => n.PublishDate < DateTime.Today.AddDays(-4));
                if (newsModel.Any()) Console.WriteLine($"[+] ExploitDb: {newsModel.Count}");

                return newsModel;
            }
            catch (Exception exception)
            {
                ShowNotify(exception.Message, exception.StackTrace, "Exploit-DB");
                return new List<Model.News>();
            }
        }

        internal static List<Model.News> SendRequestToHP(string url)
        {
            try
            {
                #region Get Website Content

                string rssContent;
                using (var wc = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls |
                                       SecurityProtocolType.Tls11 |
                                       SecurityProtocolType.Tls12;
                    rssContent = wc.DownloadString(url);
                }

                #endregion

                #region Extract News

                #endregion
                var categories = @"3COM Security Bulletins
                            3rd Party Software Security Bulletins
                            HP General SW Security Bulletins
                            HP Hardware and Firmware Security Bulletins
                            HP MPE/iX Security Bulletins
                            Multi-Platform Software Security Bulletins
                            HP NonStop Servers Security Bulletins
                            HP OpenVMS Security Bulletins
                            ProCurve Security Bulletins
                            HP Storage SW Security Bulletins
                            HP Tru64 UNIX Security Bulletins


                            HP-UX UNIX Security Bulletins";
                CsQuery.CQ dom = rssContent;
                var tables = dom["table"].Has("tr");

                var news = new List<Model.News>();
                foreach (var table in tables)
                {
                    if (!categories.Contains(table.Attributes["title"] ?? "none"))
                        continue;

                    var tb = table.ChildNodes.Where(cn => cn.NodeName.Contains("TBODY")).ToList();

                    if (tb.Any())
                        foreach (var tbItems in tb[0].ChildNodes)
                        {
                            var row = tbItems.ChildNodes?.Where(cn => cn?.FirstChild != null).ToList();
                            if (row == null) continue;
                            DateTime date = DateTime.Parse(
                                new System.Text.RegularExpressions.Regex(@"\d{4}\/\d{1,2}\/\d{1,2}")
                                .Match(row[0].InnerHTML).Value);

                            if (date < DateTime.Today.AddDays(-1)) continue;

                            news.Add(new Model.News()
                            {
                                Identifier = row[1].InnerHTML,
                                PublishDate = date,
                                Title = ((CsQuery.Implementation.HtmlAnchorElement)row[3].ChildNodes[0]).InnerHTML,
                                Description = ((CsQuery.Implementation.HtmlAnchorElement)row[3].ChildNodes[0]).InnerHTML,
                                Url = "http://support.hpe.com" + ((CsQuery.Implementation.HtmlAnchorElement)row[3].ChildNodes[0]).Href,
                                Supplier = Model.NewsSupplier.HP,
                                //NewsCategory = Model.NewsCategory.Advisory
                            });
                        }
                }
                if (news.Any()) Console.WriteLine($"[+] HP: {news.Count}");

                return news;
            }
            catch (Exception exception)
            {
                ShowNotify(exception.Message, exception.StackTrace, "HP");
                return new List<Model.News>();
            }
        }



        internal static int UpdateNewsDb()
        {
            try
            {
                #region Get News

                var distinctNewsList = new List<Model.News>();

                var hpLink = "https://support.hpe.com/portal/site/hpsc/public/kb/secBullArchive";
                var exploitDbLink = "https://www.exploit-db.com/rss.xml";
                var ciscoLinks = new List<string>
                {
                    "https://tools.cisco.com/security/center/publicationService.x?criteria=exact&cves=&keyword=&last_published_date=&limit=20&offset=0&publicationTypeIDs=6,9&securityImpactRatings=&sort=-day_sir&title=",
                    "https://tools.cisco.com/security/center/publicationService.x?criteria=exact&cves=&keyword=&last_published_date=&limit=20&offset=0&publicationTypeIDs=1,3&securityImpactRatings=&sort=-day_sir&title="
                };

                var newsSourceList = new List<string>
                {
                    "https://rss.packetstormsecurity.com/files/",
                    "https://www.us-cert.gov/ncas/current-activity.xml",
                    "https://www.us-cert.gov/ncas/alerts.xml",
                    "https://ics-cert.us-cert.gov/alerts/alerts.xml",
                    "https://ics-cert.us-cert.gov/advisories/advisories.xml",
                    "https://searchsecurity.techtarget.com/rss/Security-Wire-Daily-News.xml",
                    "https://www.vulnerability-lab.com/rss/rss.php", //6
                    "https://www.securityfocus.com/rss/vulnerabilities.xml", //7
                    "http://securityaffairs.co/wordpress/feed", //8
                    "https://vulners.com/rss.xml", //9
                    "https://exploit.kitploit.com/feeds/posts/default?alt=rss",
                    "https://www.huawei.com/en/rss-feeds/psirt/rss",
                    "https://www.novell.com/newsfeeds/rss/patches/security_notifications-daily.xml",
                    "https://www.oracle.com/ocom/groups/public/@otn/documents/webcontent/rss-otn-sec.xml",
                    "https://www.secnews24.com/feed/"
                };

                var cisco = new List<Model.News>();
                var ciscoCommon = new List<Model.News>();
                var exploitDb = new List<Model.News>();
                var listOfNews = new List<Model.News>();
                var archiveNews = new List<Model.News>();
                var hp = new List<Model.News>();

                lock (Program.Updating)
                    Parallel.Invoke(
                        () => exploitDb = SendRequestToExploitDb(exploitDbLink),
                        () => cisco = SendRequestToCisco(ciscoLinks[0]),
                        () => ciscoCommon = SendRequestToCisco(ciscoLinks[1]),
                        () => hp = SendRequestToHP(hpLink),
                        () => archiveNews = GetNewsOffline(0),
                        () =>
                        {
                            object lockHelper = new object();
                            Parallel.ForEach(newsSourceList, (source) =>
                            {
                                lock (lockHelper)
                                {
                                    var news = GetNewsOnline(source);
                                    listOfNews.AddRange(news);
                                }
                            });
                        }
                        );
                listOfNews.AddRange(exploitDb.Concat(ciscoCommon).Concat(cisco).Concat(hp));

                #endregion

                var tempList = listOfNews.ToList();

                var newItemsFetched = 0;
            again:
                distinctNewsList = new List<Model.News>();
                #region Check DB
                Parallel.ForEach(GetNewsOffline(0), (a) =>
                {
                    RemoveArchivedItems(ref tempList, a);
                });
                #endregion

                RemoveDuplicateNews(ref distinctNewsList, tempList);

                #region Archive News

                if (distinctNewsList.Count > 0)
                {
                    newItemsFetched += distinctNewsList.Count;
                    Console.WriteLine("\n" + distinctNewsList.Count + " new item(s) added \n");
                    distinctNewsList.ForEach(item => Console.WriteLine("\t - " + item.Title));
                    lock (LockHelper) DbHelper.InsertNews(CategorizeNewsByAttackType(distinctNewsList));
                    goto again;
                }
                #endregion

                return newItemsFetched;
            }
            catch (Exception exception)
            {
                ShowNotify(exception.Message, exception.StackTrace, "Update News db");
                Thread.Sleep(TimeSpan.FromSeconds(5));
                return 0;
            }
        }


        private static List<Model.News> SendRequestToTwitter(string url)
        {
            try
            {
                var res = GetNewsOnline(url, Model.NewsSupplier.Twitter);
                foreach (var newse in from newse in res
                                      let urls = newse.Title.Substring(newse.Title.IndexOf("http"))
                                      select newse)
                {
                    using (var client = new HttpClient())
                    {
                        var response = client.GetAsync(newse.Url).Result;
                        url = response.RequestMessage.RequestUri.ToString();
                    }
                    newse.Url = url;
                }

                return res;
            }
            catch (Exception exception)
            {
                ShowNotify(exception.Message, exception.StackTrace, "Twitter");
                return new List<Model.News>();
            }
        }
        private static void RemoveArchivedItems(ref List<Model.News> newsItems, Model.News archivedNews)
        {
            lock (LockHelper)
                try
                {
                    int count = 0;
                again:
                    newsItems.RemoveAll(p => p.Identifier == archivedNews.Identifier);
                    if (newsItems.Any(i => (i.Identifier == archivedNews.Identifier) && i.Supplier == archivedNews.Supplier))
                        if (count++ < 5) goto again;

                }
                catch (Exception exception)
                {
                    ShowNotify(exception.Message, exception.Source, "Remove archived News");
                }
        }

        internal static void ShowNotify(Model.InstructionModel model)
        {
            using (var notify = new NotifyIcon())
            {
                notify.Visible = true;

                notify.BalloonTipTitle = model.Title;
                notify.BalloonTipText = model.Description;

                notify.Icon = SystemIcons.Information;
                notify.ShowBalloonTip(10000);

            }
        }
        internal static void ShowNotify(string message)
        {
            ShowNotify(new Model.InstructionModel
            {
                Description = message
            });
        }
        internal static void ShowNotify(string errorMessage, string stackTrace, string where)
        {
            var id = Math.Abs(Guid.NewGuid().GetHashCode()).ToString().Substring(0, 4);
            //ShowNotify(new Model.InstructionModel
            //{
            //    Title = errorMessage,
            //    Description = stackTrace,
            //    Icon = ToolTipIcon.Warning
            //});
            Console.WriteLine($"[-] {id} - {where}: Failed!");
            try
            {
                File.AppendAllText("NewsWatchLog",
                    $"[-] {id} {DateTime.Now.ToString()}\n" +
                    $"{errorMessage}\n" +
                    $"{stackTrace}\n" +
                    $"{new string('*', 50)}\n");
            }
            catch (Exception) { }
        }

    }
}
