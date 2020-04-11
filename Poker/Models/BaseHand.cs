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
    private ulong? initialMask { get; set; }
    public BaseDeck Deck { get; set; }
    public int RunningScore { get; set; }
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
    public virtual string Description => Deck.CardDescriptions(CardsMask);

    public virtual (int, uint) Evaluate(ulong board, ulong filler = 0x0UL) => (0, 0);

    public void Reset()
    {
      RunningScore = 0;
      Wins = 0;
      Loses = 0;
      Ties = 0;
    }

    public virtual (int, uint) PokerEvaluate(ulong board, ulong filler = 0x0UL)
    {
      return Deck.PokerEvaluate(CardsMask | filler, board);
    }

    public virtual (int, uint) OmahaEvaluate(ulong board, ulong filler = 0x0UL)
    {
      return Deck.OmahaEvaluate(CardsMask | filler, board);
    }


    public BaseHand()
    {
      Deck = new StandardDeck();
      RunningScore = 0;
      Wins = 0;
      Loses = 0;
      Ties = 0;

      initialMask ??= CardsMask;
      CardsMask = initialMask.Value;
      initialMask = null;
    }

    public BaseHand(ulong cardsMask) : this()
    {
      if (Bits.BitCount(cardsMask) > CardCount) throw new ArgumentException($"A {Name} hand must have {CardCount} cards or less.");
      CardsMask = cardsMask;
    }

    public BaseHand(string cards) : this(CFHandEvaluator.Hand.ParseHand(cards ?? ""))
    {
    }

    public void StartTest()
    {
      initialMask ??= CardsMask;
      CardsMask = initialMask.Value;
    }

    public double PlayAgainstOld(BaseHand[] opponents, BaseHand board, double duration)
    {
      var dealtCards = CardsMask | board.CardsMask;
      foreach (var hand in opponents) { dealtCards |= hand.CardsMask; }

      var _global_random = new Random();

      var threadHeroHand = new ThreadLocal<BaseHand>(() => { return (BaseHand)this.MemberwiseClone(); }, true);
      var threadBoard = new ThreadLocal<BaseHand>(() => { return (BaseHand)board.MemberwiseClone(); });

      int cnt = 0;

      long end = Convert.ToInt64(Stopwatch.GetTimestamp() + (duration * Stopwatch.Frequency));
      long now;
      Action trial = delegate
      {
        int seed;
        lock (_global_random) seed = _global_random.Next();
        var rand = new Random(seed);

        var heroHand = threadHeroHand.Value;
        var board = threadBoard.Value;

        var deck = new StandardDeck(dealtCards);
        do
        {
          uint hero = 0, villain = 0;
          bool tied = false;

          deck.TestCards(heroHand, rand);
          deck.TestCards(board, rand);
          foreach (var oh in opponents)
          {
            var extraCards = deck.DealCards(oh.CardCount - Bits.BitCount(oh.CardsMask));

            (_, hero) = this.Evaluate(board.CardsMask);
            (_, villain) = oh.Evaluate(board.CardsMask | extraCards);
            if (villain > hero) { break; }
            if (villain == hero) { tied = true; }
          }
          if (villain > hero)
          {
            heroHand.Loses++;
          }
          else if (tied)
          {
            heroHand.Ties++;
          }
          else
          {
            heroHand.Wins++;
          }
          cnt++;
          deck.Reset(dealtCards);
          now = Stopwatch.GetTimestamp();
        } while (now < end);
      };

      var tasks = new List<Task>();
      for (int ctr = 1; ctr <= 1; ctr++)
        tasks.Add(Task.Factory.StartNew(trial));
      Task.WaitAll(tasks.ToArray());

      foreach (var hand in threadHeroHand.Values)
      {
        this.Wins += hand.Wins;
        this.Ties += hand.Ties;
        this.Loses += hand.Loses;
      }

      return this.Percent;
    }


    public double PlayAgainst(BaseHand[] opponents, BaseHand board, double duration)
    {
      var dealtCards = CardsMask | board.CardsMask;

      int oppCnt = opponents.Length;
      int[] opsNeed = new int[oppCnt];
      for (var i = 0; i < oppCnt; i++)
      {
        dealtCards |= opponents[i].CardsMask;
        opsNeed[i] = opponents[i].CardCount - Bits.BitCount(opponents[i].CardsMask);
      }

      var _global_random = new Random();

      int boardNeeds = board.CardCount - Bits.BitCount(board.CardsMask);
      ulong boardMask = board.CardsMask;
      int heroNeeds = this.CardCount - Bits.BitCount(this.CardsMask);
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
        uint hero = 0, villain = 0;
        ulong opFiller = 0x0UL;
        bool tied;
        ulong heroFiller;
        ulong boardFiller;
        do
        {
          tied = false;
          heroFiller = deck.DealCards(heroNeeds, rand);
          boardFiller = deck.DealCards(boardNeeds, rand);
          for (var i = 0; i < oppCnt; i++)
          {
            opFiller = deck.DealCards(opsNeed[i], rand);

            (_, hero) = this.Evaluate(boardMask | boardFiller, heroFiller);
            (_, villain) = opponents[i].Evaluate(boardMask | boardFiller, opFiller);

            if (villain > hero) { break; }
            if (villain == hero) { tied = true; }
          }
          if (villain > hero)
          {
            loses++;
          }
          else if (tied)
          {
            ties++;
          }
          else
          {
            wins++;
          }
          deck.Reset(dealtCards);
        } while (Stopwatch.GetTimestamp() < end);
      };

      var tasks = new List<Task>();
      for (int ctr = 1; ctr <= 1; ctr++)
        tasks.Add(Task.Factory.StartNew(trial));
      Task.WaitAll(tasks.ToArray());

      this.Wins = wins;
      this.Ties = ties;
      this.Loses = loses;

      return this.Percent;
    }

    public double PlayAgainst(int numOpponents, BaseHand board, double duration)
    {
      var opponents = new BaseHand[numOpponents];
      for (var i = 0; i < numOpponents; i++)
      {
        opponents[i] = (BaseHand)Activator.CreateInstance(this.GetType());
      }
      return PlayAgainst(opponents, board, duration);
    }

  }
}
