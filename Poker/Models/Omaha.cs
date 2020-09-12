using Poker.Helpers;
using System;

namespace Poker.Models
{
  public class Omaha : PokerGame
  {
    public Omaha()
    {
    }

    public override string Name { get { return "Omaha"; } }

    public override BaseDeck GetDeck(ulong dealtCards = 0)
    {
      return new StandardDeck(dealtCards);
    }

    public override BaseHand GetHand()
    {
      return new BaseHand(4);
    }

    public override HandEvaluation Evaluate(ulong cards)
    {
      throw new ArgumentException("Omaha Evaluation must include a hand and a board.");
    }
    public override HandEvaluation Evaluate(BaseHand hand)
    {
      throw new ArgumentException("Omaha Evaluation must include a hand and a board.");
    }

    public override HandEvaluation Evaluate(ulong hand, ulong board)
    {
      return EvaluateHand(hand, board);
    }

    public static HandEvaluation EvaluateHand(ulong hand, ulong board)
    {
      var handcards = Bits.IndividualMasks(hand);
      var boardcards = Bits.IndividualMasks(board);
      HandEvaluation best, next;
      ulong selectedBoardCards;

      selectedBoardCards = boardcards[0] | boardcards[1] | boardcards[2];
      best = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      //if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (next.Value > best.Value) best = next;

      selectedBoardCards = boardcards[0] | boardcards[1] | boardcards[3];
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (next.Value > best.Value) best = next;

      selectedBoardCards = boardcards[0] | boardcards[1] | boardcards[4];
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (next.Value > best.Value) best = next;

      selectedBoardCards = boardcards[0] | boardcards[2] | boardcards[3];
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (next.Value > best.Value) best = next;

      selectedBoardCards = boardcards[0] | boardcards[2] | boardcards[4];
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (next.Value > best.Value) best = next;

      selectedBoardCards = boardcards[0] | boardcards[3] | boardcards[4];
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (next.Value > best.Value) best = next;

      selectedBoardCards = boardcards[1] | boardcards[2] | boardcards[3];
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (next.Value > best.Value) best = next;

      selectedBoardCards = boardcards[1] | boardcards[2] | boardcards[4];
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (next.Value > best.Value) best = next;

      selectedBoardCards = boardcards[1] | boardcards[3] | boardcards[4];
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (next.Value > best.Value) best = next;

      selectedBoardCards = boardcards[2] | boardcards[3] | boardcards[4];
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (next.Value > best.Value) best = next;
      next = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (next.Value > best.Value) best = next;

      return best;
    }

  }
}
