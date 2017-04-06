using System;
using Discord;
using static Discord.Server;

namespace Discord
{
    public class MessageReactionAddEventArgs : EventArgs
    {
        public Emoji Emoji { get; }

        public Message Message { get; }

        public User User => Message.User;
        public Channel Channel => Message.Channel;
        public Server Server => Message.Server;

        public MessageReactionAddEventArgs(Message message, Emoji emoji)
        {
            Emoji = emoji;
        }
    }
}
