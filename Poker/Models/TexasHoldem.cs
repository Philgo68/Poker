namespace Poker.Models
{
  public class TexasHoldem : PokerGame
  {
    public TexasHoldem()
    {
    }

    public override string Name { get { return "Texas Hold'em"; } }

    public override BaseDeck GetDeck(ulong dealtCards = 0)
    {
      return new StandardDeck(dealtCards);
    }

    public override BaseHand GetHand()
    {
      return new BaseHand(2);
    }

  }
}
