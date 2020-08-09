using Microsoft.AspNetCore.Mvc;
using Poker.Helpers;
using Poker.Interfaces;
using System;
using System.Collections.Generic;

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

    public override (int, uint) Evaluate(ulong cards)
    {
      throw new ArgumentException("Omaha Evaluation must include a hand and a board.");
    }
    public override (int, uint) Evaluate(BaseHand hand)
    {
      throw new ArgumentException("Omaha Evaluation must include a hand and a board.");
    }

    public override (int, uint) Evaluate(ulong hand, ulong board)
		{
			return EvaluateHand(hand, board);
		}

    public static (int, uint) EvaluateHand(ulong hand, ulong board)
    {
      var handcards = Bits.IndividualMasks(hand);
      var boardcards = Bits.IndividualMasks(board);
      int bestType, nextType;
      uint bestRank, nextRank;
      ulong selectedBoardCards;

      selectedBoardCards = boardcards[0] | boardcards[1] | boardcards[2];
      (bestType, bestRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      //if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      selectedBoardCards = boardcards[0] | boardcards[1] | boardcards[3];
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      selectedBoardCards = boardcards[0] | boardcards[1] | boardcards[4];
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      selectedBoardCards = boardcards[0] | boardcards[2] | boardcards[3];
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      
      selectedBoardCards = boardcards[0] | boardcards[2] | boardcards[4];
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      selectedBoardCards = boardcards[0] | boardcards[3] | boardcards[4];
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank); 
      
      selectedBoardCards = boardcards[1] | boardcards[2] | boardcards[3];
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank); 
      
      selectedBoardCards = boardcards[1] | boardcards[2] | boardcards[4];
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank); 
      
      selectedBoardCards = boardcards[1] | boardcards[3] | boardcards[4];
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank); 
      
      selectedBoardCards = boardcards[2] | boardcards[3] | boardcards[4];
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[1]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[0] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[2]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[1] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);
      (nextType, nextRank) = EvaluateHand(selectedBoardCards | handcards[2] | handcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      return (bestType, bestRank);
    }

  }
}
