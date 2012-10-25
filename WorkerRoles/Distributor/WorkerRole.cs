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
using PrimeSumPersistManager;
using TraceToAzureTableStorage;

namespace Distributor
{
    public class WorkerRole : RoleEntryPoint
    {
        // The name of your queue
        const string QueueToMonitor = "Distributor";
        const string QueueToAssign = "PrimeFinder";

        // QueueClient is thread-safe. Recommended that you cache 
        // rather than recreating it on every request
        QueueClient _clientToMonitor, _clientToAssign;
        bool _isStopped;

        public override void Run()
        {
            Trace.WriteLine("Worker Role Started Running Now");
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
                        Trace.WriteLine("Processing " + receivedMessage.SequenceNumber.ToString());
                        var theRequest = receivedMessage.GetBody<PrimeSumRequest>();
                        Trace.WriteLine("Received request to sum up promes between " + theRequest.StartNumber + " and " + theRequest.EndNumber);

                        //start persistance in table storage for aggregator
                        var persistRequest = new PersistPrimeSum();
                        var sumOfPrimesNew = persistRequest.GetCurrentSumOrPersistNew(theRequest.StartNumber,
                                                                                      theRequest.EndNumber);

                        for (int numberToAssign = theRequest.StartNumber; numberToAssign <= theRequest.EndNumber; numberToAssign++)
                        {
                            var findRequest = new PrimeFindRequest() { OriginalRequest = theRequest, TheNumberToCheck = numberToAssign };
                            _clientToAssign.Send(new BrokeredMessage(findRequest));
                            Trace.WriteLine("Assigned " + numberToAssign + " to be processed by Primer Number Finders");
                        }
                        receivedMessage.Complete();
                    }

                    Trace.WriteLine(">> No message waiting");
                }
                catch (MessagingException e)
                {
                    if (!e.IsTransient)
                    {
                        Debug.WriteLine("Failed during processing with error: " + e.ToString());
                        throw;
                    }

                    Thread.Sleep(5000);
                }
                catch (OperationCanceledException)
                {
                    if (!_isStopped)
                    {
                        Debug.WriteLine("Cancelled during processing");
                        throw;
                    }
                }
            }
        }

        public override bool OnStart()
        {
            //send the traces to table Storage
            var storageConnectionString =
                CloudConfigurationManager.GetSetting("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString");
            var tlistener = new CustomTraceListener(storageConnectionString, "Distributor");
            Trace.Listeners.Add(tlistener);

            //persistance storage settings also
            ManagerSettings.StorageConnectionString = storageConnectionString;

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
