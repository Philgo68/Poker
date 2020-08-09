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
using System.ComponentModel.DataAnnotations.Schema;
using Poker.Data;


namespace Poker.Models
{
  public class Table : Entity, IHandHolder
  {
    public int TotalSeats { get; protected set; }
    public List<Seat> Seats { get; protected set; }

    [NotMapped]
    public BaseGame Game { get; protected set; }

    [NotMapped]
    public BaseDeck SampleDeck { get; protected set; }

    public string GameString
    {
      get
      {
        return Game.GetType().AssemblyQualifiedName;
      }

      set
      {
        Game = (BaseGame)Activator.CreateInstance(Type.GetType(value));
      }
    }

    // Entity Framework load
    public Table(string gameString, int totalSeats = 9)
    {
      GameString = gameString;
      TotalSeats = totalSeats;
      Seats = new List<Seat>();

      Board = null;
      SampleDeck = Game.GetDeck();
    }

    // Creating a new table
    public Table(BaseGame _game, int _totalSeats = 9)
    {
      Game = _game;
      TotalSeats = _totalSeats;
      Seats = new List<Seat>();

      Board = null;
      SampleDeck = Game.GetDeck();
    }

    [NotMapped]
    public string DisplayPhase { get; set; }

    [NotMapped]
    public string PhaseTitle { get; set; }

    [NotMapped]
    public string PhaseMessage { get; set; }

    public void OnAfterLoad()
    {
      foreach (var seat in AllSeats())
      {
        if (seat?.Hand != null)
          seat.Hand.StateHasChangedDelegate += StateHasChanged;
      }
    }

    public event Action StateHasChangedDelegate;

    private void NotifyStateChanged()
    {
      StateHasChangedDelegate?.Invoke();
    }

    public void StateHasChanged()
    {
      NotifyStateChanged();
    }


    [NotMapped]
    public BaseHand Board { get; private set; }

    [NotMapped]
    BaseHand IHandHolder.Hand => Board;

    [NotMapped]
    public int Pot { get; set; }

    public Seat OccupySeat(Player player = null, int chips = 500)
    {
      // Find a seat for them
      var positions = new HashSet<int?>();
      for (var i = 0; i < TotalSeats; i++)
      {
        positions.Add(i);
      };

      foreach (var seat in Seats)
      {
        positions.Remove(seat.Position);
      }

      var openPosition = positions.FirstOrDefault();

      if (openPosition != null)
      {
        player ??= new Player() { Name = "Computer", Computer = true };
        var seat = new Seat(openPosition.Value, player, chips);
        Seats.Add(seat);
        return seat;
      }
      return null;
    }

    public IEnumerable<Seat> AllSeats()
    {
      foreach (var seat in Seats)
      {
        yield return seat;
      }
    }

    public IEnumerable<Seat> OccupiedSeats()
    {
      foreach (var seat in Seats)
      {
        if (seat != null && (seat.Chips > 0 || seat.ChipsOut > 0 || seat.ChipsIn > 0 || seat.ChipsMoving > 0))
          yield return seat;
      }
    }

    public IEnumerable<Seat> SeatsWithHands()
    {
      foreach (var seat in Seats)
      {
        if (seat != null && (seat.Chips > 0 || seat.ChipsOut > 0 || seat.ChipsIn > 0 || seat.ChipsMoving > 0) && seat.Hand?.CardsMask > 0)
          yield return seat;
      }
    }

    public Seat Hero()
    {
      return Seats.FirstOrDefault(s => s != null && s.Player != null && !s.Player.Computer);
    }

    
    public IEnumerable<Seat> Opponents(int seat = 0)
    {
      for (var i = 0; i < TotalSeats; i++)
      {
        if (i != seat && Seats[i] != null)
          yield return Seats[i];
      }
    }


    public void SetBoard(BaseHand _board)
    {
      Board = _board;
    }
/*
    public void Reset(ulong dealtCards)
    {
      Deck.Reset(dealtCards);
    }
*/
/*
    public void CompleteCards()
    {
      var _global_random = new Random();
      for (var i = 0; i < TotalSeats; i++)
      {
        if (Seats[i] != null && Seats[i].Hand != null)
          Deck.CompleteCards(Seats[i].Hand, _global_random);
      }
    }
*/
/*    public void LayoutHands(bool allTheHands = false)
    {
      for (var i = 0; i < TotalSeats; i++)
      {
        if (Seats[i] != null && Seats[i].Hand != null && (allTheHands || Seats[i].Player.Computer))
          Seats[i].Hand.LayoutHand();
      }
    }*/
/*
    private void InitializeDeckFromTable()
    {
      var dealtCards = Board != null ? Board.CardsMask : 0UL;
      foreach (var seat in Seats.Where(s => s != null && s.Hand != null))
      {
        dealtCards |= seat.Hand.CardsMask;
      }

      Deck.Reset(dealtCards);
    }
*/
  }

