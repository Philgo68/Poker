namespace Poker.Models
{
  public class OneCardHoldem : PokerGame
  {
    public OneCardHoldem()
    {
    }

    public override string Name { get { return "One Card Hold'em"; } }

    public override BaseDeck GetDeck(ulong dealtCards = 0)
    {
      return new StandardDeck(dealtCards);
    }

    public override BaseHand GetHand()
    {
      return new BaseHand(1);
    }

  }
}
