using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using CertTechWatchBot.DL;
using Spire.Doc.Documents;
using Spire.Doc;
using System.Text.RegularExpressions;

namespace CertTechWatchBot.Control
{
    internal static class DocHelper
    {
        private const string HeaderStyleName = "HeaderStyle";
        private const string TitleStyleName = "TitleStyle";
        private const string UrlStyleName = "UrlStyle";

        public static void GenerateDocument(List<Model.News> news)
        {
            try
            {
                var document = new Document();
                var section = document.AddSection();

                #region Styling Document
                var headerStyle = new ParagraphStyle(document)
                { Name = HeaderStyleName };
                headerStyle.CharacterFormat.FontName = "Calibri";
                headerStyle.CharacterFormat.FontSize = 16;
                headerStyle.CharacterFormat.TextColor = Color.CornflowerBlue;
                headerStyle.CharacterFormat.CharacterSpacing = 0;
                headerStyle.CharacterFormat.Bold = true;
                document.Styles.Add(headerStyle);

                var titleStyle = new ParagraphStyle(document)
                { Name = TitleStyleName };
                titleStyle.CharacterFormat.FontName = "Calibri";
                titleStyle.CharacterFormat.FontSize = 14;
                //titleStyle.ParagraphFormat.BeforeSpacing = 6;
                titleStyle.CharacterFormat.Bold = true;
                titleStyle.CharacterFormat.TextColor = Color.Black;
                document.Styles.Add(titleStyle);

                var urlStyle = new ParagraphStyle(document)
                { Name = UrlStyleName };
                urlStyle.CharacterFormat.FontName = "Calibri";
                urlStyle.CharacterFormat.FontSize = 7;
                urlStyle.CharacterFormat.TextColor = Color.Black;
                document.Styles.Add(urlStyle);
                #endregion

                var mainParagraph = section.AddParagraph();
                var category = news[0].AttackCategory;
                mainParagraph.AppendText($"{category.GetEnumDescription()}");
                mainParagraph.ApplyStyle(HeaderStyleName);
                //mainParagraph.BreakCharacterFormat.FontName = "Calibri";

                //DbHelper.UpdateDbSetNewsReadBit(news);

                foreach (var newsItem in news)
                {
                    if (newsItem.AttackCategory != category)
                    {
                        category = newsItem.AttackCategory;
                        mainParagraph.AppendText("\n" + new string('*', 50));

                        mainParagraph = section.AddParagraph();
                        //mainParagraph.AppendText($"{category.GetEnumDescription()} ({news.Count(n => n.AttackCategory == category)})");
                        mainParagraph.AppendText($"{category.GetEnumDescription()}");
                        mainParagraph.ApplyStyle(HeaderStyleName);
                    }

                    mainParagraph = section.AddParagraph();
                    //mainParagraph.AppendHyperlink(newsItem.Url, newsItem.Title, HyperlinkType.WebLink);
                    mainParagraph.AppendText((newsItem.Title.Length <= 15 && newsItem.Title.Contains("CVE")) ? StripTagsCharArray(newsItem.Description) : newsItem.Title);
                    mainParagraph.ApplyStyle(TitleStyleName);

                    mainParagraph = section.AddParagraph();
                    //mainParagraph.AppendText(newsItem.Url);
                    mainParagraph.AppendText(newsItem.PublishDate.ToString(CultureInfo.InvariantCulture) + Environment.NewLine);
                    mainParagraph.AppendHyperlink(newsItem.Url, newsItem.Url, HyperlinkType.WebLink);
                    mainParagraph.ApplyStyle(UrlStyleName);
                }
                SaveDoc(document);
            }
            catch (Exception exception)
            {
                Helper.ShowNotify(exception.Message, exception.StackTrace, "Generating Document");
            }
        }
        private static string ParseHtml(string html)
        {
            const string HTML_TAG_PATTERN = "<.*?>";
            return Regex.Replace(html, HTML_TAG_PATTERN, string.Empty);
        }

        /// <summary>
        /// Remove HTML Tags from string
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private static string StripTagsCharArray(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;
            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }


        public static void SaveDoc(Document document, int totalNewsCount = 0)
        {
            document.SaveToFile
                ($"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Tech Watch - {DateTime.Now.Day} {DateTime.Now.ToString("MMM")}.docx", FileFormat.Docx);
        }

    }
}
