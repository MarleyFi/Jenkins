using System;
using System.IO;
using System.Text;

namespace Discord.Commands
{
    public static class DiscordBotLog
    {
        #region Internal Variables

        private static string logFilePath;

        private static string logPath;

        #endregion Internal Variables

        #region Constructor

        public static void Init(string logDirectory)
        {
            if (!Directory.Exists(logDirectory))
            {
                logDirectory = Path.Combine(Environment.CurrentDirectory, "files");
            }
            logPath = logDirectory;
            logFilePath = Path.Combine(logDirectory, "log.txt");
            if (!File.Exists(logFilePath))
                File.Create(logFilePath);
        }

        #endregion Constructor

        #region Methods

        public static void AppendLog(string message)
        {
            File.AppendAllText(logFilePath, message, Encoding.Unicode);
        }

        public static void WriteSingleLog(string message, string fileName)
        {
            if (Path.Combine(logPath, fileName).Equals(logFilePath))
                return;

            string singleFilePath = Path.Combine(logPath, fileName);

            File.WriteAllText(singleFilePath, message);
        }

        public static string BuildCommandExceptionMessage(Exception ex, CommandEventArgs command)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Exception thrown at ");
            sb.AppendLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString());
            sb.Append("Command: ");
            sb.AppendLine(command.Message.RawText);
            sb.Append("User: ");
            sb.Append(command.User.Name);
            sb.Append("<@");
            sb.Append(command.User.Id);
            sb.AppendLine(">");
            if(command.Channel.IsPrivate)
            {
                sb.AppendLine("In private chat");
            }
            else
            {
                sb.Append("On ");
                sb.AppendLine(command.Server.Name + " #" + command.Channel.Name);
            }
            
            sb.Append("Message: ");
            sb.AppendLine(ex.Message);
            sb.AppendLine(ex.StackTrace);
            sb.AppendLine("---------------------------------------------------------------------------------------------");
            return sb.ToString();
        }

        public static string BuildRuntimeExceptionMessage(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("- RuntimeException -");
            sb.Append("Exception thrown at ");
            sb.AppendLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString());
            sb.Append("Message: ");
            sb.AppendLine(ex.Message);
            sb.AppendLine(ex.StackTrace);
            sb.AppendLine("---------------------------------------------------------------------------------------------");
            return sb.ToString();
        }

        public static string BuildErrorMessage(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("- Critical error -");
            sb.Append("Exception thrown at ");
            sb.AppendLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString());
            sb.Append("Message: ");
            sb.AppendLine(ex.Message);
            sb.AppendLine(ex.StackTrace);
            sb.AppendLine("---------------------------------------------------------------------------------------------");
            return sb.ToString();
        }

        #endregion Methods
    }
}
