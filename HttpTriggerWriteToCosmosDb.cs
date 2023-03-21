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

            /*
                    {
                      "databaseName": "databaseName",
                      "collectionName": "collectionName"
                    }
            */

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            data.id = Guid.NewGuid();

            var connectionString = System.Environment.GetEnvironmentVariable("COSMOSDB_CONNECTION");

            using (CosmosClient client = new CosmosClient(connectionString))
            {
                var database = client.GetDatabase(data.databaseName);
                var container = database.GetContainer(data.collectionName);

                var response = await container.CreateItemAsync(data, new PartitionKey(data.id));
            }

            string responseMessage = $"{data.id}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
