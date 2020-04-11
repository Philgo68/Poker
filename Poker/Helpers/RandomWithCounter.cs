using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Poker.Helpers
{
    public class RandomWithCounter : Random
    {
        public uint CallCount { get; set; }

        public RandomWithCounter() : base()
        {
            CallCount = 0;
        }

        public override int Next(int range)
        { 
            CallCount++;
            return base.Next(range);
        }

    }
}
