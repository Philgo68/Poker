using Microsoft.AspNetCore.Mvc;
using Poker.Helpers;
using System;
using System.Collections.Generic;
using Poker.Interfaces;

//    public string Name { get; private set; } = "BaseDeck";

namespace Poker.Models
{
	public abstract class BaseDeck : IDeck
	{
		public virtual string Name { get; private set; } = "BaseDeck";
		public virtual int Suits => 0;
		public virtual string[] SuitDescriptions => null;
		public virtual string[] SuitDescriptionsLong => null;
		public virtual int Ranks => 0;
		public virtual string[] RankDescriptions => null;
		public virtual string[] RankDescriptionsLong => null;
		public virtual int CardCount => Suits * Ranks;
		public virtual ulong AllCardsMask => ((0x1UL << CardCount) - 1);
		public virtual ulong DealtCards { get; set; }
		public int RemainingInDeck { get; private set; }

		public virtual ulong PeekCards(int numberOfCards, Random rand = null)
		{
			// Bring in method info only once for speed
			var lCardCount = CardCount;
			var lDealtCards = DealtCards;
			var lRemainingInDeck = RemainingInDeck;

			// If we have just the right number of cards left, just return all the undealt cards
			if (lRemainingInDeck == numberOfCards)
			{
				return lDealtCards ^ AllCardsMask;
			}

			rand ??= new Random();

			var dealtCards = 0x0UL;
			// Deal some cards
			if (lRemainingInDeck <= (lCardCount >> 1))
			{
				// By randomly poking at an array of remaining cards looking for cards
				var cardsLeft = Bits.IndividualMasks(lDealtCards ^ AllCardsMask);
				for (int i = 0; i < numberOfCards; i++)
				{
					int dealtCard;
					// Find a random card in the deck.
					do
					{
						dealtCard = rand.Next(lRemainingInDeck);
					} while ((cardsLeft[dealtCard]) == 0);
					dealtCards |= cardsLeft[dealtCard];  // Mark it as dealt
					cardsLeft[dealtCard] = 0;
				}
			}
			else
			{
				// By randomly poking at the bit mask looking for undealt cards
				ulong dealtCard;
				for (int i = 0; i < numberOfCards; i++)
				{
					// Find a random card in the deck.
					do
					{
						dealtCard = 1UL << rand.Next(lCardCount);
					} while (((lDealtCards | dealtCards) & dealtCard) != 0);
					dealtCards |= dealtCard;  // Mark it as dealt
				}
			}
			return dealtCards;
		}

		public virtual ulong DealCards(int numberOfCards, Random rand = null)
		{
			var resultMask = PeekCards(numberOfCards, rand);
			DealtCards |= resultMask;
			RemainingInDeck -= numberOfCards;
			return resultMask;
		}

		public virtual ulong CompleteCards(int numberOfCards, ulong hand, Random rand = null)
		{
			return DealCards(numberOfCards - Bits.BitCount(hand), rand) | hand;
		}
		public virtual void CompleteCards(BaseHand hand, Random rand = null)
		{
			hand.CardsMask |= DealCards(hand.CardsNeeded, rand);
		}
		public virtual void CompleteCards(IHand hand, Random rand = null)
		{
			hand.CardsMask |= DealCards(hand.CardsNeeded, rand);
		}

		public virtual void Reset(ulong dealtCards = 0x0UL)
		{
			DealtCards = dealtCards;
			RemainingInDeck = CardCount - Bits.BitCount(dealtCards);
		}

		public BaseDeck(ulong dealtCards = 0x0UL)
		{
			Reset(dealtCards);
		}

		public virtual IEnumerable<int> CardNumbers(ulong cards)
		{
			// Retrieved this way to get Ace down to 2, Spades down to Clubs
			uint allRanks = (uint)((1 << Ranks) - 1);

			uint[] suitBits = new uint[Suits];
			for (int suit = Suits - 1; suit >= 0; suit--)
			{
				suitBits[suit] = (uint)((cards >> (Ranks * suit)) & allRanks);
			}
			for (int rank = Ranks - 1; rank >= 0; rank--)
			{
				var cardmask = 0x1UL << rank;
				for (int suit = Suits - 1; suit >= 0; suit--)
				{
					if ((suitBits[suit] & cardmask) == cardmask)
					{
						yield return suit * Ranks + rank;
					}
				}
			}
		}


		public virtual IEnumerable<string> Cards(ulong cards)
		{
			uint allRanks = (uint)((1 << Ranks) - 1);

			uint[] suitBits = new uint[Suits];
			for (int suit = Suits - 1; suit >= 0; suit--)
			{
				suitBits[suit] = (uint)((cards >> (Ranks * suit)) & allRanks);
			}
			for (int rank = Ranks - 1; rank >= 0; rank--)
			{
				var cardmask = 0x1UL << rank;
				for (int suit = Suits - 1; suit >= 0; suit--)
				{
					if ((suitBits[suit] & cardmask) == cardmask)
					{
						yield return $"{RankDescriptions[rank]}{SuitDescriptions[suit]}";
					}
				}
			}
		}

		public virtual string CardDescriptions(ulong cards)
		{
			string result = "";
			foreach (var desc in Cards(cards))
			{
				result += $"{desc}, ";
			}
			return result.TrimEnd(new Char[] { ',', ' ' });
		}
		public virtual (int, uint) PokerEvaluate(ulong cards, int numberOfCards) => (0, 0);
		public virtual (int, uint) PokerEvaluate(ulong cards)
		{
			return PokerEvaluate(cards, Bits.BitCount(cards));
		}
		public virtual (int, uint) PokerEvaluate(ulong hand, ulong board)
		{
			return PokerEvaluate(hand | board);
		}
		public virtual (int, uint) PokerEvaluate(ulong hand, ulong board, int numberOfCards)
		{
			return PokerEvaluate(hand | board, numberOfCards);
		}
		public virtual (int, uint) OmahaEvaluate(ulong hand, ulong board) => (0, 0);
	}
}
