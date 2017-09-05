using Discord;
using Discord.Commands;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class PaperRockScissors : IDisposable
    {
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

        private bool disposed = false;

        private Timer battleTimer;

        private Channel tChannel;

        private User challengerUser;

        private Choice challengerChoice = Choice.Unassigned;

        private User adversaryUser;

        private Choice adversaryChoice = Choice.Unassigned;

        private TimeSpan timeToGo;

        private Message gameMessage;

        private string reason;

        private bool isChallenge = false;

        CommandService GameCommandService = Bot.Client.GetService<CommandService>();

        private string userNotifyTemplate
        {
            get
            {
                return "You have been invited to a Paper-Rock-Scrissor-Battle against {0}{1}.\r\nYou got the choice to pick out of four opportunities:\r\n**/Scissors**" + scissorsEmote + "  **/Rock**" + rockEmote + "  **/Paper**" + paperEmote + " or just do nothing.\r\n\r\nYou got time till "+DateTime.Now.AddMinutes(5).ToLongTimeString();
            }
        }

        private string paperEmote = ":page_facing_up:";

        private string rockEmote = ":moyai:";

        private string scissorsEmote = ":scissors:";

        private enum Choice
        {
            Unassigned,
            Paper,
            Rock,
            Scissors
        }

        public PaperRockScissors(Channel tChannel, User challengerUser, User adversaryUser, bool isChallenge, int minutesToGo = 5, string reason = "")
        {
            this.tChannel = tChannel;
            this.challengerUser = challengerUser;
            this.adversaryUser = adversaryUser;
            timeToGo = new TimeSpan(0, minutesToGo, 0);
            this.reason = reason;
            this.isChallenge = isChallenge;
            SetupBattle();

            GameCommandService.CreateCommand("Scissors")
                .Description("Scrissors-choice")
                .Hide()
                .Alias(new string[] { "scissors, s" })
                .Do(async (e) =>
                {
                    if (e.User.Id == challengerUser.Id)
                    {
                        challengerChoice = Choice.Scissors;
                        CheckBattle(true);
                        await e.User.SendMessage("Your choice is scissors.");
                    }
                    if (e.User.Id == adversaryUser.Id)
                    {
                        adversaryChoice = Choice.Scissors;
                        CheckBattle(false);
                        await e.User.SendMessage("Your choice is scissors.");
                    }
                });

            GameCommandService.CreateCommand("Rock")
                .Description("Rock-choice")
                .Hide()
                .Alias(new string[] { "rock, r" })
                .Do(async (e) =>
                {
                    if (e.User.Id == challengerUser.Id)
                    {
                        challengerChoice = Choice.Rock;
                        CheckBattle(true);
                        await e.User.SendMessage("Your choice is rock.");
                    }
                    if (e.User.Id == adversaryUser.Id)
                    {
                        adversaryChoice = Choice.Rock;
                        CheckBattle(false);
                        await e.User.SendMessage("Your choice is rock.");
                    }
                });

            GameCommandService.CreateCommand("Paper")
                .Description("Paper-choice")
                .Hide()
                .Alias(new string[] { "paper, p" })
                .Do(async (e) =>
                {
                    if (e.User.Id == challengerUser.Id)
                    {
                        challengerChoice = Choice.Paper;
                        CheckBattle(true);
                        await e.User.SendMessage("Your choice is paper.");
                    }
                    if (e.User.Id == adversaryUser.Id)
                    {
                        adversaryChoice = Choice.Paper;
                        CheckBattle(false);
                        await e.User.SendMessage("Your choice is paper.");
                    }
                });
        }

        private class GameResult
        {
            public string Result;

            public GameResult(User userOne, User userTwo, Choice userOneChoice, Choice userTwoChoice)
            {
                if (userOneChoice.Equals(userTwoChoice))
                {
                    Result = "Draw.\r\n\r\nBoth chose **" + userOneChoice + "** :dove:";
                    return;
                }

                string templ = userOne.Mention + " > **" + userOneChoice + "** :crossed_swords: **" + userTwoChoice + "** < " + userTwo.Mention + "\r\n\r\n:crown: {0} won!:tada:";

                if (userOneChoice == Choice.Paper)
                {
                    if (userTwoChoice.Equals(Choice.Rock))
                    {
                        Result = string.Format(templ, userOne.Mention);
                    }
                    else // (two.Equals(Choice.Scissors))
                    {
                        Result = string.Format(templ, userTwo.Mention);
                    }
                }
                else if (userOneChoice == Choice.Rock)
                {
                    if (userTwoChoice.Equals(Choice.Paper))
                    {
                        Result = string.Format(templ, userTwo.Mention);
                    }
                    else // (two.Equals(Choice.Scissors))
                    {
                        Result = string.Format(templ, userOne.Mention);
                    }
                }
                else // one == Choice.Scissors
                {
                    if (userTwoChoice.Equals(Choice.Rock))
                    {
                        Result = string.Format(templ, userTwo.Mention);
                    }
                    else // (two.Equals(Choice.Paper))
                    {
                        Result = string.Format(templ, userOne.Mention);
                    }
                }
            }
        }

        private void SetupBattle()
        {
            if (timeToGo < TimeSpan.Zero)
            {
                return; //time already passed
            }

            battleTimer = new System.Threading.Timer(x =>
            {
                EndBattle();
            }, null, timeToGo, Timeout.InfiniteTimeSpan);

            AnnounceBattle();
        }

        private async void AnnounceBattle()
        {
            await tChannel.SendMessage(challengerUser.Mention + (isChallenge ? " challenges " : " vs. ") + adversaryUser.Mention + " in a Paper-Rock-Scissor-Battle"
                + (reason.Equals(string.Empty) ? "." : " the loser have to **" + reason + "**."));

            gameMessage = await tChannel.SendMessage("...waiting for users to pick their choices");
            NotifyUsers();
        }

        private async void NotifyUsers()
        {
            await challengerUser.SendMessage(string.Format(userNotifyTemplate, adversaryUser.Mention, (isChallenge ? " as the challenging opponent" : "")));
            await adversaryUser.SendMessage(string.Format(userNotifyTemplate, challengerUser.Mention, (isChallenge ? " as the adversary opponent" : "")));
        }

        private async void CheckBattle(bool isUserOne)
        {
            await gameMessage.Edit((isUserOne ? challengerUser.Mention : adversaryUser.Mention) + " is ready. Waiting for " + (isUserOne ? adversaryUser.Mention : challengerUser.Mention));
            if (challengerChoice != Choice.Unassigned && adversaryChoice != Choice.Unassigned) // Battle end
            {
                EndBattle();
            }
        }

        private async void EndBattle()
        {
            await gameMessage.Edit("Users are both ready!");
            GameResult game = new GameResult(challengerUser, adversaryUser, challengerChoice, adversaryChoice);
            await gameMessage.Edit(game.Result);
            //finish = true;
            battleTimer.Dispose();
            GameCommandService = null;
            Dispose();
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                // Free any other managed objects here.
                //
            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }

    }
}
