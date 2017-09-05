using Discord.Commands;
using System;

namespace DiscordBot
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Configuration cfg = new Configuration();
            DiscordBotLog.Init(cfg.directoryPath);
            try
            {
                Bot bot = new Bot(cfg);
            }
            catch (Exception ex)
            {
                DiscordBotLog.AppendLog(DiscordBotLog.BuildErrorMessage(ex));
                DiscordBotLog.WriteSingleLog(DiscordBotLog.BuildErrorMessage(ex), "error.txt");
                Bot.Client.Disconnect();
            }
        }
    }
}