using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Discord;
using Newtonsoft.Json.Linq;

namespace DiscordBot
{
    public static class Food
    {

        #region Variables

        public enum Day
        {
            Monday,
            Tuesday,
            Wednesday,
            Thursday,
            Friday,
            Saturday,
            Sunday,
            AllDays
        };

        public static bool IsVoteRunning = false;

        private static Timer nextVoteTimer;

        private static Timer voteEndTimer;

        private static Timer notificationTimer;

        private static DateTime nextVoteEnd;

        public static TimeSpan timeTillVoteStart;

        public static TimeSpan timeTillVoteEnd;

        public static DateTime NextVoteStart;

        public static Channel voteChannel;

        public static Dictionary<ulong, string> userVotes = new Dictionary<ulong, string>();

        public static Dictionary<string, int> votes = new Dictionary<string, int>();

        #endregion Variables

        #region Methods

        public static void ScheduleNextVote(string voteStartString = "", string voteEndString = "")
        {
            DateTime dateNow = DateTime.Now;
            DateTime timeStart = Supporter.GetParsedDateTime(Supporter.ValidateTime(voteStartString) ? voteStartString : Bot.Config.DailyVoteStart);
            DateTime timeEnd = Supporter.GetParsedDateTime(Supporter.ValidateTime(voteEndString) ? voteEndString : Bot.Config.DailyVoteEnd);
            if (timeStart > dateNow)
                timeTillVoteStart = timeStart - dateNow;
            else
            {
                timeStart = timeStart.AddDays(1);
                timeEnd = timeEnd.AddDays(1);// Time already passed
                timeTillVoteStart = timeStart - dateNow;
            }
            if (timeStart.DayOfWeek == DayOfWeek.Saturday)
            {
                timeTillVoteStart = timeTillVoteStart.Add(new TimeSpan(2, 0, 0, 0));
                timeStart = timeStart.AddDays(2);
                timeEnd = timeEnd.AddDays(2);
            }
            else if (timeStart.DayOfWeek == DayOfWeek.Sunday)
            {
                timeTillVoteStart.Add(new TimeSpan(1, 0, 0, 0));
                timeStart = timeStart.AddDays(1);
                timeEnd = timeEnd.AddDays(1);
            }

            NextVoteStart = timeStart;
            SetUpNextVoteTimer(timeTillVoteStart, timeEnd);
        }

        public static void StartVote(DateTime voteEnd, Channel channel)
        {
            if (IsVoteRunning)
            {
                return;
            }
            userVotes.Clear();
            votes.Clear();
            voteChannel = channel;
            DateTime dateNow = DateTime.Now;
            nextVoteEnd = voteEnd;
            if (nextVoteEnd > dateNow)
                timeTillVoteEnd = nextVoteEnd - dateNow;
            else
            {
                nextVoteEnd = nextVoteEnd.AddDays(1); // Time already passed
                timeTillVoteEnd = nextVoteEnd - dateNow;
            }
            SetUpVoteEndTimer(timeTillVoteEnd);
            int totalMinutes = Convert.ToInt32(timeTillVoteEnd.TotalMinutes / 8);
            TimeSpan timeTillNotification = timeTillVoteEnd.Subtract(new TimeSpan(0,
                totalMinutes,
                0));
            SetUpNotificationTimer(timeTillNotification);
            IsVoteRunning = true;
            AnnouceVote();
        }

        private static async void AnnouceVote()
        {
            await voteChannel.SendMessage("Wo wollt Ihr heute essen?\r\n/**food** /**foodAll**");
            var answers = Food.GetFoodOptionsSeparately(false);
            if (answers.Length == 0)
            {
                answers = Food.GetFoodOptionsSeparately(true);
            }
            foreach (var foodOption in answers)
            {
                await voteChannel.SendMessage(foodOption);
            }
            await voteChannel.SendMessage("Abstimmen mit /**vote** <Kürzel>");
            await voteChannel.SendMessage("--> Das Voting ist gültig bis " + nextVoteEnd.ToLongTimeString());
        }

