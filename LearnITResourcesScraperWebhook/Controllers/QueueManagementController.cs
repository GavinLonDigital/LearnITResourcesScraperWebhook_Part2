using Azure.Storage.Queues;
using LearnITResourcesScraperWebhook.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace LearnITResourcesScraperWebhook.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueueManagementController : ControllerBase
    {
        private readonly QueueClient queueClient;

        public QueueManagementController(QueueClient queueClient)
        {
            this.queueClient = queueClient;
        }
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ProcessingDirective processingDirective)
        {
            try
            {
                var messageJSON = JsonConvert.SerializeObject(processingDirective);
                await queueClient.SendMessageAsync(messageJSON);

                Console.WriteLine($"Message sent to queue to start scraping: {messageJSON}");

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(StatusCodes.Status500InternalServerError);
            }


        }
    }
}
