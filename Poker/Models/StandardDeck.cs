using Poker.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Poker.Models
{
  [Serializable]
  public class StandardDeck : BaseDeck
  {
    public override string Name => "Standard Deck";
    public override int Suits => 4;
    public override string[] SuitDescriptions => new string[] { "c", "d", "h", "s" };
    public override string[] SuitDescriptionsLong => new string[] { "Clubs", "Diamonds", "Hearts", "Spades" };
    public override int Ranks => 13;
    public override string[] RankDescriptions => new string[] { "2", "3", "4", "5", "6", "7", "8", "9", "T", "J", "Q", "K", "A" };
    public override string[] RankDescriptionsLong => new string[] { "2", "3", "4", "5", "6", "7", "8", "9", "T", "J", "Q", "K", "A" };
    public StandardDeck(ulong dealtCards = 0x0UL) : base(dealtCards) { }
  }
}