  public class TableDealer
  {
    const double standardTime = 2;

    public TableDealer(PokerDbContext dbContext, Table table)
    {
      Table = table;

      Deck = Table.Game.GetDeck();
      _gamePhase = -1;
      _dbContext = dbContext;
      
      SetupDisplayActions();
    }

    public Table Table { get; private set; }
    public BaseDeck Deck { get; private set; }

    private int _gamePhase;
    private PokerDbContext _dbContext;
    private DisplayStage[] _displayStages;
    private Dictionary<DisplayStage, Func<double>> _displayActions;
    private void SetupDisplayActions()
    {
      _displayActions = new Dictionary<DisplayStage, Func<double>>();

      _displayActions[DisplayStage.DealtCards] = () =>
      {
        Table.DisplayPhase = null;
        Table.DisplayPhase = "dealtCards";
        foreach (var seat in Table.SeatsWithHands())
        {
          seat.ChipsMoving = 0;
        }
        NotifyStateChanged();
        return standardTime;
      };

      _displayActions[DisplayStage.BetsOut] = () =>
      {
        Table.DisplayPhase = null;
        Table.DisplayPhase = "betsOut";
        foreach (var seat in Table.SeatsWithHands())
        {
          seat.ChipsMoving = seat.ChipsOut;
          seat.Chips -= seat.ChipsOut;
          seat.ChipsOut = 0;
        }
        NotifyStateChanged();
        return standardTime / 3;
      };

      _displayActions[DisplayStage.Scooping] = () =>
      {
        Table.DisplayPhase = "scooping";
        NotifyStateChanged();
        return 1.0;
      };

      _displayActions[DisplayStage.PotScooped] = () =>
      {
        Table.DisplayPhase = "scooping scooped";
        foreach (var seat in Table.SeatsWithHands())
        {
          Table.Pot += seat.ChipsMoving;
          seat.ChipsMoving = 0;
        }
        NotifyStateChanged();
        return 0;
      };

      _displayActions[DisplayStage.Delivering] = () =>
      {
        Table.DisplayPhase = "scooping returning";
        NotifyStateChanged();
        return 1.0;
      };

      _displayActions[DisplayStage.DeliverPot] = () =>
      {
        Table.DisplayPhase = "returning delivered";
        Table.Pot = 0;
        foreach (var seat in Table.SeatsWithHands())
        {
          seat.ChipsMoving = seat.ChipsIn;
          seat.Chips += seat.ChipsIn;
          seat.ChipsIn = 0;
        }
        NotifyStateChanged();
        return standardTime;
      };

    }

    private void NotifyStateChanged()
    {
      Table.StateHasChanged();
    }

    private void StateHasChanged()
    {
      NotifyStateChanged();
      if (_displayStages == null) TransitionToNextPhase();
    }

    public void SetBoard(BaseHand _board)
    {
      Table.SetBoard(_board);
    }

    public void Reset(ulong dealtCards)
    {
      Deck.Reset(dealtCards);
    }

    public void DealHands()
    {
      var _global_random = new Random();
      foreach (var seat in Table.OccupiedSeats())
      {
        seat.Hand = Table.Game.GetHand();
        seat.Hand.StateHasChangedDelegate += StateHasChanged;
        Deck.CompleteCards(seat.Hand, _global_random);
      }
    }

    public void LayoutHands(bool allTheHands = false)
    {
      foreach (var seat in Table.AllSeats())
      {
        if (seat?.Hand != null && (allTheHands || seat.Player.Computer))
          seat.Hand.LayoutHand();
      }
    }

    public void DisplayNextStage()
    {
      if (_displayStages.Length > 0)
      {
        var wait = _displayActions[_displayStages[0]]();
        _displayStages = _displayStages.Skip(1).ToArray();
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
      _gamePhase++;

      if (_gamePhase >= Table.Game.PhaseCount)
        _gamePhase = 0;

      // Execute the next game step
      _displayStages = Table.Game.ExecutePhase(_gamePhase, this);

      // Check Chips 
      var inC = 0;
      var outC = 0;
      foreach (var seat in Table.SeatsWithHands())
      {
        inC += seat.ChipsIn;
        outC += seat.ChipsOut;
      }
      if (inC != outC)
      {
        throw new Exception("Chips are out of balance!!");
      }

      if (_displayStages == null)
      {
        // not ready for this phase quite yet, back up and try again
        _gamePhase--;
      }
      else
      {
        DisplayNextStage();
      }
    }

    public void CleanChips()
    {
      // Clear Chips off the table
      Table.Pot = 0;
      foreach (var seat in Table.AllSeats())
      {
        if (seat != null)
        {
          seat.ChipsMoving = 0;
          seat.ChipsIn = 0;
          seat.ChipsOut = 0;
        }
      }
    }

    public void StoreTable()
    {
      _dbContext.SaveChanges();
    }

    public void CleanTableForNewHand()
    {
      // Clear table and get new blank hands for all seats with chips.
      CleanChips();
      Table.PhaseTitle = "";
      Table.PhaseMessage = "";

      // Reset the deck
      Deck.Reset();

      // Clear the Board
      SetBoard(null);

      // Mark the Seats for the Next Hand
      foreach (var seat in Table.AllSeats())
      {
        if (seat != null)
        {
          seat.Hand = null;
        }
      }

      // Save to database
      StoreTable();
    }
  }

