﻿using Microsoft.AspNetCore.Mvc;
using Poker.Helpers;
using Poker.Interfaces;
using System;
using System.Collections.Generic;

namespace Poker.Models
{
	public class TexasHoldem : PokerGame
	{
		public TexasHoldem()
		{
		}

		public override string Name { get { return "Texas Hold'em"; } }

		public override IDeck GetDeck(ulong dealtCards = 0)
		{
			return new StandardDeck(dealtCards);
		}

		public override IHand GetHand()
		{
			return new BaseHand(2);
		}

		public override IHand GetBoard()
		{
			return new BaseHand(5);
		}

	}
}