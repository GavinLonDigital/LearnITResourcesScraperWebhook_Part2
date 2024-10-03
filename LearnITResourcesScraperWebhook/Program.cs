
using LearnITResourcesScraperWebhook.Data;
using Microsoft.EntityFrameworkCore;
using LearnITResourcesScraperWebhook.Controllers;
using LearnITResourcesScraperWebhook.BackgroundServices;
using Microsoft.Extensions.Azure;
using Azure.Storage.Queues;
using Azure.Identity;

namespace LearnITResourcesScraperWebhook
{
    public class Program
    {
        public enum ScraperType
        {
            TIOBE,
            Amazon,
            YouTube
        }
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var connectionString = builder.Configuration.GetConnectionString("ScrapeCaptureDbContext");

            builder.Services.AddDbContext<ScrapeCaptureDbContext>(options => options.UseSqlServer(connectionString));
            builder.Services.AddSingleton<FetchDataService>();
            builder.Services.AddHostedService(provider => provider.GetRequiredService<FetchDataService>());


            builder.Services.AddHttpClient();

            builder.Services.AddAzureClients(build =>
                 build.AddClient<QueueClient, QueueClientOptions>((options, _, _) =>
                 {
                     options.MessageEncoding = QueueMessageEncoding.Base64;
                     var credential = new DefaultAzureCredential();
                     //      var queueUri = new Uri("https://tempscraper.queue.core.windows.net/scraperdata");
                     var queueConnectionString = builder.Configuration.GetConnectionString("QueueConnection");
                     var connectionString = queueConnectionString;
                     var queueName = builder.Configuration.GetSection("FunctionQueue")["QueueName"]; 

                     Console.WriteLine($"Queue name: {queueName}");
                     Console.WriteLine($"Queue ConnectionSTring: {queueConnectionString}");
                     
                     return new QueueClient(connectionString, queueName, options);
                 })
            );


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();


            app.Run();
        }
    }
}
