using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;

namespace azure_functions
{
    public static class HttpTriggerWriteToCosmosDb
    {
        [FunctionName("HttpTriggerWriteToCosmosDb")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string databaseName = "databaseName";
            string collectionName = "collectionName";

            dynamic foo = new JObject();
            foo.id = Guid.NewGuid();
            foo.name = name;

            var connectionString = System.Environment.GetEnvironmentVariable("COSMOSDB_CONNECTION");

            using (CosmosClient client = new CosmosClient(connectionString))
            {
                var database = client.GetDatabase(databaseName);
                var container = database.GetContainer(collectionName);

                var response = await container.CreateItemAsync(foo, new PartitionKey(foo.id));
            }

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
