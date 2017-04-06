using Newtonsoft.Json;

namespace Discord.API.Client.Rest
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AddReactionRequest : IRestRequest<Message>
    {
        string IRestRequest.Method => "PUT";
        string IRestRequest.Endpoint => $"channels/{ChannelId}/messages/{MessageId}/reactions/{Emoji}:{EmojiId}";
        object IRestRequest.Payload => this;

        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public string Emoji { get; set; }
        public ulong EmojiId { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; } = "";

        public AddReactionRequest(ulong channelId, ulong messageId, string emoji, ulong emojiId)
        {
            ChannelId = channelId;
            MessageId = messageId;
            Emoji = emoji;
            EmojiId = emojiId;
        }
    }
}
