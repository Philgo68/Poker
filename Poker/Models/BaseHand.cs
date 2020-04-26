using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Threading;
using System.Threading.Tasks;
using Poker.CFHandEvaluator;
using Poker.Helpers;
using Poker.Interfaces;

namespace Poker.Models
{
  public class BaseHand : IHand
  {
    private readonly int cardCount;
    private int cardsNeeded;
    private ulong cardsMask;

    public BaseHand(int _cardCount = 5)
    {
      cardCount = _cardCount;
      cardsNeeded = _cardCount;
      Wins = 0;
      Loses = 0;
      Ties = 0;
      CommunityCards = false;
    }

    public BaseHand(ulong _cardsMask, int _cardCount = 5) : this(_cardCount)
    {
      SetCards(_cardsMask);
    }

    public BaseHand(string cards, int _cardCount = 5) : this(_cardCount)
    {
      SetCards(cards);
    }

    public virtual int CardsNeeded => cardsNeeded;
    public ulong CardsMask { 
      get => cardsMask;
      set
      {
        cardsMask = value;
        cardsNeeded = cardCount - Bits.BitCount(value);
      }
    }

    public BaseDeck Deck { get; set; }
    public long Wins { get; set; }
    public long Loses { get; set; }
    public long Ties { get; set; }
    public virtual decimal Percent => 100.0m * Math.Round((Wins + Ties / 2.0m) / (Wins + Ties + Loses), 6);
    public virtual int CompareTo(object obj)
    {
      return -Percent.CompareTo(((BaseHand)obj).Percent);
    }
    public bool CommunityCards { get; set; }
    public virtual string Name => "BaseHand";
    public virtual int CardCount => cardCount;
    public virtual string CardDescriptions => Deck.CardDescriptions(CardsMask);
    public virtual IEnumerable<int> CardNumbers => Deck.CardNumbers(CardsMask);

    public virtual (int, uint) Evaluate(ulong board)
    {
      return Evaluate(CardsMask, board);
    }

    public virtual (int, uint) Evaluate(ulong hero, ulong board)
    {
      // Default to Poker Evaluation of the Hand
      return Deck.PokerEvaluate(hero, board);
    }

    public virtual void Reset()
    {
      Wins = 0;
      Loses = 0;
      Ties = 0;
    }



    public virtual void SetCards(ulong cardsMask)
    {
      CardsMask = cardsMask;
      if (Bits.BitCount(cardsMask) > CardCount) throw new ArgumentException($"A {Name} hand must have {CardCount} cards or less.");
    }

    public virtual void SetCards(string cards)
    {
      CardsMask = CFHandEvaluator.Hand.ParseHand(cards ?? "");
      if (Bits.BitCount(CardsMask) > CardCount) throw new ArgumentException($"A {Name} hand must have {CardCount} cards or less.");
    }

    public virtual void AddCards(ulong cardsMask)
    {
      CardsMask |= cardsMask;
      if (Bits.BitCount(cardsMask) > CardCount) throw new ArgumentException($"A {Name} hand must have {CardCount} cards or less.");
    }

    public virtual void AddCards(string cards)
    {
      CardsMask |= CFHandEvaluator.Hand.ParseHand(cards ?? "");
      if (Bits.BitCount(CardsMask) > CardCount) throw new ArgumentException($"A {Name} hand must have {CardCount} cards or less.");
    }

    public virtual void CompleteCards(BaseDeck deck, Random rand = null)
    {
      deck.CompleteCards(this, rand);
    }

    public virtual long LayoutHand(double duration = 0.1)
    {
      return 0;
    }

    public virtual int PlayAgainst(ulong heroFiller, BaseHand[] opponents, ulong[] opsFillers, ulong boardMask)
    {
      var tied = false;
      uint villain = 0;
      (_, uint hero) = this.Evaluate(CardsMask | heroFiller, boardMask);
      for (var i = 0; i < opsFillers.Length; i++)
      {
        (_, villain) = this.Evaluate(opponents[i].CardsMask | opsFillers[i], boardMask);

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

    public virtual int PlayAgainst(BaseHand[] opponents, BaseHand board)
    {
      var dealtCards = CardsMask | board.CardsMask;
      for (var i = 0; i < opponents.Length; i++)
      {
        dealtCards |= opponents[i].CardsMask;
      }

      var rand = new Random();
      var deck = new StandardDeck(dealtCards);
      ulong heroFiller;
      ulong boardFiller;
      ulong[] opsFillers = new ulong[opponents.Length];

      // Get the random cards
      heroFiller = deck.DealCards(this.CardsNeeded, rand);
      boardFiller = deck.DealCards(board.CardsNeeded, rand);
      for (var i = 0; i < opponents.Length; i++)
      {
        opsFillers[i] = deck.DealCards(opponents[i].CardsNeeded, rand);
      }

      // Play the hand out
      return this.PlayAgainst(heroFiller, opponents, opsFillers, board.CardsMask | boardFiller);
    }

    public virtual long PlayAgainst(BaseHand[] opponents, BaseHand board, double duration)
    {
      var dealtCards = CardsMask | board.CardsMask;

      int oppCnt = opponents.Length;
      int[] opsNeeds = new int[oppCnt];

      for (var i = 0; i < oppCnt; i++)
      {
        dealtCards |= opponents[i].CardsMask;
        opsNeeds[i] = opponents[i].CardsNeeded;
      }

      var _global_random = new Random();

      int boardNeeds = board.CardsNeeded;
      ulong boardMask = board.CardsMask;
      int heroNeeds = this.CardsNeeded;
      ulong heroMask = this.CardsMask;

      long wins = 0;
      long loses = 0;
      long ties = 0;
      long end = Convert.ToInt64(Stopwatch.GetTimestamp() + (duration * Stopwatch.Frequency));
      Action trial = delegate
      {
        int seed;
        lock (_global_random) seed = _global_random.Next();
        var rand = new Random(seed);
        var deck = new StandardDeck(dealtCards);
        ulong heroFiller;
        ulong boardFiller;
        ulong[] opsFillers = new ulong[oppCnt];
        do
        {
          // Get the random cards
          heroFiller = deck.DealCards(heroNeeds, rand);
          boardFiller = deck.DealCards(boardNeeds, rand);
          for (var i = 0; i < oppCnt; i++)
          {
            opsFillers[i] = deck.DealCards(opsNeeds[i], rand);
          }

          // Play the hand out
          var result = this.PlayAgainst(heroFiller, opponents, opsFillers, boardMask | boardFiller);
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

      this.Wins = wins;
      this.Ties = ties;
      this.Loses = loses;

      return wins + ties + loses;
    }
    public long PlayAgainst(BaseHand oppHand, int numOpponents, BaseHand board, double duration)
    {
      var opponents = new BaseHand[numOpponents];
      for (var i = 0; i < numOpponents; i++)
      {
        opponents[i] = (i > 0 || oppHand == null) ? (BaseHand)Activator.CreateInstance(this.GetType()) : oppHand;
      }
      return PlayAgainst(opponents, board, duration);
    }

    public long PlayAgainst(int numOpponents, BaseHand board, double duration)
    {
      return PlayAgainst(null, numOpponents, board, duration);
    }

  }
}
