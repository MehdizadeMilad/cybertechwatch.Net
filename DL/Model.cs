using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using CertTechWatchBot.Control;

namespace CertTechWatchBot.DL
{
    internal static class Model
    {
        #region Last Telegram Message ID
        private static long _LastMessageId;

        public static long LastMsgId
        {
            get
            {
                return _LastMessageId == 0 ?
                    (_LastMessageId = DbHelper.GetSettingValue(SettingsValue.LastMessageId).ToInteger()) :
                    _LastMessageId;

            }
            set
            {
                if (_LastMessageId >= value) return;
                _LastMessageId = value;
                DbHelper.SetSettingValue(SettingsValue.LastMessageId, value.ToString());
            }
        }
        #endregion

        internal class News : ICloneable<News>
        {
            #region Private Properties

            private AttackCategory _attackCategory;
            private string _title;

            #endregion

            #region public Properties
            public int Id { get; set; }
            public string Url { get; set; }
            public string Description { get; set; }
            public NewsSupplier Supplier { get; set; }
            public DateTime PublishDate { get; set; }

            public bool Reported { get; set; }

            /// <summary>
            /// to determine if this news item is duplicate or not
            /// </summary>
            public bool IsDuplicate { get; set; }
            #endregion

            #region public Computing Properties

            public string Identifier { get; set; }

            public string Title
            {
                get { return _title; }//.RemoveAttackName(AttackCategory, NewsCategory); }
                set { _title = value; }
            }

            /// <summary>
            /// Exploit Type
            /// </summary>
            public AttackCategory AttackCategory
            {
                get
                {
                    return NewsCategory == NewsCategory.Exploit
                    ? DetermineAttackCategory(Description)
                    : (NewsCategory == NewsCategory.Advisory ? AttackCategory.Advisory : AttackCategory.Others);
                }
                set { _attackCategory = value; }
            }


            /// <summary>
            /// Advisory
            /// Exploits
            /// Tools
            /// </summary>
            public NewsCategory NewsCategory
            {
                get
                {
                    return Supplier == NewsSupplier.ExploitDb ? NewsCategory.Exploit : DetermineNewsCategory(Description);
                }
            }

            public NewsCategory DetermineNewsCategory(string newsDescription)
            {
                newsDescription = newsDescription.ToLower();

                var advisoryKeyWords = new List<string>
                { "advisory", "security notice", "security advisories", "bulletin", "security fix", "multiple", "update" };
                foreach (var key in advisoryKeyWords)
                    if (newsDescription.Contains(key))
                        return NewsCategory.Advisory;

                if (newsDescription.Contains("tool"))
                    return NewsCategory.Tool;

                var exploitKeyWords = new List<string> { "payload", "suffer", "vulnerability", "exploit", "vulnerable", "risk", "patch", "payload" };
                foreach (var key in exploitKeyWords)
                    if (newsDescription.Contains(key))
                        return NewsCategory.Exploit;

                return NewsCategory.None;
            }

            public AttackCategory DetermineAttackCategory(string newsDescription)
            {
                newsDescription = newsDescription.ToLower();

                if (newsDescription.Contains("xss") ||
                    newsDescription.Contains("cross site scripting") ||
                    newsDescription.Contains("cross-site scripting"))
                    _attackCategory = AttackCategory.Xss;

                else if (newsDescription.Contains("csrf") ||
                         newsDescription.Contains("cross site request forgery"))
                    _attackCategory = AttackCategory.CSRF;

                else if (newsDescription.Contains("sql"))
                    _attackCategory = AttackCategory.Sqli;

                else if (newsDescription.Contains("buffer") || newsDescription.Contains("overflow") ||
                    newsDescription.Contains("format string"))
                    _attackCategory = AttackCategory.BufferOverflow;

                else if (newsDescription.Contains("command execution"))
                    _attackCategory = AttackCategory.CommandExecution;

                else if (newsDescription.Contains("command injection"))
                    _attackCategory = AttackCategory.CommandInjection;

                else if (newsDescription.Contains("arbitrary code") ||
                    newsDescription.Contains("code execution") ||
                    newsDescription.Contains("execute arbitrary code"))
                    _attackCategory = AttackCategory.CodeExecution;

                else if (newsDescription.Contains("dos") || newsDescription.Contains("denial of service"))
                    _attackCategory = AttackCategory.DenialOfService;

                else if (newsDescription.Contains("directory traversal") ||
                    newsDescription.Contains("path traversal"))
                    _attackCategory = AttackCategory.DirectoryTraversal;

                else if (newsDescription.Contains("lfi") ||
                    newsDescription.Contains("file inclusion"))
                    _attackCategory = AttackCategory.LocalFileInclusion;

                else if (newsDescription.Contains("rfi") ||
                    newsDescription.Contains("file inclusion"))
                    _attackCategory = AttackCategory.RemoteFileInclusion;

                else if (newsDescription.Contains("leak") ||
                    newsDescription.Contains("disclosure") ||
                    newsDescription.Contains("expose") ||
                    newsDescription.Contains("sensitive information"))
                    _attackCategory = AttackCategory.InformationExposure;

                else if (newsDescription.Contains("privilege escalation") ||
                         newsDescription.Contains("privilege") ||
                         newsDescription.Contains("escalation"))
                    _attackCategory = AttackCategory.PrivilegeEscalation;

                else if (newsDescription.Contains("use-after-free"))
                    _attackCategory = AttackCategory.UseAfterFree;

                if (_attackCategory == AttackCategory.Advisory)
                    _attackCategory = AttackCategory.Others;

                return _attackCategory;
            }

