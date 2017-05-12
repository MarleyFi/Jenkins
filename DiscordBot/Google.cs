using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace DiscordBot
{
    internal class Google
    {
        #region Methods

        public static string GetShortenURL(string url)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(string.Format("https://www.googleapis.com/urlshortener/v1/url?key={0}",
                            Bot.Config.GoogleAPIKey));
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = new JavaScriptSerializer().Serialize(new
                {
                    longUrl = url,
                });

                streamWriter.Write(json);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                var responseObject = Newtonsoft.Json.Linq.JObject.Parse(result);
                var shortenURL = responseObject.GetValue("id").ToString();
                return shortenURL;
            }
        }

        #endregion Methods
    }
}