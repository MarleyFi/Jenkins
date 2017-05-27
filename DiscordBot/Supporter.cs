using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Discord;

namespace DiscordBot
{
    public static class Supporter
    {
        public static bool YesOrNo(int probability)
        {
            Random rnd = new Random();
            return rnd.Next(0, probability) == 0 ? true : false;
        }

        public static int GetRandom(int max)
        {
            Random rnd = new Random();
            int random = 0;
            for (int i = 0; i < rnd.Next(3, 10); i++)
            {
                random = rnd.Next(0, max);
            }
            return random;
        }

        public static int GetRandom(int min, int max)
        {
            Random rnd = new Random();
            return rnd.Next(min, max);
        }

        public static string RollDice()
        {
            Random rnd = new Random();
            string emoji;
            int randomInt = rnd.Next(1, 6);
            switch (randomInt)
            {
                case 1:
                    emoji = ":one:";
                    break;

                case 2:
                    emoji = ":two:";
                    break;

                case 3:
                    emoji = ":three:";
                    break;

                case 4:
                    emoji = ":four:";
                    break;

                case 5:
                    emoji = ":five:";
                    break;

                case 6:
                    emoji = ":six:";
                    break;

                default:
                    emoji = ":game_die:";
                    break;
            }
            return string.Format("I diced {0}"
                , emoji);
        }

        public static string BuildInsult(string insult, string name)
        {
            return (insult.Replace("*", name));
        }

        public static string BuildList(string name, string[] items)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("<- - - **{0}** - - ->", name));

