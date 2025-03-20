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

        private static readonly Azure.AzureKeyCredential EventGridTopicPrimarykey = Environment.GetEnvironmentVariable("EventGridTopicKey");

        private static EventGridPublisherClient eventGridPublisherClient = new Uri("https://bookstoreevents.westus2-1.eventgrid.azure.net/api/events", EventGridTopicPrimarykey)



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
                logger.LogInformation("Recieved JSON for:" + book.title);
                book.id = Guid.NewGuid().ToString(); //assign ID?
                await container.CreateItemAsync(book, new PartitionKey(book.genre));
                return req.CreateResponse(HttpStatusCode.OK);
            }
        }
    }
}
