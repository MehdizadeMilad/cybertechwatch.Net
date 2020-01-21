using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using CertTechWatchBot.DL;

namespace CertTechWatchBot.Control
{
    internal static class DbHelper
    {
        #region User Manager

        public static void RequestForRegisterUser(Model.Users userInfo)
        {
            //using (var db = new NewsDb())
            //{
            //    db.Users.Add(new DL.User
            //    {
            //        TelegramId = userInfo.TelegramId,
            //        UserName = userInfo.UserName,
            //        Role = (int)Model.UserRole.Guest,
            //        IsActive = false,
            //        RegisterDate = DateTime.Now
            //    });
            //    db.SaveChanges();
            //}

            //TODO inform Admin

        }

        public static Model.Users GetUserInfo(long telegramId)
        {
            //using (var db = new NewsDb())
            //{
            //    var userInfo = db.Users.FirstOrDefault(u => u.TelegramId == telegramId);
            //    if (userInfo?.TelegramId > 0)
            //    {
            //        return new Model.Users
            //        {
            //            TelegramId = userInfo.TelegramId,
            //            UserRole = (Model.UserRole)userInfo.Role,
            //            Id = userInfo.id,
            //            UserName = userInfo.UserName,
            //            RegisterDate = userInfo.RegisterDate,
            //            IsActive = userInfo.IsActive
            //        };
            //    }
            //    return new Model.Users();
            //}

            return null;
        }


        internal static void ApproveUser(long telegramId)
        {
            //using (var db = new NewsDb())
            //{
            //    var user = db.Users.Single(u => u.TelegramId == telegramId);
            //    user.IsActive = true;
            //    user.Role = (int)Model.UserRole.NewsReader;

            //    db.SaveChanges();
            //}

            //TelegramHelper.NotifyApprovedRequest(telegramId);
        }

        #endregion

        #region News Manager

        public static List<Model.News> SelectNews(int dayBeforeToday = 0)
        {
            var threshold = DateTime.Today.AddDays(dayBeforeToday);
            var newsList = new List<Model.News>();

            using (var db = new SQLiteConnection("Data Source=NewsArchive.db;Version=3;"))
            {
                db.Open();
                var query = "select * from News";

                var cmd = new SQLiteCommand(query, db);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                    newsList.Add(new Model.News
                    {
                        Identifier = reader["Identifier"].ToString(),
                        PublishDate = reader["PublishDate"].ToString().ToDate(),
                        Supplier = (Model.NewsSupplier)reader["Supplier"].ToString().ToInteger(),
                        Title = reader["Title"].ToString(),
                        Description = reader["Description"].ToString(),
                        Url = reader["Url"].ToString(),
                        Reported = reader["Reported"].ToString() == "True" ? true : false,
                        //NewsCategory = reader["NewsCategory"].ToString().IsFilled() ?
                        //            (Model.NewsCategory)Enum.Parse(typeof(Model.NewsCategory), reader["NewsCategory"].ToString())
                        //            : Model.NewsCategory.None
                    });
            }

            var dbnews = dayBeforeToday == 0 ? newsList.ToList() :
                newsList.Where(n => n.PublishDate >= threshold).ToList();

            return dbnews;
        }

        public static void InsertNews(List<Model.News> listOfNews)
        {
            if (listOfNews.Count <= 0) return;

            #region Insert News
            using (var db = new SQLiteConnection("Data Source=NewsArchive.db;Version=3;"))
            {
                db.Open();

                var query = "";
                SQLiteCommand cmd;

                foreach (var newsItem in listOfNews)
                {
                    try
                    {
                        query = $"insert into News (Identifier, Url, Title, Supplier, Description, PublishDate, Reported, NewsCategory)" +
                          $"VALUES" +
                          $"(      '{newsItem.Identifier}'," +
                                $" '{newsItem.Url}'," +
                                $" '{newsItem.Title.Replace("\'", "")}'," +
                                $" '{(int)newsItem.Supplier}'," +
                                $" '{newsItem.Description.Replace("\'", "")}'," +
                                $" '{newsItem.PublishDate.ToString("yyyy-MM-dd hh:mm:ss")}', " +
                                $"'False'," +
                                $"'{(int)newsItem.NewsCategory}')";

                        cmd = new SQLiteCommand(query, db);
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception)
                    {

                    }
                }
                #endregion
            }
        }


        internal static void CreateDb()
        {
            using (var db = new SQLiteConnection("Data Source=NewsArchive.db;Version=3;"))
            {
                db.Open();

                var query = "";
                SQLiteCommand cmd;

                try
                {
                    query = @"CREATE TABLE [News] ( 
                        [Id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                        [Identifier] varchar(200)  NOT NULL,
                        [Url] varchar(500)  NOT NULL,
                        [Title] varchar(500)  NOT NULL,
                        [Supplier] int NOT NULL,
                        [Description] varchar(500) NOT NULL,
                        [PublishDate] varchar(50) NOT NULL,
                        [Reported] bit default False,
                        [NewsCategory] varchar(50) NULL,
                        CONSTRAINT  uniqueIdentifier UNIQUE (Identifier)
                        )";
                    cmd = new SQLiteCommand(query, db);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Helper.ShowNotify(e.Message, e.StackTrace, "Create db");
                }
            }
        }

        internal static void UpdateDbSetNewsReadBit(List<Model.News> newsList)
        {

            using (var db = new SQLiteConnection("Data Source=NewsArchive.db;Version=3;"))
            {
                db.Open();

                var query = "";
                SQLiteCommand cmd;

                foreach (var item in newsList)
                {
                    try
                    {
                        query = $"UPDATE News SET Reported = 'True' WHERE Identifier = '{item.Identifier}';";
                        cmd = new SQLiteCommand(query, db);
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        Helper.ShowNotify(e.Message, e.StackTrace, "Update db bit");
                    }
                }
            }

        }

        #endregion

        #region Settings Helper

        public static string GetSettingValue(Model.SettingsValue settingId)
        {
            //using (var db = new NewsDb())
            //{
            //    try
            //    {
            //        return db.Helpers.Single(s => s.SettingId == (int)settingId).Value;
            //    }
            //    catch
            //    {
            //        return "0";
            //    }
            //}
            return null;
        }

        public static void SetSettingValue(Model.SettingsValue settingId, string value, string description = "")
        {
            try
            {
                //using (var db = new NewsDb())
                //{
                //    db.Helpers.Add(new DL.Helper
                //    {
                //        SettingId = (int)settingId,
                //        Value = value,
                //        Description = settingId.GetEnumDescription(),
                //        EntryDate = DateTime.Now
                //    });

                //    db.SaveChanges();
                //}
            }
            catch
            {

                //using (var db = new NewsDb())
                //{
                //    db.Helpers.Single(s => s.SettingId == (int)settingId).Value = value;
                //    db.SaveChanges();
                //}

            }
        }

        #endregion

        public static List<Model.Users> GetListOfUsers(Model.UserRole userRole)
        {
            //var users = new List<Model.Users>();
            //using (var db = new NewsDb())
            //    db.Users.Where(u => u.Role == (int)userRole).ToList().ForEach(u => users.Add(new Model.Users(u)));

            //return users;
            return null;
        }
    }
}
