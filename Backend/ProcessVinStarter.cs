using Azure.Storage.Blobs;
using Backend.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.IO;
using System.Net;
using System.Text.Json;
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


        [OpenApiOperation(operationId: "StartVinCsvProcessing", tags: new[] { "VIN Processing" }, Summary = "Starts a Durable Function orchestration to process a VIN CSV file from Blob Storage.", Description = "This endpoint initiates a long-running workflow to download a specified CSV file from Azure Blob Storage, and then process its VIN data.")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ImportInput), Description = "Details of the CSV file to import. Specifies the container name and blob filename.", Required = false)] // Changed to Required = false as it defaults to hardcoded values
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Accepted, contentType: "application/json", bodyType: typeof(DurableOrchestrationStatusResponse), Description = "Orchestration instance started successfully. Returns status check URLs.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "Invalid request body format (JSON deserialization failure).")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Description = "The specified Blob file was not found in storage.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "An unexpected error occurred during processing or blob download.")]
        //[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
        // --- End OpenAPI Annotations ---

        [Function(nameof(StartVinCsvProcessing))]
        public async Task<HttpResponseData> StartVinCsvProcessing(
      [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
      [DurableClient] DurableTaskClient client)
        {
            _logger.LogInformation("HTTP trigger received VIN CSV processing request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            ImportInput starterInput = null;
            try
            {
                // PropertyNameCaseInsensitive = true handles "ContainerName" or "blobName" in JSON
                starterInput = JsonSerializer.Deserialize<ImportInput>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Failed to deserialize request body so using default: {ex.Message}");
            }


            // --- HARDCODED BLOB DETAILS ---
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string containerName = string.IsNullOrEmpty(starterInput?.ContainerName) ? "evntest" : starterInput.ContainerName;          // Your hardcoded container name
            string blobName = string.IsNullOrEmpty(starterInput?.Filename) ? "sample-vin-data.csv" : starterInput.Filename;   // Your hardcoded blob name
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

            // 2. Create the orchestration input object
            var input = new OrchestratorInput
            {
                Base64CsvContent = base64Csv,
                OriginalFileName = blobName // Use the original file name for tracking
            };

            // 3. Schedule the orchestration instance
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(ProcessVinOrchestration.VinProcessingOrchestration), input);

            _logger.LogInformation($"Started VIN processing orchestration with ID = '{instanceId}', passing {csvBytes.Length} bytes.");

            // 4. Return the standard Durable Functions status response.
            return await client.CreateCheckStatusResponseAsync(req, instanceId);

        }
    }
}