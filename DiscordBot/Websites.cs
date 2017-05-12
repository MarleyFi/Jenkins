using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Discord;

namespace DiscordBot
{
    internal class Websites
    {
        #region Methods

        public void AddWebsite(string url, string tags = "")
        {
            Jenkins.Database.Tables["WEBSITES"].Rows.Add(url, tags.ToLower(), tags.Contains("nsfw") ? true : false);
            Jenkins.Write();
        }

        public string GetWebsite(string[] tags)
        {

            var websites = Jenkins.Database.Tables["WEBSITES"].AsEnumerable();
            var foundWebsites = websites;
            System.Text.RegularExpressions.Regex regEx = new System.Text.RegularExpressions.Regex(@"\\");
            foreach (var tag in tags)
            {
                foundWebsites = foundWebsites.Where(ws => ExtractTagsToList(ws.Field<string>("TAGS")).AsEnumerable().Any(tg => new Regex(@"/*" + tag + @"*").IsMatch(tg))); // ||
                //ExtractTagsToList(ws.Field<string>("TAGS")).AsEnumerable().Contains(tag) ||
                //ExtractTagsToList(ws.Field<string>("TAGS")).AsEnumerable().Any(tg => tg.StartsWith(tag)) ||
                //ExtractTagsToList(ws.Field<string>("TAGS")).AsEnumerable().Any(tg => tg.EndsWith(tag)));
            }

            if (foundWebsites.Count() >= 1)
            {
                string answer = "";
                var website = foundWebsites.First();
                if (website.Field<string>("TAGS").Count() >= 1)
                {
                    answer = ConcatTagsForUsers(website.Field<string>("TAGS") + "\r\n");
                }
                return answer + website.Field<string>("URL");
            }
            else
            {
                websites = Jenkins.Database.Tables["WEBSITES"].AsEnumerable()
                .Where(ws => ws.Field<string>("URL").ToLower().Contains(tags.First().ToLower()));
                if (websites.Count() >= 1)
                {
                    return websites.First().Field<string>("URL");
                }
            }
            return "Not website found for **" + ConvertTagsForDatabase(tags, false) + "**";
        }

        public string GetRandomWebsite(bool nsfw = false)
        {
            DataTable websitesTable = Jenkins.Database.Tables["WEBSITES"];
            var websites = websitesTable.AsEnumerable();
            websites = websites.Where(r => r.Field<bool>("NSFW").Equals(nsfw));
            Random rnd = new Random();
            string answer = "";
            var website = websites.ElementAt(rnd.Next(0, websites.Count()));
            if (website.Field<string>("TAGS").Count() >= 1)
            {
                answer = ConcatTagsForUsers(website.Field<string>("TAGS") + "\r\n");
            }
            return answer + website.Field<string>("URL");
        }


        public string ListWebsites()
        {
            var websiteRows = Jenkins.Database.Tables["WEBSITES"].AsEnumerable();
            return Supporter.BuildList("Websites", websiteRows.Select(ws => ws.Field<string>("URL")).ToArray());
        }

        public string DelWebsite(string keyword)
        {
            var websites = Jenkins.Database.Tables["WEBSITES"].AsEnumerable().Where(ws => ws.Field<string>("URL").ToLower().Contains(keyword.ToLower()));
            if (websites.Count() == 1)
            {
                websites.First().Delete();
                Jenkins.Write();
                return "`" + websites.First().Field<string>("URL") + "` was deleted";
            }
            return "Not website found for **" + keyword + "**";
        }

        private string ConcatTagsForUsers(string tags)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var tag in ExtractTagsToList(tags))
            {
                sb.Append("#**" + tag + "** ");
            }
            return sb.ToString();
        }

        public string ConvertTagsForDatabase(string[] tags, bool skipFirst = true)
        {
            var tagList = tags.AsEnumerable().ToList();
            if (skipFirst)
                tagList.RemoveAt(0);

            if (tagList.Count == 0)
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();
            foreach (var tag in tagList)
            {
                if (!tag.EndsWith(","))
                {
                    sb.Append(tag + " ");
                }
                else
                {
                    sb.Append(tag);
                }
            }
            return sb.ToString().Remove(sb.Length - 1);
        }

        public string[] ExtractTagsToList(string tags)
        {
            return tags.Split(',');
        }

        #endregion Methods
    }
}