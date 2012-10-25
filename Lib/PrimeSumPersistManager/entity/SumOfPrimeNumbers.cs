using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace PrimeSumPersistManager
{
    public class SumOfPrimeNumbers : TableServiceEntity
    {
        public SumOfPrimeNumbers()
        {
        }

        public SumOfPrimeNumbers(int startNumber, int endNumber)
        {
            this.PartitionKey = startNumber.ToString();
            this.RowKey = endNumber.ToString();
            this.Sum = "0";
            this.IsFinished = false.ToString();
        }

        public string Sum { get; set; }
        public string IsFinished { get; set; }
    }
}
