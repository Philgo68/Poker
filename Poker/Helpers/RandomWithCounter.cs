using System;

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
