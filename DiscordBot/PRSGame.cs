using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot
{
    internal class PRSGame
    {
        #region Public Variables

        public string Id;

        public List<Player> Players = new List<Player>(2);

        public Channel PublicChannel;

        public Message GameMessage;

        public bool Done = false;

        #region Properties

        public User[] Users
        {
            get
            {
                return (new User[] { Players[0].DiscordUser, Players[1].DiscordUser });
            }
        }

        public User FirstUser
        {
            get
            {
                return Players[0].DiscordUser;
            }
        }

        public User SecondUser
        {
            get
            {
                return Players[1].DiscordUser;
            }
        }

        public Player FirstPlayer
        {
            get
            {
                return Players[0];
            }
        }

        public Player SecondPlayer
        {
            get
            {
                return Players[1];
            }
        }

        public Player.Choice FirstPlayerChoice
        {
            get
            {
                return Players[0].PlayerChoice;
            }
            set
            {
                Players[0].PlayerChoice = value;
            }
        }

        public Player.Choice SecondPlayerChoice
        {
            get
            {
                return Players[1].PlayerChoice;
            }
            set
            {
                Players[1].PlayerChoice = value;
            }
        }

        #endregion Properties

        #endregion Public Variables

        #region Private Variables

        private string userNotifyTemplate
        {
            get
            {
                return "You have been invited to a Paper-Rock-Scrissor-Battle against {0}{1}.\r\nYou got the choice to pick out of four opportunities:\r\n**/Scissors**" + scissorsEmote + "  **/Rock**" + rockEmote + "  **/Paper**" + paperEmote + " or just do nothing.\r\n\r\nYou got time till " + DateTime.Now.AddMinutes(5).ToLongTimeString();
            }
        }

        private const string paperEmote = ":page_facing_up:";

        private const string rockEmote = ":moyai:";

        private const string scissorsEmote = ":scissors:";

        private bool isChallenge = false;

        private bool isBotGame = false;

        #endregion Private Variables

        #region Constructor

        public PRSGame(string id, Channel publicChannel, User challengerUser, User adversaryUser, bool isChallenge)
        {
            this.Id = id;
            this.PublicChannel = publicChannel;
            this.isBotGame = Bot.Client.CurrentUser.Id.Equals(adversaryUser.Id);
            Players.Add(new Player(challengerUser));
            Players.Add(new Player(adversaryUser));
            this.isChallenge = isChallenge;
            AnnounceBattle();
        }

        #endregion Constructor

        #region Methods

        private async void AnnounceBattle()
        {
            await PublicChannel.SendMessage(":crossed_swords: " + FirstUser.Mention + (isChallenge ? " challenges " : " vs. ") + SecondUser.Mention + " in a Paper-Rock-Scissor-Battle :crossed_swords:");
            GameMessage = await PublicChannel.SendMessage("...waiting for users to pick their choices");
            NotifyUsers();
        }

        private async void NotifyUsers()
        {
            await FirstUser.SendMessage(string.Format(userNotifyTemplate, SecondUser.Mention, (isChallenge ? " as the challenging opponent" : "")));
            if (isBotGame)
            {
                SetBotChoice(Bot.Client.CurrentUser.Id, GetRandomChoice());
            }
            else
            {
                await SecondUser.SendMessage(string.Format(userNotifyTemplate, FirstUser.Mention, (isChallenge ? " as the adversary opponent" : "")));
            }
        }

        public async Task<bool> CheckBattle(User user)
        {
            await GameMessage.Edit(user.Mention + " is ready. Waiting for his opponent..."); // ToDo: rly have to be Task<bool> ?
            return (FirstPlayerChoice != Player.Choice.Unassigned && SecondPlayerChoice != Player.Choice.Unassigned); // Is battle ready?
        }

        public async Task<bool> CheckBattle(string username)
        {
            await GameMessage.Edit(username + " is ready. Waiting for his opponent..."); // ToDo: rly have to be Task<bool> ?
            return (FirstPlayerChoice != Player.Choice.Unassigned && SecondPlayerChoice != Player.Choice.Unassigned); // Is battle ready?
        }

        public async void EndBattle()
        {
            await GameMessage.Edit("Users are both ready!");
            GameResult game = new GameResult(Players[0], Players[1]);
            await GameMessage.Edit(game.Result);
            Done = true;
        }

        public async Task<bool> SetPlayerChoice(User user, Player.Choice choice)
        {
            await user.PrivateChannel.SendMessage("Your choice is " + choice + GetChoiceEmote(choice));
            Players.Where(player => player.DiscordUser.Id.Equals(user.Id)).First().PlayerChoice = choice;
            return (CheckBattle(user).Result);
        }

        public bool SetBotChoice(ulong id, Player.Choice choice)
        {
            Players.Where(player => player.DiscordUser.Id.Equals(id)).First().PlayerChoice = choice;
            return (CheckBattle(Bot.Client.CurrentUser.Mention).Result);
        }

        public string GetChoiceEmote(Player.Choice choice)
        {
            switch (choice)
            {
                case Player.Choice.Unassigned:
                    return ":x:";
                case Player.Choice.Paper:
                    return paperEmote;
                case Player.Choice.Rock:
                    return rockEmote;
                case Player.Choice.Scissors:
                    return scissorsEmote;
                default:
                    return ":x:";
            }
        }

        private Player.Choice GetRandomChoice()
        {
            var choices = Enum.GetValues(typeof(Player.Choice));
            return (Player.Choice)choices.GetValue(Supporter.GetRandom(1, 3));
        }

        #endregion Methods

        public class Player
        {
            public User DiscordUser;

            public Choice PlayerChoice = Choice.Unassigned;

            public Player(User user)
            {
                this.DiscordUser = user;
            }

            public enum Choice
            {
                Unassigned,
                Paper,
                Rock,
                Scissors
            }
        }

        private class GameResult
        {
            public string Result;

            public GameResult(Player firstPlayer, Player secondPlayer)
            {
                if (firstPlayer.PlayerChoice.Equals(secondPlayer.PlayerChoice))
                {
                    Result = "Draw.\r\n\r\nBoth chose **" + firstPlayer.PlayerChoice + "** :dove:";
                    return;
                }

                string templ = firstPlayer.DiscordUser.Mention + " > **" + firstPlayer.PlayerChoice + "** :crossed_swords: **" + secondPlayer.PlayerChoice + "** < " + secondPlayer.DiscordUser.Mention + "\r\n\r\n:crown: {0} won!:tada:";

                if (firstPlayer.PlayerChoice == Player.Choice.Paper)
                {
                    if (secondPlayer.PlayerChoice.Equals(Player.Choice.Rock))
                    {
                        Result = string.Format(templ, firstPlayer.DiscordUser.Mention);
                    }
                    else // (two.Equals(Choice.Scissors))
                    {
                        Result = string.Format(templ, secondPlayer.DiscordUser.Mention);
                    }
                }
                else if (firstPlayer.PlayerChoice == Player.Choice.Rock)
                {
                    if (secondPlayer.PlayerChoice.Equals(Player.Choice.Paper))
                    {
                        Result = string.Format(templ, secondPlayer.DiscordUser.Mention);
                    }
                    else // (two.Equals(Choice.Scissors))
                    {
                        Result = string.Format(templ, firstPlayer.DiscordUser.Mention);
                    }
                }
                else // one == Choice.Scissors
                {
                    if (secondPlayer.PlayerChoice.Equals(Player.Choice.Rock))
                    {
                        Result = string.Format(templ, secondPlayer.DiscordUser.Mention);
                    }
                    else // (two.Equals(Choice.Paper))
                    {
                        Result = string.Format(templ, firstPlayer.DiscordUser.Mention);
                    }
                }
            }
        }
    }
}