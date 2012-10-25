using System.Data.Services.Client;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TraceToAzureTableStorage
{
    public class CustomTraceContext : TableServiceContext
    {
        private static CloudStorageAccount storageAccount;

        static CustomTraceContext()
        {
            storageAccount = CloudStorageAccount.Parse(CustomTraceSettings.StorageConnectionString);
        }

        public CustomTraceContext () : base (storageAccount.TableEndpoint.AbsoluteUri, storageAccount.Credentials)
        {
            var tableClient = storageAccount.CreateCloudTableClient();
            tableClient.CreateTableIfNotExist("Traces");
        }

        public DataServiceQuery<CustomTraceObject> Traces
        {
            get { return CreateQuery<CustomTraceObject>("Traces"); }
        }
    }
}
