using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TraceToAzureTableStorage
{
    public class CustomTraceListener : TraceListener
    {
        public CustomTraceListener(string storageConnectionString, string applicationName)
        {
            CustomTraceSettings.StorageConnectionString = storageConnectionString;
            CustomTraceSettings.ApplicationName = applicationName;
        }

        public override void Write(string message)
        {
            WriteLine(message);
        }

        public override void WriteLine(string message)
        {
            var traceContext = new CustomTraceContext();
            var traceToWrite = new CustomTraceObject()
                                   {
                                       PartitionKey = CustomTraceSettings.ApplicationName,
                                       RowKey = DateTime.Now.ToString("yyyyMMdd hh:mm:ss tt - ") + DateTime.Now.Ticks.ToString(),
                                       Message = message
                                   };
            traceContext.AddObject("Traces",traceToWrite);
            traceContext.SaveChanges();
        }
    }
}
