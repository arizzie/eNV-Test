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
        private const int BatchSize = 100; // Define your batch size here

        public ProcessVinOrchestration(ILogger<ProcessVinOrchestration> logger)
        {
            _logger = logger;
        }

        [Function(nameof(VinProcessingOrchestration))]
        public async Task<string> VinProcessingOrchestration(
            [OrchestrationTrigger] TaskOrchestrationContext context,
            string base64Csv)
        {
            _logger.LogInformation($"Starting CSV processing orchestration for instance ID: {context.InstanceId}");

            if (string.IsNullOrEmpty(base64Csv))
            {
                _logger.LogError("Orchestration received empty or null Base64 CSV string from starter.");
                context.SetCustomStatus(new { progress = 0, message = "Orchestration failed: Empty Base64 CSV." });
                return "Orchestration failed: Missing or invalid Base64 CSV.";
            }

            // 1. Read and Parse CSV (Activity Function)
            _logger.LogInformation("Calling ReadCsvActivity to parse CSV data.");
            List<Vehicle> records = await context.CallActivityAsync<List<Vehicle>>(nameof(ProccessVinActivities.ReadCsvActivity), base64Csv);

            if (records == null || !records.Any())
            {
                _logger.LogWarning($"No records found in CSV for orchestration ID: {context.InstanceId}");
                return "No records processed.";
            }

            _logger.LogInformation($"Successfully read {records.Count} records from CSV. Starting batch processing.");

            // 2. Chunk records into batches
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

            // 3. Process each batch concurrently
            var parallelBatchTasks = new List<Task<string>>();
            foreach (var batch in batches)
            {
                // Call an activity function for each batch
                parallelBatchTasks.Add(context.CallActivityAsync<string>(nameof(ProccessVinActivities.ProcessBatchActivity), batch.ToList()));
            }

            // Convert the list of tasks to a HashSet for efficient removal
            var tasksToMonitor = new HashSet<Task<string>>(parallelBatchTasks);

            // 4.Update Progress:
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

            // 5. Final status update
            _logger.LogInformation($"All {totalBatches} batches processed. CSV processing completed for {records.Count} records.");
            context.SetCustomStatus(new { progress = 100, message = "CSV processing completed." });

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