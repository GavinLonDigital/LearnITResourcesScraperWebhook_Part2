using Azure.Storage.Queues;
using LearnITResourcesScraperWebhook.Data;
using LearnITResourcesScraperWebhook.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace LearnITResourcesScraperWebhook.DataHandlers
{
    public class AmazonDataHandler:DataHandler
    {

        IList<AmazonBook>? amazonBooks;
        private const string stage = "2";

        public AmazonDataHandler(dynamic JSONResult, ScrapeCaptureDbContext scrapeCaptureDbContext,QueueClient queueClient) : base((object)JSONResult, scrapeCaptureDbContext, queueClient)
        {
  
        }
        public override void PrepareData(JArray bookData)
        {

            Console.WriteLine("Preparing Amazon data for our database");

            try
            {
                SetLanguageId(bookData);

                IList<AmazonBook> bookList = bookData.Select(b => new AmazonBook
                {
                    Title = (string)b["title"]!,
                    Url = (string)b["url"]!,
                    Rating = (string)b["rating"]!,
                    Reviews = (string)b["reviews"]!,
                    ImageURL = (string)b["image"]!,
                    Input = (string)b["input"]["search"]!,
                    Price = (string)b["price"],
                    PreviousPrice = (string)b["previous_price"],
                    LanguageId = LanguageId

                    //Need to include a timestamp
                }).ToList();

                this.amazonBooks = bookList;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
               
            }



        }
        public override async Task<bool> AddData()
        {
            Console.WriteLine($"Adding Amazon data to our database - LanguageId: {LanguageId}");
            try
            {
                if (this.amazonBooks?.Count > 0)
                {
                    await this.scrapeCaptureDbContext.AddRangeAsync(this.amazonBooks);
                    await SaveData();
                    await AddDirectiveToQueue(stage);


                    return true;
                }
    
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
            return false;

        }


        public async override Task RemoveData()
        {
            Console.WriteLine($"Removing Amazon data - LanguageID: {LanguageId}");
            try
            {
               await scrapeCaptureDbContext.Database.ExecuteSqlRawAsync($"DELETE FROM AmazonBooks WHERE LanguageId ={LanguageId}");
            }
            catch (Exception ex)
            {

               Console.WriteLine($"{ex.Message}");
            }
        }
        public override JArray GetDataToProcess(dynamic data)
        {
            return (JArray)data;
        }
        private void SetLanguageId(JArray data)
        {
            try
            {
                string input = (string)data[1]["input"]["search"];
                LanguageId = this.scrapeCaptureDbContext.TIOBERankedLanguages.FirstOrDefault(d => d.LanguageName == input).Id;

            }
            catch (Exception ex)
            {

               Console.WriteLine($"{ex.Message}");
            }         

        }
    }



}
