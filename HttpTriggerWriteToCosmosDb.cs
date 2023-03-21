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
                    "databaseName": "databaseName",
                    "collectionName": "collectionName"
                }
            */

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            if (data == null)
            {
                // for local env, allow a 'get'
                data = new System.Dynamic.ExpandoObject();
                data.databaseName = Environment.GetEnvironmentVariable("COSMOSDB_DATABASE_NAME");
                data.collectionName = Environment.GetEnvironmentVariable("COSMOSDB_COLLECTION_NAME");
            }

            //ensure id is unique
            data.id = Guid.NewGuid().ToString();
            data.databaseName = "databaseName";
            data.collectionName = "collectionName";

            var connectionString = System.Environment.GetEnvironmentVariable("COSMOSDB_CONNECTION");

            try
            {
                using (CosmosClient client = new CosmosClient(connectionString))
                {
                    var database = client.GetDatabase(data.databaseName);
                    var container = database.GetContainer(data.collectionName);

                    var response = await container.CreateItemAsync(data, new PartitionKey(data.id));
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
