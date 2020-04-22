using Microsoft.AspNetCore.Mvc;
using Poker.Helpers;
using System;
using System.Collections.Generic;
using Poker.Interfaces;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Poker.Models
{
	public interface IHandHolder
	{
		public IHand Hand { get; }
	}

	public class Seat : IHandHolder
	{
		public IHand Hand { get; set; }
		public IPlayer Player { get; set; }

		public Seat(IHand hand, IPlayer player = null)
		{
			Hand = hand;
			Player = player;
		}
	}

	public class BaseTable : ITable
	{
		private IGame game;
		private int totalSeats;

		private IHand board;
		private List<Seat> seats;

		private IDeck deck;

		public BaseTable(IGame _game, int _totalSeats = 9)
		{
			game = _game;
			totalSeats = _totalSeats;
			seats = new List<Seat>();
			board = null;
			deck = game.GetDeck();
		}

		public string Name => "BaseTable";

		public IDeck Deck => deck;

		IHand IHandHolder.Hand => board;

		public BaseTable(ulong dealtCards = 0x0UL)
		{
			Reset(dealtCards);
		}

		public Seat AddHand(IHand hand, IPlayer player = null)
		{
			// Find a seat for them
			if (seats.Count == totalSeats)
				return null;
			var newSeat = new Seat(hand, player);
			seats.Add(newSeat);
			return newSeat;
		}

		public Seat Hero()
		{
			return seats[0];
		}

		public IEnumerable<Seat> Opponents()
		{
			for (var i = 1; i < seats.Count; i++)
			{
				yield return seats[i];
			}
		}

		public void Reset(ulong dealtCards)
		{
			deck.Reset(dealtCards);
		}


		public void PlayHand() { }

		public void SetBoard(IHand _board)
		{
			board = _board;
		}

		private int TestHero()
		{
			var boardMask = board.CardsMask | board.FillerMask;
			var tied = false;
			uint villain = 0;

			(_, uint hero) = game.Evaluate(seats[0].Hand.CardsMask | seats[0].Hand.FillerMask, boardMask);
			for (var i = 1; i < seats.Count; i++)
			{
				(_, villain) = game.Evaluate(seats[i].Hand.CardsMask | seats[i].Hand.FillerMask, boardMask);

				if (villain > hero) { break; }
				if (villain == hero) { tied = true; }
			}
			if (villain > hero)
			{
				return -1;
			}
			else if (tied)
			{
				return 0;
			}
			else
			{
				return 1;
			}
		}

		private void InitializeDeckFromTable()
		{
			var dealtCards = board.CardsMask;
			foreach (var seat in seats)
			{
				dealtCards |= seat.Hand.CardsMask;
			}

			deck.Reset(dealtCards);
		}

		public long PlayHandTrials(double duration)
		{
			InitializeDeckFromTable();
			var dealtCards = deck.DealtCards;

			var _global_random = new Random();

			long wins = 0;
			long loses = 0;
			long ties = 0;
			long end = Convert.ToInt64(Stopwatch.GetTimestamp() + (duration * Stopwatch.Frequency));
			Action trial = delegate
			{
				int seed;
				lock (_global_random) seed = _global_random.Next();
				var rand = new Random(seed);
				var deck = game.GetDeck(dealtCards);
				do
				{
					// Get the random cards
					board.FillerMask = deck.DealCards(board.CardsNeeded, rand);
					foreach (var seat in seats)
					{
						seat.Hand.FillerMask = deck.DealCards(seat.Hand.CardsNeeded, rand);
					}

					// Play the hand out
					var result = this.TestHero();
					switch (result.CompareTo(0))
					{
						case -1:
							loses -= result;
							break;
						case 0:
							ties++;
							break;
						default:
							wins += result;
							break;
					}
					deck.Reset(dealtCards);
				} while (Stopwatch.GetTimestamp() < end);
			};

			var tasks = new List<Task>();
			for (int ctr = 1; ctr <= Environment.ProcessorCount; ctr++)
				tasks.Add(Task.Factory.StartNew(trial));
			Task.WaitAll(tasks.ToArray());

			seats[0].Hand.Wins = wins;
			seats[0].Hand.Ties = ties;
			seats[0].Hand.Loses = loses;

			return wins + ties + loses;
		}







	}
}
