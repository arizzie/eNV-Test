using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Backend
{
    public class ProcessVinStarter
    {
        private readonly ILogger<ProcessVinStarter> _logger;

        public ProcessVinStarter(ILogger<ProcessVinStarter> logger)
        {
            _logger = logger;
        }

        [Function(nameof(StartVinCsvProcessing))]
        public async Task<HttpResponseData> StartVinCsvProcessing(
      [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
      [DurableClient] DurableTaskClient client)
        {
            _logger.LogInformation("HTTP trigger received VIN CSV processing request.");

            using (var dummyMs = new MemoryStream())
            {
                await req.Body.CopyToAsync(dummyMs);
            }

            // --- HARDCODED BLOB DETAILS ---
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string containerName = "evntest";          // Your hardcoded container name
            string blobName = "sample-vin-data.csv";   // Your hardcoded blob name
            _logger.LogInformation($"Simulating file upload by reading Blob: Container='{containerName}', Blob='{blobName}'");
            // --- END HARDCODED BLOB DETAILS ---

            byte[] csvBytes;
            try
            {
                // 1. Download the CSV from Blob Storage directly in the starter
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                {
                    _logger.LogError($"Simulated Blob '{blobName}' not found in container '{containerName}'.");
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    notFoundResponse.WriteString($"Simulated file '{blobName}' not found.");
                    return notFoundResponse;
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    await blobClient.DownloadToAsync(ms); // Asynchronously download to memory
                    csvBytes = ms.ToArray();
                    _logger.LogInformation($"Successfully downloaded simulated blob '{blobName}'. Size: {csvBytes.Length} bytes.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading simulated blob '{blobName}' in HTTP starter: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.WriteString($"Failed to download simulated file: {ex.Message}");
                return errorResponse;
            }


            string base64Csv = Convert.ToBase64String(csvBytes);

            // 2. Schedule the orchestration instance
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(ProcessVinOrchestration.VinProcessingOrchestration), csvBytes);

            _logger.LogInformation($"Started VIN processing orchestration with ID = '{instanceId}', passing {csvBytes.Length} bytes.");

            // 3. Return the standard Durable Functions status response.
            return await client.CreateCheckStatusResponseAsync(req, instanceId);

        }
    }
}