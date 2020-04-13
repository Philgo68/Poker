using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Poker.Models
{
  public class HoldemHand : BaseHand
  {

    public override string Name { get { return "HoldEm"; } }
    public override byte CardCount { get { return 2; } }
    public HoldemHand() : base() { }
    public HoldemHand(ulong cardsMask) : base(cardsMask) { }
    public HoldemHand(string cards) : base(cards) { }
  }
}
