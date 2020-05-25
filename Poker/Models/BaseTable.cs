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

	public enum DisplayStage
	{
		DealtCards,
		BetsOut,
		PotScooped,
		DeliverPot
	}

	public interface IHandHolder
	{
		public IHand Hand { get; }
	}

	public class Seat : IHandHolder
	{
		public IHand Hand { get; set; }
		public IPlayer Player { get; set; }
		public int Chips { get; set; }
		public int ChipsMoving { get; set; }

		// Pre - calculated fro display actions
		public int ChipsOut { get; set; }
		public int ChipsIn { get; set; }
		public int HandsWon { get; set; }

		public Seat(IHand hand, IPlayer player = null, int chips = 500)
		{
			Hand = hand;
			Player = player;
			Chips = chips;
		}
	}

	public class BaseTable : IHandHolder
	{
		const int standardTime = 4;

		private IDeck deck;
		private int game_phase;
		private DisplayStage[] displayStages;
		private readonly Dictionary<DisplayStage, Func<int>> DisplayActions = new Dictionary<DisplayStage, Func<int>>();
		private int pot;

		public BaseTable(IGame _game, int _totalSeats = 9)
		{
			game = _game;
			totalSeats = _totalSeats;
			seats = new Seat[totalSeats];
			board = null;
			deck = game.GetDeck();
			game_phase = 0;

			DisplayActions[DisplayStage.DealtCards] = () => {
				NotifyStateChanged();
				return standardTime * 2;
			};

			DisplayActions[DisplayStage.BetsOut] = () => {
				foreach (var seat in OccupiedSeats())
				{
					if (seat.Hand is IHand Hand)
					{
						seat.ChipsMoving = seat.ChipsOut;
						seat.Chips -= seat.ChipsOut;
						seat.ChipsOut = 0;
					}
				}
				NotifyStateChanged();
				return standardTime;
			};

			DisplayActions[DisplayStage.PotScooped] = () => {
				foreach (var seat in OccupiedSeats())
				{
					if (seat.Hand is IHand Hand)
					{
						pot += seat.ChipsMoving;
						seat.ChipsMoving = 0;
					}
				}
				NotifyStateChanged();
				return standardTime;
			};

			DisplayActions[DisplayStage.DeliverPot] = () => {
				pot = 0;
				foreach (var seat in OccupiedSeats())
				{
					if (seat.Hand is IHand Hand)
					{
						seat.ChipsMoving = seat.ChipsIn;
						seat.Chips += seat.ChipsIn;
					}
				}
				NotifyStateChanged();
				return standardTime;
			};

		}

		public event Action StateHasChangedDelegate;

		private void NotifyStateChanged()
		{
			StateHasChangedDelegate?.Invoke();
		}

		private void StateHasChanged()
		{
			NotifyStateChanged();
			if (displayStages == null) TransitionToNextPhase();
		}

		public string Name => "BaseTable";
		public int totalSeats { get; private set; }
		public Seat[] seats { get; private set; }
		public IHand board { get; private set; }
		public IGame game { get; private set; }

		public IDeck Deck => deck;

		IHand IHandHolder.Hand => board;

		public BaseTable(ulong dealtCards = 0x0UL)
		{
			Reset(dealtCards);
		}

		public Seat OccupySeat(IHand hand, IPlayer player = null)
		{
			// Find a seat for them
			var seatIndex = Array.IndexOf(seats, null);
			if (seatIndex == -1)	return null;

			seats[seatIndex] = new Seat(hand, player);

			hand.StateHasChangedDelegate += StateHasChanged;

			InitializeDeckFromTable();

			return seats[seatIndex];
		}

		public IEnumerable<Seat> OccupiedSeats()
		{
			for (var i = 0; i < totalSeats; i++)
			{
				if (seats[i] != null && seats[i].Chips > 0 && seats[i].Hand.CardsMask > 0)
					yield return seats[i];
			}
		}


		public Seat Hero()
		{
			return seats[0];
		}

		public IEnumerable<Seat> Opponents()
		{
			for (var i = 1; i < totalSeats; i++)
			{
				if (seats[i] != null)
					yield return seats[i];
			}
		}

		public void SetBoard(IHand _board)
		{
			board = _board;
		}

		public void Reset(ulong dealtCards)
		{
			deck.Reset(dealtCards);
		}

		public void CompleteCards()
		{
			var _global_random = new Random();
			for (var i = 0; i < totalSeats; i++)
			{
				if (seats[i] != null && seats[i].Hand != null)
					deck.CompleteCards(seats[i].Hand, _global_random);
			}
		}

		public void LayoutHands(bool includingHero = true)
		{
			for (var i = (includingHero ? 0 : 1); i < totalSeats; i++)
			{
				if (seats[i] != null && seats[i].Hand != null)
					seats[i].Hand.LayoutHand();
			}
		}

		private int TestHero(IHand[] fillers, IHand boardFiller )
		{
			var boardMask = board.CardsMask | boardFiller.CardsMask;
			var tied = false;
			uint villain = 0;
			uint hero = 0;

			if (fillers[0].Changed || boardFiller.Changed) {
				fillers[0].LastEvaluation = game.Evaluate(seats[0].Hand.CardsMask | fillers[0].CardsMask, boardMask);
				fillers[0].Changed = false;
			}
			(_, hero) = fillers[0].LastEvaluation;
			for (var i = 1; i < totalSeats; i++)
			{
				if (seats[i] != null) 
				{
					if (fillers[i].Changed || boardFiller.Changed)
					{
						fillers[i].LastEvaluation = game.Evaluate(seats[i].Hand.CardsMask | fillers[i].CardsMask, boardMask);
						fillers[i].Changed = false;
					}
					(_, villain) = fillers[i].LastEvaluation;

					if (villain > hero) { break; }
					if (villain == hero) { tied = true; }
				}
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
			var dealtCards = board != null ? board.CardsMask : 0UL;
			foreach (var seat in seats.Where(s => s != null && s.Hand != null))
			{
				dealtCards |= seat.Hand.CardsMask;
			}

			deck.Reset(dealtCards);
		}

		public long PlayHandTrials(double duration = 0.0, ulong iterations = 0UL)
		{
			InitializeDeckFromTable();
			var dealtCards = deck.DealtCards;

			var _global_random = new Random();

			long wins = 0;
			long loses = 0;
			long ties = 0;

			int threadCnt = Environment.ProcessorCount / 2;
			iterations /= Convert.ToUInt16(threadCnt);

			Action<object> trial = delegate(object state)
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
					fillers.Add(new BaseHand((seat == null || seat.Hand == null) ? 0 : seat.Hand.CardsNeeded));
				}

				// For each completed set of hands
				foreach (var completeFillers in fillers.CompletedHands(deck, board, threadNum, threadCnt, rand, duration, iterations))
				{
					// Play the hand out
					var result = this.TestHero(completeFillers.ToArray(), board);
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

		public void DisplayNextStage()
		{
			if (displayStages.Length > 0)
			{
				var wait = DisplayActions[displayStages[0]]();
				displayStages = displayStages.Skip(1).ToArray();
				Task WaitTask = Task.Delay(wait * 1000);
				WaitTask.ContinueWith(t => DisplayNextStage());
			}
			else
			{
				TransitionToNextPhase();
			}
		}

		public void TransitionToNextPhase()
		{
			// Execute the next game step
			displayStages = game.ExecutePhase(game_phase, this);

			if (displayStages == null)
			{
				// not ready to move on
			}
			else
			{
				game_phase++;
				DisplayNextStage();
			}
		}

		public void CleanTableForNextHand()
		{
			// Get new blank hands for all seats with chips.
			for (var i = 1; i < totalSeats; i++)
			{
				if (seats[i] != null && seats[i].Chips > 0)
					seats[i].Hand = game.GetHand();
			}

			// Reset the deck
			deck.Reset();

			// Ready to start new hand
			game_phase = 0;
		}
	}
}
