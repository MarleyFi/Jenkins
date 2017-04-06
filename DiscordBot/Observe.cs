using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordBot
{
    internal class Observe
    {
        #region Methods

        public static string[] GetAllServerNames()
        {
            var servers = Bot.Client.Servers;
            List<string> serverNames = new List<string>();
            foreach (var server in servers)
            {
                serverNames.Add(server.Name);
            }
            return serverNames.ToArray();
        }

        public static string[] GetAllObservingServerNames()
        {
            var servers = Jenkins.Database.Tables["OBSERVE"].AsEnumerable();
            List<string> serverNames = new List<string>();
            foreach (var server in servers)
            {
                serverNames.Add(server.Field<string>("SERVERNAME"));
            }
            return serverNames.ToArray();
        }

        public static void AddServer(string name)
        {
            Server server;
            if(TryGetServerByName(name, out server))
            {
                Jenkins.Database.Tables["OBSERVE"].Rows.Add(server.Id, server.Name);
                Jenkins.Write();
            }
        }

        public static void DelServer(string name)
        {
            var observingTable = Jenkins.Database.Tables["OBSERVE"].AsEnumerable();
            ulong serverId = 0;
            if (TryGetServerIdByName(name, out serverId))
            {
                observingTable.Where(r => r.Field<ulong>("SERVERID").Equals(serverId)).First().Delete();
                Jenkins.Write();
            }
        }

        public static bool IsServerObserved(ulong serverId)
        {
            var observingTable = Jenkins.Database.Tables["OBSERVE"].AsEnumerable();
            var servers = observingTable.Where(r => r.Field<ulong>("SERVERID").Equals(serverId));
            return (servers != null && servers.Count() >= 1);
        }

        private static bool TryGetServerNameById(ulong serverId, out string serverName)
        {
            var server = Bot.Client.GetServer(serverId);
            serverName = (server == null ? "" : server.Name);
            return (server != null);
        }

        private static bool TryGetServerById(ulong serverId, out Server server)
        {
            server = Bot.Client.GetServer(serverId);
            return (server != null);
        }

        private static bool TryGetServerIdByName(string name, out ulong serverId)
        {
            var server = Bot.Client.Servers.Where(r => r.Name.ToLower().Contains(name.ToLower())).First();
            serverId = (server == null ? 0 : server.Id);
            return (server != null);
        }

        private static bool TryGetServerByName(string name, out Server server)
        {
            server = Bot.Client.Servers.Where(r => r.Name.ToLower().Contains(name.ToLower())).First();
            return (server != null);
        }
        #endregion Methods
    }
}
