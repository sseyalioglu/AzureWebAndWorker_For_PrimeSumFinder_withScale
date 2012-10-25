using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TraceToAzureTableStorage
{
    public class CustomTraceObject : TableServiceEntity
    {
        public string Message { get; set; }
    }
}
