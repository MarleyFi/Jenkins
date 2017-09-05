using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DiscordBot
{
    public static class GameManager
    {
        public static CommandService GameCommandService = Bot.Client.GetService<CommandService>();

        private static Dictionary<string, Timer> timers = new Dictionary<string, Timer>();

        private static Dictionary<string, PRSGame> activeGames = new Dictionary<string, PRSGame>();

        private static List<User> activePlayers = new List<User>();

        public static void Init()
        {
            GameCommandService.CreateCommand("PaperRockScissor")
            .Description("Challenge someone to a Paper-Rock-Scissor game")
            .Parameter("users", ParameterType.Unparsed)
            .Alias(new string[] { "battle", "game" })
            .Do(async (e) =>
            {
                await e.Message.Delete();
                if (e.Message.IsMentioningMe() && e.Message.MentionedUsers.Count() == 1)
                {
                    StartNewGame(e.Channel, e.User, true);
                    return;
                }
                if (e.Message.IsMentioningMe() && e.Message.MentionedUsers.Count() == 2)
                {
                    User opponent = e.Message.MentionedUsers.Where(user => user.Id != Bot.Client.CurrentUser.Id).First();
                    StartNewGame(e.Channel, opponent, true);
                    return;
                }
                if (e.Message.MentionedUsers.Count() == 0 ||
                e.Message.MentionedUsers.Count() >= 3 ||
                e.Channel.IsPrivate ||
                e.Message.MentionedUsers.First() == e.User ||
                (e.Message.MentionedUsers.Count() == 2 && e.Message.MentionedUsers.ElementAt(0) == e.Message.MentionedUsers.ElementAt(1)))
                {
                    return;
                }
                if (e.Message.MentionedUsers.Count() == 1)
                {
                    StartNewGame(e.Channel, e.User, e.Message.MentionedUsers.First(), true);
                }
                if (e.Message.MentionedUsers.Count() == 2)
                {
                    StartNewGame(e.Channel, e.Message.MentionedUsers.ElementAt(0), e.Message.MentionedUsers.ElementAt(1), false);
                }
            });

            GameCommandService.CreateCommand("Scissors")
                .Description("Scrissors-choice")
                .Hide()
                .Alias(new string[] { "scissors, s" })
                .Do((e) =>
                {
                    var game = GetGameOfUser(e.User.Id);
                    if (game == null)
                        return;

                    if (game.SetPlayerChoice(e.User, PRSGame.Player.Choice.Scissors).Result)
                    {
                        FinishBattle(game);
                    }
                    else if (e.Channel == game.PublicChannel)
                    {
                        game.PublicChannel.SendMessage("Ich glaube, es wäre für dich von Vorteil, wenn du mir deine Wahl im privaten Chat schreibst, " + e.User.Mention + " :thinking:");
                    }
                });

            GameCommandService.CreateCommand("Rock")
                .Description("Rock-choice")
                .Hide()
                .Alias(new string[] { "rock, r" })
                .Do((e) =>
                {
                    var game = GetGameOfUser(e.User.Id);
                    if (game == null)
                        return;
                    
                    if (game.SetPlayerChoice(e.User, PRSGame.Player.Choice.Rock).Result)
                    {
                        FinishBattle(game);
                    }
                    else if(e.Channel == game.PublicChannel)
                    {
                        game.PublicChannel.SendMessage("Ich glaube, es wäre für dich von Vorteil, wenn du mir deine Wahl im privaten Chat schreibst, " + e.User.Mention+ " :thinking:");
                    }
                });

            GameCommandService.CreateCommand("Paper")
                .Description("Paper-choice")
                .Hide()
                .Alias(new string[] { "paper, p" })
                .Do((e) =>
                {
                    var game = GetGameOfUser(e.User.Id);
                    if (game == null)
                        return;

                    if (game.SetPlayerChoice(e.User, PRSGame.Player.Choice.Paper).Result)
                    {
                        FinishBattle(game);
                    }
                    else if (e.Channel == game.PublicChannel)
                    {
                        game.PublicChannel.SendMessage("Ich glaube, es wäre für dich von Vorteil, wenn du mir deine Wahl im privaten Chat schreibst, " + e.User.Mention + " :thinking:");
                    }
                });
        }

        public static async void StartNewGame(Channel publicChannel, User challengerUser, User adversaryUser, bool isChallenge)
        {
            var auditGame = GetGameOfUser(challengerUser.Id);
            if(auditGame != null)
            {
                await publicChannel.SendMessage("Das Spiel konnte nicht gestartet werden, da " + challengerUser.Mention + " gegenwärtig an einem anderen Spiel teilnimmt.");
                return;
            }
            auditGame = GetGameOfUser(adversaryUser.Id);
            if (auditGame != null)
            {
                await publicChannel.SendMessage("Das Spiel konnte nicht gestartet werden, da " + adversaryUser.Mention + " gegenwärtig an einem anderen Spiel teilnimmt.");
                return;
            }

            string id = GetId();
            var game = new PRSGame(id, publicChannel, challengerUser, adversaryUser, isChallenge);
            activeGames.Add(id, game);
            UpdatePlayerList();
            SetupBattletimer(game.Id);
        }

        public static async void StartNewGame(Channel publicChannel, User challengerUser, bool isChallenge)
        {
            var auditGame = GetGameOfUser(challengerUser.Id);
            User me = publicChannel.GetUser(Bot.Client.CurrentUser.Id);
            if (auditGame != null)
            {
                await publicChannel.SendMessage("Das Spiel konnte nicht gestartet werden, da " + challengerUser.Mention + " gegenwärtig an einem anderen Spiel teilnimmt.");
                return;
            }
            auditGame = GetGameOfUser(me.Id);
            if (auditGame != null)
            {
                await publicChannel.SendMessage("Das Spiel konnte nicht gestartet werden, da " + me.Mention + " gegenwärtig an einem anderen Spiel teilnimmt.");
                return;
            }

            string id = GetId();
            var game = new PRSGame(id, publicChannel, challengerUser, me, isChallenge);
            activeGames.Add(id, game);
            UpdatePlayerList();
            SetupBattletimer(game.Id);
        }

        private static void SetupBattletimer(string id)
        {
            timers.Add(id, new System.Threading.Timer(x =>
           {
               PRSGame prsGame;
               if (activeGames.TryGetValue(id, out prsGame))
               {
                   FinishBattle(prsGame);
               }
               else
               {
                   Bot.NotifyDevs("Could not find game for ID: " + id);
               }
           }, null, new TimeSpan(0, 5, 0), Timeout.InfiniteTimeSpan));
        }

        private static void UpdatePlayerList()
        {
            List<User> players = new List<User>();
            foreach (var game in activeGames)
            {
                if (!players.Contains(game.Value.Players[0].DiscordUser))
                    players.Add(game.Value.Players[0].DiscordUser);

                if (!players.Contains(game.Value.Players[1].DiscordUser))
                    players.Add(game.Value.Players[1].DiscordUser);
            }
            activePlayers = new List<User>();
        }

        private static PRSGame GetGameOfUser(ulong userId)
        {
            foreach (var game in activeGames.Values)
            {
                if (game.Users[0].Id.Equals(userId))
                    return game;
                if (game.Users[1].Id.Equals(userId))
                    return game;
            }
            return null;
        }

        private static void FinishBattle(PRSGame game)
        {
            game.EndBattle();
            activeGames.Remove(game.Id);
            timers.Remove(game.Id);
            UpdatePlayerList();
        }

        private static bool TryGetGame(string guid, out PRSGame game)
        {
            game = activeGames.Values.Where(prsGame => prsGame.Id.Equals(guid)).FirstOrDefault(); // Deprecated
            return (game.Id == guid);
        }

        public static string GetId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static bool IsPlayerAvailable(User user)
        {
            return !activePlayers.Contains(user);
        }
    }
}