using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Discord;
using Discord.Audio;
using Discord.Commands;
using NAudio.Wave;
using Cleverbot.Net;
using osu_api;

namespace DiscordBot
{
    internal class Bot
    {
        private DiscordClient client;
        private CommandService command;
        private Quotes quotes;
        private Users users;
        private string token = "MTk5Nzg0MDYxNTUwNDYwOTI5.C2D2kQ.pZ0i8hagMbZSE4cKCB-zsWddXtY";
        CleverbotSession session;
        private bool muted = false;
        public Bot()
        {
            #region Config

            var client = new DiscordClient();

            quotes = new Quotes(Path.Combine(Environment.CurrentDirectory,"files","quotes.txt"));
            
            users = new Users();

            session = CleverbotSession.NewSession(users.cleverBotAPIUser, users.cleverBotAPIKey);

            client = new DiscordClient(input =>
            {
                input.LogLevel = LogSeverity.Info;
                input.LogHandler = Log;
            });

            client.UsingCommands(input =>
            {
                input.PrefixChar = '/';
                input.AllowMentionPrefix = true;
            });

            client.UsingAudio(x =>
            {
                x.Mode = AudioMode.Outgoing;
            });

            #endregion Config

            #region Events

            client.MessageReceived += async (s, e) =>
            {

                if(e.Message.IsMentioningMe())
                {
                    await e.Channel.SendMessage(Talk(Supporter.RemoveMention(e.Message.Text))); 
                }
                else if (!muted && users.insultList.ContainsKey(e.Message.User.Id) && Supporter.RollDice(9))
                {
                    string authorName = e.User.Name;
                    await e.Channel.SendMessage(Supporter.BuildInsult(authorName));
                }
            };


            #endregion Events

            command = client.GetService<CommandService>();

            command.CreateCommand("help").Do(async (e) =>
            {
                await e.Message.Delete();
                await e.Channel.SendMessage("Hello, my Name is **Jenkins**!"
                    + "\r\n- /help - this command, sir. Are you retarded?"
                    + "\r\n- @Jenkins - Sit down and tell me all your secrets. I'm at your side!"
                    + "\r\n- /quote - You'll get one quote of my huge collection"
                    + "\r\n- /addQuote - adds your super fancy quote to my vocabulary"
                    + "\r\n- /findQuote - I'll search for a quote on your order, sir!"
                    + "\r\n- /mute - One word and I'll shut my dirty mouth!"
                    + "\r\n- /unmute - The show must go on"
                    );
            });

            command.CreateCommand("pardy").Do(async (e) =>
            {
                await e.Message.Delete();
                if (e.User.VoiceChannel != null)
                {
                    Channel vChannel = e.User.VoiceChannel;
                    var vClient = await client.GetService<AudioService>().Join(vChannel);

                    string path = Path.Combine(Environment.CurrentDirectory, "mp3", "pardy.mp3");
                    var channelCount = client.GetService<AudioService>().Config.Channels; // Get the number of AudioChannels our AudioService has been configured to use.
                    var OutFormat = new WaveFormat(48000, 16, channelCount); // Create a new Output Format, using the spec that Discord will accept, and with the number of channels that our client supports.
                    using (var MP3Reader = new Mp3FileReader(path)) // Create a new Disposable MP3FileReader, to read audio from the filePath parameter
                    using (var resampler = new MediaFoundationResampler(MP3Reader, OutFormat)) // Create a Disposable Resampler, which will convert the read MP3 data to PCM, using our Output Format
                    {
                        resampler.ResamplerQuality = 60; // Set the quality of the resampler to 60, the highest quality
                        int blockSize = OutFormat.AverageBytesPerSecond / 50; // Establish the size of our AudioBuffer
                        byte[] buffer = new byte[blockSize];
                        int byteCount;

                        while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0) // Read audio into our buffer, and keep a loop open while data is present
                        {
                            if (byteCount < blockSize)
                            {
                                // Incomplete Frame
                                for (int i = byteCount; i < blockSize; i++)
                                    buffer[i] = 0;
                            }
                            vClient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                        }
                    }
                    //streamFile(vClient, newKids);

                    //await client.GetService<AudioService>().Leave(vChannel);
                }
            }
             );

