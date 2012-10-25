using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DemoEntities
{
    public class PrimeAggregateRequest
    {
        public PrimeSumRequest OriginalRequest { get; set; }
        public int ThePrimeToAggregate { get; set; }
    }
}
