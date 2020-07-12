using Microsoft.AspNetCore.Mvc;
using Poker.Helpers;
using System;
using System.Collections.Generic;
using Poker.Interfaces;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Operations;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Poker.Models
{
  [Serializable]
  public class BaseTable : IHandHolder, ISavable
  {
    const double standardTime = 2;

    private BaseDeck deck;
    private int game_phase;

    private DisplayStage[] displayStages;

    [NonSerialized]
    private Dictionary<DisplayStage, Func<double>> DisplayActions;
    public int pot;
    public string DisplayPhase { get; set; }
    public string PhaseTitle { get; set; }
    public string PhaseMessage { get; set; }

    private string StorageFile { get; set; }

    public BaseTable(BaseGame _game, int _totalSeats = 9)
    {
      game = _game;
      totalSeats = _totalSeats;
      seats = new Seat[totalSeats];
      board = null;
      deck = game.GetDeck();
      game_phase = 0;
      SetupDisplayActions();
    }
    public void SetupDisplayActions()
    {
      DisplayActions = new Dictionary<DisplayStage, Func<double>>();

      DisplayActions[DisplayStage.DealtCards] = () =>
      {
        DisplayPhase = null;
        DisplayPhase = "dealtCards";
        foreach (var seat in OccupiedSeats())
        {
          seat.ChipsMoving = 0;
        }
        NotifyStateChanged();
        return standardTime;
      };

      DisplayActions[DisplayStage.BetsOut] = () =>
      {
        DisplayPhase = null;
        DisplayPhase = "betsOut";
        foreach (var seat in OccupiedSeats())
        {
          seat.ChipsMoving = seat.ChipsOut;
          seat.Chips -= seat.ChipsOut;
          seat.ChipsOut = 0;
        }
        NotifyStateChanged();
        return standardTime / 3;
      };

      DisplayActions[DisplayStage.Scooping] = () =>
      {
        DisplayPhase = "scooping";
        NotifyStateChanged();
        return 1.0;
      };

      DisplayActions[DisplayStage.PotScooped] = () =>
      {
        DisplayPhase = "scooping scooped";
        foreach (var seat in OccupiedSeats())
        {
          Pot += seat.ChipsMoving;
          seat.ChipsMoving = 0;
        }
        NotifyStateChanged();
        return 0;
      };

      DisplayActions[DisplayStage.Delivering] = () =>
      {
        DisplayPhase = "scooping returning";
        NotifyStateChanged();
        return 1.0;
      };

      DisplayActions[DisplayStage.DeliverPot] = () =>
      {
        DisplayPhase = "returning delivered";
        Pot = 0;
        foreach (var seat in OccupiedSeats())
        {
          seat.ChipsMoving = seat.ChipsIn;
          seat.Chips += seat.ChipsIn;
          seat.ChipsIn = 0;
        }
        NotifyStateChanged();
        return standardTime;
      };

    }

    [OnDeserialized()]
    internal void OnDeserializedMethod(StreamingContext context)
    {
      SetupDisplayActions();
      foreach (var seat in AllSeats())
      {
        if (seat?.Hand != null)
          seat.Hand.StateHasChangedDelegate += StateHasChanged;
      }
    }

    [field: NonSerialized]
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

    public void Store()
    {
      DbAccess.Save<BaseTable>(this);
    }

    public string Name => "BaseTable";
    public int totalSeats { get; private set; }

    public Seat[] seats { get; private set; }

    public BaseHand board { get; private set; }

    public BaseGame game { get; private set; }

    public BaseDeck Deck => deck;

    BaseHand IHandHolder.Hand => board;

    public int Pot { get => pot; private set => pot = value; }

    public Guid Id { get; set; } = Guid.NewGuid();

    public BaseTable(ulong dealtCards = 0x0UL)
    {
      Reset(dealtCards);
    }

    public Seat OccupySeat(BaseHand hand, BasePlayer player = null, int chips = 500)
    {
      // Find a seat for them
      var seatIndex = Array.IndexOf(seats, null);
      if (seatIndex == -1) return null;

      player ??= new BasePlayer() { Name = "Computer", Computer = true };

      seats[seatIndex] = new Seat(hand, player, chips);

      hand.StateHasChangedDelegate += StateHasChanged;

      InitializeDeckFromTable();

      return seats[seatIndex];
    }

    public IEnumerable<Seat> OccupiedSeats()
    {
      foreach (var seat in seats)
      {
        if (seat != null && (seat.Chips > 0 || seat.ChipsOut > 0 || seat.ChipsIn > 0 || seat.ChipsMoving > 0) && seat.Hand?.CardsMask > 0)
          yield return seat;
      }
    }

    public IEnumerable<Seat> AllSeats()
    {
      foreach (var seat in seats)
      {
        yield return seat;
      }
    }


    public Seat Hero()
    {
      return seats.FirstOrDefault(s => s != null && s.Player != null && !s.Player.Computer);
    }

    public IEnumerable<Seat> Opponents(int seat = 0)
    {
      for (var i = 0; i < totalSeats; i++)
      {
        if (i != seat && seats[i] != null)
          yield return seats[i];
      }
    }

    public void SetBoard(BaseHand _board)
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

    public void LayoutHands(bool allTheHands = false)
    {
      for (var i = 0; i < totalSeats; i++)
      {
        if (seats[i] != null && seats[i].Hand != null && (allTheHands || seats[i].Player.Computer))
          seats[i].Hand.LayoutHand();
      }
    }

    private int TestHero(BaseHand[] fillers, BaseHand boardFiller)
    {
      var boardMask = board.CardsMask | boardFiller.CardsMask;
      var tied = false;
      uint villain = 0;
      uint hero = 0;

      if (fillers[0].Changed || boardFiller.Changed)
      {
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

      Action<object> trial = delegate (object state)
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
        Task WaitTask = Task.Delay(Convert.ToInt32(wait * 1000.0));
        WaitTask.ContinueWith(t => DisplayNextStage());
      }
      else
      {
        TransitionToNextPhase();
      }
    }

    public void TransitionToNextPhase()
    {
      // Save before executing phase zero
      if (game_phase == 0)
        Store();

      // Execute the next game step
      displayStages = game.ExecutePhase(game_phase, this);

      // Check Chips 
      var inC = 0;
      var outC = 0;
      foreach (var seat in OccupiedSeats())
      {
        inC += seat.ChipsIn;
        outC += seat.ChipsOut;
      }
      if (inC != outC)
      {
        inC = outC;
      }

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

    public void CleanChips()
    {
      var totalChips = 0.0;
      var seatsCount = 0.0;
      // Clear Chips
      for (var i = 0; i < totalSeats; i++)
      {
        if (seats[i] != null)
        {
          seats[i].ChipsMoving = 0;
          seats[i].ChipsIn = 0;
          seats[i].ChipsOut = 0;
          Pot = 0;
          totalChips += seats[i].Chips;
          seatsCount += 1.0;
        }
      }
    }

    public void CleanTableForNextHand()
    {
      // Clear table and get new blank hands for all seats with chips.
      CleanChips();
      PhaseTitle = "";
      PhaseMessage = "";

      for (var i = 0; i < totalSeats; i++)
      {
        if (seats[i] != null)
        {
          if (seats[i].Chips > 0)
          {
            seats[i].Hand = game.GetHand();
            seats[i].Hand.StateHasChangedDelegate += StateHasChanged;
          }
          else
            seats[i].Hand = null;
        }
      }
      SetBoard(null);

      // Reset the deck
      deck.Reset();

      // Ready to start new hand
      game_phase = -1;
    }
  }
}