            command.CreateCommand("lul").Do(async (e) =>
            {
                await e.Message.Delete();
                if (e.User.VoiceChannel != null)
                {
                    Channel vChannel = e.User.VoiceChannel;
                    var vClient = await client.GetService<AudioService>().Join(vChannel);

                    string path = Path.Combine(Environment.CurrentDirectory,"mp3" ,"lul.mp3");
                    var channelCount = client.GetService<AudioService>().Config.Channels; // Get the number of AudioChannels our AudioService has been configured to use.
                    var OutFormat = new WaveFormat(48000, 16, channelCount); // Create a new Output Format, using the spec that Discord will accept, and with the number of channels that our client supports.
                    using (var MP3Reader = new Mp3FileReader(path)) // Create a new Disposable MP3FileReader, to read audio from the filePath parameter
                    using (var resampler = new MediaFoundationResampler(MP3Reader, OutFormat)) // Create a Disposable Resampler, which will convert the read MP3 data to PCM, using our Output Format
                    {
                        resampler.ResamplerQuality = 60; // Set the quality of the resampler to 60, the highest quality
                        int blockSize = OutFormat.AverageBytesPerSecond / 50; // Establish the size of our AudioBuffer
                        byte[] buffer = new byte[blockSize];
                        int byteCount;

                        while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0) // Read audio into our buffer, and keep a loop open while data is present
                        {
                            if (byteCount < blockSize)
                            {
                                // Incomplete Frame
                                for (int i = byteCount; i < blockSize; i++)
                                    buffer[i] = 0;
                            }
                            vClient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                        }
                    }
                    //await client.GetService<AudioService>().Leave(vChannel);
                }
            }
             );

            command.CreateCommand("play")
                .Parameter("text", ParameterType.Required)
                .Do(async (e) =>
            {
                await e.Message.Delete();
                if (e.User.VoiceChannel != null)
                {
                    client.UsingAudio(x =>
                    {
                        x.Mode = AudioMode.Outgoing;
                    });
                    var vClient = await client.GetService<AudioService>().Join(e.User.VoiceChannel);

                    string newKids = @"https://www.youtubeinmp3.com/fetch/?video=http://www.youtube.com/watch?v=7Foftj4voa0";

                    await vClient.Disconnect();
                }
            });
            
                command.CreateCommand("mute").Do(async (e) =>
                {
                    await e.Message.Delete();
                    muted = true;
                });

            command.CreateCommand("unmute").Do(async (e) =>
            {
                await e.Message.Delete();
                muted = false;
            });

            command.CreateCommand("join").Do(async (e) =>
            {
                await e.Message.Delete();
                await client.GetService<AudioService>().Join(e.User.VoiceChannel);
            });

            command.CreateCommand("leave").Do(async (e) =>
            {
                await e.Message.Delete();
                await client.GetService<AudioService>().Leave(e.User.VoiceChannel);
            });

            command.CreateCommand("disconnect").Do(async (e) =>
            {
                if(users.usersNames["marlz"] == e.Message.User.Id)
                {
                    await e.Message.Delete();
                    await client.Disconnect();
                }
                
            });

            command.CreateCommand("ping").Do(async (e) =>
            {
                await e.Channel.SendMessage("Pong!");
            }
            );

            command.CreateCommand("quote")
                .Alias(new string[] { "q" })
                .Do(async (e) =>
            {
                await e.Message.Delete();
                await e.Channel.SendMessage(quotes.GetRandomQuote());
            });

            command.CreateCommand("shutdown")
                .Do(async (e) =>
                {
                    if (users.usersNames["marlz"] == e.Message.User.Id)
                    {
                        await e.Message.Delete();
                        await e.Channel.SendMessage("Shutting down...");
                        await client.Disconnect();
                        Environment.Exit(0);
                    }
                });

            command.CreateCommand("addQuote")
                .Alias(new string[] { "aq", "addquote" })
                .Description("Add a quote\r\nExample: /addQuote text:'Superawesome quote!'")
                .Parameter("text", ParameterType.Required)
                .Do(async (e) =>
            {
                await e.Message.Delete();
                string newQuote = e.GetArg(0);
                quotes.AddQuote(newQuote);
                await e.Channel.SendMessage("A new quote has been added, Sir!");
            });

            command.CreateCommand("findQuote")
                .Alias(new string[] { "fq", "findquote" })
                .Description("Search for specific a quote\r\nExample: /searchQuote text:'Superawes'")
                .Parameter("text", ParameterType.Required)
                .Do(async (e) =>
                {
                    string quote;
                    await e.Message.Delete();
                    string searchKey = e.GetArg(0);
                    if (quotes.GetSpecificQuote(searchKey, out quote))
                    {
                        await e.Channel.SendMessage(quote);
                    }
                    else
                    {
                        await e.User.SendMessage(quote);
                    }
                });



            #region Essentials

            client.ExecuteAndWait(async () =>
            {
                while (true)
                {
                    await client.Connect(token, TokenType.Bot);
                    Console.WriteLine("successfully connected...");
                    break;
                }
            });

            #endregion Essentials
        }

        private string Talk(string msg)
        {
            return session.Send(msg);
        }

        private string GetOsuStats()
        {
            var api = new osuAPI("Your API Key");
            var redback = api.GetUser("Redback", Mode.osu);
            return redback.count_rank_ss;
        }

        private void Log(object sender, LogMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }
    }
}