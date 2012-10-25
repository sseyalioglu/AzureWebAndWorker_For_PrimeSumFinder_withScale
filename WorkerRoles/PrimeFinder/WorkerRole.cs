using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using DemoEntities;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using TraceToAzureTableStorage;

namespace PrimeFinder
{
    public class WorkerRole : RoleEntryPoint
    {
        // The name of your queue
        const string QueueToMonitor = "PrimeFinder";
        const string QueueToAssign = "Aggregator";

        // QueueClient is thread-safe. Recommended that you cache 
        // rather than recreating it on every request
        QueueClient _clientToMonitor, _clientToAssign;
        bool _isStopped;

        public override void Run()
        {
            while (!_isStopped)
            {
                try
                {
                    // Receive the message
                    BrokeredMessage receivedMessage = null;
                    receivedMessage = _clientToMonitor.Receive();

                    if (receivedMessage != null)
                    {
                        // Process the message
                        Trace.WriteLine("Processing", receivedMessage.SequenceNumber.ToString());

                        var receivedValue = receivedMessage.GetBody<PrimeFindRequest>();
                        IfPrimeThenQueueToAggregator(receivedValue);

                        receivedMessage.Complete();
                    }
                }
                catch (MessagingException e)
                {
                    if (!e.IsTransient)
                    {
                        Trace.WriteLine(e.Message);
                        throw;
                    }

                    Thread.Sleep(10000);
                }
                catch (OperationCanceledException e)
                {
                    if (!_isStopped)
                    {
                        Trace.WriteLine(e.Message);
                        throw;
                    }
                }
            }
        }

        private void IfPrimeThenQueueToAggregator(PrimeFindRequest theFindRequest)
        {
            if (theFindRequest.TheNumberToCheck.IsPrime())
            {
                Trace.WriteLine(theFindRequest.TheNumberToCheck + " is a primne number");

                var thePrimeToAggregateRequest = new PrimeAggregateRequest()
                {
                    OriginalRequest = theFindRequest.OriginalRequest,
                    ThePrimeToAggregate = theFindRequest.TheNumberToCheck
                };
                _clientToAssign.Send(new BrokeredMessage(thePrimeToAggregateRequest));
            }
        }



        public override bool OnStart()
        {
            //send the traces to table Storage
            var storageConnectionString =
                CloudConfigurationManager.GetSetting("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString");
            var tlistener = new CustomTraceListener(storageConnectionString, "PrimeFinder");
            Trace.Listeners.Add(tlistener);

            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Create the queue if it does not exist already
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            if (!namespaceManager.QueueExists(QueueToMonitor))
            {
                namespaceManager.CreateQueue(QueueToMonitor);
            }

            if (!namespaceManager.QueueExists(QueueToAssign))
            {
                namespaceManager.CreateQueue(QueueToAssign);
            }

            // Initialize the connection to Service Bus Queue
            _clientToMonitor = QueueClient.CreateFromConnectionString(connectionString, QueueToMonitor);
            _clientToAssign = QueueClient.CreateFromConnectionString(connectionString, QueueToAssign);
            _isStopped = false;
            return base.OnStart();
        }

        public override void OnStop()
        {
            // Close the connection to Service Bus Queue
            _isStopped = true;
            _clientToMonitor.Close();
            _clientToAssign.Close();
            base.OnStop();
        }
    }
}
