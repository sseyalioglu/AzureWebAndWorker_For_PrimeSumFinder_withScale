using Microsoft.WindowsAzure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;


namespace PrimeSumPersistManager
{
    public class PersistPrimeSum
    {
        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ManagerSettings.StorageConnectionString);

        private const string TheTableName = "SumOfPrimeNumbers";
        private CloudTableClient tableClient;
        private TableServiceContext tableServiceContext;

        public PersistPrimeSum()
        {
            tableClient = storageAccount.CreateCloudTableClient();
            tableClient.CreateTableIfNotExist(TheTableName);
            tableServiceContext = tableClient.GetDataServiceContext();
            tableServiceContext.IgnoreResourceNotFoundException = true;
        }

        public SumOfPrimeNumbers GetCurrentSumOrPersistNew(int startNumber, int endNumber)
        {
            //check if this was requested earlier or not
            SumOfPrimeNumbers primeNumberSumRangeQuery =
                (from i in tableServiceContext.CreateQuery<SumOfPrimeNumbers>(TheTableName)
                 where
                     i.PartitionKey == startNumber.ToString() && i.RowKey == endNumber.ToString()
                 select i).FirstOrDefault();

            if (primeNumberSumRangeQuery == null)
            {

                var persistNew = new SumOfPrimeNumbers(startNumber, endNumber);
                tableServiceContext.AddObject(TheTableName, persistNew);
                tableServiceContext.SaveChangesWithRetries();
                return persistNew;
            }

            return primeNumberSumRangeQuery;
        }

        public void StoreLatestSum(SumOfPrimeNumbers theLatestSum)
        {
            tableServiceContext.UpdateObject(theLatestSum);
            tableServiceContext.SaveChangesWithRetries();
        }
    }


}
