
using Azure.Storage.Queues;
using LearnITResourcesScraperWebhook.Data;
using LearnITResourcesScraperWebhook.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace LearnITResourcesScraperWebhook.DataHandlers
{
    public class YouTubeDataHandler : DataHandler
    {

        IList<YouTubeChannel>? youtubeChannelList;
        private const string stage = "3";


        public YouTubeDataHandler(dynamic JSONResult, ScrapeCaptureDbContext scrapeCaptureDbContext, QueueClient queueClient):base((object)JSONResult,scrapeCaptureDbContext, queueClient)
        {

        }
        public async override Task<bool> AddData()
        {
            Console.WriteLine($"Adding YouTube data to our database - LanguageId: {LanguageId}");
            try
            {
                if (this.youtubeChannelList?.Count > 0)
                {
                    await this.scrapeCaptureDbContext.AddRangeAsync(this.youtubeChannelList);
                    await SaveData();

                    await AddDirectiveToQueue(stage);

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return false;
        }

        public override JArray GetDataToProcess(dynamic data)
        {
           return (JArray)data;
        }

        public override void PrepareData(JArray YTChannelsJArray)
        {
            Console.WriteLine("Preparing YouTube data for our database");

            try
            {
                SetLanguageId(YTChannelsJArray);

                IList<YouTubeChannel> channelList = YTChannelsJArray.Select(b => new YouTubeChannel
                {
                    ChannelName = (string)b["channel"]!,
                    Url = (string)b["url"]!,
                    Subscribers = (string)b["subscribers"]!,
                    Input = (string)b["query"]!,
                    LanguageId = LanguageId
                }).ToList();

                this.youtubeChannelList = channelList;
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }


        }

        public override async Task RemoveData()
        {
            try
            {
                await scrapeCaptureDbContext.Database.ExecuteSqlRawAsync($"DELETE FROM YouTubeChannels WHERE LanguageId ={LanguageId}");
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
        }
        private void SetLanguageId(JArray data)
        {
            try
            {
                string input = (string)data[1]["query"];
                LanguageId = this.scrapeCaptureDbContext.TIOBERankedLanguages.FirstOrDefault(d => d.LanguageName == input).Id;

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }

        }

    }
}
