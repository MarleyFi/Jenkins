using System;
using System.Linq;

namespace DiscordBot
{
    public class QuoteDAO
    {
        public string id { get; set; }

        public string quote { get; set; }

        public string owner { get; set; }

        public string ownerId { get; set; }

        public string datebirth { get; set; }

        public string datecreated { get; set; }

        public string dateedited { get; set; }

        public int rating { get; set; }
    }
}