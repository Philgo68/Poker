using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Poker.Models
{
  public class SevenCardHand : BaseHand
  {
    public override string Name { get { return "Seven Card"; } }
    public override byte CardCount { get { return 7; } }
    public SevenCardHand() : base() { }
    public SevenCardHand(ulong cardsMask) : base(cardsMask) { }
    public SevenCardHand(string cards) : base(cards) { }
  }
}