            foreach (var item in items)
            {
                sb.Append("- > ");
                sb.AppendLine(item);
            }
            return sb.ToString();
        }

        public static string BuildList(string name, List<string> items)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("<- - - **{0}** - - ->", name));

            foreach (var item in items)
            {
                sb.Append("- > ");
                sb.AppendLine(item);
            }
            return sb.ToString();
        }

        public static string BuildQuote(string message, string owner)
        {
            return string.Format("'**{0}**'\r\n- {1}"
                , message
                , owner);
        }

        public static string BuildStats(string name, int msgCount, int talkedToMe, int commandCount, DateTime lastActivity, DateTime registerDate, int moronPerc)
        {
            return string.Format("<- - - **{0}'s** stats - - ->\r\n"
                + "Messages sent: **{1}**\r\n"
                + "{0} talked to me **{2} times**\r\n"
                + "I've worked for {0} **{3} times**\r\n"
                + "Last activity was **{4}**\r\n"
                + "First time seen was {5}\r\n"
                + "Moron: {6}\r\n",
                name,
                msgCount,
                talkedToMe,
                commandCount,
                GetTimeAgo(lastActivity),
                registerDate.ToLongDateString(),
                BarBuilder(moronPerc, 100)
                );
        }

        public static string BuildFoodOption(string krz, string name, string desc = null, string days = null, string info = null, bool showDayTag = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("**");
            sb.Append(name);
            sb.Append(" - [**");
            sb.Append(krz);
            sb.Append("**]**");

            if (days != null && showDayTag) // DayTags
            {
                sb.Append(" | ");
                foreach (var day in days.Split(','))
                {
                    string dayString = day.Trim(' ');
                    if (dayString != Food.Day.AllDays.ToString())
                    {
                        sb.Append("**[**");
                        sb.Append(dayString);
                        sb.Append("**]** ");
                    }
                    else
                    {
                        sb.Append("**[**");
                        sb.Append("AllDays");
                        sb.Append("**]** ");
                    }
                }
            }
            sb.AppendLine();

            if (desc != null)
            {
                sb.Append("*");
                sb.Append(desc);
                sb.AppendLine("*");
            }

            if (info != null)
            {
                sb.AppendLine();
                sb.AppendLine("**→** " + info);
            }
            sb.AppendLine("> - - - - - - - - - - - - - - <");
            return sb.ToString();
        }

        public static string BuildFoodOptionWithoutFormation(string krz, string name, string desc = null, string days = null, string info = null, bool showDayTag = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name);
            sb.Append(" - [");
            sb.Append(krz);
            sb.Append("]");

            if (days != null && showDayTag) // DayTags
            {
                sb.Append(" | ");
                foreach (var day in days.Split(','))
                {
                    string dayString = day.Trim(' ');
                    if (dayString != Food.Day.AllDays.ToString())
                    {
                        sb.Append("[");
                        sb.Append(dayString);
                        sb.Append("] ");
                    }
                    else
                    {
                        sb.Append("[AllDays]");
                    }
                }
            }
            sb.AppendLine();

            if (desc != null)
            {
                sb.AppendLine();
                sb.Append(desc);
            }

            if (info != null)
            {
                sb.AppendLine();
                sb.AppendLine("→ " + info);
            }
            //sb.AppendLine("> - - - - - - - - - - - - - - <");
            return sb.ToString();
        }

        public static string BuildDebugUserMessage(string message, User user, Channel channel)
        {
            if (channel.IsPrivate)
            {
                return string.Format("User **{1}** used in **{3}** at *{2}*\r\n- >**{0}**"
                , message
                , user.Name
                , DateTime.Now.ToLongTimeString()
                , "private-chat");
            }
            else
            {
                return string.Format("User **{1}** used in **#{3}** on **{4}** at *{2}*\r\n- >**{0}**"
                , message
                , user.Name
                , DateTime.Now.ToLongTimeString()
                , channel.Name
                , channel.Server.Name);
            }
        }

        public static string BuildLogMessage(MessageEventArgs msg)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("```");
            sb.Append(msg.Server.Name);
            sb.Append(" in #");
            sb.Append(msg.Channel.Name);
            sb.Append(" at ");
            sb.AppendLine(msg.Message.Timestamp.ToLongTimeString());
            sb.Append(msg.User.Name);
            sb.Append(": ");
            sb.Append(msg.Message.Text);
            sb.AppendLine("```");
            return sb.ToString();
        }

        public static string RemoveMention(string text)
        {
            if (!text.Contains("@"))
            {
                return text;
            }
            string result = text.Replace("@Jenkins", string.Empty);
            if (result.Length > 0)
            {
                result.Remove(0, 1);
            }
            return result;
        }

        public static DateTime GetParsedDateTime(string time)
        {
            var DailyTime = Supporter.ValidateTime(time) ? time : "12:00:00";
            var timeParts = DailyTime.Split(new char[1] { ':' });

            var dateNow = DateTime.Now;
            return (new DateTime(dateNow.Year, dateNow.Month, dateNow.Day,
                       int.Parse(timeParts[0]), int.Parse(timeParts[1]), int.Parse(timeParts[2])));
        }

        public static DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            // The (... + 7) % 7 ensures we end up with a value in the range [0, 6]
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
        }

        public static double Celcius(double f)
        {
            double c = 5.0 / 9.0 * (f - 32);

            return c;
        }

        public static List<string> SplitMessage(string text)
        {
            List<string> messageParts = new List<string>();
            string cuttedMessage = text;
            int index = 0;
            while (cuttedMessage.Length > 2000)
            {
                messageParts.Add(cuttedMessage.Substring(0, 2000));
                cuttedMessage = cuttedMessage.Substring(2000);
                index++;
            }
            messageParts.Add(cuttedMessage);
            return messageParts;
        }

        public static string BuildStreamBroadcast(DataRow stream)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("**");
            sb.Append(stream.Field<string>("CHANNEL"));
            sb.Append("** ");
            sb.Append("is now streaming ");
            sb.Append("**");
            sb.Append(stream.Field<string>("GAME"));
            sb.Append("** ");
            sb.AppendLine("here: ");
            sb.Append(stream.Field<string>("URL"));
            return sb.ToString();
        }

        public static string BarBuilder(int value, int max)
        {
            StringBuilder bar = new StringBuilder();
            double barValue = 0.0;

            bar.Append("[");
            barValue = (Convert.ToDouble(value)) / (Convert.ToDouble(max)) * 100;

            for (int i = 1; i < 50; i++)
            {
                bar.Append((barValue >= i) ? "|" : ":");
            }
            bar.Append(Convert.ToInt32(barValue) + "%");
            for (int i = 50; i < 100; i++)
            {
                bar.Append((barValue >= i) ? "|" : ":");
            }
            bar.Append("]");
            return bar.ToString();
        }

        public static string GetTimeAgo(DateTime time)
        {
            const int SECOND = 1;
            const int MINUTE = 60 * SECOND;
            const int HOUR = 60 * MINUTE;
            const int DAY = 24 * HOUR;
            const int MONTH = 30 * DAY;

            var ts = new TimeSpan(DateTime.Now.Ticks - time.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * MINUTE)
                return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";

            if (delta < 2 * MINUTE)
                return "a minute ago";

            if (delta < 45 * MINUTE)
                return ts.Minutes + " minutes ago";

            if (delta < 90 * MINUTE)
                return "an hour ago";

            if (delta < 24 * HOUR)
                return ts.Hours + " hours ago";

            if (delta < 48 * HOUR)
                return "yesterday";

            if (delta < 30 * DAY)
                return ts.Days + " days ago";

            if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            else
            {
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year ago" : years + " years ago";
            }
        }

        public static string GetTimeSince(DateTime time)
        {
            const int SECOND = 1;
            const int MINUTE = 60 * SECOND;
            const int HOUR = 60 * MINUTE;
            const int DAY = 24 * HOUR;
            const int MONTH = 30 * DAY;

            var ts = new TimeSpan(DateTime.Now.Ticks - time.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * MINUTE)
                return ts.Seconds == 1 ? "one second" : ts.Seconds + " seconds";

            if (delta < 2 * MINUTE)
                return "a minute";

            if (delta < 45 * MINUTE)
                return ts.Minutes + " minutes";

            if (delta < 90 * MINUTE)
                return "an hour";

            if (delta < 24 * HOUR)
                return ts.Hours + " hours";

            if (delta < 48 * HOUR)
                return "yesterday";

            if (delta < 30 * DAY)
                return ts.Days + " days";

            if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month" : months + " months";
            }
            else
            {
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year" : years + " years";
            }
        }

        public static string GetPercentageString(int value, int maximum)
        {
            return ((Convert.ToDouble(value) / Convert.ToDouble(maximum))).ToString("P2");
        }

        public static string GetDuration(this TimeSpan span)
        {
            if (span == TimeSpan.Zero) return "0 minutes";

            var sb = new StringBuilder();
            if (span.Days > 0)
                sb.AppendFormat("{0} day{1} ", span.Days, span.Days > 1 ? "s" : String.Empty);
            if (span.Hours > 0)
                sb.AppendFormat("{0} hour{1} ", span.Hours, span.Hours > 1 ? "s" : String.Empty);
            if (span.Minutes > 0)
                sb.AppendFormat("{0} minute{1} ", span.Minutes, span.Minutes > 1 ? "s" : String.Empty);
            return sb.ToString();
        }

        public static string GetDurationGerman(this TimeSpan span)
        {
            if (span == TimeSpan.Zero) return "0 minuten";

            var sb = new StringBuilder();
            if (span.Days > 0)
                sb.AppendFormat("{0} Tag{1} ", span.Days, span.Days > 1 ? "e" : String.Empty);
            if (span.Hours > 0)
                sb.AppendFormat("{0} Stunde{1} ", span.Hours, span.Hours > 1 ? "n" : String.Empty);
            if (span.Minutes > 0)
                sb.AppendFormat("{0} Minute{1} ", span.Minutes, span.Minutes > 1 ? "n" : String.Empty);
            return sb.ToString();
        }

        public static string BuildExceptionMessage(Exception e, string whileFunction = "", object parameter = null)
        {
            StringBuilder sb = new StringBuilder("Exception thrown");
            if (whileFunction != string.Empty)
                sb.Append(" while **" + whileFunction + "**\r\n");

            sb.AppendLine("Exception: " + e.Message);

            if (e.InnerException != null)
                sb.AppendLine("InnerException: " + e.InnerException.Message);

            if (parameter != null)
            {
                sb.Append("Function-Parameter: ");
                sb.AppendLine(parameter.ToString());
                sb.AppendLine("Typeof: " + parameter.GetType().ToString());
            }

            sb.AppendLine("Occurrence time: " + DateTime.Now.ToLongTimeString());

            return sb.ToString();
        }

        public static string GetFileDateString(DateTime dt)
        {
            return string.Format("{0}_{1}_{2}",
                dt.ToString("MMM", CultureInfo.InvariantCulture),
                dt.Day,//dt.ToString("ddd", CultureInfo.InvariantCulture),
                dt.Year);
        }

        public static bool ValidateTime(string time)
        {
            DateTime ignored;
            return DateTime.TryParseExact(time, "HH:mm:ss",
                                          CultureInfo.InvariantCulture,
                                          DateTimeStyles.None,
                                          out ignored);
        }

        #region Discord

        public static string[] GetAllServerNames()
        {
            var servers = Bot.Client.Servers;
            List<string> serverNames = new List<string>();
            foreach (var server in servers)
            {
                serverNames.Add(server.Name);
            }
            return serverNames.ToArray();
        }

        public static bool TryGetServerNameById(ulong serverId, out string serverName)
        {
            var server = Bot.Client.GetServer(serverId);
            serverName = (server == null ? "" : server.Name);
            return (server != null);
        }

        public static bool TryGetServerById(ulong serverId, out Server server)
        {
            server = Bot.Client.GetServer(serverId);
            return (server != null);
        }

        public static bool TryGetServerIdByName(string name, out ulong serverId)
        {
            var server = Bot.Client.Servers.Where(r => r.Name.ToLower().Contains(name.ToLower())).First();
            serverId = (server == null ? 0 : server.Id);
            return (server != null);
        }

        public static bool TryGetServerByName(string name, out Server server)
        {
            server = Bot.Client.Servers.Where(r => r.Name.ToLower().Contains(name.ToLower())).First();
            return (server != null);
        }

        public static Color GetRandomColor()
        {
            Random random = new Random();
            return new Color(random.Next(256), random.Next(256), random.Next(256));
        }

        #endregion Discord
    }
}