using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using GoogleMaps.LocationServices;
using System.Data;

namespace DiscordBot
{
    internal class Bot
    {
        #region Internal Variables

        public static Configuration Config; // Config (lol)
        public static DiscordClient Client; // Jenkins

        private CommandService command;

        #endregion Internal Variables

        #region Bot

        #region Init

        public Bot(Configuration config)
        {
            Config = config;
            Client = new DiscordClient();
            Jenkins.Init(); // JenkinsDB-initialising
            Memes.Init(); // Why static?
            Spotify.Init();
            Cleverbot.Init();
            Jenkins.Twitch.Init(); // Why not?
            //Jenkins.GamesSync.Init();

            Client = new DiscordClient(input => // Setting up Discord-Client
            {
                input.LogLevel = LogSeverity.Info;
                input.LogHandler = Log;
            });

            Client.UsingCommands(input => // Setting up Command-section
            {
                input.PrefixChar = '/';
                input.AllowMentionPrefix = true;
            });

            Client.UsingAudio(x => // Setting up Audio-Client
            {
                x.Mode = AudioMode.Outgoing;
            });

            command = Client.GetService<CommandService>();

            #endregion Init

            #region Events

            Client.MessageReceived += async (s, e) =>
        {
            if (e.Message.User.IsBot)
            {
                return;
            }
            Jenkins.Users.CheckUser(e.Message.User);
            if (e.Message.Text.StartsWith("/"))
            {
                Jenkins.Users.CountUpCommands(e.Message.User);
                if (Config.Debug && !Jenkins.Users.IsUserDev(e.Message.User.Id))
                {
                    NotifyDevs(Supporter.BuildDebugUserMessage(e.Message.Text, e.Message.User, e.Message.Channel));
                }
                return;
            }
            Jenkins.Users.CountUpMessages(e.Message.User);

            if ((e.Message.IsMentioningMe() && !Config.Muted) || e.Message.Channel.IsPrivate)
            {
                await e.Message.Channel.SendIsTyping();
                string answer = Cleverbot.IsServiceAvailable ? Cleverbot.TalkWithCleverBot(Supporter.RemoveMention(e.Message.Text)) : Cleverbot.Talk(Supporter.RemoveMention(e.Message.Text));
                await e.Channel.SendMessage(answer);
                Jenkins.Users.CountUpTalkedToMe(e.Message.User);
                if (Config.Debug && !Jenkins.Users.IsUserDev(e.Message.User.Id))
                {
                    NotifyDevs(string.Format("Answered in {0} to **{1}**\r\n**Message:** '*{2}*'\r\n**Answer:**  '*{3}*'",
                        e.Channel.IsPrivate ? "**private Chat**" : "#**" + e.Channel.Name + "** on Server **" + e.Server.Name + "**",
                        e.Message.User.Name,
                        e.Message.Text,
                        answer));
                }
            }
            else if (Supporter.YesOrNo(Config.RandomTalkChance) && !Config.Muted)
            {
                await e.Message.Channel.SendIsTyping();
                string answer = Cleverbot.IsServiceAvailable ? Cleverbot.TalkWithCleverBot(e.Message.Text) : Cleverbot.Talk(e.Message.Text);
                if (!(!e.Message.Channel.IsPrivate && answer == "The Cleverbot-API is currently unavailable."))
                {
                    if (Config.TTSEnabled)
                    {
                        await e.Channel.SendTTSMessage(answer);
                    }
                    else
                    {
                        await e.Channel.SendMessage(answer);
                    }
                    if (Config.Debug && !Jenkins.Users.IsUserDev(e.Message.User.Id))
                    {
                        NotifyDevs(string.Format("Answered randomly in {0} to **{1}**\r\n**Message:** '*{2}*'\r\n**Answer:**  '*{3}*'",
                            e.Channel.IsPrivate ? "**private Chat**" : "#**" + e.Channel.Name + "** on Server **" + e.Server.Name + "**",
                            e.Message.User.Name,
                            e.Message.Text,
                            answer));
                    }
                }
            }
            else if (!Config.Muted && Jenkins.Insults.IsUserVictim(e.Message.User.Id) && Supporter.YesOrNo(Config.RandomActionChance))
            {
                string authorName = e.User.Name;
                string insult = Supporter.BuildInsult(Jenkins.Insults.GetRandomInsult(), authorName);
                if (Config.TTSEnabled)
                {
                    await e.Channel.SendTTSMessage(insult);
                }
                else
                {
                    await e.Channel.SendMessage(insult);
                }
            }
            if (!e.Channel.IsPrivate)
            {
                if (Observe.IsServerObserved(e.Server.Id))
                {
                    NotifyDevs(Supporter.BuildLogMessage(e));
                }
            }
        };

            Client.UserJoined += (s, e) =>
            {
                if (e.Server.Id != 201693574889340928)
                    return; // Zockeria

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Willkommen auf dem Server " + e.Server.Name + ", " + e.User.Mention);
                sb.AppendLine();
                sb.AppendLine("Die Regeln sollten klar sein: Keine Beleidigungen, keine Werbung und weder kratzen noch treten. lul.");
                sb.AppendLine("Alle **Admins** des Servers sind Orange gefärbt, alle Bots blau.");
                sb.AppendLine();
                sb.AppendLine("Noch bist **du** ein unregistrierter Parasit und hast keine Rolle/Gruppe. Wenn du das ändern möchtest und mit den Regeln einverstanden bist, schreibe mir **/accept**.");
                sb.AppendLine();
                sb.AppendLine("**Infos**");
                sb.AppendLine();
                sb.AppendLine("Ich synchronisieren all deine gespielten Spiele mit den Gruppe auf diesem Server. Das hat den Vorteil das du zB. `@Grand Theft Auto V` in den Chat schreiben kannst, fragen wer bock hat und alle die dieses Spiel ebenfalls besitzt werden im gleichen zuge benachrichtigt.");
                sb.AppendLine();
                sb.AppendLine("**Commands**");
                sb.AppendLine();
                sb.AppendLine("**/stats** Deine stats");
                sb.AppendLine("**/funFact** Ein random FunFact");
                sb.AppendLine("**/rgif** Ein random GIF");
                sb.AppendLine("**/website** Ein random website (meist verstörend)");
                sb.AppendLine();
                sb.AppendLine("--> **/help** All meine Commands");
                e.User.SendMessage(sb.ToString());
                NotifyDevs("User " + e.User.Name + " joined server **Zockeria**.\r\n" + "Waiting for response of user /accept ...");
            };

            Client.Ready += (s, e) =>
            {
                Jenkins.GamesSync.Init();
            };

            #endregion Events

            #region Commands

            #region Accept

            command.CreateCommand("accept")
                .Description("ACCEPT")
                .Hide()
                .Do(async (e) =>
                {
                    try
                    {
                        var server = Client.GetServer(201693574889340928);
                        var user = server.FindUsers(e.User.Name).First();
                        var memberRole = server.Roles.Where(role => role.Name.Equals("Member")).First();
                        if (!user.HasRole(memberRole))
                        {
                            await user.AddRoles(memberRole);
                            await server.DefaultChannel.SendMessage("Willkommen " + e.User.Mention + " in " + server.Name + "!");
                            NotifyDevs("Received /accept\r\nGranted user " + e.User.Name + " permissions.");
                        }
                        else
                        {
                            await e.User.SendMessage("Du bist bereits auf dem Server " + server.Name + " registriert.");
                        }
                    }
                    catch (Exception ex)
                    {
                        NotifyDevs(Supporter.BuildExceptionMessage(ex, "Accept"));
                    }
                });

            #endregion Accept

            #region General

            command.CreateCommand("help")
        .Alias(new string[] { "commands", "cmds", "?" })
        .Description("Helping things and stuff :question:")
        .Do(async (e) =>
        {
            await e.Message.Delete();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Hello, my name is **Jenkins**!");
            sb.AppendLine();
            foreach (var cmd in command.AllCommands)
            {
                if (!cmd.IsHidden || Jenkins.Users.IsUserDev(e.User.Id))
                {
                    if (cmd.Parameters.Count() == 1)
                    {
                        sb.Append(string.Format("- /**{0}** <{1}> - {2} ",
                                                    cmd.Text,
                                                    cmd.Parameters.First().Name,
                                                    cmd.Description
                                                    ));
                    }
                    else if (cmd.Parameters.Count() == 2)
                    {
                        sb.Append(string.Format("- /**{0}** <{1}> <{2}> - {3} ",
                                                    cmd.Text,
                                                    cmd.Parameters.ElementAt(0).Name,
                                                    cmd.Parameters.ElementAt(1).Name,
                                                    cmd.Description
                                                    ));
                    }
                    else
                    {
                        sb.Append(string.Format("- /**{0}** - {1} ",
                                                    cmd.Text,
                                                    cmd.Description
                                                    ));
                    }
                    if (cmd.Aliases.Count() >= 1)
                    {
                        foreach (var alias in cmd.Aliases)
                        {
                            sb.Append(string.Format("**[**{0}**]** ",
                                alias
                                ));
                        }
                    }
                    sb.AppendLine();
                    if (sb.Length > 1750)
                    {
                        await e.Channel.SendMessage(sb.ToString());
                        sb.Clear();
                    }
                }
            }
            await e.Channel.SendMessage(sb.ToString());
        });

            command.CreateCommand("weather")
                .Description("Guess it... :white_sun_cloud:")
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    var msg = await e.Channel.SendMessage("I'll look out for the forecast");
                    var weatherResponse = GetWeather(e.Message.User, msg);
                    string weatherString = weatherResponse.Result;
                    var weatherObject = Newtonsoft.Json.Linq.JObject.Parse(weatherString);
                    WeatherDAO currentWeather = weatherObject.GetValue("currently").ToObject<WeatherDAO>();
                    await msg.Edit(BuildForecast(currentWeather));
                });

