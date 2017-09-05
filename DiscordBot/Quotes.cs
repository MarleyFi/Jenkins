using System;
using System.Data;
using System.Linq;
using System.Text;
using Discord;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace DiscordBot
{
    internal class Quotes
    {
        #region Methods

        public void AddQuote(User user, string message, string owner)
        {
            Jenkins.Database.Tables["QUOTES"].Rows.Add(user.Id, message, owner, DateTime.Now);
            Jenkins.Write();
        }

        public string GetRandomQuote()
        {
            DataTable quotesTable = Jenkins.Database.Tables["QUOTES"];
            var quotes = quotesTable.AsEnumerable();
            Random rnd = new Random();
            var quote = quotes.ElementAt<DataRow>(Supporter.GetRandom(quotes.Count()));
            return Supporter.BuildQuote(quote["QUOTE"].ToString(), quote["OWNER"].ToString());
        }

        public string GetQuote(string message)
        {
            DataTable quotesTable = Jenkins.Database.Tables["QUOTES"];
            var quotes = quotesTable.AsEnumerable();
            quotes = quotes.Where(r => r.Field<string>("MESSAGE").ToLower().Contains(message.ToLower()));
            var quote = quotes.FirstOrDefault();
            return Supporter.BuildQuote(quote["MESSAGE"].ToString(), quote["OWNER"].ToString());
        }

        public string GetOwnerIdByName(string name)
        {
            DataTable ownersTable = Jenkins.Database.Tables["OWNERS"];
            var owners = ownersTable.AsEnumerable();
            owners = owners.Where(r => r.Field<string>("name").ToLower().Contains(name.ToLower()));
            return owners.FirstOrDefault()["ID"].ToString();
        }

        public string GetOwnerNameById(string id)
        {
            DataTable ownersTable = Jenkins.Database.Tables["OWNERS"];
            var owners = ownersTable.AsEnumerable();
            owners = owners.Where(r => r.Field<string>("ID").ToLower().Contains(id.ToLower()));
            return owners.FirstOrDefault()["NAME"].ToString();
        }

        public string GetQuoteOf(string owner)
        {
            DataTable quotesTable = Jenkins.Database.Tables["QUOTES"];
            var quotes = quotesTable.AsEnumerable();
            quotes = quotes.Where(r => r.Field<string>("OWNER").ToLower().Contains(owner.ToLower()));
            Random rnd = new Random();
            var quote = quotes.ElementAt<DataRow>(Supporter.GetRandom(quotes.Count()));
            return Supporter.BuildQuote(quote["MESSAGE"].ToString(), quote["OWNER"].ToString());
        }

        public DataRow[] GetQuotesOf(string owner)
        {
            DataTable quotesTable = Jenkins.Database.Tables["QUOTES"];
            var quotes = quotesTable.AsEnumerable();
            quotes = quotes.Where(r => r.Field<string>("OWNER").ToLower().Contains(owner.ToLower()));
            return quotes.ToArray<DataRow>();
        }

        public string ListQuotes(User user, ulong serverId)
        {
            DataTable quotesTable = Jenkins.Database.Tables["QUOTES"];
            var quotes = quotesTable.AsEnumerable();
            if (!Jenkins.Users.IsUserAdmin(user.Id, serverId))
            {
                quotes = quotes.Where(r => r.Field<ulong>("USERID").Equals(user.Id));
            }
            int index = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<- - - **Quotes** - - ->");
            foreach (var quote in quotes)
            {
                sb.Append("- [");
                sb.Append(index);
                sb.Append("] '");
                sb.Append(quote.Field<string>("MESSAGE"));
                sb.Append("'");
                sb.AppendLine();
                sb.Append("by ");
                sb.Append("*" + quote.Field<string>("OWNER") + "*");
                sb.AppendLine();
                index++;
            }
            if (quotes.Count() == 0)
            {
                sb.AppendLine("There are no quotes yet :(");
                sb.AppendLine("PS: You can add some with /addQuote");
            }
            return sb.ToString();
        }

        public string GetQuoteStatistics()
        {
            DataTable quotesTable = Jenkins.Database.Tables["QUOTES"];
            var quotes = quotesTable.AsEnumerable();
            var quotesOfUser = quotes.GroupBy(x => x.Field<string>("OWNER"))
                        .Where(group => group.Count() >= 1)
                        .Select(group => group.Key);

            Dictionary<string, int> quotesPerUser = new Dictionary<string, int>();

            foreach (string owner in quotesOfUser)
            {
                quotesPerUser.Add(owner, GetQuotesCountOfOwner(owner, quotes));
            }

            var sortedQuoteDictionary = quotesPerUser.OrderByDescending(quote => quote.Value);
            StringBuilder sb = new StringBuilder().AppendLine("<- - - **Quote statistics** - - ->");
            sb.AppendLine();
            foreach (var item in sortedQuoteDictionary)
            {
                sb.AppendLine("- > **" + item.Key + "** " + item.Value + (item.Value >= 2 ? " quotes" : " quote") + " - **" + Supporter.GetPercentageString(item.Value, quotes.Count()) + "**");
            }
            sb.AppendLine();
            sb.AppendLine("**" + quotes.Count() + "** total count of quotes");
            return sb.ToString();
        }

        

        private int GetQuotesCountOfOwner(string owner, EnumerableRowCollection<DataRow> quotes)
        {
            return quotes.Where(quote => quote.Field<string>("OWNER").Equals(owner)).Count();
        }

        public void DelQuote(User user, int index, ulong serverId)
        {
            DataTable quotesTable = Jenkins.Database.Tables["QUOTES"];
            var quotes = quotesTable.AsEnumerable();
            if (!Jenkins.Users.IsUserAdmin(user.Id, serverId))
            {
                quotes = quotes.Where(r => r.Field<string>("USERID").Equals(user.Id));
            }
            quotes.ElementAt<DataRow>(index).Delete();
            Jenkins.Write();
        }

        #region Quotes-API

        public void SyncQuotes()
        {
            List<QuoteDAO> quotes = new List<QuoteDAO>();
            var response = Bot.HttpClient.GetStringAsync("http://api.h2591678.stratoserver.net?action=allQuotes");
            response.Wait();
            string responseString = response.Result;
            var objects = JArray.Parse(responseString);
            foreach (var quoteObject in objects)
            {
                quotes.Add(quoteObject.ToObject<QuoteDAO>());
            }

            DataTable quotesTable = Jenkins.Database.Tables["QUOTES"];
            var quotesEnum = quotesTable.AsEnumerable();

            foreach (var quoteEnum in quotesEnum)
            {
                string quoteText = quoteEnum.Field<string>("MESSAGE").Replace("'", "´");
                string ownerText = quoteEnum.Field<string>("OWNER");
                DateTime datecreated = quoteEnum.Field<DateTime>("REGISTERDATE");
                if (findQuote(quoteText) == null)
                {
                    newQuote(quoteText, ownerText, "0", datecreated.ToString("yyyy-MM-dd HH:mm:ss"));
                }
            }



            //QuoteDAO[] quotes = resultObject..ToObject<WeatherDAO>();
            //var values = new Dictionary<string, string>
            //{
            //    { "action", "allQuotes" }
            //};

            //var content = new FormUrlEncodedContent(values);

            //var postResponse = Bot.HttpClient.PostAsync("http://api.h2591678.stratoserver.net/index.php", content);
            //postResponse.Wait();
            //var result = postResponse.Result;
        }

        public QuoteDAO findQuote(string keyword)
        {
            var response = Bot.HttpClient.GetStringAsync(string.Format("http://api.h2591678.stratoserver.net?action=quoteWith&keyword={0}", keyword));
            response.Wait();
            string responseString = response.Result;
            if(responseString == "[]")
            {
                return null;
            }
            var quoteArray = JArray.Parse(responseString);
            QuoteDAO quote = quoteArray.First().ToObject<QuoteDAO>();
            return quote;
        }

        public QuoteDAO quoteByOwner(string owner)
        {
            var response = Bot.HttpClient.GetStringAsync(string.Format("http://api.h2591678.stratoserver.net?action=quoteOf&owner={0}", owner));
            response.Wait();
            string responseString = response.Result;
            if (responseString == "[]")
            {
                return null;
            }
            var quoteArray = JArray.Parse(responseString);
            QuoteDAO quote = quoteArray.First().ToObject<QuoteDAO>();
            return quote;
        }

        public List<QuoteDAO> quotesByOwner(string owner)
        {
            List<QuoteDAO> quotes = new List<QuoteDAO>();
            var response = Bot.HttpClient.GetStringAsync(string.Format("http://api.h2591678.stratoserver.net?action=quotesOf&owner={0}", owner));
            response.Wait();
            string responseString = response.Result;
            if (responseString == "[]")
            {
                return null;
            }
            var quoteArray = JArray.Parse(responseString);
            foreach (var quoteObject in quoteArray)
            {
                quotes.Add(quoteObject.ToObject<QuoteDAO>());
            }
            return quotes;
        }

        public QuoteDAO randomQuote(int rating = 0)
        {
            List<QuoteDAO> quotes = new List<QuoteDAO>();
            var response = Bot.HttpClient.GetStringAsync(string.Format("http://api.h2591678.stratoserver.net?action=randomQuote&keyword={0}", rating));
            response.Wait();
            string responseString = response.Result;
            if (responseString == "[]")
            {
                return null;
            }
            var quoteArray = JArray.Parse(responseString);
            QuoteDAO quote = quoteArray.First().ToObject<QuoteDAO>();
            return quote;
        }

        public void newQuote(string quote, string owner, string rating = "0", string datecreated = "")
        {
            var values = new Dictionary<string, string>
            {
                { "action", "newQuote" },
                { "quote", quote },
                { "owner", owner },
                { "rating", rating },
                { "datecreated", datecreated }
            };

            var content = new FormUrlEncodedContent(values);

            var postResponse = Bot.HttpClient.PostAsync("http://api.h2591678.stratoserver.net/index.php", content);
            postResponse.Wait();
            var result = postResponse.Result;
        }

        public string quoteStatistics()
        {
            List<QuoteStatDAO> quoteStats = new List<QuoteStatDAO>();
            var response = Bot.HttpClient.GetStringAsync("http://api.h2591678.stratoserver.net?action=quoteStatistics");
            response.Wait();
            string responseString = response.Result;
            if (responseString == "[]")
            {
                return null;
            }
            var quoteArray = JArray.Parse(responseString);
            foreach (var quoteObject in quoteArray)
            {
                quoteStats.Add(quoteObject.ToObject<QuoteStatDAO>());
            }
            StringBuilder sb = new StringBuilder().AppendLine("<- - - **Quote statistics** - - ->");
            sb.AppendLine();
            int quotesCount = quoteStats.Sum(stat => stat.quotes);
            foreach (var quoteStat in quoteStats)
            {
                sb.AppendLine("- > **" + quoteStat.name + "** " + quoteStat.quotes + (quoteStat.quotes >= 2 ? " quotes" : " quote") + " - **" + Supporter.GetPercentageString(quoteStat.quotes, quotesCount) + "**" + (quoteStat.rating == null ? string.Empty :" average rating: **"+ quoteStat.rating+"/5**").ToString());
            }
            sb.AppendLine();
            sb.AppendLine("**" + quotesCount + "** total count of quotes");
            return sb.ToString();
        }

        #endregion Quotes-API

        #endregion Methods
    }
}