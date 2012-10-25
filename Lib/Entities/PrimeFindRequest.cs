using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DemoEntities
{
    public class PrimeFindRequest
    {
        public PrimeSumRequest OriginalRequest { get; set; }
        public int TheNumberToCheck { get; set; }
    }
}
