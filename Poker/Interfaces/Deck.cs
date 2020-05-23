using Microsoft.AspNetCore.Mvc;
using Poker.Helpers;
using System;
using System.Collections.Generic;

namespace Poker.Interfaces
{
	public interface IDeck
	{
		public string Name { get; }
		public int Suits { get; }
		public string[] SuitDescriptions { get; }
		public string[] SuitDescriptionsLong { get; }
		public int Ranks { get; }
		public string[] RankDescriptions { get; }
		public string[] RankDescriptionsLong { get; }
		public int CardCount { get; }
		public ulong AllCardsMask { get; }
		public ulong DealtCards { get; set; }
		public int RemainingInDeck { get; }

		public ulong PeekCards(int numberOfCards, Random rand = null);

		public ulong DealCards(int numberOfCards, Random rand = null);

		public ulong CompleteCards(int numberOfCards, ulong hand, Random rand = null);

		public void CompleteCards(IHand hand, Random rand = null);

		public void Reset(ulong dealtCards = 0x0UL);

		public IEnumerable<int> CardNumbers(ulong cards);

		public IEnumerable<string> Cards(ulong cards);

		public string CardDescriptions(ulong cards);

		public (int, uint) PokerEvaluate(ulong cards, int numberOfCards);
		public (int, uint) PokerEvaluate(ulong cards);
		public (int, uint) PokerEvaluate(ulong hand, ulong board);
		public (int, uint) PokerEvaluate(ulong hand, ulong board, int numberOfCards);
		public (int, uint) OmahaEvaluate(ulong hand, ulong board);

	}
}
