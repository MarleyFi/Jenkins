using System;
using System.Linq;

namespace DiscordBot
{
    public class QuoteStatDAO
    {
        public string id { get; set; }

        public string name { get; set; }

        public string datebirth { get; set; }

        public int quotes { get; set; }

        public double ?rating { get; set; }
    }
}