            command.CreateCommand("dice")
                .Description("I'll roll a dice for you, Sir! :game_die:")
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    await e.Message.Channel.SendMessage(Supporter.RollDice());
                });

            command.CreateCommand("ping")
                .Description("For really important connection-Tests and stuff")
                .Do(async (e) =>
                {
                    await e.Channel.SendMessage("Pong! - Jenkins.NET");
                });

            command.CreateCommand("shortURL")
                .Description("I'll shorten a URL for you :pen_ballpoint:")
                .Parameter("url", ParameterType.Required)
                .Alias(new string[] { "shorturl", "shortUrl", "url", "URL" })
                .Do(async (e) =>
                {
                    await e.Message.Delete();

                    await e.Channel.SendMessage(Google.GetShortenURL(e.Args[0].ToString()));
                });

            command.CreateCommand("memes")
                .Description("All memes I'm holding for ya :tophat:")
                .Alias(new string[] { "shorturl", "shortUrl", "url", "URL" })
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    if (Jenkins.Users.IsUserDev(e.Message.User.Id))
                        Memes.Init();
                    await e.Message.User.SendMessage(Supporter.BuildList("Memes", Memes.GetMemes()));
                });

            command.CreateCommand("meme")
                .Description("Post a certain meme")
                .Parameter("name", ParameterType.Required)
                .Alias(new string[] { "pic", "post", "send", "react" })
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    string meme;
                    if (Memes.TryGetMeme(e.Args[0], out meme))
                    {
                        await e.Channel.SendMessage("**#" + meme + "**");
                        await e.Channel.SendFile(Memes.GetMemePath(meme));
                    }
                    else
                    {
                        await e.Message.User.SendMessage(string.Format("The meme **{0}** does not exist :(\r\n"
                            + Supporter.BuildList("Memes", Memes.GetMemes()),
                            e.Args[0].ToString()));
                    }
                });

            command.CreateCommand("website")
                .Description("Try it!")
                .Parameter("tags", ParameterType.Unparsed)
                .Alias(new string[] { "pr0", "useless", "lol", "web", "browser", "internet" })
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    if (e.Args[0] != string.Empty)
                    {
                        await e.Channel.SendMessage(Jenkins.Websites.GetWebsite(Jenkins.Websites.ExtractTagsToList(e.Args[0]), e.Message.Text.ToLower().Equals("website") ? "website" : ""));
                    }
                    else
                    {
                        await e.Channel.SendMessage(Jenkins.Websites.GetRandomWebsite(e.Message.Text.ToLower().Replace("/", "")));
                    }

                });

            command.CreateCommand("websites")
                .Description("A list of all my websites")
                .Parameter("tags", ParameterType.Unparsed)
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    await e.Channel.SendMessage(Jenkins.Websites.ListWebsites());
                });

            command.CreateCommand("addWebsite")
                .Description("Adds a website append tags with commas like nsfw or just names")
                .Parameter("tags", ParameterType.Multiple)
                .Alias(new string[] { "addwebsite", "aw" })
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    string ws = Jenkins.Websites.GetWebsiteByUrl(e.Args[0]);
                    if (ws == string.Empty)
                    {
                        Jenkins.Websites.AddWebsite(e.Args[0].ToString(), Jenkins.Websites.ConvertTagsForDatabase(e.Args));
                        await e.User.SendMessage("Added `" + e.Args[0] + "`");
                        return;
                    }
                    await e.User.SendMessage("URL is already here: \r\n" + ws);
                });

            command.CreateCommand("delWebsite")
                .Description("Deletes a website")
                .Parameter("keyword", ParameterType.Required)
                .Hide()
                .Alias(new string[] { "delwebsite", "dw" })
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                        return;

                    await e.Message.Delete();
                    await e.Channel.SendMessage(Jenkins.Websites.DelWebsite(e.Args[0].ToString()));
                });

            #endregion General

            #region Users

            command.CreateCommand("users")
                .Description("All users I've ever seen writing :busts_in_silhouette: ")
                .Do(async (e) =>
            {
                await e.Message.Delete();
                await e.Channel.SendMessage(Jenkins.Users.ListUsers());
            });

            command.CreateCommand("stats")
                .Description("I'm counting everything since your first steps. Accept it.")
                .Do(async (e) =>
            {
                await e.Message.Delete();
                await e.Channel.SendMessage(Jenkins.Users.GetUserStats(e.Message.User.Id));
            });

            command.CreateCommand("statsOf")
                .Description("View the stats of someone else. HueHueHue.")
                .Parameter("name", ParameterType.Required)
                .Alias(new string[] { "so", "statsof" })
                .Do(async (e) =>
                {
                    await e.Message.Delete();

                    ulong userId;
                    if (!Jenkins.Users.TryGetUserId(e.Args[0].ToString(), out userId))
                    {
                        await e.User.SendMessage(string.Format("I'm sorry but I dont know **{0}** :(\r\nHint: It have to be the real name, not the nickname."
                            , (e.Args[0].ToString())));
                        return;
                    }
                    await e.Channel.SendMessage(Jenkins.Users.GetUserStats(userId));
                });

            command.CreateCommand("admins")
    .Description("All my existing masters for this server")
    .Do(async (e) =>
    {
        await e.Message.Delete();
        string adminMsg = Supporter.BuildList("Admins", Jenkins.Users.GetAdminNames(e.Message.Server.Id));
        await e.Message.Channel.SendMessage(adminMsg);
    });

            #endregion Users

            #region Insults

            command.CreateCommand("insult")
                .Description("Insulting an user of your choice :exclamation:")
                .Alias(new string[] { "i" })
                .Parameter("name", ParameterType.Required)
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    await e.Channel.SendTTSMessage(Supporter.BuildInsult(Jenkins.Insults.GetRandomInsult(true), e.Args[0].ToString()));
                });

            command.CreateCommand("addInsult")
                .Description("Add a insult to my Vacabulary Hint: '*' will be the Username")
                .Parameter("text", ParameterType.Required)
                .Alias(new string[] { "ai", "addinsult" })
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    Jenkins.Insults.AddInsult(e.Message.User, e.Args[0]);
                });

            command.CreateCommand("insultMe")
                .Description("If you insist...")
                .Alias(new string[] { "im", "insultme" })
                .Do(async (e) =>
            {
                await e.Message.Delete();
                await e.Channel.SendMessage(Supporter.BuildInsult(Jenkins.Insults.GetRandomInsult(true), e.Message.User.Name));
            });

            command.CreateCommand("allInsults")
                .Description("All insults in my vocabulary")
                .Alias(new string[] { "insultlist", "allinsults", "insultList" })
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    await e.Channel.SendMessage(Jenkins.Insults.ListInsults());
                });

            command.CreateCommand("insults")
                .Description("All your own created insults")
                .Alias(new string[] { "myinsults" })
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    await e.Channel.SendMessage(Jenkins.Insults.ListInsultsForUser(e.Message.User, e.Message.Server.Id));
                });

            command.CreateCommand("delInsult")
                .Description("Delete one of your insults")
                .Parameter("index", ParameterType.Required)
                .Alias(new string[] { "di", "delinsult" })
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    Jenkins.Insults.DelInsult(e.Message.User, int.Parse(e.Args[0]), e.Message.Server.Id);
                });

            command.CreateCommand("addVictim")
                .Description("He should suffer too of you insist")
                .Parameter("name", ParameterType.Required)
                .Alias(new string[] { "av", "addvictim" })
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserAdmin(e.Message.User.Id, e.Message.Server.Id))
                    {
                        return;
                    }
                    await e.Message.Delete();
                    ulong userID;
                    if (Jenkins.Users.TryGetUserId(e.Args[0], out userID))
                    {
                        Jenkins.Insults.AddVictim(userID);
                    }
                    else
                    {
                        await e.Channel.SendMessage(string.Format("I'm sorry but I dont know **{0}** :(\r\nHint: It have to be the real name, not the nickname."
                            + "\r\nPS: You can also add a Victim through the ID and /addVictimByID <ID>"
                            , (e.Args[0].ToString())));
                    }
                });

            command.CreateCommand("addVictimByID")
                .Hide()
                .Parameter("id", ParameterType.Required)
                .Alias(new string[] { "avbi", "addvictimbyid" })
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserAdmin(e.Message.User.Id, e.Message.Server.Id))
                    {
                        return;
                    }
                    await e.Message.Delete();
                    Jenkins.Insults.AddVictim(ulong.Parse(e.Args[0].ToString()));
                });

            command.CreateCommand("delVictim")
                .Description("And he not...")
                .Parameter("name", ParameterType.Required)
                .Alias(new string[] { "dv", "delvictim" })
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserAdmin(e.Message.User.Id, e.Message.Server.Id))
                    {
                        return;
                    }
                    await e.Message.Delete();

                    ulong userId;
                    if (Jenkins.Users.TryGetUserId(e.Args[0].ToString(), out userId))
                    {
                        Jenkins.Insults.DelVictim(userId);
                    }
                });

            command.CreateCommand("victims")
                .Description("They all shall suffer")
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    await e.Channel.SendMessage(Jenkins.Insults.ListVictims());
                });

            #endregion Insults

            #region Quotes

            command.CreateCommand("quotes")
                .Description("Quotes statistics")
                .Alias(new string[] { "qs" })
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    if (Config.TTSEnabled)
                    {
                        await e.Channel.SendTTSMessage(Jenkins.Quotes.GetQuoteStatistics());
                    }
                    else
                    {
                        await e.Channel.SendMessage(Jenkins.Quotes.GetQuoteStatistics());
                    }
                });

            command.CreateCommand("quote")
                .Description("You'll get one quote of my huge collection :bookmark: ")
                .Alias(new string[] { "q" })
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    if (Config.TTSEnabled)
                    {
                        await e.Channel.SendTTSMessage(Jenkins.Quotes.GetRandomQuote());
                    }
                    else
                    {
                        await e.Channel.SendMessage(Jenkins.Quotes.GetRandomQuote());
                    }
                });

            command.CreateCommand("quoteOf")
                .Description("I'll search for a quote of the owner you prefer")
                .Alias(new string[] { "qo", "quoteof" })
                .Parameter("name", ParameterType.Required)
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    string quote = Jenkins.Quotes.GetQuoteOf(e.Args[0].ToString());
                    if (quote != string.Empty || quote != null)
                    {
                        if (Config.TTSEnabled)
                        {
                            await e.Channel.SendTTSMessage(quote);
                        }
                        else
                        {
                            await e.Channel.SendMessage(quote);
                        }
                    }
                    else
                    {
                        await e.User.SendMessage(string.Format("I'm sorry but theres no quote matching your search for **{0}** :(", e.Args[0].ToString()));
                    }
                });

            command.CreateCommand("quotesOf")
                .Description("I'll search for all quotes of the owner you prefer")
                .Alias(new string[] { "qso", "quotesof" })
                .Parameter("name", ParameterType.Unparsed)
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    DataRow[] quotes = Jenkins.Quotes.GetQuotesOf(e.Args[0].ToString());
                    if (quotes.Length >= 1)
                    {
                        StringBuilder sb = new StringBuilder();
                        int internalCounter = 0;
                        for (int i = 0; i < quotes.Length; i++)
                        {
                            sb.AppendFormat((i + 1).ToString() + ". " + Supporter.BuildQuote(quotes[i]["MESSAGE"].ToString(), quotes[i]["OWNER"].ToString()));
                            sb.AppendLine();
                            sb.AppendLine();
                            internalCounter++;
                            if (internalCounter == 3 || i == (quotes.Length - 1))
                            {
                                await e.Channel.SendMessage(sb.ToString());
                                internalCounter = 0;
                                sb.Clear();
                            }
                        }
                    }
                    else
                    {
                        await e.User.SendMessage(string.Format("I'm sorry but theres no quote matching your search for **{0}** :(", e.Args[0].ToString()));
                    }
                });

            command.CreateCommand("addQuote")
                .Alias(new string[] { "aq", "addquote" })
                .Description("Adds your super fancy quote to my vocabulary")
                .Parameter("text", ParameterType.Required)
                .Parameter("owner", ParameterType.Required)
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    string newQuote = e.GetArg(0);
                    string owner = e.GetArg(1);
                    Jenkins.Quotes.AddQuote(e.Message.User, newQuote, owner);
                    await e.User.SendMessage("A new quote has been added, Sir!");
                });

            command.CreateCommand("findQuote")
                .Alias(new string[] { "fq", "findquote" })
                .Description("I'll search for a quote on your order, Sir!")
                .Parameter("text", ParameterType.Unparsed)
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    string searchKey = e.GetArg(0);
                    string quote = Jenkins.Quotes.GetQuote(searchKey);

                    if (quote != string.Empty || quote != null)
                    {
                        if (Config.TTSEnabled)
                        {
                            await e.Channel.SendTTSMessage(quote);
                        }
                        else
                        {
                            await e.Channel.SendMessage(quote);
                        }
                    }
                    else
                    {
                        await e.User.SendMessage(string.Format("I'm sorry but theres no quote matching your search for **{0}** :(", e.Args[0].ToString()));
                    }
                });

            command.CreateCommand("listQuotes")
                .Description("Shows all quotes you've created")
                .Alias(new string[] { "lq", "listquotes" })
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    string quotes = Jenkins.Quotes.ListQuotes(e.Message.User, e.Message.Server.Id);

                    foreach (var message in Supporter.SplitMessage(quotes))
                    {
                        await e.Channel.SendMessage(message);
                    }
                });

            command.CreateCommand("delQuote")
                .Description("Removes one of ya quotes")
                .Parameter("index", ParameterType.Required)
                .Alias(new string[] { "dq", "delquote", "removeQuote", "removequote" })
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserAdmin(e.Message.User.Id, e.Message.Server.Id))
                    {
                        return;
                    }
                    await e.Message.Delete();
                    Jenkins.Quotes.DelQuote(e.Message.User, int.Parse(e.Args[0].ToString()), e.Message.Server.Id);
                });

            #endregion Quotes

            #region FunFacts

            command.CreateCommand("funFact")
                .Description("Guess it")
                .Parameter("params", ParameterType.Multiple)
                .Alias(new string[] { "ff", "funfact" })
                .Do(async (e) =>
                {
                    string result = "";
                    string info = "";
                    await e.Channel.SendIsTyping();
                    if (e.Args.Length == 0)
                    {
                        result = Jenkins.FunFacts.GetFunFact(out info);
                    }
                    else if (e.Args.Length == 1)
                    {
                        if (e.Args[0].ToString().Equals("today"))
                        {
                            result = Jenkins.FunFacts.GetFunFact(out info, DateTime.Now.ToString("MM/dd"));
                        }
                        else
                        {
                            result = Jenkins.FunFacts.GetFunFact(out info, e.Args[0]);
                        }
                    }
                    else if (e.Args.Length == 2)
                    {
                        result = Jenkins.FunFacts.GetFunFact(out info, e.Args[0], e.Args[1]);
                    }
                    await e.Channel.SendMessage(info + "\r\n" + result);
                });

            #endregion FunFacts

            #region Twitch

            command.CreateCommand("watchChannel")
                .Description("Adds a Twitch-Channel to the watchlist of this channel :eye:")
                            .Parameter("name", ParameterType.Required)
                            .Alias(new string[] { "wc", "watchchannel", "addChannel", "addchannel", "followChannel", "followchannel" })
                            .Do(async (e) =>
                {
                    if (e.Message.Channel.IsPrivate)
                    {
                        return;
                    }
                    int observingTwitchChannelsInThisDiscordChannel = Jenkins.Twitch.GetObservingTwitchChannelsForDiscordChannel(e.Message.Channel.Id).Length;
                    if (observingTwitchChannelsInThisDiscordChannel >= Config.TwitchChannelLimit && !Jenkins.Users.IsUserAdmin(e.Message.User.Id, e.Message.Server.Id))
                    {
                        await e.Message.Channel.SendMessage(string.Format("There are already **{0}/{1}** Twitch-Channels on my watchlist for **#{2}**"
                            , observingTwitchChannelsInThisDiscordChannel
                            , Config.TwitchChannelLimit
                            , e.Message.Channel.Name));
                        return;
                    }
                    await e.Message.Delete();
                    var msg = await e.Channel.SendMessage("Checking channel...");

                    if (!Jenkins.Twitch.IsTwitchChannelRegiseredByName(e.Args[0]))
                    {
                        Jenkins.Twitch.RegisterTwitchChannel(e.Args[0], e.Message.Channel);
                        NotifyDevs(string.Format("Twitch-Channel **{0}** was registered by **{1}** in **{2}**",
                            e.Args[0],
                            e.Message.User.Name,
                            e.Message.Channel.Server.Name + " -> #" + e.Message.Channel.Name));
                    }

                    if (!Jenkins.Twitch.IsDiscordChannelFollowingTwitchChannel(e.Args[0], e.Message.Channel.Id))
                    {
                        Jenkins.Twitch.AddTwitchChannelToWatchlistOfDiscordChannel(e.Args[0], e.Message.User, e.Message.Channel);
                        await msg.Edit(string.Format("Twitch-Channel **{0}** was added to watchlist in **#{1}** by **{2}**"
                            , e.Args[0]
                            , e.Message.Channel.Name
                            , e.Message.User.Name));
                        return;
                    }
                    await msg.Edit(string.Format("Channel **{0}** is already in watchlist."
                            , e.Args[0]));
                });

            command.CreateCommand("unwatchChannel")
                .Description("Removes the desired Twitch-Channel from the watchlist")
                .Parameter("name", ParameterType.Required)
                .Alias(new string[] { "uc", "unwatchchannel", "delChannel", "delchannel", "unfollowChannel", "unfollowchannel" })
                .Do(async (e) =>
                {
                    if (e.Message.Channel.IsPrivate)
                    {
                        return;
                    }
                    await e.Message.Delete();
                    if (Jenkins.Twitch.IsDiscordChannelFollowingTwitchChannel(e.Args[0], e.Message.Channel.Id))
                    {
                        Jenkins.Twitch.RemoveTwitchChannelFromWatchlistOfDiscordChannel(e.Args[0], e.Message.Channel);
                        await e.Channel.SendMessage(string.Format("Twitch-Channel **{0}** was removed from watchlist for **#{1}** by **{2}**"
                            , e.Args[0]
                            , e.Message.Channel.Name
                            , e.Message.User.Name));
                        return;
                    }
                    await e.Channel.SendMessage(string.Format("Twitch-Channel **{0}** isn't in watchlist for **#{1}**"
                            , e.Args[0]
                            , e.Message.Channel.Name));
                });

            command.CreateCommand("channels")
                .Description("View all Twitch-Channels I'm watching for this channel")
                .Alias(new string[] { "followingChannels", "followingchannels" })
                .Do(async (e) =>
                {
                    if (e.Message.Channel.IsPrivate)
                    {
                        return;
                    }
                    await e.Message.Delete();
                    string[] watchlist = Jenkins.Twitch.GetObservingTwitchChannelsForDiscordChannel(e.Message.Channel.Id);
                    string oberservingChannels = string.Empty;
                    foreach (var channelName in watchlist)
                    {
                        oberservingChannels = oberservingChannels + "- > **" + channelName + "**\r\n";
                    }
                    await e.Channel.SendMessage(string.Format("I'm observing **{0}/{1}** Twitch-Channels in **#{2}**\r\n{3}"
                        , watchlist.Length
                        , Config.TwitchChannelLimit
                        , e.Message.Channel.Name
                        , oberservingChannels
                        ));
                });

            #endregion Twitch

            #region Giphy

            command.CreateCommand("rgif")
                .Description("I'm spitting out a random GIF :frame_photo:")
                .Do(async (e) =>
            {
                await e.Message.Delete();
                Giphy.GetRandomGIF(e.Message.Channel);
            });

            command.CreateCommand("tgif")
                .Description("The currently top-trending GIF on Giphy")
                .Do(async (e) =>
            {
                await e.Message.Delete();
                Giphy.GetTrendingGIF(e.Message.Channel);
            });

            command.CreateCommand("gif")
                .Description("I'll search a GIF for you, darling <3")
                .Parameter("keyword", ParameterType.Required)
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    Giphy.GetGIF(e.Args[0], e.Message.Channel);
                });

            #endregion Giphy

            #region VoiceCommands

            command.CreateCommand("pardy")
                .Description("Maaskantje joooonge! :loud_sound:")
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                        return;

                    await e.Message.Delete();
                    Channel vChannel = e.User.VoiceChannel;

                    if (vChannel != null)
                    {
                        string path = Path.Combine(Environment.CurrentDirectory, "files", "mp3", "pardy.mp3");
                        Audio.StreamFileToVoiceChannel(path, vChannel);
                    }
                }
             );

            command.CreateCommand("attack")
                .Description("Type the name of the vChannel hehe xd")
                .Parameter("channel", ParameterType.Required)
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                        return;

                    await e.Message.Delete();
                    Channel vChannel = null;
                    if (e.Args[0] != null)
                    {
                        var channels = e.Server.FindChannels(e.Args[0], ChannelType.Voice);
                        foreach (var voiChannel in channels)
                        {
                            vChannel = voiChannel;
                        }
                    }
                    else
                    {
                        vChannel = e.User.VoiceChannel;
                    }

                    if (vChannel != null)
                    {
                        string path = Path.Combine(Environment.CurrentDirectory, "files", "mp3", "pardy.mp3");
                        Audio.StreamFileToVoiceChannel(path, vChannel);
                    }
                }
             );

            command.CreateCommand("slowClap")
                .Description("Excellent :clap:")
                .Alias(new string[] { "slowclap", "clap" })
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                        return;

                    await e.Message.Delete();
                    Channel vChannel = e.User.VoiceChannel;

                    if (vChannel != null)
                    {
                        string path = Path.Combine(Environment.CurrentDirectory, "files", "mp3", "slowClap.mp3");
                        Audio.StreamFileToVoiceChannel(path, vChannel);
                    }
                }
             );

            command.CreateCommand("airhorn")
                .Description("Are u a MLG? :loud_sound: ")
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    Channel vChannel = e.User.VoiceChannel;

                    if (vChannel != null)
                    {
                        string path = Path.Combine(Environment.CurrentDirectory, "files", "mp3", "airhorn.mp3");
                        Audio.StreamFileToVoiceChannel(path, vChannel);
                    }
                }
             );

            command.CreateCommand("lul")
                .Description("At your own risk")
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                        return;

                    await e.Message.Delete();
                    Channel vChannel = e.User.VoiceChannel;

                    if (vChannel != null)
                    {
                        string path = Path.Combine(Environment.CurrentDirectory, "files", "mp3", "lul.mp3");
                        Audio.StreamFileToVoiceChannel(path, vChannel);
                    }
                }
        );

            command.CreateCommand("play")
                .Description("Streams a YouTube Video to your vChannel :musical_note:")
                .Parameter("url", ParameterType.Required)
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                        return;

                    return; // Discord.js

                    await e.Message.Delete();
                    Channel vChannel = e.User.VoiceChannel;
                    if (vChannel != null)
                    {
                        var botMsg = await e.Message.Channel.SendMessage("Checking URL...");
                        Channel vChannelToStream = vChannel;
                        Audio.DownloadAndPlayURL(e.Args[0], botMsg, e.Message.User, vChannel);
                    }
                    else
                    {
                        await e.Message.User.SendMessage("You have to be in a Voicechannel for this");
                    }
                }
                    );

            command.CreateCommand("random")
                .Description("I'll stream some weird random shit to your vChannel, dude")
                .Do((e) =>
                {
                    return; // Discord.js
                }
                    );

            command.CreateCommand("stop")
                .Description("Stops the playing audio")
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                        return;

                    return; // Discord.js

                    await e.Message.Delete();
                    Audio.IsPlaying = false;
                }
                    );

            #endregion VoiceCommands

            #region Spotify

            command.CreateCommand("playSong")
               .Description("Plays a Song on Spotify")
               .Parameter("name", ParameterType.Required)
                .Alias(new string[] { "ps", "playsong" })
               .Do(async (e) =>
               {
                   if (!Config.SpotifyEnabled)
                   {
                       return;
                   }
                   await e.Message.Delete();
                   var track = Spotify.GetTrack(e.Args[0]);
                   await e.Channel.SendMessage(track.Artists + " " + track.Name);
               });

            command.CreateCommand("playList")
               .Description("Plays a Playlist on Spotify")
               .Parameter("name", ParameterType.Required)
                .Alias(new string[] { "pl", "playlist", "playPlaylist", "playplaylist" })
               .Do(async (e) =>
               {
                   if (!Config.SpotifyEnabled)
                   {
                       return;
                   }
                   await e.Message.Delete();
                   var playlist = Spotify.GetPlaylist(e.Args[0]);
               });

            #endregion Spotify

            #region Administration

            command.CreateCommand("globalChannels")
                .Alias(new string[] { "allChannels", "allchannels", "globalchannels" })
                .Hide()
                .Do(async (e) =>
                {
                    if (e.Message.Channel.IsPrivate || !Jenkins.Users.IsUserDev(e.Message.User.Id))
                    {
                        return;
                    }
                    await e.Message.Delete();
                    string[] globalTwitchChannelList = Jenkins.Twitch.GetGlobalObservingTwitchChannels();
                    string oberservingChannels = string.Empty;
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("< - - - **All Twitch-Channels** - - - >");
                    sb.AppendLine();
                    foreach (var twitchChannel in globalTwitchChannelList)
                    {
                        sb.AppendLine("TwitchChannel: **" + twitchChannel + "**");
                        ulong[] discordChannelIds = Jenkins.Twitch.GetFollowingDiscordChannelsForTwitchChannel(twitchChannel);
                        foreach (var discordChannel in discordChannelIds)
                        {
                            var channel = Client.GetChannel(discordChannel);
                            sb.AppendLine("|-> **#" + channel.Name + "** on server **" + channel.Server.Name + "**");
                        }
                        sb.AppendLine();
                    }
                    await e.Channel.SendMessage(sb.ToString());
                });

            command.CreateCommand("settings")
                .Description("My current configuration :gear:")
                .Alias(new string[] { "config", "cfg", "preferences" })
                .Do(async (e) =>
                {
                    //if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                    //    return;

                    await e.Message.Delete();
                    await e.Channel.SendMessage(Config.GetConfiguration(!e.Message.Channel.IsPrivate && !Jenkins.Users.IsUserDev(e.Message.User.Id)));
                });

            command.CreateCommand("loadConfig")
                .Description("Reloads my config")
                .Alias(new string[] { "loadconfig", "reloadconfig", "reloadConfig" })
                .Hide()
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                        return;

                    await e.Message.Delete();
                    Config.LoadConfig();
                });

            command.CreateCommand("say")
                .Description("I will announce news in your name")
                .Parameter("text", ParameterType.Unparsed)
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserAdmin(e.Message.User.Id, e.Message.Server.Id))
                    {
                        return;
                    }
                    await e.Message.Delete();
                    await e.Message.Channel.SendTTSMessage(e.Args[0]);
                });

            command.CreateCommand("mute")
                .Description("One word and I'll shut my dirty mouth!")
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserAdmin(e.Message.User.Id, e.Message.Server.Id))
                    {
                        return;
                    }
                    await e.Message.Delete();
                    Config.Muted = true;
                });

            command.CreateCommand("unmute")
                .Description("The show must go on")
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserAdmin(e.Message.User.Id, e.Message.Server.Id))
                    {
                        return;
                    }
                    await e.Message.Delete();
                    Config.Muted = false;
                });

            command.CreateCommand("enableTTS")
                .Description("I'll turn into a speaking war-machine")
                .Alias(new string[] { "ttson", "TTSOn", "enabletts" })
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserAdmin(e.Message.User.Id, e.Message.Server.Id))
                    {
                        return;
                    }
                    await e.Message.Delete();
                    Config.TTSEnabled = true;
                });

            command.CreateCommand("disableTTS")
                .Description("Shy Ronnie?")
                .Alias(new string[] { "ttsoff", "TTSOff", "disabletts" })
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserAdmin(e.Message.User.Id, e.Message.Server.Id))
                    {
                        return;
                    }
                    await e.Message.Delete();
                    Config.TTSEnabled = false;
                });

            command.CreateCommand("nuke")
                .Description("Nuke the chat")
                .Alias(new string[] { "clear" })
                .Parameter("count", ParameterType.Required)
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserAdmin(e.Message.User.Id, e.Message.Server.Id))
                    {
                        return;
                    }
                    await e.Message.Delete();
                    int count = int.Parse(e.Args[0]) + 1;
                    if (count > Config.NukeLimit)
                    {
                        count = Config.NukeLimit;
                    }
                    var messages = await e.Message.Channel.DownloadMessages(count);
                    await e.Message.Channel.DeleteMessages(messages);
                });

            command.CreateCommand("nukeUser")
                .Description("Deleting the users messages")
                .Parameter("user", ParameterType.Required)
                .Parameter("count", ParameterType.Optional)
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserAdmin(e.Message.User.Id, e.Message.Server.Id))
                    {
                        return;
                    }
                    await e.Message.Delete();
                    int count = 25;
                    if (e.Args[1] != "")
                    {
                        count = int.Parse(e.Args[1]) + 1;
                    }

                    if (count > Config.NukeLimit)
                    {
                        count = Config.NukeLimit;
                    }
                    var users = e.Message.Server.Users.Where(r => r.Name.Equals(e.Args[0]));
                    var user = users.First();
                    if (user == null)
                    {
                        await e.Message.User.SendMessage("Theres no user called " + e.Args[0] + " on this server :(");
                        return;
                    }
                    var messages = await e.Message.Channel.DownloadMessages(count);
                    List<Message> userMessages = new List<Message>();
                    foreach (var message in messages)
                    {
                        if (message.User.Id == user.Id)
                            userMessages.Add(message);
                    }
                    await e.Message.Channel.DeleteMessages(userMessages.ToArray());
                });

            command.CreateCommand("promote")
                .Description("Promotes a normal user to a super-sonic Admin!")
                .Parameter("name", ParameterType.Required)
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                        return;

                    await e.Message.Delete();
                    ulong userId = 0;
                    if (Jenkins.Users.TryGetUserId(e.Args[0].ToString(), out userId))
                    {
                        Jenkins.Users.PromoteToAdmin(userId, e.Message.Server.Id);
                    }
                    else /*if(userId == 0)*/
                    {
                        await e.Message.Channel.SendMessage(string.Format("I don't know {0} :("
                            , e.Args[0].ToString()));
                    }
                });

            command.CreateCommand("degrade")
                .Description("Turns an Admin to one of the common mob")
                .Parameter("name", ParameterType.Required)
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                        return;

                    await e.Message.Delete();
                    ulong userId;
                    if (Jenkins.Users.TryGetUserId(e.Args[0].ToString(), out userId))
                    {
                        Jenkins.Users.DegradeToUser(userId, e.Server.Id);
                    }
                    else
                    {
                        await e.Message.Channel.SendMessage(string.Format("I don't know {0} :("
                            , e.Args[0].ToString()));
                    }
                });

            command.CreateCommand("setGame")
                .Description("Configure my current Game")
                .Hide()
                .Parameter("name", ParameterType.Unparsed)
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                        return;

                    await e.Message.Delete();
                    if (e.Args[0].ToString() != string.Empty)
                    {
                        Game gm = new Game(e.Args[0], GameType.Default, "http://z0r.de/342");
                        Client.SetGame(gm);
                    }
                    else
                    {
                        Client.SetGame(null);
                    }
                });

            command.CreateCommand("helpop")
                .Description("Ask all admins a question")
                .Parameter("text", ParameterType.Unparsed)
                .Do(async (e) =>
                {
                    await e.Message.Delete();
                    NotifyDevs(e.Args[0]);
                });

            command.CreateCommand("backup")
                .Description("Provides information about the backup preferences")
                .Hide()
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                        return;

                    await e.Message.Delete();
                    await e.Message.Channel.SendMessage(Jenkins.CheckAndScheduleBackUp());
                });

            command.CreateCommand("shutdown")
                .Description("Bye bye.")
                .Hide()
                .Do(async (e) =>
                {
                    if (Jenkins.Users.IsUserDev(e.Message.User.Id))
                    {
                        Jenkins.Write();
                        await e.Message.Delete();
                        await e.Channel.SendMessage("Shutting down...");
                        Thread.Sleep(1000);
                        await Client.Disconnect();
                        Environment.Exit(0);
                    }
                });

            #endregion Administration

            #region Food

            command.CreateCommand("foodHelp")
                            .Description("FOOD")
                            .Hide()
                            .Alias(new string[] { "foodhelp", "helpFood", "helpfood", "votehelp", "voteHelp" })
                            .Do(async (e) =>
                            {
                                await e.Message.Delete();
                                StringBuilder sb = new StringBuilder();
                                sb.AppendLine("< - - - **Food-Help** - - - >");
                                sb.AppendLine();
                                sb.AppendLine("-> /**foodHelp**");
                                sb.AppendLine("-> /**food** - Alle möglichkeiten für diesen Tag");
                                sb.AppendLine("-> /**food <Tag>** - Alle möglichkeiten für den gewählten Tag. Bspw: '**/food Monday**'");
                                sb.AppendLine("-> /**foodAll** - Alle möglichkeiten die **Tagesunabhängig** sind Bspw: McDonalds oder KFC");
                                sb.AppendLine("-> /**getFood <KRZ>** - Eine spezifische möglichkeit ausgeben");
                                sb.AppendLine("-> /**getFoodAll** - ALLE möglichkeiten ausgeben");
                                sb.AppendLine("-> /**addFood <KRZ> <NAME> <DESC> <DAYS> <INFO>** - Fügt eine möglichkeit hinzu. Beispiel:\r\n"
                                    + "*/addFood krz:'KFC' name:'Kentucky Fried Chicken' desc:'KFC in Gießen' days:'AllDays' info:'Tel: 01805 4646'*");
                                sb.AppendLine("< - - - **Vote-Help** - - - >");
                                sb.AppendLine();
                                sb.AppendLine("-> /**vote <KRZ>** - Für etwas abstimmen bswp: /vote RL für *Real in Gießen*");
                                sb.AppendLine("-> /**votes** - Übersicht der bereits abgegebenen stimmen");
                                await e.Channel.SendMessage(sb.ToString());
                            });

            command.CreateCommand("foodBlock")
                            .Description("FOOD")
                            .Hide()
                            .Alias(new string[] { "foodb", "foodblock" })
                            .Parameter("day", ParameterType.Optional)
                            .Do(async (e) =>
                            {
                                await e.Message.Delete();
                                var answers = Food.GetFoodOptionsSeparately(false, e.Args[0]);
                                if (answers.Length == 0)
                                {
                                    answers = Food.GetFoodOptionsSeparately(true, e.Args[0]);
                                }
                                foreach (var foodOption in answers)
                                {
                                    await e.Channel.SendMessage(foodOption);
                                }
                            });

            command.CreateCommand("food")
                            .Description("FOOD")
                            .Hide()
                            .Alias(new string[] { "imhungry" })
                            .Parameter("day", ParameterType.Optional)
                            .Do(async (e) =>
                            {
                                await e.Message.Delete();
                                string answer = Food.GetFoodOptions(false, e.Args[0]);
                                if (answer == "")
                                {
                                    answer = Food.GetFoodOptions(true, e.Args[0]);
                                }
                                if (answer.Length > 2000)
                                {
                                    var answers = Food.GetFoodOptionsSeparately(false, e.Args[0]);
                                    if (answers.Length == 0)
                                    {
                                        answers = Food.GetFoodOptionsSeparately(true, e.Args[0]);
                                    }
                                    foreach (var foodOption in answers)
                                    {
                                        await e.Channel.SendMessage(foodOption);
                                    }
                                    return;
                                }
                                await e.Channel.SendMessage(answer);
                            });

            command.CreateCommand("foodAll")
                            .Description("FOOD")
                            .Hide()
                            .Alias(new string[] { "fooda", "allfood", "allFood", "foodall" })
                            .Parameter("day", ParameterType.Optional)
                            .Do(async (e) =>
                            {
                                await e.Message.Delete();
                                string answer = Food.GetFoodOptions(true, e.Args[0]);
                                if (answer.Length > 2000)
                                {
                                    var answers = Food.GetFoodOptionsSeparately(true, e.Args[0]);
                                    foreach (var foodOption in answers)
                                    {
                                        await e.Channel.SendMessage(foodOption);
                                    }
                                    return;
                                }
                                await e.Channel.SendMessage(answer);
                            });

            command.CreateCommand("foodAllb")
                            .Description("FOOD")
                            .Hide()
                            .Alias(new string[] { "foodb", "allfoodb", "allFoodb", "foodallb" })
                            .Parameter("day", ParameterType.Optional)
                            .Do(async (e) =>
                            {
                                await e.Message.Delete();

                                var answers = Food.GetFoodOptionsSeparately(true, e.Args[0]);
                                foreach (var foodOption in answers)
                                {
                                    await e.Channel.SendMessage(foodOption);
                                }
                            });

            command.CreateCommand("addFoodOption")
                            .Description("MORE FOOD")
                            .Hide()
                            .Alias(new string[] { "addFoodOption", "addfood", "addfoodoption", "addFoodoption", "addfoodOption" })
                            .Parameter("krz", ParameterType.Required)
                            .Parameter("name", ParameterType.Required)
                            .Parameter("desc", ParameterType.Optional)
                            .Parameter("days", ParameterType.Optional)
                            .Parameter("info", ParameterType.Optional)
                            .Do(async (e) =>
                            {
                                await e.Message.Delete();
                                string krz = e.GetArg("krz");
                                string name = e.GetArg("name");
                                string desc = e.GetArg("desc");
                                string days = e.GetArg("days");
                                string info = e.GetArg("info");
                                string[] daysArray = days.Trim(' ').Split(',');
                                List<Food.Day> dayList = new List<Food.Day>();
                                foreach (var dayString in daysArray)
                                {
                                    Food.Day dayEnum;
                                    string day = dayString.Trim(' ');
                                    if (Enum.TryParse(day, true, out dayEnum))
                                    {
                                        dayList.Add(dayEnum);
                                    }
                                }
                                Food.AddFoodOption(krz, name, desc, dayList.AsEnumerable(), info);

                                await e.Channel.SendMessage("**" + e.Args[0] + "** was added.");
                            });

            command.CreateCommand("getFoodOption")
                            .Description("FOOD")
                            .Hide()
                            .Alias(new string[] { "foodOption", "foodoption", "getFood", "getfood" })
                            .Parameter("krz", ParameterType.Required)
                            .Do(async (e) =>
                            {
                                await e.Message.Delete();
                                await e.Channel.SendMessage(Food.GetFoodOption(e.Args[0]));
                            });

            command.CreateCommand("getFoodOptions")
                            .Description("FOOD")
                            .Hide()
                            .Alias(new string[] { "foodOptions", "foodoptions", "getFoodAll", "getfoodall" })
                            .Do(async (e) =>
                            {
                                await e.Message.Delete();
                                var messagesToSend = Supporter.SplitMessage(Food.GetFoodOptions());
                                foreach (var msg in messagesToSend)
                                {
                                    await e.Channel.SendMessage(msg);
                                }
                            });

            command.CreateCommand("startVote")
                            .Description("FOOD")
                            .Hide()
                            .Alias(new string[] { "startvote" })
                            .Parameter("time", ParameterType.Optional)
                            .Do(async (e) =>
                            {
                                if (!Jenkins.Users.IsUserDev(e.User.Id))
                                {
                                    return;
                                }
                                await e.Message.Delete();
                                if (Supporter.ValidateTime(e.Args[0]))
                                    Food.StartVote(Supporter.GetParsedDateTime(e.Args[0]), e.Channel);
                            });

            command.CreateCommand("nextVote")
                            .Description("FOOD")
                            .Hide()
                            .Alias(new string[] { "nextvote" })
                            .Do(async (e) =>
                            {
                                if (!Jenkins.Users.IsUserDev(e.User.Id))
                                {
                                    return;
                                }
                                await e.Message.Delete();
                                string msg = "Next vote will start at **" + Food.NextVoteStart.ToLongDateString() + " " + Food.NextVoteStart.ToShortTimeString() + "**";

                                Channel voteChannel = Client.GetChannel(Config.DailyVoteChannel);
                                if (voteChannel != null)
                                {
                                    msg = msg + " in Channel **#" + voteChannel.Name + "** on Server **" + voteChannel.Server.Name + "**";
                                }
                                await e.Channel.SendMessage(msg);
                            });

            command.CreateCommand("vote")
                            .Description("FOOD")
                            .Hide()
                            .Alias(new string[] { "voteFood", "votefood" })
                            .Parameter("krz", ParameterType.Optional)
                            .Do(async (e) =>
                            {
                                if (!Food.IsVoteRunning)
                                {
                                    await e.Channel.SendMessage("Für was willst du abstimmen, " + e.Message.User.Mention + " ?");
                                    return;
                                }
                                if (Food.IsValidKRZ(e.Args[0]))
                                {
                                    await e.Message.Delete();
                                    if (!Food.IsValidDayForKRZ(e.Args[0]))
                                    {
                                        await e.Channel.SendMessage(string.Format("**{0}** [**{1}**] ist heute nicht verfügbar, {2}",
                                            Food.GetFoodName(e.Args[0]),
                                            e.Args[0].ToUpper(),
                                            e.Message.User.Mention));
                                        return;
                                    }
                                    string foodName;
                                    Food.Vote(e.Args[0], e.Message.User.Id, out foodName);
                                }
                                else
                                {
                                    await e.Channel.SendMessage("**" + e.Args[0] + "** " + "ist kein valides Kürzel, " + e.Message.User.Mention + "\r\nProbiere es mit /**foodAll**");
                                }
                            });

            command.CreateCommand("votes")
                            .Description("FOOD")
                            .Hide()
                            .Alias(new string[] { "voteFood", "votefood" })
                            .Do(async (e) =>
                            {
                                if (!Food.IsVoteRunning)
                                {
                                    return;
                                }
                                await e.Message.Delete();
                                await e.Channel.SendMessage(Food.GetVotes());
                            });

            command.CreateCommand("delFoodOption")
                            .Description("FOOD")
                            .Hide()
                            .Alias(new string[] { "delfoodOption", "delfoodoption" })
                            .Parameter("krz", ParameterType.Required)
                            .Do(async (e) =>
                            {
                                if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                                {
                                    return;
                                }
                                await e.Message.Delete();
                                Food.DelFoodOption(e.Args[0]);
                                await e.Channel.SendMessage("**" + e.Args[0] + "** was delted");
                            });

            command.CreateCommand("schnaut")
                            .Description("FOOD")
                            .Hide()
                            .Do(async (e) =>
                            {
                                if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                                {
                                    return;
                                }
                                await e.Message.Delete();
                                Food.UpdateSchnaut("http://geiooo.net/schnaut", "http://geiooo.net/schnaut/read.php");
                            });

            #endregion Food

            #region Observe

            command.CreateCommand("observeHelp")
                            .Description("Observe")
                            .Hide()
                            .Alias(new string[] { "oh", "observehelp" })
                            .Do(async (e) =>
                            {
                                if (!Jenkins.Users.IsUserDev(e.User.Id))
                                {
                                    return;
                                }
                                await e.Message.Delete();
                                StringBuilder sb = new StringBuilder();
                                sb.AppendLine("< - - - **Observe-Help**:spy: - - - >");
                                sb.AppendLine();
                                sb.AppendLine("-> /**watchServer <Name>** [**ws**]");
                                sb.AppendLine("-> /**unwatchServer <Name>** [**us**]");
                                sb.AppendLine("-> /**allServers** [**servers**] [**as**]");
                                sb.AppendLine("-> /**observingServers** [**os**]");
                                await e.Channel.SendMessage(sb.ToString());
                            });

            command.CreateCommand("watchServer")
                            .Description("Observe")
                            .Hide()
                            .Alias(new string[] { "watchserver", "ws" })
                            .Parameter("name", ParameterType.Required)
                            .Do(async (e) =>
                            {
                                if (!Jenkins.Users.IsUserDev(e.User.Id))
                                {
                                    return;
                                }
                                await e.Message.Delete();
                                Observe.AddServer(e.Args[0]);
                            });

            command.CreateCommand("unwatchServer")
                            .Description("Observe")
                            .Hide()
                            .Alias(new string[] { "unwatchserver", "us" })
                            .Parameter("name", ParameterType.Required)
                            .Do(async (e) =>
                            {
                                if (!Jenkins.Users.IsUserDev(e.User.Id))
                                {
                                    return;
                                }
                                await e.Message.Delete();
                                Observe.DelServer(e.Args[0]);
                            });

            command.CreateCommand("allServers")
                            .Description("Observe")
                            .Hide()
                            .Alias(new string[] { "servers", "as" })
                            .Do(async (e) =>
                            {
                                if (!Jenkins.Users.IsUserDev(e.User.Id))
                                {
                                    return;
                                }
                                await e.Message.Delete();
                                await e.Channel.SendMessage(Supporter.BuildList("All servers", Observe.GetAllServerNames()));
                            });

            command.CreateCommand("observingServers")
                            .Description("Observe")
                            .Hide()
                            .Alias(new string[] { "observingservers", "os" })
                            .Do(async (e) =>
                            {
                                if (!Jenkins.Users.IsUserDev(e.User.Id))
                                {
                                    return;
                                }
                                await e.Message.Delete();
                                await e.Channel.SendMessage(Supporter.BuildList("All observing servers", Observe.GetAllObservingServerNames()));
                            });

            #endregion Observe

            #region GamesSync

            command.CreateCommand("gamesHelp")
                            .Description("GamesSync")
                            .Hide()
                            .Alias(new string[] { "gh", "gameshelp" })
                            .Do(async (e) =>
                            {
                                if (!Jenkins.Users.IsUserDev(e.User.Id))
                                {
                                    return;
                                }
                                await e.Message.Delete();
                                StringBuilder sb = new StringBuilder();
                                sb.AppendLine("< - - - **GamesSync-Help**:spy: - - - >");
                                sb.AppendLine();
                                sb.AppendLine("-> /**gamesHelp** [**gh**]");
                                sb.AppendLine("-> /**syncServer <Name>** [**syncserver**]");
                                sb.AppendLine("-> /**unSyncServer <Name>** [**unsyncserver**]");
                                sb.AppendLine("-> /**allSyncServers** [**syncServers**]");
                                await e.Channel.SendMessage(sb.ToString());
                            });

            command.CreateCommand("syncServer")
                            .Description("GamesSync")
                            .Hide()
                            .Alias(new string[] { "syncserver" })
                            .Parameter("name", ParameterType.Required)
                            .Do(async (e) =>
                            {
                                if (!Jenkins.Users.IsUserDev(e.User.Id))
                                {
                                    return;
                                }
                                await e.Message.Delete();
                                ulong serverId;
                                if (Supporter.TryGetServerIdByName(e.Args[0].ToString(), out serverId))
                                {
                                    Jenkins.GamesSync.AddServer(serverId);
                                    await e.User.SendMessage("Successfully enabled GamesSync for server **" + Client.GetServer(serverId).Name + "**");
                                }
                                else
                                {
                                    await e.User.SendMessage("Could not find server");
                                }
                            });

            command.CreateCommand("unSyncServer")
                            .Description("GamesSync")
                            .Hide()
                            .Alias(new string[] { "unsyncServer", "unsyncserver" })
                            .Parameter("name", ParameterType.Required)
                            .Do(async (e) =>
                            {
                                if (!Jenkins.Users.IsUserDev(e.User.Id))
                                {
                                    return;
                                }
                                await e.Message.Delete();
                                ulong serverId;
                                if (Supporter.TryGetServerIdByName(e.Args[0].ToString(), out serverId))
                                {
                                    Jenkins.GamesSync.DelServer(serverId);
                                    await e.User.SendMessage("Successfully disabled GamesSync for server **" + Client.GetServer(serverId).Name + "**");
                                }
                                else
                                {
                                    await e.User.SendMessage("Could not find server");
                                }
                            });

            command.CreateCommand("allSyncServers")
                            .Description("GamesSync")
                            .Hide()
                            .Alias(new string[] { "syncServers", "syncservers" })
                            .Do(async (e) =>
                            {
                                if (!Jenkins.Users.IsUserDev(e.User.Id))
                                {
                                    return;
                                }
                                await e.Message.Delete();
                                await e.Channel.SendMessage(Supporter.BuildList("All syncing servers", Jenkins.GamesSync.GetAllSyncingServerNames()));
                            });

            command.CreateCommand("sync")
                            .Description("GamesSync")
                            .Hide()
                            .Do((e) =>
                            {
                                if (!Jenkins.Users.IsUserDev(e.User.Id))
                                {
                                    return;
                                }
                                Jenkins.GamesSync.CheckServers();
                            });


            #endregion GamesSync

            #region Test

            command.CreateCommand("throwException")
                .Description("Guess it")
                .Hide()
                .Do(async (e) =>
                {
                    if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                    {
                        return;
                    }
                    int[] numberz = { 1, 2, 3 };
                    await e.Channel.SendMessage(numberz[3].ToString());
                });

            command.CreateCommand("test")
                .Description("Guess it")
                .Hide()
                .Do((e) =>
                {
                    if (!Jenkins.Users.IsUserDev(e.Message.User.Id))
                    {
                        return;
                    }
                    Jenkins.GamesSync.Init();
                });

            #endregion Test

            #endregion Commands

            #region Runtime

            Client.ExecuteAndWait(async () =>
    {
        while (true)
        {
            await Client.Connect(Config.DiscordToken, TokenType.Bot);
            Console.WriteLine("Jenkins successfully connected...");
            break;
        }
    });

            #endregion Runtime
        }

        #endregion Bot

        #region Methods

        public static async void NotifyDevs(string text)
        {
            var adminArray = Jenkins.Users.GetDevIDs();
            foreach (var adminID in adminArray)
            {
                try
                {
                    var adminChannel = Client.CreatePrivateChannel(adminID);
                    adminChannel.Wait();
                    await adminChannel.Result.SendMessage(text);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + " while NotifyDevs()");
                }
            }
        }

        public static async void SendMessage(string text, Channel channel)
        {
            await channel.SendMessage(text);
        }

        private async Task<string> GetWeather(User user, Message msg)
        {
            var address = "Frankfurt, Germany";
            string request = "";
            var locationService = new GoogleLocationService();
            var point = locationService.GetLatLongFromAddress(address);

            var latitude = point.Latitude;
            var longitude = point.Longitude;

            using (var client = new HttpClient())
            {
                client.Timeout = (new TimeSpan(0, 0, 5));
                try
                {
                    request = "https://api.darksky.net/forecast/a6e1a984fca6e6be6a2fc56e7e1b377c/50.1109,8.6821";
                    // request = string.Format("https://api.darksky.net/forecast/{0}/{1},{2}"
                    //, config.DarkskyAPIKey
                    //, latitude
                    //, longitude);
                    var rq = client.GetStringAsync(request);
                    rq.Wait();
                    return rq.Result;
                }
                catch (Exception e)
                {
                    // ToDo: Error on VM
                    await user.SendMessage(e.Message);
                    NotifyDevs(Supporter.BuildExceptionMessage(e, "GetWeather()", request));
                    await msg.Edit("I can't do this right now :(");
                    return null;
                }
            }
        }

        private string BuildForecast(WeatherDAO weather)
        {
            return string.Format("-- Weather forecast :white_sun_small_cloud:  for {0} --\r\n\r\nIt's **{1}** on **{2}C°** (feels like {3}C°) with a windspeed :dash: at **{4}m/s**"
                , DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString()
                , weather.summary
                , Supporter.Celcius(weather.temperature).ToString("0.#")
                , Supporter.Celcius(weather.apparentTemperature).ToString("0.#")
                , weather.windSpeed);
        }

        private void Log(object sender, LogMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        #endregion Methods
    }
}