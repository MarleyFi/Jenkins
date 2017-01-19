using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    class Users
    {
        public Dictionary<string, ulong> usersNames;
        public Dictionary<ulong, string> users;
        public Dictionary<ulong, string> insultList;

        public string cleverBotAPIUser = "uIb9o6e9VYNgLSBQ";
        public string cleverBotAPIKey = "N67iXe8rBr5hwGXjfN8tANKQ0NeYNvJf";

        public Users()
        {
            usersNames = new Dictionary<string, ulong>();
            usersNames.Add("gerrie", 208232302965293066);
            usersNames.Add("craank", 104311642359083008);
            usersNames.Add("marlz", 111794715690549248);
            usersNames.Add("fanubert", 105363673861627904);
            usersNames.Add("hermsen", 164630818822684683);
            usersNames.Add("geiooo", 164780677806555145);
            usersNames.Add("dave", 197433963785093121);
            usersNames.Add("ramon", 188003297452359680);
            usersNames.Add("nordmann", 104631999632785408);

            users = new Dictionary<ulong, string>();
            users.Add(208232302965293066, "gerrie");
            users.Add(104311642359083008, "craank");
            users.Add(111794715690549248, "marlz");
            users.Add(105363673861627904, "fanubert");
            users.Add(164630818822684683, "hermsen");
            users.Add(164780677806555145, "geiooo");
            users.Add(197433963785093121, "dave");
            users.Add(188003297452359680, "ramon");
            users.Add(104631999632785408, "nordmann");

            insultList = new Dictionary<ulong, string>();
            insultList.Add(208232302965293066, "gerrie");
            insultList.Add(104311642359083008, "craank");
            insultList.Add(105363673861627904, "fanubert");
            insultList.Add(164780677806555145, "geiooo");
            insultList.Add(164630818822684683, "hermsen");
        }

        
        
    }
}
