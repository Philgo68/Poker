using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Poker.Models
{
  public class OmahaHand : BaseHand
  {
    public override string Name { get { return "Omaha"; } }
    public override int CardCount { get { return 4; } }
    public OmahaHand() : base() { }
    public OmahaHand(ulong cardsMask) : base(cardsMask) { }
    public OmahaHand(string cards) : base(cards) { }
    public override (int, uint) Evaluate(ulong hero, ulong board)
    {
      return Deck.OmahaEvaluate(hero, board);
    }
  }
}
