using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Poker.Models
{
  public class OneCardHand : BaseHand
  {
    public override string Name { get { return "One Card"; } }
    public override byte CardCount { get { return 1; } }
    public OneCardHand() : base() { }
    public OneCardHand(ulong cardsMask) : base(cardsMask) { }
    public OneCardHand(string cards) : base(cards) { }

    public override (int, uint) Evaluate(ulong board, ulong filler = 0x0UL)
    {
      return base.PokerEvaluate(board, filler);
    }
  }
}
