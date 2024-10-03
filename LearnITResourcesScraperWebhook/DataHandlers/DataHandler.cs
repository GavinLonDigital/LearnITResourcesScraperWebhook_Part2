using Azure.Storage.Queues;
using LearnITResourcesScraperWebhook.Data;
using LearnITResourcesScraperWebhook.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace LearnITResourcesScraperWebhook.DataHandlers
{
    public abstract class DataHandler
    {
        private readonly dynamic JSONResult;

        protected readonly ScrapeCaptureDbContext scrapeCaptureDbContext;
        protected readonly QueueClient queueClient;
        public int LanguageId { get; set; } = 0;

        public DataHandler(dynamic JSONResult, ScrapeCaptureDbContext scrapeCaptureDbContext, QueueClient queueClient)
        {

            this.JSONResult = JSONResult;
            this.scrapeCaptureDbContext = scrapeCaptureDbContext;
            this.queueClient = queueClient;
        }

        public abstract void PrepareData(JArray data);

        public abstract JArray GetDataToProcess(dynamic data);

        public abstract Task<bool> AddData();

        public abstract Task RemoveData();

        public bool PrepData()
        {
            Console.WriteLine($"PrepData is called");
            try
            {


            if (JSONResult?.GetType().ToString() == "Newtonsoft.Json.Linq.JObject") //could be JArray 
            {
                //BrightData sends back message to say that the dataset is still being collected
                if (JSONResult?.ContainsKey("status") && (JSONResult?.status == "collecting" || JSONResult?.status == "building"))
                {
                    Console.WriteLine("BrightData dataset is not ready yet -> Status: " + JSONResult?.status);

                    return false;
                }

            }
            if (JSONResult?.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
            {
                Console.WriteLine($"BrightData dataset available as JArray");

                JArray jArrayData = this.GetDataToProcess(JSONResult);

                this.PrepareData(jArrayData);   //Data is prepared based on type of DataHandler object (e.g. TIOBEDataHandler, AmazonDataHandler etc...)

                return true; //disable process

            }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
              
            }

            return false;


        }

        protected async Task SaveData()
        {
            Console.WriteLine("Saving data to our database");
            try
            {
                await scrapeCaptureDbContext.SaveChangesAsync();
                Console.WriteLine("Data successfully added to our database");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                
            }

        }
        protected async Task AddDirectiveToQueue(string stage)
        {
            try
            {
                var languageName = await GetNextLanguageToProcess();


                if (languageName != null)
                {
                    await InsertMessageIntoQueue(stage, languageName);
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }

        }
        protected async Task InsertMessageIntoQueue(string stage, string language)
        {
            try
            {
                string jsonQueueMessage = "";
                
                Console.WriteLine($"Next Language to process {language}");

                if (stage == "2")
                {
                    jsonQueueMessage = "{'Stage':'" + stage + "','Input':'" + language + "'}";
                }
                else if (stage == "3")
                {
                    jsonQueueMessage = "{'Stage':'" + stage + "','Input':{ 'query':'" + language + "','keywords':['code', 'programming']}}";
                }
                Console.WriteLine("Adding to queue this message: " + jsonQueueMessage);

                await queueClient.SendMessageAsync(jsonQueueMessage);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task<string> GetNextLanguageToProcess()
        {
            try
            {

                TIOBERankedLanguage? tIOBERankedLanguageData = await (from t in scrapeCaptureDbContext.TIOBERankedLanguages
                                                                      where t.Id == LanguageId
                                                                      select new TIOBERankedLanguage
                                                                      {
                                                                          LanguageName = t.LanguageName,
                                                                          RankOrder = t.RankOrder

                                                                      }).SingleOrDefaultAsync();

                Console.WriteLine($"Current Language {tIOBERankedLanguageData?.LanguageName}");



                if (tIOBERankedLanguageData != null)
                {
                    TIOBERankedLanguage? nextTIOBERankedLanguageData = await (from t in this.scrapeCaptureDbContext.TIOBERankedLanguages
                                                                              where t.RankOrder == (tIOBERankedLanguageData.RankOrder + 1)
                                                                              select new TIOBERankedLanguage
                                                                              {
                                                                                  LanguageName = t.LanguageName,
                                                                                  RankOrder = t.RankOrder

                                                                              }).SingleOrDefaultAsync();
                    return nextTIOBERankedLanguageData.LanguageName;
                }
            }
            catch (Exception)
            {

                throw;
            }

            return "";

        }
    }
}