            #endregion

            public News(CiscoJsonModel jsonNews)
            {
                Identifier = jsonNews.Identifier;
                Title = jsonNews.Title;
                Description = jsonNews.Name + Environment.NewLine + jsonNews.Summary;
                Supplier = NewsSupplier.CiscoCert;
                Url = jsonNews.Url;
                PublishDate = jsonNews.LastPublished;
            }

            public News()
            {
            }

            public News Clone()
            {
                return new News
                {
                    Id = Id,
                    AttackCategory = AttackCategory,
                    Description = Description,
                    Identifier = Identifier,
                    PublishDate = PublishDate,
                    Supplier = Supplier,
                    Title = Title,
                    Url = Url,
                    Reported = Reported
                };
            }
        }

        internal class CiscoJsonModel
        {
            public string Id { get; set; }
            public string Identifier { get; set; }
            public string Title { get; set; }
            public string Version { get; set; }
            public DateTime FirstPublished { get; set; }
            public DateTime LastPublished { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
            public string Severity { get; set; }
            public string Workarounds { get; set; }
            public string Cwe { get; set; }
            public string Cve { get; set; }
            public string CiscoBugId { get; set; }
            public string Status { get; set; }
            public string Summary { get; set; }
            public string TotalCount { get; set; }
            public object RelatedResource { get; set; }
        }


        internal class Users
        {
            public int Id { get; set; }
            public long TelegramId { get; set; }
            public string UserName { get; set; }
            public DateTime RegisterDate { get; set; }
            public UserRole UserRole { get; set; }
            public bool IsActive { get; set; }
            private string _textEntered;
            public string TextEntered
            {
                get { return _textEntered.Sanitize(); }
                set
                {
                    //if The User just came, dont judge as a threat; 
                    LastChange = DateTime.Now.AddSeconds(-10);
                    _textEntered = value.Sanitize();
                }
            }
            public DateTime LastChange { get; set; }
            public bool IsOld => (DateTime.Now - LastChange).Minutes > 60;
            public bool IsDos
            {
                get
                {
                    var now = DateTime.Now;

                    return (now - LastChange).Seconds < 3;

                }
            }


            public Users()
            {

            }
            //public Users(User user)
            //{
            //    Id = user.id;
            //    TelegramId = user.TelegramId;
            //    UserName = user.UserName;
            //    RegisterDate = user.RegisterDate;
            //    UserRole = (UserRole)user.Role;
            //    IsActive = user.IsActive;
            //}

        }

        public class InstructionModel
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public ToolTipIcon Icon { get; set; }
        }

        #region Enums

        public enum NewsSupplier
        {
            None,
            PacketStorm,
            CxSecurity,
            CiscoCert,
            UsCert,
            TechTarget,
            ExploitDb,
            Twitter,
            VulnerableLab,
            NVD,
            SecFocus,
            SecAffair,
            Vulners,
            Microsoft,
            HP,
            Kitploit,
            Oracle,
            Novel,
            Huawei,
            Sec24
        }

        public enum UserRole
        {
            Guest,
            NewsReader,
            Admin
        }

        public enum NewsCategory
        {
            None,
            Exploit,
            Advisory,
            Tool
        }

        public enum SettingsValue
        {
            [Description("Last Telegram Message ID")]
            LastMessageId,

        }

        public enum AttackCategory
        {
            [Description("Advisory")]
            Advisory,
            [Description("Buffer Overflow")]
            BufferOverflow,
            [Description("Code Execution")]
            CodeExecution,
            [Description("Command Execution")]
            CommandExecution,
            [Description("Command Injection")]
            CommandInjection,
            [Description("Cross Site Request Forgery")]
            CSRF,
            [Description("Denial Of Service")]
            DenialOfService,
            [Description("Directory Traversal")]
            DirectoryTraversal,
            [Description("Information Exposure")]
            InformationExposure,
            [Description("Local File Inclusion")]
            LocalFileInclusion,
            [Description("Others")]
            Others,
            [Description("Remote File Inclusion")]
            RemoteFileInclusion,
            [Description("SQL Injection")]
            Sqli,
            [Description("Cross Site Scripting")]
            Xss,
            [Description("Privilege Escalation")]
            PrivilegeEscalation,
            [Description("Use After Free")]
            UseAfterFree
        }


        #endregion
    }

    internal interface ICloneable<T>
    {
        T Clone();
    }
}