  public class TableTrialer : IHandHolder
  {

    public TableTrialer(Table table)
    {
      Table = table;

      Board = null;
      Deck = Table.Game.GetDeck();

      // Watch for Seat Changes
      foreach (var seat in Table.AllSeats())
      {
        if (seat?.Hand != null)
          seat.Hand.StateHasChangedDelegate += StateHasChanged;
      }
    }

    public Table Table { get; private set; }
    public BaseHand Board { get; private set; }
    BaseHand IHandHolder.Hand => Board;
    public BaseDeck Deck { get; private set; }

    public event Action StateHasChangedDelegate;

    private void NotifyStateChanged()
    {
      StateHasChangedDelegate?.Invoke();
    }

    private void StateHasChanged()
    {
      NotifyStateChanged();
    }

    public Seat Hero()
    {
      return Table.Seats.FirstOrDefault(s => s != null && s.Player != null && !s.Player.Computer);
    }

    public IEnumerable<Seat> Opponents(int seat = 0)
    {
      for (var i = 0; i < Table.TotalSeats; i++)
      {
        if (i != seat && Table.Seats[i] != null)
          yield return Table.Seats[i];
      }
    }

    public void SetBoard(BaseHand _board)
    {
      Board = _board;
    }

    public void Reset(ulong dealtCards)
    {
      Deck.Reset(dealtCards);
    }

    public void CompleteCards()
    {
      var _global_random = new Random();
      for (var i = 0; i < Table.TotalSeats; i++)
      {
        if (Table.Seats[i] != null && Table.Seats[i].Hand != null)
          Deck.CompleteCards(Table.Seats[i].Hand, _global_random);
      }
    }

    public void LayoutHands(bool allTheHands = false)
    {
      for (var i = 0; i < Table.TotalSeats; i++)
      {
        if (Table.Seats[i] != null && Table.Seats[i].Hand != null && (allTheHands || Table.Seats[i].Player.Computer))
          Table.Seats[i].Hand.LayoutHand();
      }
    }

    private int TestHero(BaseHand[] fillers, BaseHand boardFiller)
    {
      var boardMask = Board.CardsMask | boardFiller.CardsMask;
      var tied = false;
      uint villain = 0;
      uint hero = 0;

      if (fillers[0].Changed || boardFiller.Changed)
      {
        fillers[0].LastEvaluation = Table.Game.Evaluate(Table.Seats[0].Hand.CardsMask | fillers[0].CardsMask, boardMask);
        fillers[0].Changed = false;
      }
      (_, hero) = fillers[0].LastEvaluation;
      for (var i = 1; i < Table.TotalSeats; i++)
      {
        if (Table.Seats[i] != null)
        {
          if (fillers[i].Changed || boardFiller.Changed)
          {
            fillers[i].LastEvaluation = Table.Game.Evaluate(Table.Seats[i].Hand.CardsMask | fillers[i].CardsMask, boardMask);
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
      var dealtCards = Board != null ? Board.CardsMask : 0UL;
      foreach (var seat in Table.Seats.Where(s => s != null && s.Hand != null))
      {
        dealtCards |= seat.Hand.CardsMask;
      }

      Deck.Reset(dealtCards);
    }

    public long PlayHandTrials(double duration = 0.0, ulong iterations = 0UL)
    {
      InitializeDeckFromTable();
      var dealtCards = Deck.DealtCards;

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

        var deck = Table.Game.GetDeck(dealtCards);

        // Setup Filler Hands
        var board = new BaseHand(Board.CardsNeeded);
        var fillers = new Fillers();
        foreach (var seat in Table.Seats)
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

      Table.Seats[0].Hand.Wins = wins;
      Table.Seats[0].Hand.Ties = ties;
      Table.Seats[0].Hand.Loses = loses;

      return wins + ties + loses;
    }
  }
}
