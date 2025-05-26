using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Models
{
    public class BlobUploadActivityInput
    {
        public string ContainerName { get; set; }
        public string BlobName { get; set; }
        public string FileContent { get; set; } // The processed CSV data as a string
        public string ConnectionStringName { get; set; } // e.g., "AzureWebJobsStorage" or "YourCustomStorageConnection"
    }
}
