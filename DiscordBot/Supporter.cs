using System;
using System.IO;

namespace DiscordBot
{
    public static class Supporter
    {
        public static bool RollDice(int probability)
        {
            Random rnd = new Random();
            return rnd.Next(0, probability) == 0 ? true : false;
        }

        public static string BuildInsult(string name)
        {
            Random rnd = new Random();
            int index = rnd.Next(0, Insults.insults.Length);
            return (Insults.insults[index].Replace("*", name));
        }

        public static string RemoveMention(string text)
        {
            string result = text.Replace("@Jenkins", string.Empty).Remove(0,1);
            return result;
        }

        public static string TryFindDropbox(/*string fileName*/)
        {
            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string defaultDropboxPath = Path.Combine(userPath, "Dropbox");
            if (Directory.Exists(defaultDropboxPath))
            {
                return defaultDropboxPath;
            }
            else
            {
                try
                {
                    var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dropbox\\host.db");
                    var dbBase64Text = Convert.FromBase64String(File.ReadAllText(dbPath));
                    var folderPath = System.Text.ASCIIEncoding.ASCII.GetString(dbBase64Text);
                    return dbPath;
                }
                catch (Exception)
                {
                    return null;
                }
                    
            }
        }
    }
}
