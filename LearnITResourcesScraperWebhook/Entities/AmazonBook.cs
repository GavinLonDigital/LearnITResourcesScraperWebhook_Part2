namespace LearnITResourcesScraperWebhook.Entities
{
    public class AmazonBook
    {
        public int Id { get; set; }
        public string? Input { get; set; }
        public int LanguageId { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string? Rating { get; set; }
        public string? Reviews { get; set; }
        public string? Price { get; set; }
        public string? PreviousPrice { get; set; }
        public string? ImageURL { get; set; }
    }
}