        public static int Vote(string vote, ulong userId, out string foodName)
        {
            if (userVotes.Keys.Contains(userId)) // User hat bereits gevotet
            {
                string oldVote = userVotes[userId];
                votes[oldVote] = votes[oldVote] - 1;
                if (votes[oldVote] == 0)
                {
                    votes.Remove(oldVote);
                }
                userVotes[userId] = vote.ToLower();
                voteChannel.SendMessage("<@" + userId + "> hat seine Stimme geändert zu **" + vote.ToUpper() + "**");
            }
            else // Neuer Vote
            {
                userVotes.Add(userId, vote.ToLower());
                voteChannel.SendMessage("Eine neue Stimme für **" + vote.ToUpper() + "** wurde von <@" + userId + "> abgegeben");
            }

            int count = 0;
            if (votes.TryGetValue(vote.ToLower(), out count))
            {
                votes[vote.ToLower()] = count + 1;
            }
            else
            {
                votes.Add(vote.ToLower(), 1);
            }

            foodName = GetFoodName(vote);

            return count + 1;
        }

        public static string GetVotes()
        {
            List<string> voteStrings = new List<string>();
            foreach (var vote in votes)
            {
                voteStrings.Add(string.Format("**{2}** stimme{3} für **{1}** [**{0}**]",
                    vote.Key.ToUpper(),
                    GetFoodName(vote.Key),
                    vote.Value,
                    vote.Value > 1 ? "n" : ""));
            }
            return Supporter.BuildList("Votes", voteStrings.ToArray()) + "\r\n\r\nVoting endet in **" + Supporter.GetDurationGerman(nextVoteEnd - DateTime.Now) + "**";
        }

        public static void EndVote()
        {
            int votesTotal = userVotes.Count;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("< - - - **Voting beendet** - - - >");
            sb.AppendLine();
            foreach (var vote in votes)
            {
                string usersVotedfor = "";
                foreach (var userVote in userVotes)
                {
                    if (userVote.Value == vote.Key)
                    {
                        usersVotedfor = usersVotedfor + "<@" + userVote.Key + "> ";
                    }
                }
                sb.AppendLine(string.Format("**{0}**/{1} stimmen für **{2}** [**{3}**] -> {4}",
                    vote.Value,
                    votesTotal,
                    GetFoodName(vote.Key),
                    vote.Key.ToUpper(),
                    usersVotedfor));
            }

            if (votesTotal == 0)
            {
                sb.AppendLine("Es wurden keine Stimmen abgegeben");
            }
            string abstinenceUserMentions = "";
            Server server = voteChannel.Server;
            var onlineUsers = server.Users
                .Where(r =>
                r.IsBot.Equals(false))
                .Where(r =>
                r.Status.Value.Equals("online") ||
                r.Status.Value.Equals("idle") ||
                r.Status.Value.Equals("dnd") ||
                r.Status.Value.Equals("invisible"));
            foreach (var user in onlineUsers)
            {
                if (!userVotes.Keys.Contains(user.Id))
                {
                    abstinenceUserMentions = abstinenceUserMentions + user.Mention + " ";
                }
            }
            sb.AppendLine();
            sb.AppendLine("< - - - - - - - - - - - - - - - - - - - - >");
            sb.AppendLine();
            if (abstinenceUserMentions != "")
                sb.AppendLine("Enthalten haben sich " + abstinenceUserMentions);

            voteChannel.SendMessage(sb.ToString());
            IsVoteRunning = false;
            ScheduleNextVote();
        }

        private static void SetUpNextVoteTimer(TimeSpan timeToGo, DateTime nextVoteEnd)
        {
            if (timeToGo < TimeSpan.Zero)
            {
                Bot.NotifyDevs("Unhandled exception(?) in SetUpNextVoteTimer()\r\n" + timeToGo.ToString());
                //SetUpTimer((new TimeSpan(1,0,0,0) - timeToGo.Negate()));
                return;//time already passed
            }
            nextVoteTimer = new System.Threading.Timer(x =>
            {
                StartVote(nextVoteEnd, Bot.Client.GetChannel(Bot.Config.DailyVoteChannel));
            }, null, timeToGo, Timeout.InfiniteTimeSpan);
        }

