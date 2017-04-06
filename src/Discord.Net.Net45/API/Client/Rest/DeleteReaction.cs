using Newtonsoft.Json;

namespace Discord.API.Client.Rest
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DeleteReactionRequest : IRestRequest
    {
        string IRestRequest.Method => "DELETE";
        string IRestRequest.Endpoint => $"channels/{ChannelId}/messages/{MessageId}/reactions/{Emoji}:{EmojiId}";
        object IRestRequest.Payload => null;

        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public string Emoji { get; set; }
        public ulong EmojiId { get; set; }

        public DeleteReactionRequest(ulong channelId, ulong messageId, string emoji, ulong emojiId)
        {
            ChannelId = channelId;
            MessageId = messageId;
            Emoji = emoji;
            EmojiId = emojiId;
        }
    }
}
