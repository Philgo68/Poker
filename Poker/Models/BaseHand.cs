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

namespace Poker.Models
{
  public class BaseHand : IComparable
  {
    public ulong CardsMask { get; set; }
    public BaseDeck Deck { get; set; }
    public int Wins { get; set; }
    public int Loses { get; set; }
    public int Ties { get; set; }
    public virtual double Percent => 100 * Math.Round((Wins + Ties / 2.0) / (Wins + Ties + Loses), 6);
    public virtual int CompareTo(object obj)
    {
      return -Percent.CompareTo(((BaseHand)obj).Percent);
    }

    public virtual string Name => "BaseHand";
    public virtual byte CardCount => 0;
    public virtual string CardDescriptions => Deck.CardDescriptions(CardsMask);
    public virtual IEnumerable<int> CardNumbers => Deck.CardNumbers(CardsMask);
    public virtual int CardsNeeded => CardCount - Bits.BitCount(CardsMask);
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

    public BaseHand()
    {
      Deck = new StandardDeck();
      Wins = 0;
      Loses = 0;
      Ties = 0;
    }

    public BaseHand(ulong cardsMask) : this()
    {
      SetCards(cardsMask);
    }

    public BaseHand(string cards) : this()
    {
      SetCards(cards);
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

    public virtual void CompleteCards(BaseDeck deck, Random rand = null)
    {
      deck.CompleteCards(this, rand);
    }

    public virtual int LayoutHand(double duration = 0.1) 
    {
      return 0;
    }

    public virtual int PlayAgainst(ulong heroFiller, ulong[] opsMasks, ulong boardMask)
    {
      var tied = false;
      uint villain = 0;
      (_, uint hero) = this.Evaluate(CardsMask | heroFiller, boardMask);
      for (var i = 0; i < opsMasks.Length; i++)
      {
        (_, villain) = this.Evaluate(opsMasks[i], boardMask);

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

    public virtual int PlayAgainst(BaseHand[] opponents, BaseHand board, double duration)
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

      int wins = 0;
      int loses = 0;
      int ties = 0;
      long end = Convert.ToInt64(Stopwatch.GetTimestamp() + (duration * Stopwatch.Frequency));
      Action trial = delegate
      {
        int seed;
        lock (_global_random) seed = _global_random.Next();
        var rand = new Random(seed);
        var deck = new StandardDeck(dealtCards);
        ulong heroFiller;
        ulong boardFiller;
        ulong[] opsMasks = new ulong[oppCnt];
        do
        {
          // Get the random cards
          heroFiller = deck.DealCards(heroNeeds, rand);
          boardFiller = deck.DealCards(boardNeeds, rand);
          for (var i = 0; i < oppCnt; i++)
          {
            opsMasks[i] = deck.DealCards(opsNeeds[i], rand) | opponents[i].CardsMask;
          }

          // Play the hand out
          var result = this.PlayAgainst(heroFiller, opsMasks, boardMask | boardFiller); 
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
    public int PlayAgainst(BaseHand oppHand, int numOpponents, BaseHand board, double duration)
    {
      var opponents = new BaseHand[numOpponents];
      for (var i = 0; i < numOpponents; i++)
      {
        opponents[i] = (i > 0 || oppHand == null) ? (BaseHand)Activator.CreateInstance(this.GetType()) : oppHand;
      }
      return PlayAgainst(opponents, board, duration);
    }

    public int PlayAgainst(int numOpponents, BaseHand board, double duration)
    {
      return PlayAgainst(null, numOpponents, board, duration);
    }

  }
}
