using System.Security.Policy;

namespace LearnITResourcesScraperWebhook.Entities
{
    public class YouTubeChannel
    {
        public int Id { get; set; }
        public int LanguageId { get; set; }
        public  string? Url { get; set; }
        public string? ChannelName { get; set; }
        public string? Subscribers { get; set; }
        public string? Input { get; set; }
    
    }
}
