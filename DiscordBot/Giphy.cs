using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json.Linq;

namespace DiscordBot
{
    internal class Giphy
    {
        public static async void GetRandomGIF(Channel channel)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    string response = await client.GetStringAsync(string.Format("http://api.giphy.com/v1/gifs/random?api_key={0}"
                   , Bot.Config.GiphyAPIKey));
                    var requestObject = Newtonsoft.Json.Linq.JObject.Parse(response);
                    var dataString = requestObject.GetValue("data").ToString();
                    var dataObject = Newtonsoft.Json.Linq.JObject.Parse(dataString);
                    string gifURL = dataObject.GetValue("image_original_url").ToString();
                    await channel.SendMessage(gifURL);
                }
                catch (Exception e)
                {
                    await channel.SendMessage(e.Message);
                }
            }
        }

        public static async void GetGIF(string keyword, Channel channel)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    string response = await client.GetStringAsync(string.Format("http://api.giphy.com/v1/gifs/search?q={0}&api_key={1}&limit={2}"
                   , keyword
                   , Bot.Config.GiphyAPIKey
                   , Bot.Config.GiphySearchLimit));
                    var requestObject = Newtonsoft.Json.Linq.JObject.Parse(response);
                    var dataString = requestObject.GetValue("data").ToString();
                    JArray dataObject = Newtonsoft.Json.Linq.JArray.Parse(dataString);
                    if(dataObject.Count == 0)
                    {
                        await channel.SendMessage(string.Format("There are no results for **{0}** :("
                                                    , keyword));
                        return;
                    }
                    var arrayString = dataObject.ElementAt(Supporter.GetRandom(dataObject.Count)).ToString();
                    var arrayObject = Newtonsoft.Json.Linq.JObject.Parse(arrayString);
                    var imagesString = arrayObject.GetValue("images").ToString();
                    var imagesObject = Newtonsoft.Json.Linq.JObject.Parse(imagesString);
                    var fixedHeightString = imagesObject.GetValue("fixed_height").ToString();
                    var fixedHeightObject = Newtonsoft.Json.Linq.JObject.Parse(fixedHeightString);
                    string gifURL = fixedHeightObject.GetValue("url").ToString();
                    await channel.SendMessage(string.Format("**#{0}**\r\n{1}"
                        , keyword
                        , gifURL));
                }
                catch (Exception e)
                {
                    if (e.Message.Equals("Sequence contains no elements") || e.Message.Equals("Die Sequenz enthält keine Elemente") || e.Data.ToString() == "System.Collections.ListDictionaryInternal")
                    {
                        
                        return;
                    }
                    Bot.NotifyDevs(Supporter.BuildExceptionMessage(e, "GetGIF()", keyword));
                }
            }
        }

        public static async void GetTrendingGIF(Channel channel)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    string response = await client.GetStringAsync(string.Format("http://api.giphy.com/v1/gifs/trending?api_key={0}&limit={1}"
                   , Bot.Config.GiphyAPIKey
                   , Bot.Config.GiphySearchLimit));
                    var requestObject = Newtonsoft.Json.Linq.JObject.Parse(response);
                    var dataString = requestObject.GetValue("data").ToString();
                    JArray dataObject = Newtonsoft.Json.Linq.JArray.Parse(dataString);
                    var arrayString = dataObject.ElementAt(Supporter.GetRandom(Bot.Config.GiphySearchLimit)).ToString();
                    var arrayObject = Newtonsoft.Json.Linq.JObject.Parse(arrayString);
                    var imagesString = arrayObject.GetValue("images").ToString();
                    var imagesObject = Newtonsoft.Json.Linq.JObject.Parse(imagesString);
                    var fixedHeightString = imagesObject.GetValue("fixed_height").ToString();
                    var fixedHeightObject = Newtonsoft.Json.Linq.JObject.Parse(fixedHeightString);
                    string gifURL = fixedHeightObject.GetValue("url").ToString();
                    await channel.SendMessage(gifURL);
                }
                catch (Exception e)
                {
                    Bot.NotifyDevs(Supporter.BuildExceptionMessage(e, "GetTrendingGIF()"));
                }
            }
        }
    }
}
