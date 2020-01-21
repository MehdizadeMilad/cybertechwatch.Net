using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CertTechWatchBot.DL;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CertTechWatchBot.Control
{
    internal static class TelegramHelper
    {
        internal static void SendListOfNewsById(Message message, List<Model.News> news, bool isUpdate = false)
        {
            SendListOfNewsById(message.Chat.Id, news, isUpdate);
        }
        internal static void SendListOfNewsById(string id, List<Model.News> news, bool isUpdate = false)
        {
            SendListOfNewsById(Int64.Parse(id), news, isUpdate);
        }
        internal static void SendListOfNewsById(long id, List<Model.News> news, bool isUpdate = false)
        {
            var datee = DateTime.Today.AddDays(DateTime.Today.ToString("dddd") == "Saturday" ? -3 : -1);
            var dateSuffix = datee.Day % 10 == 1 && datee.Day != 11 ? "st" :
                datee.Day % 10 == 2 && datee.Day != 12 ? "nd" :
                    datee.Day % 10 == 3 && datee.Day != 13 ? "rd" :
                    "th";
            var requestedDate = isUpdate ?
                    String.Format("{0:dd}{1} {0:MMMM}", DateTime.Now, dateSuffix) :
                    String.Format("{0:dd}{1} {0:MMMM}", datee, dateSuffix);

            if (!news.Any())
            {
                SendMessageToClientById(id, $"No News Messages since {requestedDate}");
                return;
            }

            var previousCategory = news.FirstOrDefault()?.AttackCategory;
            var messageToSend = isUpdate ?
                $"Updated on {news.FirstOrDefault()?.PublishDate}\n<b>{previousCategory}</b>\n\n" :
                $"List of News <i>since {requestedDate}</i>:\n<b>{previousCategory}</b>\n\n";

            foreach (var newsItem in news)
            {
                Thread.Sleep(250);
                if (!newsItem.AttackCategory.Equals(previousCategory))
                {
                    SendMessageToClientById(id, messageToSend);
                    previousCategory = newsItem.AttackCategory;
                    messageToSend = $"<b>{previousCategory}</b>\n\n";
                }
                messageToSend += $"{newsItem.Title.Replace("<", "before").Replace(">", "after")}\n{newsItem.PublishDate}\n{newsItem.Url}\n\n";
            }

            SendMessageToClientById(id, messageToSend);
        }


        internal static void IntroMessage(Message message)
        {
            SendMessageToClientById(message.Chat.Id, $"Welcome {message.Chat.FirstName} {message.Chat.LastName}\n" +
                                                     $"To get latest news, please touch /news");
        }

        internal static void NotifyApprovedRequest(long telegramId)
        {
            SendMessageToClientById(telegramId, $"Congratulations,\n Your account has been approved.\n" +
                                               $"Press /news to get The lates news ");
        }
        public static async Task<Message> SendInlineMessageToClient(long userId, string messageText, InlineKeyboardMarkup key = null)
        {
            try
            {
                var res = await Program.Tel.SendTextMessageAsync(userId, messageText, replyMarkup: key);
                return res;
            }
            catch (Exception exception)
            {
                if (exception.Message.ToLower().Contains("block"))
                {
                    return null;
                }
                throw;
            }
        }

        public static async void SendMessageToClientById(
                                                            long id,
                                                            string messageText,
                                                            ReplyKeyboardMarkup key = null,
                                                            [CallerMemberName] string methodName = null,
                                                            [CallerFilePath] string sourceFile = null,
                                                            [CallerLineNumber] int lineNumber = 0
            )
        {
            try
            {
                if (messageText.Length > 4096 && messageText.Length < 8193)
                {
                    var part1 = messageText.Substring(0, 4096);
                    var part2 = messageText.Substring(4097);

                    await SendFormattedMessageToClientById(id, part1);
                    await SendFormattedMessageToClientById(id, part2);
                }
                else
                {
                    await SendFormattedMessageToClientById(id, messageText);
                }
            }
            catch (Exception)
            {
                //LogEvent(exception);
            }

        }

        private static async Task<Message> SendFormattedMessageToClientById(long id, string messageText)
        {
            return await Program.Tel.SendTextMessageAsync(id, messageText, ParseMode.Html, disableWebPagePreview: true);
        }

        public static async void UpdateInlineMessage(Message message, string messageText, InlineKeyboardMarkup key = null)
        {
            var receiverId = message.Chat.Id;
            //var messageId = message.GetUserInfoFromCache().VotingMessageId;
            try
            {
                await Program.Tel.EditMessageTextAsync(receiverId, 00000000, messageText, replyMarkup: key);
            }
            catch (Exception)
            {
                //LogEvent(exception);
            }
        }

        public static async void SendMessageToClient(
                                                        Message message,
                                                        string messageText,
                                                        ReplyKeyboardMarkup key = null,
                                                        [CallerMemberName] string methodName = null,
                                                        [CallerFilePath] string sourceFile = null,
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        long targetId = default(long))
        {
            try
            {
                var receiverId = message?.Chat?.Id ?? targetId;

                #region message with Key attached

                if (key?.Keyboard != null)
                {
                    if (messageText.Length > 4096 && messageText.Length < 8193)
                    {
                        var part1 = messageText.Substring(0, 4096);
                        var part2 = messageText.Substring(4097);

                        await Program.Tel.SendTextMessageAsync(receiverId, $"1) {Environment.NewLine}" + part1);
                        await Program.Tel.SendTextMessageAsync(receiverId, $"2) {Environment.NewLine} " + part2, replyMarkup: key);
                        return;
                    }
                    if (messageText.Length > 8192)
                    {
                        var part1 = messageText.Substring(0, 4096);
                        var part2 = messageText.Substring(4097, 4096);
                        var part3 = messageText.Substring(8193);

                        await Program.Tel.SendTextMessageAsync(receiverId, $"1) {Environment.NewLine}" + part1);
                        await Program.Tel.SendTextMessageAsync(receiverId, $"2) {Environment.NewLine} " + part2);
                        await Program.Tel.SendTextMessageAsync(receiverId, $"3) {Environment.NewLine} " + part3, replyMarkup: key);
                        return;
                    }

                    await Program.Tel.SendTextMessageAsync(receiverId, messageText, replyMarkup: key);
                }
                #endregion

                else
                {
                    if (messageText.Length > 4096)
                    {
                        var part1 = messageText.Substring(0, 4096);
                        var part2 = messageText.Substring(4097);
                        await Program.Tel.SendTextMessageAsync(receiverId, part1);
                        await Program.Tel.SendTextMessageAsync(receiverId, part2);
                        return;
                    }
                    await Program.Tel.SendTextMessageAsync(receiverId, messageText);
                }
            }
            #region Catch

            catch (Exception exception)
            {
                exception.Source = $"{sourceFile} - {methodName} - {lineNumber}";
                try
                {
                    if (exception.Message.ToLower().Contains("block"))
                    {
                        return;
                    }

                    await Program.Tel.SendTextMessageAsync(message?.Chat?.Id ?? 0, "پوزش، دوباره امتحان کنید");
                }
                catch (Exception)
                {
                }
                //LogEvent(exception);
                throw;
            }
            #endregion
        }

        public static InlineKeyboardMarkup InlineIntro()
        {
            //var url = new InlineKeyboardButton
            //{
            //    Url = "http://www.eligasht.com/contactus/",
            //    Text = Resources.ContactUs
            //};
            //var url1 = new InlineKeyboardButton
            //{
            //    Url = "http://www.eligasht.com",
            //    Text = Resources.Website
            //};
            //var url2 = new InlineKeyboardButton
            //{
            //    Url = "http://www.eligasht.com/blog",
            //    Text = Resources.Weblog
            //};
            //var keyboard = new InlineKeyboardMarkup(new[]
            //{
            //        new[]
            //        {
            //            url
            //        },
            //        new[]
            //        {
            //            url1
            //        },
            //        new[]
            //        {
            //            url2
            //        }
            //    });
            return null;
        }
        internal static async Task SendFlashMessage(string queryId, string message, bool showAlert = false)
        {
            await Program.Tel.AnswerCallbackQueryAsync(queryId, message, showAlert);
        }

        internal static async void OnMessageReceived(object sender, MessageEventArgs e)
        {
            var message = e.Message;
            if (message.Text.Trim().Sanitize() == "/start")
            {
                IntroMessage(message);
                return;
            }

            if (!message.Chat.Username.IsFilled())
            {
                SendMessageToClientById(message.Chat.Id, "You need to specify UserName for your Telegram account.");
                return;
            }

            if (!Helper.IsUserAuthentic(message)) return;


            if (!Helper.IsUserBehaviourNormal(message)) return;
            if (message.Chat.Type == ChatType.Group)
            {
            }




            switch (message.Text)
            {
                case "update":
                    Helper.UpdateNewsDb();
                    break;

                case "news":
                    //var news = DbHelper.SelectNews((DateTime.Today.ToString("dddd") == "Saturday") ? 3 : 1);
                    var news = Helper.GetNewsOffline();
                    SendListOfNewsById(message, news);
                    break;

                case "cert":
                    var usSert = Helper.GetNewsOnline("https://www.us-cert.gov/ncas/all.xml", Model.NewsSupplier.UsCert);
                    SendListOfNewsById(message, usSert);
                    break;

                case "Twitter":
                    var twitterUrl = Helper.GetNewsOnline("https://twitrss.me/twitter_user_to_rss/?user=CVEnew", Model.NewsSupplier.Twitter);
                    SendListOfNewsById(message, twitterUrl);
                    break;
            }

        }

        internal static void OnReceiveError(object sender, ReceiveErrorEventArgs args)
        {
            try
            {
                //DataAccess.LogEvent(new Exception(
                //                $"ARGS:\n{args.Serialize()}SENDER:\n{sender.Serialize()}{new string('*', 30)}"));
            }
            catch (Exception)
            {
            }
        }

        internal static async void OnCallbackQueryReceived(object sender, CallbackQueryEventArgs e)
        {
            try
            {
                var adminsResponse = e.CallbackQuery.Data;

                if (adminsResponse.IsNumeric())
                {
                    DbHelper.ApproveUser(Int64.Parse(adminsResponse));
                    SendFlashMessage(e.CallbackQuery.Id, "The User activated.");
                    return;
                }

            }
            #region Catch

            catch (Exception exception)
            {
                //DataAccess.LogEvent(exception);
            }

            #endregion
        }

        internal static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs args)
        {
        }

        internal static void InitializeTelegramAPI()
        {
            Program.Tel.OnMessage += OnMessageReceived;
            Program.Tel.OnReceiveError += OnReceiveError;
            Program.Tel.OnCallbackQuery += OnCallbackQueryReceived;
            Program.Tel.OnMessageEdited += OnMessageReceived;
            Program.Tel.OnInlineResultChosen += BotOnChosenInlineResultReceived;

            

            //Program.UserManagementTime.Elapsed += (sender, eventArgs) => Helper.ClearOnlineUsersCache();
            //Program.UserManagementTime.Start();

            Program.Tel.StartReceiving();
        }
    }
}
