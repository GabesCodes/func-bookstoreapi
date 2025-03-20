using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System;
using Microsoft.Azure.Cosmos;
using System.Web;
using Azure.Messaging.EventGrid;
using static System.Net.WebRequestMethods;
using Azure;

namespace func_bookstoreapi
{
    public class AddBookFunction
    {
        private readonly ILogger<AddBookFunction> _logger;
        public Book book;

        private static readonly string DatabaseId = "BookstoreDB";
        private static readonly string ContainerId = "Books";
        private static readonly string EndpointUri = Environment.GetEnvironmentVariable("CosmosDBEndpoint");
        private static readonly string PrimaryKey = Environment.GetEnvironmentVariable("CosmosDBKey");

        private static CosmosClient cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
        private static Container container = cosmosClient.GetContainer(DatabaseId, ContainerId);

        private static readonly string EventGridTopicPrimarykey = Environment.GetEnvironmentVariable("EventGridTopicKey");
        private static readonly string EventGridTopicUri = Environment.GetEnvironmentVariable("EventGridTopicEndpoint");
        private static EventGridPublisherClient eventGridPublisherClient = new EventGridPublisherClient(
            new Uri(EventGridTopicUri), 
            new AzureKeyCredential(EventGridTopicPrimarykey)
            
            );

        public AddBookFunction(ILogger<AddBookFunction> logger)
        {
            _logger = logger; //just creating a way to log events for the function
        }
        

        [Function("AddBookFunc")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "books")] HttpRequestData req,
            FunctionContext context)
        {

            var logger = context.GetLogger("AddBookFunc");
            logger.LogInformation("Received a request to add a new book.");
            
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync(); //reads the raw json request i send with book info
            if (string.IsNullOrEmpty(requestBody))
            {
                var badRequest = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                logger.LogInformation("Request body is empty.");
                return badRequest;
            }
            else
            {
                logger.LogInformation("Receiving JSON request:" + requestBody );
                
                var book = JsonConvert.DeserializeObject<Book>(requestBody); //converts JSON into a book object
                book.timestamp = DateTime.UtcNow;

                logger.LogInformation("Recieved JSON for:" + book.title);
                book.id = Guid.NewGuid().ToString(); //assign ID?
                await container.CreateItemAsync(book, new PartitionKey(book.genre));

                var eventGridEvent = new EventGridEvent(
                    subject: $"New Book Added: {book.title}",
                    eventType: "BookAdded",
                    dataVersion: "1.0",
                    data: new{
                        Id = book.id,
                        Title = book.title,
                        Author = book.author,
                        Price = book.price,
                        Timestamp = book.timestamp
                    }
                );

                logger.LogInformation("Event Data: " + JsonConvert.SerializeObject(eventGridEvent));

                try{
                    await eventGridPublisherClient.SendEventAsync(eventGridEvent);
                    logger.LogInformation("Successfully sent event for book: " + book.title);
                }
                catch (Exception ex)
                {
                    logger.LogError("Failed to send event grid event: " + ex.Message);
                }

                return req.CreateResponse(HttpStatusCode.OK);
            }
            
        }
    }
}
