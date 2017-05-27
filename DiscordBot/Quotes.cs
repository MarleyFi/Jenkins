using System;
using System.Data;
using System.Linq;
using System.Text;
using Discord;
using System.Collections.Generic;

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
            return Supporter.BuildQuote(quote["MESSAGE"].ToString(), quote["OWNER"].ToString());
        }

        public string GetQuote(string message)
        {
            DataTable quotesTable = Jenkins.Database.Tables["QUOTES"];
            var quotes = quotesTable.AsEnumerable();
            quotes = quotes.Where(r => r.Field<string>("MESSAGE").ToLower().Contains(message.ToLower()));
            var quote = quotes.FirstOrDefault();
            return Supporter.BuildQuote(quote["MESSAGE"].ToString(), quote["OWNER"].ToString());
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
                sb.AppendLine("- > **" + item.Key + "** " + item.Value + (item.Value >= 2 ? " quotes" : " quote")+" - **"+Supporter.GetPercentageString(item.Value, quotes.Count())+"**");
            }
            sb.AppendLine();
            sb.AppendLine("**"+quotes.Count()+"** total count of quotes");
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

        #endregion Methods
    }
}