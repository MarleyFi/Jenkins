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

        public string GetWebsite(string[] tags, string explicitTag = "")
        {
            var websites = Jenkins.Database.Tables["WEBSITES"].AsEnumerable();
            var foundWebsites = websites;
            if (!explicitTag.Equals(string.Empty))
            {
                foundWebsites = websites.Where(r => r.Field<string>("TAGS").Contains(explicitTag.ToLower()));
            }
            foreach (var tag in tags)
            {
                foundWebsites = foundWebsites.Where(ws => ExtractTagsToList(ws.Field<string>("TAGS")).AsEnumerable().Any(tg => new Regex(@"/*" + tag + @"*").IsMatch(tg))); // ||
                //ExtractTagsToList(ws.Field<string>("TAGS")).AsEnumerable().Contains(tag) ||
                //ExtractTagsToList(ws.Field<string>("TAGS")).AsEnumerable().Any(tg => tg.StartsWith(tag)) ||
                //ExtractTagsToList(ws.Field<string>("TAGS")).AsEnumerable().Any(tg => tg.EndsWith(tag)));
            }

            if (foundWebsites.Count() >= 1)
            {
                return GetWebsiteString(foundWebsites.ElementAt(new Random().Next(foundWebsites.Count())));
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
            return "No website found for " + ConcatTagsForUsers(string.Join(",", tags.Select(s => s.Trim())));
        }

        public string GetWebsiteByUrl(string partOfURL)
        {
            var websites = Jenkins.Database.Tables["WEBSITES"].AsEnumerable().Where(website => website.Field<string>("URL").ToLower().Contains(partOfURL.ToLower()));
            return websites.Count() >= 1 ? GetWebsiteString(websites.First()) : "";
        }

        public string GetRandomWebsite(string explicitTag = "", bool nsfw = false)
        {
            DataTable websitesTable = Jenkins.Database.Tables["WEBSITES"];
            var websites = websitesTable.AsEnumerable();
            websites = websites.Where(r => r.Field<bool>("NSFW").Equals(nsfw));

            if (!explicitTag.Equals(string.Empty))
            {
                if(explicitTag.ToLower().Equals("pr0"))
                {
                    websites = websites.Where(r => r.Field<string>("URL").ToLower().Contains("pr0gramm.com"));
                }
                else
                {
                    websites = websites.Where(r => r.Field<string>("TAGS").Contains(explicitTag.ToLower()));
                }
            }
            return GetWebsiteString(websites.ElementAt(new Random().Next(websites.Count())));
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
            return "No website found for **" + keyword + "**";
        }

        private string GetWebsiteString(DataRow website)
        {
            string answer = "";
            if (!website.Field<string>("TAGS").Contains("website") && website.Field<string>("TAGS").Count() >= 1)
            {
                answer = ConcatTagsForUsers(website.Field<string>("TAGS") + "\r\n");
            }
            return answer + website.Field<string>("URL");
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