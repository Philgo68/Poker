using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Poker.Models
{
  public class FiveCardHand : BaseHand
  {
    public override string Name { get { return "Five Card"; } }
    public override byte CardCount { get { return 5; } }
    public FiveCardHand() : base() { }
    public FiveCardHand(ulong cardsMask) : base(cardsMask) { }
    public FiveCardHand(string cards) : base(cards) { }

    public override (int, uint) Evaluate(ulong board, ulong filler = 0x0UL)
    {
      return base.PokerEvaluate(board, filler);
    }
  }
}