        private static void SetUpVoteEndTimer(TimeSpan timeToGo)
        {
            if (timeToGo < TimeSpan.Zero)
            {
                Bot.NotifyDevs("Unhandled exception(?) in SetUpVoteEndTimer()\r\n" + timeToGo.ToString());
                //SetUpTimer((new TimeSpan(1,0,0,0) - timeToGo.Negate()));
                return; //time already passed
            }
            voteEndTimer = new System.Threading.Timer(x =>
            {
                EndVote();
            }, null, timeToGo, Timeout.InfiniteTimeSpan);
        }

        private static void SetUpNotificationTimer(TimeSpan timeToGo)
        {
            if (timeToGo < TimeSpan.Zero)
            {
                Bot.NotifyDevs("Unhandled exception(?) in SetUpNotificationTimer()\r\n" + timeToGo.ToString());
                return;
            }
            notificationTimer = new System.Threading.Timer(x =>
            {
                voteChannel.SendMessage("Das Voting läuft noch **" + Supporter.GetDurationGerman(nextVoteEnd - DateTime.Now) + "**\r\nStimmen können noch eingereicht werden mit /**vote** <KRZ>");
                NotifyAbstinenceUsers();
            }, null, timeToGo, Timeout.InfiniteTimeSpan);
            Bot.NotifyDevs("Notification set for " + DateTime.Now.Add(timeToGo).ToLongTimeString() + " in " + Supporter.GetDuration(timeToGo));
        }

        private static async void NotifyAbstinenceUsers()
        {
            Server server = voteChannel.Server;
            var onlineUsers = server.Users
                .Where(r =>
                r.IsBot.Equals(false))
                .Where(r =>
                r.Status.Value.Equals("online") ||
                r.Status.Value.Equals("idle") ||
                r.Status.Value.Equals("dnd") ||
                r.Status.Value.Equals("invisible"));
            if (onlineUsers.Count() == 0)
                return;
            int count = 0;
            foreach (var user in onlineUsers)
            {
                if (!userVotes.Keys.Contains(user.Id))
                {
                    await user.SendMessage(string.Format("Bisher habe ich noch keine Stimme von dir für die Abstimmung auf dem Server **{0}** aufgezeichnet.\r\nDu hast noch bis **{1}** Zeit dein vote abzugeben ansonsten wird deine Stimme als enthalten gezählt.\r\n/**vote** /**food** /**foodAll**",
                        server.Name,
                        nextVoteEnd.ToLongTimeString()));
                    count++;
                }
            }
            Bot.NotifyDevs(string.Format("Notifed {0} users to vote", count));
        }


        public static void AddFoodOption(string krz, string name, string desc = null, IEnumerable<Day> days = null, string info = null)
        {
            var table = Jenkins.Database.Tables["FOODOPTIONS"];
            if (table.Rows.Find(krz) != null)
            {
                return;
            }
            if (desc == "")
                desc = null;

            string daysString = null;
            if (days.Count() == 0)
            {
                days = null;
            }
            else
            {
                foreach (var day in days)
                {
                    daysString = daysString + day.ToString() + ", ";
                }
                daysString = daysString.Remove(daysString.Length - 2);
            }

            if (info == "")
                info = null;

            table.Rows.Add(krz, name, desc, daysString, info);
            Jenkins.Write();
        }

