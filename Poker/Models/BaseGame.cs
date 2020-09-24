using System;

namespace Poker.Models
{

  public enum HandTypes
  {
    HighCard = 0,
    Pair = 1,
    TwoPair = 2,
    Trips = 3,
    Straight = 4,
    Flush = 5,
    FullHouse = 6,
    FourOfAKind = 7,
    StraightFlush = 8
  }

  public struct HandEvaluation
  {
    public HandTypes Type;
    public uint Value;
    public ulong Cards;

    public HandEvaluation(ulong cards, HandTypes type, uint value)
    {
      Cards = cards;
      Type = type;
      Value = value;
    }
  }

  public abstract class BaseGame
  {

    public BaseGame()
    {
    }

    public abstract string Name { get; }

    public abstract BaseDeck GetDeck(ulong dealtCards = 0x0UL);

    public abstract BaseHand GetHand();
    public virtual BaseHand GetBoard()
    {
      return new BaseHand(5)
      {
        CommunityCards = true
      };
    }

    protected System.Collections.Generic.List<Func<TableDealer, DisplayStage[]>> PhaseActions { get; set; }
    public int PhaseCount => PhaseActions.Count;

    public virtual DisplayStage[] ExecutePhase(int game_phase, TableDealer tableDealer)
    {
      return PhaseActions[game_phase](tableDealer);
    }

    public virtual HandEvaluation Evaluate(BaseHand hand)
    {
      return Evaluate(hand.CardsMask);
    }
    public virtual HandEvaluation Evaluate(BaseHand hand, BaseHand board)
    {
      return Evaluate(hand.CardsMask, board.CardsMask);
    }
    public virtual HandEvaluation Evaluate(ulong hand, ulong board)
    {
      return Evaluate(hand | board);
    }
    public abstract HandEvaluation Evaluate(ulong cards);
  }
}
