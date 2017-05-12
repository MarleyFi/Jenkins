using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace DiscordBot
{
    internal class FunFacts
    {
        private string[] types =
        {
            "trivia",
            "math",
            "date",
            "year"
        };

        public string TryParseType(string arg)
        {
            int n;
            Regex r = new Regex("^[a-zA-Z0-9]*$");
            if (int.TryParse(arg, out n))
            {
                return "math";
            }
            else if (arg.Contains("/") || IsMonth(arg))
            {
                return "date";
            }
            else if (r.IsMatch(arg))
            {
                return "trivia";
            }
            else
            {
                Bot.NotifyDevs("Could not parse argument: " + arg);
                return "trivia";
            }
        }

        private bool IsMonth(string arg)
        {
            DateTime dt;
            return DateTime.TryParse(arg, out dt);
        }

        private string ConvertDate(string arg)
        {
            DateTime dt;
            if (DateTime.TryParse(arg, out dt))
            {
                return dt.ToString("MM/dd").Replace('.', '/');
            }
            else
            {
                Bot.NotifyDevs("Could not parse date '" + arg + "' in " + this.GetType().FullName);
                return "random";
            }
        }

        public string GetFunFact(out string info, string term = "random", string type = "")
        {
            if (type == "" || !types.Contains(type))
            {
                type = TryParseType(term);
                if (type == "date")
                    term = ConvertDate(term);
            }

            using (var webClient = new WebClient())
            {
                string request = string.Format("http://numbersapi.com/{0}/{1}"
                   , term
                   , type);
                try
                {
                    info = "#FunFact about **" + term + "**";
                    return webClient.DownloadString(request);
                }
                catch (Exception e)
                {
                    Bot.NotifyDevs(Supporter.BuildExceptionMessage(e, this.GetType().FullName, request));
                    info = "";
                    return string.Empty;
                }
            }
        }
    }
}