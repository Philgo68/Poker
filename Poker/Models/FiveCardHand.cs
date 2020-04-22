using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Poker.Models
{
  public class FiveCardHand : BaseHand
  {
    //    public string Name { get; private set; } = "Raul";
    public override string Name => "Five Card";
    public override int CardCount => 5;
    public FiveCardHand() : base() { }
    public FiveCardHand(ulong cardsMask) : base(cardsMask) { }
    public FiveCardHand(string cards) : base(cards) { }
  }
}