        public static string GetFoodOptions(bool all, string day = null)
        {
            if (day == "")
            {
                day = DateTime.Now.DayOfWeek.ToString();
            }
            Day dayEnum;
            if (!Enum.TryParse(day, true, out dayEnum))
            {
                return day + " isn't a valid day";
            }

            DataTable foodOptionsTable = Jenkins.Database.Tables["FOODOPTIONS"];
            var foodOptionsForDay = foodOptionsTable.AsEnumerable()
                .Where(r => r.Field<string>("DAYS").Contains(dayEnum.ToString()));
            StringBuilder sb = new StringBuilder();
            if (foodOptionsForDay.Count() >= 1)
            {
                sb.AppendLine(string.Format("< - - - **Specific possibilities for {0}** - - ->", day));
                sb.AppendLine();
            }


            foreach (var foodOption in foodOptionsForDay)
            {
                string krz = foodOption.Field<string>("KRZ");
                string name = foodOption.Field<string>("NAME");
                string desc = foodOption.Field<string>("DESC");
                string days = foodOption.Field<string>("DAYS");
                string info = foodOption.Field<string>("INFO");

                sb.Append(Supporter.BuildFoodOption(krz, name, desc, days, info));
                sb.AppendLine();
            }
            if (!all)
                return sb.ToString();

            var foodOptionsEveryday = foodOptionsTable.AsEnumerable()
                .Where(r => r.Field<string>("DAYS").Contains(Day.AllDays.ToString()));

            if (foodOptionsEveryday.Count() >= 1)
            {
                sb.AppendLine("< - - - **General possibilities** - - ->");
                sb.AppendLine();
            }

            foreach (var foodOption in foodOptionsEveryday)
            {
                string krz = foodOption.Field<string>("KRZ");
                string name = foodOption.Field<string>("NAME");
                string desc = foodOption.Field<string>("DESC");
                string days = foodOption.Field<string>("DAYS");
                string info = foodOption.Field<string>("INFO");

                sb.Append(Supporter.BuildFoodOption(krz, name, desc, days, info));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static string[] GetFoodOptionsSeparately(bool all, string day = null)
        {
            List<string> optionsList = new List<string>();
            if (day == "" || day == null)
            {
                day = DateTime.Now.DayOfWeek.ToString();
            }
            Day dayEnum;
            if (!Enum.TryParse(day, true, out dayEnum))
            {
                optionsList.Add(day + " isn't a valid day");
                return optionsList.ToArray();
            }

            DataTable foodOptionsTable = Jenkins.Database.Tables["FOODOPTIONS"];
            var foodOptionsForDay = foodOptionsTable.AsEnumerable()
                .Where(r => r.Field<string>("DAYS").Contains(dayEnum.ToString()));
            StringBuilder sb = new StringBuilder();
            if (foodOptionsForDay.Count() >= 1)
            {
                sb.AppendLine("```ini");
                sb.AppendLine(string.Format("[ - - - Specific possibilities for {0} - - - ]```", day));

                sb.AppendLine();
                optionsList.Add(sb.ToString());
                sb.Clear();
            }


            foreach (var foodOption in foodOptionsForDay)
            {
                string krz = foodOption.Field<string>("KRZ");
                string name = foodOption.Field<string>("NAME");
                string desc = foodOption.Field<string>("DESC");
                string days = foodOption.Field<string>("DAYS");
                string info = foodOption.Field<string>("INFO");
                sb.AppendLine("```http");
                sb.Append(Supporter.BuildFoodOptionWithoutFormation(krz, name, desc, days, info));
                sb.Append("```");
                sb.AppendLine();
                optionsList.Add(sb.ToString());
                sb.Clear();
            }
            if (!all)
                return optionsList.ToArray();

            var foodOptionsEveryday = foodOptionsTable.AsEnumerable()
                .Where(r => r.Field<string>("DAYS").Contains(Day.AllDays.ToString()));

            if (foodOptionsEveryday.Count() >= 1)
            {
                sb.AppendLine("```ini");
                sb.AppendLine("[ - - - General possibilities - - - ]```");
                sb.AppendLine();
                optionsList.Add(sb.ToString());
                sb.Clear();
            }

            foreach (var foodOption in foodOptionsEveryday)
            {
                string krz = foodOption.Field<string>("KRZ");
                string name = foodOption.Field<string>("NAME");
                string desc = foodOption.Field<string>("DESC");
                string days = foodOption.Field<string>("DAYS");
                string info = foodOption.Field<string>("INFO");
                sb.AppendLine("```http");
                sb.Append(Supporter.BuildFoodOptionWithoutFormation(krz, name, desc, days, info));
                sb.Append("```");
                sb.AppendLine();
                optionsList.Add(sb.ToString());
                sb.Clear();
            }

            return optionsList.ToArray();
        }

        public static string GetFoodOption(string krz)
        {
            DataTable foodOptionsTable = Jenkins.Database.Tables["FOODOPTIONS"];
            var foodOptionsForDay = foodOptionsTable.AsEnumerable()
                .Where(r => r.Field<string>("KRZ").ToLower().Equals(krz.ToLower()));
            if (foodOptionsForDay.Count() == 0)
            {
                return "There's no food-option called **" + krz + "**";
            }
            string name = foodOptionsForDay.First().Field<string>("NAME");
            string desc = foodOptionsForDay.First().Field<string>("DESC");
            string days = foodOptionsForDay.First().Field<string>("DAYS");
            string info = foodOptionsForDay.First().Field<string>("INFO");
            return Supporter.BuildFoodOption(krz, name, desc, days, info, true);
        }

        public static string GetFoodOptions()
        {
            DataTable foodOptionsTable = Jenkins.Database.Tables["FOODOPTIONS"];
            var foodOptionsKrz = foodOptionsTable.AsEnumerable();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<- - - **All food-options** - - ->");
            sb.AppendLine();

            foreach (var foodOption in foodOptionsKrz)
            {
                sb.AppendLine(GetFoodOption(foodOption.Field<string>("KRZ")));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static void DelFoodOption(string krz)
        {
            DataTable foodOptionsTable = Jenkins.Database.Tables["FOODOPTIONS"];
            foodOptionsTable.Rows.Find(krz).Delete();
            Jenkins.Write();
        }

        public static string GetFoodName(string krz)
        {
            DataTable foodOptionsTable = Jenkins.Database.Tables["FOODOPTIONS"];
            var foodOptionsForDay = foodOptionsTable.AsEnumerable()
                .Where(r => r.Field<string>("KRZ").ToLower().Contains(krz.ToLower())).First();
            return foodOptionsForDay.Field<string>("NAME");
        }

        public static bool IsValidKRZ(string krz)
        {
            DataTable foodOptionsTable = Jenkins.Database.Tables["FOODOPTIONS"];
            var foodOptionsForDay = foodOptionsTable.AsEnumerable()
                .Where(r => r.Field<string>("KRZ").ToLower().Contains(krz.ToLower()));
            if (foodOptionsForDay.Count() >= 1)
                foodOptionsForDay = foodOptionsForDay.Where(r => r.Field<string>("KRZ").ToLower().Equals(krz.ToLower()));
            return (foodOptionsForDay.Count() == 1);
        }

        public static bool IsValidDayForKRZ(string krz)
        {
            DataTable foodOptionsTable = Jenkins.Database.Tables["FOODOPTIONS"];
            var foodOptionsForDay = foodOptionsTable.AsEnumerable()
                .Where(r => r.Field<string>("KRZ").ToLower().Equals(krz.ToLower()));
            string day = DateTime.Now.DayOfWeek.ToString();
            return (foodOptionsForDay.ElementAt(0).Field<string>("DAYS").Contains(day) || foodOptionsForDay.ElementAt(0).Field<string>("DAYS").Contains(Day.AllDays.ToString()));
        }

        public static void UpdateSchnaut(string url, string readUrl)
        {
            using (WebClient client = new WebClient())
            {
                var proc = Process.Start("http://geiooo.net/schnaut");

                Timer readNewOptionsTimer = new System.Threading.Timer(x =>
                {

                    var task = client.DownloadStringTaskAsync(readUrl);
                    task.Wait();
                    var html = task.Result;
                    RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Singleline;
                    Regex regx = new Regex("<body>(?<theBody>.*)</body>", options);

                    Match match = regx.Match(html);

                    if (match.Success)
                    {
                        var result = match.Value;
                        result = result.Replace("</body>", "");
                        result = result.Replace("<body>", "");
                        ParseSchnautDataLol(result);
                    }
                }, null, new TimeSpan(0, 0, 5), Timeout.InfiniteTimeSpan);
            }
        }

        private static void ParseSchnautDataLol(string jsonData)
        {
            List<SchnautOffer> offers = new List<SchnautOffer>();
            var schnautObject = JObject.Parse(jsonData).GetValue("dishes");
            var schnautArray = JArray.Parse(schnautObject.ToString());

            foreach (var offer in schnautArray)
            {
                var day = "";
                var desc = "";
                var price = "";

                bool gotValue = false;
                int index = 0;

                while (!gotValue)
                {
                    string value = offer.ElementAt(index).ToString();
                    if (value != "")
                    {
                        day = value;
                        gotValue = true;
                    }
                    else
                    {

                    }
                    index = index + 1;
                }

                gotValue = false;
                while (!gotValue)
                {
                    string value = offer.ElementAt(index).ToString();
                    if (value != "")
                    {
                        desc = value;
                        gotValue = true;
                    }
                    else
                    {

                    }
                    index = index + 1;
                }

                gotValue = false;
                while (!gotValue)
                {
                    string value = offer.ElementAt(index).ToString();
                    if (value != "")
                    {
                        price = value;
                        gotValue = true;
                    }
                    else
                    {

                    }
                    index = index + 1;
                }
                offers.Add(new SchnautOffer()
                {
                    OfferDay = GetDay(day),
                    Desc = desc,
                    Price = price
                });
            }
            UpdateSchnautOffers(offers);
        }

        private static void UpdateSchnautOffers(List<SchnautOffer> offers)
        {
            bool allOffersdeleted = false;
            int index = 1;
            while (!allOffersdeleted)
            {
                if (IsValidKRZ("SCHN" + index))
                {
                    DelFoodOption("SCHN" + index);
                    index = index + 1;
                }
                else
                {
                    allOffersdeleted = true;
                }
            }
            index = 1;
            List<string> addedItems = new List<string>();
            foreach (var offer in offers)
            {
                List<Food.Day> dayList = new List<Food.Day>();
                dayList.Add(offer.OfferDay);
                AddFoodOption("SCHN" + index, "Schnaut Launsbach", offer.Desc + " - " + offer.Price + "€", dayList.AsEnumerable(), "Vorbestellen unter 0641 82225");
                addedItems.Add("SCHN" + index);
                index = index + 1;
            }
            Bot.NotifyDevs(Supporter.BuildList("Added offers", addedItems.ToArray()));
            Jenkins.Write();
        }

        private static Day GetDay(string day)
        {

            //switch (day)
            //{
            //    case "montag":
            //        return Day.Monday;
            //    case "dienstag":
            //        return Day.Tuesday;
            //    case "mittwoch":
            //        return Day.Wednesday;
            //    case "donnerstag":
            //        return Day.Thursday;
            //    case "freitag":
            //        return Day.Friday;
            //}
            string dayOfWeek = day.ToLower();
            if (dayOfWeek.Contains("montag"))
            {
                return Day.Monday;
            }
            else if (dayOfWeek.Contains("dienstag"))
            {
                return Day.Tuesday;
            }
            else if (dayOfWeek.Contains("mittwoch"))
            {
                return Day.Wednesday;
            }
            else if (dayOfWeek.Contains("donnerstag"))
            {
                return Day.Thursday;
            }
            else if (dayOfWeek.Contains("freitag"))
            {
                return Day.Friday;
            }
            else
            {
                return Day.AllDays;
            }
        }

        #endregion Methods

        #region Helperclass

        private class SchnautOffer
        {
            public string Desc { get; set; }

            public string Price { get; set; }

            public Day OfferDay { get; set; }
        }

        #endregion Helperclass
    }
}