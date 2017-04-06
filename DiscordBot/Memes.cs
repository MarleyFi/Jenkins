using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    internal class Memes
    {
        #region Variables

        private static string directoryPath;

        private static List<string> memePaths;

        private static List<string> memeList;

        private static List<string> availabeMemes;

        #endregion Variables

        #region Methods

        public static void Init()
        {
            directoryPath = Path.Combine(Environment.CurrentDirectory, "files", "memes");
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            memePaths = new List<string>();
            memeList = new List<string>();
            availabeMemes = new List<string>();

            foreach (var fileName in Directory.GetFiles(directoryPath, "*", SearchOption.TopDirectoryOnly).AsEnumerable())
            {
                memePaths.Add(fileName);
                availabeMemes.Add(Path.GetFileNameWithoutExtension(fileName).ToLower());
                memeList.Add(Path.GetFileNameWithoutExtension(fileName));
            }
        }

        public static List<string> GetMemes()
        {
            return memeList;
        }

        public static string GetMemePath(string name)
        {
            if(availabeMemes.Contains(name.ToLower()))
            {
                return memePaths[availabeMemes.IndexOf(name.ToLower())];
            }
            return string.Empty;
        }

        public static bool TryGetMeme(string name, out string meme)
        {
            meme = "";
            for (int i = 0; i < availabeMemes.Count; i++)
            {
                string currentMeme = availabeMemes[i];
                if (currentMeme.StartsWith(name.ToLower()) || currentMeme.Contains(name.ToLower()))
                {
                    meme = memeList[i];
                    return true;
                }
            }
            if (availabeMemes.Contains(name.ToLower()))
            {
                meme = memeList[availabeMemes.IndexOf(name)];
                return true;
            }
            return false;
        }

        #endregion Methods
    }
}
