using System;

namespace Identity.MongoDb.Models
{
    public class FutureOccurrence : Occurrence
    {
        public FutureOccurrence()
        {
        }

        public FutureOccurrence(DateTime willOccurOn) : base(willOccurOn)
        {
            
        }
    }
}
