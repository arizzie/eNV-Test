using Backend.Models;
using Data.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    public class ProcessVinOrchestration
    {
        private readonly ILogger<ProcessVinOrchestration> _logger;
        private const int BatchSize = 40; // Define your batch size here

        public ProcessVinOrchestration(ILogger<ProcessVinOrchestration> logger)
        {
            _logger = logger;
        }

        [Function(nameof(VinProcessingOrchestration))]
        public async Task<string> VinProcessingOrchestration(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            _logger.LogInformation($"Starting CSV processing orchestration for instance ID: {context.InstanceId}");

            OrchestratorInput input = context.GetInput<OrchestratorInput>();

            if (input == null || string.IsNullOrEmpty(input.Base64CsvContent) || string.IsNullOrEmpty(input.OriginalFileName))
            {
                _logger.LogError("Orchestration received empty or null Base64 CSV string from starter.");
                context.SetCustomStatus(new { progress = 0, message = "Orchestration failed: Empty Base64 CSV." });
                return "Orchestration failed: Missing or invalid Base64 CSV.";
            }

            // 1. Read and Parse CSV (Activity Function)
            _logger.LogInformation("Calling ReadCsvActivity to parse CSV data.");
            List<Vehicle> records = await context.CallActivityAsync<List<Vehicle>>(nameof(ProccessVinActivities.ReadCsvActivity), input.Base64CsvContent);

            if (records == null || !records.Any())
            {
                _logger.LogWarning($"No records found in CSV for orchestration ID: {context.InstanceId}");
                return "No records processed.";
            }

            // 2. Prepare input for the blob upload activity
            var blobUploadInput = new BlobUploadActivityInput
            {
                ContainerName = "processed-csv-output", // Choose a meaningful container name
                BlobName = $"processed-{input.OriginalFileName ?? "default"}-{context.InstanceId}.csv", // Generate a unique blob name
                FileContent = input.Base64CsvContent,
                ConnectionStringName = "AzureWebJobsStorage" // Use "AzureWebJobsStorage" or a custom connection string name
            };

            // 3. Call the new blob upload activity
            Uri uploadedBlobUri = await context.CallActivityAsync<Uri>(nameof(ProccessVinActivities.ArchiveOriginalCsvBlob), blobUploadInput);

            // 4. Update custom status (optional, but good for UI updates)
            context.SetCustomStatus(new
            {
                Status = "Completed",
                OutputUri = uploadedBlobUri.ToString(),
                Message = "CSV processing and upload complete."
            });

            _logger.LogInformation($"Successfully read {records.Count} records from CSV. Starting batch processing.");

            // 5. Chunk records into batches
            var batches = records.Chunk(BatchSize).ToList();
            int totalBatches = batches.Count;
            int completedBatches = 0;

            // Set initial status to 0%
            context.SetCustomStatus(new CustomStatus
            {
                Progress = 0,
                Message = $"Processing batches: {completedBatches} of {totalBatches} completed.",
                CompletedCount = completedBatches,
                TotalCount = totalBatches,
            });

            // 6. Process each batch concurrently
            var parallelBatchTasks = new List<Task<string>>();
            foreach (var batch in batches)
            {
                // Call an activity function for each batch
                parallelBatchTasks.Add(context.CallActivityAsync<string>(nameof(ProccessVinActivities.ProcessBatchActivity), batch.ToList()));
            }

            // Convert the list of tasks to a HashSet for efficient removal
            var tasksToMonitor = new HashSet<Task<string>>(parallelBatchTasks);

            // 7.Update Progress:
            // Use Task.WhenAny to process tasks as they complete
            while (tasksToMonitor.Any())
            {
                // Wait for any of the remaining tasks to complete
                Task<string> completedTask = await Task.WhenAny(tasksToMonitor);

                // Remove the completed task from the set
                tasksToMonitor.Remove(completedTask);

                // Increment completed count
                completedBatches++;

                // Calculate progress (as a double, then round)
                double progress = (double)completedBatches / totalBatches * 100;

                // Update the custom status of the orchestration
                context.SetCustomStatus(new CustomStatus
                {
                    Progress = Math.Round(progress, 0), // Round to nearest whole number
                    Message = $"Processing batches: {completedBatches} of {totalBatches} completed.",
                    CompletedCount = completedBatches,
                    TotalCount = totalBatches,
                });

                _logger.LogInformation($"Orchestration ID: {context.InstanceId} - Progress: {Math.Round(progress, 0)}%");
            }

            // 8. Final status update
            _logger.LogInformation($"All {totalBatches} batches processed. CSV processing completed for {records.Count} records.");
            context.SetCustomStatus(
                new CustomStatus
                {
                    Progress = 100, // Round to nearest whole number
                    Message = $"CSV processing completed successfully!",
                    CompletedCount = completedBatches,
                    TotalCount = totalBatches
                });

            return $"CSV processing completed for {records.Count} records in {batches.Count} batches.";
        }
        private class CustomStatus
        {
            public double Progress { get; set; }
            public string Message { get; set; }
            public int CompletedCount { get; set; }
            public int TotalCount { get; set; }
        }
    }

}