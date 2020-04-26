using Microsoft.AspNetCore.Mvc;
using Poker.Helpers;
using Poker.Interfaces;
using System;
using System.Collections.Generic;

namespace Poker.Models
{
	public class OneCardHoldem : PokerGame
	{
		public OneCardHoldem()
		{
		}

		public override string Name { get { return "One Card Hold'em"; } }

		public override IDeck GetDeck(ulong dealtCards = 0)
		{
			return new StandardDeck(dealtCards);
		}

		public override IHand GetHand()
		{
			return new BaseHand(1);
		}

		public override IHand GetBoard()
		{
			return new BaseHand(5);
		}

	}
}
