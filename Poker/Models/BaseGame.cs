using Microsoft.AspNetCore.Mvc;
using Poker.Helpers;
using System;
using System.Collections.Generic;
using Poker.Interfaces;

namespace Poker.Models
{
	[Serializable]
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

		public virtual DisplayStage[] ExecutePhase(int game_phase, TableDealer tableDealer)
		{
			return new DisplayStage[] {};
		}

		public abstract int PhaseCount { get; }

		public virtual (int, uint) Evaluate(BaseHand hand)
		{
			return Evaluate(hand.CardsMask);
		}
		public virtual (int, uint) Evaluate(BaseHand hand, BaseHand board)
		{
			return Evaluate(hand.CardsMask, board.CardsMask);
		}
		public virtual (int, uint) Evaluate(ulong hand, ulong board)
		{
			return Evaluate(hand | board);
		}
		public abstract (int, uint) Evaluate(ulong cards);
	}
}
