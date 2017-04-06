using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Cleverbot.Net;

namespace DiscordBot
{
    internal class Cleverbot
    {
        private static CleverbotSession cleverBotSession;

        public static bool IsServiceAvailable;

        public static void Init()
        {
            if (Bot.Config.CleverbotEnabled)
            {
                IsServiceAvailable = CreateCleverBotSession(Bot.Config.CleverbotNick);
                if (!IsServiceAvailable)
                {
                    try
                    {
                        cleverBotSession = CleverbotSession.NewSession(Bot.Config.CleverbotAPIUser, Bot.Config.CleverbotAPIKey);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error while setting up Cleverbot-API...\r\nExceptionMsg: " + e.Message);
                    }
                }
            }
        }

        public static string Talk(string msg)
        {
            try
            {
                var msgTask = cleverBotSession.SendAsync(msg);
                msgTask.Wait();
                return msgTask.Result;
            }
            catch (Exception e)
            {
                Bot.NotifyDevs(Supporter.BuildExceptionMessage(e, "Talk()", msg));
                return "The Cleverbot-API is currently unavailable.";
            }
        }

        /// <summary>
        /// Manual API-Call
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string TalkWithCleverBot(string text) 
        {
            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>
            {
       { "user", Bot.Config.CleverbotAPIUser },
       { "key", Bot.Config.CleverbotAPIKey },
       { "nick", Bot.Config.CleverbotNick },
       { "text", text },
       { "locale", "de-DE" }
            };

                var content = new FormUrlEncodedContent(values);

                var response = client.PostAsync("https://cleverbot.io/1.0/ask", content);

                var responseString = response.Result.Content.ReadAsStringAsync();
                response.Wait();
                var responseObject = Newtonsoft.Json.Linq.JObject.Parse(responseString.Result.ToString());
                var answer = responseObject.GetValue("response").ToString();
                return answer;
            }
        }

        private static bool CreateCleverBotSession(string name = "Jenkins")
        {
            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>
            {
       { "user", Bot.Config.CleverbotAPIUser },
       { "key", Bot.Config.CleverbotAPIKey },
       { "nick", name },
       { "locale", "de-DE" }
            };

                var content = new FormUrlEncodedContent(values);

                var response = client.PostAsync("https://cleverbot.io/1.0/create", content);

                response.Wait(10000);
                if (response.IsCompleted == false)
                {
                    return false;
                }
                var responseString = response.Result.Content.ReadAsStringAsync();
                var responseObject = Newtonsoft.Json.Linq.JObject.Parse(responseString.Result.ToString());
                var status = responseObject.GetValue("status").ToString();
                return (status == "success" || status == "Error: reference name already exists");
            }
        }
    }
}
