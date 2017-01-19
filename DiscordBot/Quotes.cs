using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    class Quotes
    {

        private string filePath;

        public Quotes(string path)
        {
            filePath = path;
            ReadQuotes();
        }

        private void ReadQuotes()
        {
            try
            {
                quotes = File.ReadAllLines(filePath);
            }
            catch (Exception e)
            {
                quotes = exampleQuotes;
            }
        }

        public void AddQuote(string quote)
        {
            List<string> quoteList = getListOfQuotes();
            quoteList.Add(quote);
            quotes = quoteList.ToArray();
            File.WriteAllLines(filePath, quotes);
        }

        public string GetRandomQuote()
        {
            Random rnd = new Random();
            int quoteIndex = rnd.Next(0, quotes.Length);
            return "'**"+quotes[quoteIndex]+"**'\r\n";
        }

        public bool GetSpecificQuote(string text, out string quote)
        {
            List<string> quoteList = getListOfQuotes();

            List<string> searchResults = quoteList.FindAll(s => s.Contains(text));
            if(searchResults.Count >= 1)
            {
                quote = searchResults[0];
                return true;
            }
            string command = string.Format("/addQuote text:'{0}'", text);
            quote = string.Format("I've found nothing matching your search to {0} :(\r\nBut I could add it to my vocabulary for you with this command:\r\n\r\n{1}", text, command);
            return false;
        }

        private List<string> getListOfQuotes()
        {
            return new List<string>(quotes);
        }

        public string[] exampleQuotes = {
            "Ich fick richtig gerne!",
            "Ficken das isses. Schön die alde weghauen.",
            "Flatsch" };

        public string[] quotes;

    }
}
