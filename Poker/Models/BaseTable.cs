using Microsoft.AspNetCore.Mvc;
using Poker.Helpers;
using System;
using System.Collections.Generic;
using Poker.Interfaces;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

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

		private int TestHero(List<IHand> fillers, IHand boardFiller )
		{
			var boardMask = board.CardsMask | boardFiller.CardsMask;
			var tied = false;
			uint villain = 0;

			(_, uint hero) = game.Evaluate(seats[0].Hand.CardsMask | fillers[0].CardsMask, boardMask);
			for (var i = 1; i < seats.Count; i++)
			{
				(_, villain) = game.Evaluate(seats[i].Hand.CardsMask | fillers[i].CardsMask, boardMask);

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

		public long PlayHandTrials(double duration = 0.0, long iterations = 0L)
		{
			InitializeDeckFromTable();
			var dealtCards = deck.DealtCards;

			var _global_random = new Random();

			long wins = 0;
			long loses = 0;
			long ties = 0;

			var threadCnt = Environment.ProcessorCount;
			threadCnt = 4;

			Action<object?> trial = delegate(object? state)
			{
				Random rand = null;

				int threadNum = ((int?)state).Value;
				Console.WriteLine(threadNum);

				if ((duration > 0) || (iterations > 0))
				{
					int seed;
					lock (_global_random) seed = _global_random.Next();
					rand = new Random(seed);
				} 

				long _wins = 0;
				long _loses = 0;
				long _ties = 0;

				var deck = game.GetDeck(dealtCards);

				// Setup Filler Hands
				var board = new BaseHand(this.board.CardsNeeded);
				var fillers = new Fillers();
				foreach (var seat in seats)
				{
					fillers.Add(new BaseHand(seat.Hand.CardsNeeded));
				}

				// For each completed set of hands
				foreach (var completeFillers in fillers.CompletedHands(deck, board, threadNum, threadCnt, rand, duration, iterations))
				{
					// Play the hand out
					var result = this.TestHero(completeFillers, board);
					switch (result.CompareTo(0))
					{
						case -1:
							_loses -= result;
							break;
						case 0:
							_ties++;
							break;
						default:
							_wins += result;
							break;
					}
				}

				// Save results
				Interlocked.Add(ref loses, _loses);
				Interlocked.Add(ref ties, _ties);
				Interlocked.Add(ref wins, _wins);
			};

			var tasks = new List<Task>();
			for (int ctr = 1; ctr <= threadCnt; ctr++)
				tasks.Add(Task.Factory.StartNew(trial, ctr));
			Task.WaitAll(tasks.ToArray());

			seats[0].Hand.Wins = wins;
			seats[0].Hand.Ties = ties;
			seats[0].Hand.Loses = loses;

			return wins + ties + loses;
		}
	}
}
