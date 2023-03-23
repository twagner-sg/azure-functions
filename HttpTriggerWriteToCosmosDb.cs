using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace azure_functions
{
    public class CosmosDbEntity
    {
        public string pk { get; set; }
        public string id { get; set; }
        public string databaseName { get; set; }
        public string collectionName { get; set; }
        public string message { get; set; }
        public string errorMessage { get; set; }
    }

    public static class HttpTriggerWriteToCosmosDb
    {
        [FunctionName("HttpTriggerWriteToCosmosDb")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            /*
             
                http://localhost:7156/api/HttpTriggerWriteToCosmosDb

                {
                    "id": "uniqueValue",
                    "databaseName": "databaseName",
                    "collectionName": "collectionName",
                    "source": "localhost-postman"
                }
            */

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<CosmosDbEntity>(requestBody);

            data.id = data?.id ?? Guid.NewGuid().ToString();
            data.pk = data?.pk ?? Guid.NewGuid().ToString();

            var connectionString = System.Environment.GetEnvironmentVariable("COSMOSDB_CONNECTION");

            try
            {
                using (CosmosClient client = new CosmosClient(connectionString))
                {
                    var database = client.GetDatabase(data.databaseName);
                    var container = database.GetContainer(data.collectionName);

                    var response = await container.CreateItemAsync(data);
                }
            }
            catch (Exception ex)
            {
                data.errorMessage = ex.Message;
            }

            string responseMessage = JsonConvert.SerializeObject(data);

            return new OkObjectResult(responseMessage);
        }
    }
}
