﻿using Microsoft.AspNetCore.Mvc;
using Poker.Helpers;
using System;
using System.Collections.Generic;
using Poker.Interfaces;

namespace Poker.Models
{
	public abstract class BaseGame : IGame
	{
		public BaseGame()
		{
		}

		public abstract string Name { get; }

		public abstract IDeck GetDeck(ulong dealtCards = 0x0UL);

		public abstract IHand GetHand();
		public abstract IHand GetBoard();

		public virtual (int, uint) Evaluate(IHand hand)
		{
			return Evaluate(hand.CardsMask);
		}
		public virtual (int, uint) Evaluate(IHand hand, IHand board)
		{
			return Evaluate(hand.CardsMask | board.CardsMask);
		}
		public virtual (int, uint) Evaluate(ulong hand, ulong board)
		{
			return Evaluate(hand | board);
		}
		public abstract (int, uint) Evaluate(ulong cards);
	}
}