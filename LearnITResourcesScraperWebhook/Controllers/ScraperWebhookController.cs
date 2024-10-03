using LearnITResourcesScraperWebhook.Data;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;
using LearnITResourcesScraperWebhook.Extensions;
using Newtonsoft.Json;
using static LearnITResourcesScraperWebhook.Program;
using LearnITResourcesScraperWebhook.BackgroundServices;
using Azure.Storage.Queues;

namespace LearnITResourcesScraperWebhook.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScraperWebhookController : ControllerBase
    {
        private readonly FetchDataService fetchDataService;
        private readonly QueueClient queueClient;

        public ScraperWebhookController(FetchDataService fetchDataService, QueueClient queueClient)
        {
            this.fetchDataService = fetchDataService;
            this.queueClient = queueClient;
        }

        [HttpPost()]
        public async Task<IActionResult> Post()
        {
            try
            {
                var requestBody = await Request.Body.ReadAsStringAsync();

                dynamic dynamicObject = JsonConvert.DeserializeObject(requestBody);

                Console.WriteLine($"Raw data sent from BrightData to Webhook: {dynamicObject}");

                if (dynamicObject != null)
                {
                    var fetchDatasetId = (string)dynamicObject.id;

                    if (dynamicObject["collector_id"] == "c_lv3l43x61hnemlo6b5") //TIOBE
                    {
                        Console.WriteLine("Webhook called from BrightData with TIOBE Results");

                        fetchDataService.InitializeTimerCode(fetchDatasetId, ScraperType.TIOBE, this.queueClient);
                    }
                    else if (dynamicObject["collector_id"] == "c_lty5ig4yjkcqtf2en") //Amazon
                    {
                        Console.WriteLine("Webhook called from BrightData with Amazon Results");

                        fetchDataService.InitializeTimerCode(fetchDatasetId, ScraperType.Amazon, this.queueClient);
                    }
                    else if (dynamicObject["collector_id"] == "c_ltygmbmm1405s7q7le") // YouTube
                    {
                        Console.WriteLine("Webhook called from BrightData with YouTube Results");

                        fetchDataService.InitializeTimerCode(fetchDatasetId, ScraperType.YouTube, this.queueClient);

                    }
                }
                else
                {
                    Console.WriteLine("Null was returned when attempting to deserialize JSON data sent to our Webhook from BrightData");                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
         
 

            return Ok();
        }
    }
}
