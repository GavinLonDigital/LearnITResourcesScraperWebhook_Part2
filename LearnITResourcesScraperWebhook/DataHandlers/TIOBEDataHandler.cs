using Azure.Storage.Queues;
using LearnITResourcesScraperWebhook.Data;
using LearnITResourcesScraperWebhook.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace LearnITResourcesScraperWebhook.DataHandlers
{
    public class TIOBEDataHandler:DataHandler
    {

        private IList<TIOBERankedLanguage>? rankedLanguages;
        private const string stage = "2";

        public TIOBEDataHandler(dynamic JSONResult, ScrapeCaptureDbContext scrapeCaptureDbContext, QueueClient queueClient) : base((object)JSONResult, scrapeCaptureDbContext, queueClient)
        {

        }
        public override void PrepareData(JArray languageDataArray)
        {

            Console.WriteLine("Preparing TIOBE data for our database");

            try
            {

                IList<TIOBERankedLanguage> languages = languageDataArray.Select(b => new TIOBERankedLanguage
                {

                    RankOrder = Int32.Parse((string)b["ranking"]!),
                    LanguageName = (string)b["pLang"]!,
                    ImagePath = (string)b["imagePath"]!

                }).ToList();


                this.rankedLanguages = languages;

                Console.WriteLine($"Count of languages in TIOBE Index data: {this.rankedLanguages.Count}");
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }

        }

        public override async Task<bool> AddData()
        {
            Console.WriteLine($"Adding TIOBE data to our database");
            try
            {
                if (this.rankedLanguages?.Count > 0)
                {
                    await this.scrapeCaptureDbContext.AddRangeAsync(this.rankedLanguages);
                    await SaveData();
                    await TriggerScraper();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }

            return false;
        }


        public async override Task RemoveData()
        {
            Console.WriteLine($"Removing TIOBE data and all related data (Amazon, YouTube)");
            try
            {
                await this.scrapeCaptureDbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE [YouTubeChannels]");
                await this.scrapeCaptureDbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE [AmazonBooks]");
                await this.scrapeCaptureDbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE [TIOBERankedLanguages]");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }

        public override JArray GetDataToProcess(dynamic data)
        {
            //This is where JArray JSON is extracted from dynamic object
            return (JArray)data[0].data.rankingArr;
        }
        private async Task TriggerScraper()
        {
            try
            {
                TIOBERankedLanguage? TIOBERankedLanguage = await this.scrapeCaptureDbContext.TIOBERankedLanguages.SingleOrDefaultAsync(l => l.RankOrder == 1); //Start the next stage from the top ranked language

                if (TIOBERankedLanguage != null)
                {
                    await InsertMessageIntoQueue(stage, TIOBERankedLanguage.LanguageName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}

