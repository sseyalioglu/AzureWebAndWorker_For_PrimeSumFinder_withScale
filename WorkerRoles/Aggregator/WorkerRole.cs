using DemoEntities;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using PrimeSumPersistManager;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using TraceToAzureTableStorage;

namespace Aggregator
{
    public class WorkerRole : RoleEntryPoint
    {
        // The name of your queue
        const string QueueName = "Aggregator";
        private string _storageConnectionString;
        // QueueClient is thread-safe. Recommended that you cache 
        // rather than recreating it on every request
        QueueClient _client;
        bool _isStopped;

        public override void Run()
        {
            //grab the value from table Storage
            var storageAccount = CloudStorageAccount.Parse(_storageConnectionString);
            var storageClient = storageAccount.CreateCloudTableClient();
            storageClient.CreateTableIfNotExist("SumOfPrimeNumbers");

            while (!_isStopped)
            {
                try
                {
                    // Receive the message
                    BrokeredMessage receivedMessage = null;
                    receivedMessage = _client.Receive();

                    if (receivedMessage != null)
                    {
                        // Process the message
                        Trace.WriteLine("Processing", receivedMessage.SequenceNumber.ToString());

                        var thePrimeNumberToSum = receivedMessage.GetBody<PrimeAggregateRequest>();

                        //start persistance
                        var persistRequest = new PersistPrimeSum();
                        var sumOfPrimesLatest = persistRequest.GetCurrentSumOrPersistNew(thePrimeNumberToSum.OriginalRequest.StartNumber,
                                                                                      thePrimeNumberToSum.OriginalRequest.EndNumber);
                        sumOfPrimesLatest.Sum = (thePrimeNumberToSum.ThePrimeToAggregate + int.Parse(sumOfPrimesLatest.Sum)).ToString();

                        persistRequest.StoreLatestSum(sumOfPrimesLatest);

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

        public override bool OnStart()
        {
            //send the traces to table Storage
            var storageConnectionString =
                CloudConfigurationManager.GetSetting("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString");
            var tlistener = new CustomTraceListener(storageConnectionString, "Aggregator");
            Trace.Listeners.Add(tlistener);

            //persistance storage settings also
            ManagerSettings.StorageConnectionString = storageConnectionString;

            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Create the queue if it does not exist already
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            if (!namespaceManager.QueueExists(QueueName))
            {
                namespaceManager.CreateQueue(QueueName);
            }

            // Initialize the connection to Service Bus Queue
            _client = QueueClient.CreateFromConnectionString(connectionString, QueueName);
            _isStopped = false;

            _storageConnectionString = CloudConfigurationManager.GetSetting("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString");
            return base.OnStart();
        }

        public override void OnStop()
        {
            // Close the connection to Service Bus Queue
            _isStopped = true;
            _client.Close();
            base.OnStop();
        }
    }
}
