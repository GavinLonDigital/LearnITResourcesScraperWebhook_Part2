
using Azure.Storage.Queues;
using LearnITResourcesScraperWebhook.Data;
using LearnITResourcesScraperWebhook.DataHandlers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics.Metrics;
using System.Net.Http.Headers;
using static LearnITResourcesScraperWebhook.Program;
using static System.Net.WebRequestMethods;

namespace LearnITResourcesScraperWebhook.BackgroundServices
{
    public class FetchDataService : BackgroundService
    {
        private const string BrightDataSetBaseURI = "https://api.brightdata.com/dca/dataset";
        private ScraperType scraperType = ScraperType.TIOBE;
        private bool IsEnabled { get; set; }
        private dynamic? JSONResult{ get; set; }

        private readonly PeriodicTimer periodicTimer = new(TimeSpan.FromMilliseconds(5000));
        private readonly IServiceScopeFactory? factory;
        private readonly IServiceProvider? provider;

        private string fetchDatasetId;

        private QueueClient queueClient;

        private int counter = 0;
        public FetchDataService(IServiceScopeFactory factory)
        {
            IsEnabled = false;
            this.factory = factory;
        }

        public void InitializeTimerCode(string fetchDatasetId, ScraperType scraperType, QueueClient queueClient)
        {
            this.fetchDatasetId = fetchDatasetId;
            this.scraperType = scraperType;
            this.queueClient = queueClient;
            
            IsEnabled = true;

            Console.WriteLine($"Background Timer activated for ScraperType {scraperType}");

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (await periodicTimer.WaitForNextTickAsync(stoppingToken)
                            && !stoppingToken.IsCancellationRequested)
                {
                    if (IsEnabled)
                    {
                        this.counter++;
                        Console.WriteLine($"Trying to fetch data from BrightData - try - {counter}");
                        await ProcessDataSetAsync();

                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

        }
        private async Task ProcessDataSetAsync()
        {
            try
            {
                DataHandler dataHandler = null!;

                await using AsyncServiceScope asyncScope = this.factory.CreateAsyncScope();

                ScrapeCaptureDbContext scrapeCaptureDbContext = asyncScope.ServiceProvider.GetRequiredService<ScrapeCaptureDbContext>();

                HttpClient httpClient = asyncScope.ServiceProvider.GetRequiredService<HttpClient>();

                dynamic dynamicJSONData = await GetDataSetFromBrightData(httpClient);

                if (this.scraperType == ScraperType.TIOBE)
                {
                    dataHandler = new TIOBEDataHandler(dynamicJSONData, scrapeCaptureDbContext, queueClient);
                }
                else if (this.scraperType == ScraperType.Amazon)
                {
                    dataHandler = new AmazonDataHandler(dynamicJSONData, scrapeCaptureDbContext, queueClient);

                }
                else if (this.scraperType == ScraperType.YouTube)
                {
                    dataHandler = new YouTubeDataHandler(dynamicJSONData, scrapeCaptureDbContext, queueClient);
                }

                if (dataHandler != null)
                {
                   bool dataPrepped = dataHandler.PrepData();
                    if (dataPrepped)
                    {
                        await dataHandler.RemoveData(); //initialize database

                        bool dataAdded = await dataHandler.AddData();

                        IsEnabled = !dataAdded; // stops the service - which will be activated again by BrightData callback
                    }
                }
                else
                {
                    Console.WriteLine("Invalid Scraper Type");

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
 

        }
        private void PrepareHttpClientHeader(HttpClient httpClient)
        {
            try
            {

                httpClient.DefaultRequestHeaders.Authorization =
                 new AuthenticationHeaderValue("Bearer", "22f606a9-94ec-4aeb-a624-151a97519057");
                // new AuthenticationHeaderValue("Bearer", "Your Bright Data Token Goes Here"); //Use secret to store this information //My note: this line goes into GitHub instead of the line above

                httpClient.DefaultRequestHeaders
                    .Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                Console.WriteLine("Client HTTP Header prepared");
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task<dynamic> GetDataSetFromBrightData(HttpClient httpClient)
        {
            try
            {
                PrepareHttpClientHeader(httpClient);

                var requestUri = $"{BrightDataSetBaseURI}?id={this.fetchDatasetId}";

                Console.WriteLine("Attempting to request dataset from BrightData");

                var response = await httpClient.GetAsync(requestUri);

                var statusCode = response.StatusCode;

                Console.WriteLine($"HTTP Request for dataset from BrightData Status Code: {statusCode} ");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    dynamic jsonObj = JsonConvert.DeserializeObject<dynamic>(json)!;

                    Console.WriteLine($"Dataset returned from BrightData -  raw data: {jsonObj}");

                    return jsonObj!;
                }
                else
                {
                    Console.WriteLine("Request to BrightData for dataset failed.");
                }
            }
            catch (Exception)
            {

                throw;
            }
            return null!;

        }

    }
}
