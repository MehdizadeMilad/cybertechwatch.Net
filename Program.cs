using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using CertTechWatchBot.Control;
using Helper = CertTechWatchBot.Control.Helper;
using Timer = System.Timers.Timer;

namespace CertTechWatchBot
{
    class Program
    {
        #region Import DLL
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        #endregion




        #region Static Properties
        public static Timer NewsUpdaterScheduller = new Timer(new TimeSpan(0, 0, 5, 0).TotalMilliseconds);// 900,000 ms => 15 minutes
        internal static object Updating = "lockHelper";
        //public static Timer UserManagementTime = new Timer(new TimeSpan(0, 1, 0, 0).TotalMilliseconds);
        #endregion

        static void Main(string[] args)
        {
            try
            {
                ShowWindow(GetConsoleWindow(), SW_HIDE);

                NewsUpdaterScheduller.Elapsed += (sender, eventArgs) => StartUpdating();
                NewsUpdaterScheduller.Start();

                new Thread(() =>
                {
                    #region Hide to Notify Area
                    var contextMenu = new ContextMenu();

                    #region Context Menu Items

                    var menuItems = new[]
                        {
                    new MenuItem("Update news archive",(s,e)=>StartUpdating()),
                    new MenuItem("Generate Document",(s,e)=> GenerateDoc()),
                    new MenuItem ("Hide",(s, e) => ShowWindow(GetConsoleWindow(),SW_HIDE)),
                    new MenuItem("Exit", (s,e) => Environment.Exit(1)),
                    };

                    #endregion
                    var ni = new NotifyIcon
                    {
                        Icon = new System.Drawing.Icon(@"..\..\Res\MainIcon.ico"),
                        ContextMenu = contextMenu,
                        BalloonTipText = @"Hey There",
                        Visible = true
                    };

                    contextMenu.MenuItems.AddRange(menuItems);



                    ni.DoubleClick += delegate
                    {
                        ShowWindow(GetConsoleWindow(), SW_SHOW);
                    };

                    Application.Run();
                    #endregion

                }).Start();

                DbHelper.CreateDb();

                while (true)
                    if (Console.Read() == 'x')
                        Environment.Exit(1);
            }
            catch { }
        }

        private static void GenerateDoc()
        {
            Console.WriteLine($"Generating Doc on {DateTime.Now}");
            var news = Helper.CategorizeNewsByAttackType(Helper.GetUnGeneratedNews());
            if (!news.Any())
            {
                Helper.ShowNotify("No News to generate Document");
                return;
            }

            DocHelper.GenerateDocument(news.Take(100).ToList());
            if (news.Count > 100)
                DocHelper.GenerateDocument(news.Skip(100).ToList());
            Helper.ShowNotify("TechWatch document generated");

            new Thread(() =>
            {
                DbHelper.UpdateDbSetNewsReadBit(news);
            }).Start();
        }

        private static void StartUpdating()
        {
            Console.WriteLine($"Getting updates on {DateTime.Now}");
            var freshNewsCount = Helper.UpdateNewsDb();
            if (freshNewsCount > 0)
                Helper.ShowNotify(freshNewsCount + " fresh news downloaded");
            Console.WriteLine($"Update finished on {DateTime.Now}");
            Console.WriteLine(new string('*', 50));
        }
    }

}
