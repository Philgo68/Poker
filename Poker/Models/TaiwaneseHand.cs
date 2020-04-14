using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Poker.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using YYClass;

namespace Poker.Models
{
  public class TaiwaneseHand : BaseHand
  {
    private OneCardHand topHand;
    private HoldemHand middleHand;
    private OmahaHand bottomHand;
    private bool HandsLaidOut => (TopHand != null && MiddleHand != null && BottomHand != null);

    public override string Name => "Taiwanese";
    public override byte CardCount => 7;
    public override string CardDescriptions => $"{TopHand?.CardDescriptions ?? "x"} / {MiddleHand?.CardDescriptions ?? "xx"} / {BottomHand?.CardDescriptions ?? "xxxx"}";
    public int RunningScore { get; set; }
    public OneCardHand TopHand { get => topHand; set { topHand = value; CardsMask = (TopHand?.CardsMask ?? 0) | (MiddleHand?.CardsMask ?? 0) | (BottomHand?.CardsMask ?? 0); } }
    public HoldemHand MiddleHand { get => middleHand; set { middleHand = value; CardsMask = (TopHand?.CardsMask ?? 0) | (MiddleHand?.CardsMask ?? 0) | (BottomHand?.CardsMask ?? 0); } }
    public OmahaHand BottomHand { get => bottomHand; set { bottomHand = value; CardsMask = (TopHand?.CardsMask ?? 0) | (MiddleHand?.CardsMask ?? 0) | (BottomHand?.CardsMask ?? 0); } }

    public override int CompareTo(object obj)
    {
      return -RunningScore.CompareTo(((TaiwaneseHand)obj).RunningScore);
    }

    public TaiwaneseHand() : base() 
    {
      RunningScore = 0;
    }

    public TaiwaneseHand(ulong cardsMask) : this()
    {
      SetCards(cardsMask);
    }

    public TaiwaneseHand(string cards) : base()
    {
      SetCards(cards);
    }

    public override void SetCards(string cards)
    {
      var hhs = (cards ?? "").Split("/");
      if (hhs.Length == 1) base.SetCards(hhs[0]);
      if (hhs.Length >= 2)
      {
        TopHand = new OneCardHand(hhs[0]);
        MiddleHand = new HoldemHand(hhs[1]);
      }
      if (hhs.Length >= 3) BottomHand = new OmahaHand(hhs[2]);
    }

    public TaiwaneseHand(OneCardHand topHand, HoldemHand middleHand, OmahaHand bottomHand) : this()
    {
      TopHand = topHand;
      MiddleHand = middleHand;
      BottomHand = bottomHand;
    }
    public override void Reset()
    {
      base.Reset();
      RunningScore = 0;
    }
    public override void CompleteCards(BaseDeck deck, Random rand = null)
    {
      if (!HandsLaidOut)
      {
        // Fill in the 7 dealt cards
        deck.CompleteCards(this, rand);
      }
      else
      {
        // Complete each sub-hand individually
        deck.CompleteCards(this.TopHand, rand);
        deck.CompleteCards(this.MiddleHand, rand);
        deck.CompleteCards(this.BottomHand, rand);
      }
    }

    public override long LayoutHand(double duration = 0.1)
    {
      long cnt = 0;
      if (!HandsLaidOut)
      {
        var rand = new Random();
        long end = Convert.ToInt64(Stopwatch.GetTimestamp() + (duration * Stopwatch.Frequency));
        var possibleLayouts = AllLayouts();
        Action testboards = delegate
        {
          long now;
          int seed;
          lock (rand) seed = rand.Next();
          var threadRand = new Random(seed);
          // Layout is done with no knowledge of board or opponents cards, so just eliminate my cards from the deck
          var deck = new StandardDeck(CardsMask);
          do
          {
            cnt++;
            var boardmask = deck.PeekCards(5, threadRand);
            foreach (var hand in possibleLayouts)
            {
              hand.ScoreOnBoard(boardmask);
            }
            now = Stopwatch.GetTimestamp();
          } while (now < end);
        };

        var tasks = new List<Task>();
        for (int ctr = 1; ctr <= Environment.ProcessorCount; ctr++)
          tasks.Add(Task.Factory.StartNew(testboards));
        Task.WaitAll(tasks.ToArray());

        Array.Sort(possibleLayouts);
        TopHand = possibleLayouts[0].TopHand;
        MiddleHand = possibleLayouts[0].MiddleHand;
        BottomHand = possibleLayouts[0].BottomHand;
      }
      return cnt;
    }


    public int PointTopHand(int handType)
    {
      return handType switch
      {
        2 => 1,
        3 => 2,
        4 => 3,
        5 => 3,
        6 => 4,
        7 => 6,
        8 => 12,
        _ => 0
      };
    }

    public int PointMiddleHand(int handType)
    {
      return handType switch
      {
        2 => 0,
        3 => 1,
        4 => 2,
        5 => 2,
        6 => 3,
        7 => 5,
        8 => 10,
        _ => 0
      };
    }

    public int PointBottomHand(int handType)
    {
      return handType switch
      {
        2 => 0,
        3 => 0,
        4 => 0,
        5 => 0,
        6 => 2,
        7 => 4,
        8 => 8,
        _ => 0
      };
    }

    public int ScoreTopHand(ulong boardmask)
    {
      (var handType, _) = TopHand.Evaluate(boardmask);
      return PointTopHand(handType);
    }

    public int ScoreMiddleHand(ulong boardmask)
    {
      (var handType, _) = MiddleHand.Evaluate(boardmask);
      return PointMiddleHand(handType);
    }

    public int ScoreBottomHand(ulong boardmask)
    {
      (var handType, _) = BottomHand.Evaluate(boardmask);
      return PointBottomHand(handType);
    }

    public void ScoreOnBoard(ulong boardmask)
    {
      RunningScore += ScoreTopHand(boardmask) + ScoreMiddleHand(boardmask) + ScoreBottomHand(boardmask);
    }

    public override int PlayAgainst(ulong heroFiller, BaseHand[] opponents, ulong[] opsFillers, ulong boardMask)
    {
      int winCnt = 0;
      int wins = 0, loses = 0;

      // Top hands
      (int heroType, uint hero) = TopHand.Evaluate(TopHand.CardsMask | heroFiller, boardMask);

      // Must check them all to get the best opponent hand
      var rankTies = 0;
      var typeTies = 0;
      (int villianType, uint villain) = TopHand.Evaluate(((TaiwaneseHand)opponents[0]).TopHand.CardsMask | opsFillers[0], boardMask);
      for (var i = 1; i < opponents.Length; i++)
      {
        (int checkType, uint check) = TopHand.Evaluate(((TaiwaneseHand)opponents[i]).TopHand.CardsMask | opsFillers[i], boardMask);
        if (check > villain)
        {
          rankTies = 0;
          if (checkType > villianType) typeTies = 0;
          (villianType, villain) = (checkType, check);
        }
        else if (check == villain)
        {
          rankTies++;
          typeTies++;
        }
        else if (checkType == villianType) typeTies++;
      }

      switch (hero.CompareTo(villain))
      {
        case 1:
          wins += 1 * opponents.Length;  // one from all the beaten players
          wins += PointTopHand(heroType) * (opponents.Length - ((heroType > villianType) ? 0 : typeTies));  // Bonus from all players (subtracting all those with the same type of hand
          winCnt++;
          break;
        case -1:
          loses += 1 + ((villianType > heroType) ? PointTopHand(villianType) : 0); // one to the villain's pot plus the villain's bonus if it's a better type
          winCnt--;
          break;
        default:
          break;
      }

      // Middle hands
      (heroType, hero) = MiddleHand.Evaluate(MiddleHand.CardsMask | heroFiller, boardMask);

      // Must check them all to get the best opponent hand
      rankTies = 0;
      typeTies = 0;
      (villianType, villain) = MiddleHand.Evaluate(((TaiwaneseHand)opponents[0]).MiddleHand.CardsMask | opsFillers[0], boardMask);
      for (var i = 1; i < opponents.Length; i++)
      {
        (int checkType, uint check) = MiddleHand.Evaluate(((TaiwaneseHand)opponents[i]).MiddleHand.CardsMask | opsFillers[i], boardMask);
        if (check > villain)
        {
          rankTies = 0;
          if (checkType > villianType) typeTies = 0;
          (villianType, villain) = (checkType, check);
        }
        else if (check == villain)
        {
          rankTies++;
          typeTies++;
        }
        else if (checkType == villianType) typeTies++;
      }

      switch (hero.CompareTo(villain))
      {
        case 1:
          wins += 2 * opponents.Length;  // 2 from all the beaten players
          wins += PointMiddleHand(heroType) * (opponents.Length - ((heroType > villianType) ? 0 : typeTies));  // Bonus from all players (subtracting all those with the same type of hand
          winCnt++;
          break;
        case -1:
          loses += 2 + ((villianType > heroType) ? PointMiddleHand(villianType) : 0); // 2 to the villain's pot plus the villain's bonus if it's a better type
          winCnt--;
          break;
        default:
          break;
      }

      // Bottom hands
      (heroType, hero) = BottomHand.Evaluate(BottomHand.CardsMask | heroFiller, boardMask);

      // Must check them all to get the best opponent hand
      rankTies = 0;
      typeTies = 0;
      (villianType, villain) = BottomHand.Evaluate(((TaiwaneseHand)opponents[0]).BottomHand.CardsMask | opsFillers[0], boardMask);
      for (var i = 1; i < opponents.Length; i++)
      {
        (int checkType, uint check) = BottomHand.Evaluate(((TaiwaneseHand)opponents[i]).BottomHand.CardsMask | opsFillers[i], boardMask);
        if (check > villain)
        {
          rankTies = 0;
          if (checkType > villianType) typeTies = 0;
          (villianType, villain) = (checkType, check);
        }
        else if (check == villain)
        {
          rankTies++;
          typeTies++;
        }
        else if (checkType == villianType) typeTies++;
      }

      switch (hero.CompareTo(villain))
      {
        case 1:
          wins += 3 * opponents.Length;  // 3 from all the beaten players
          wins += PointBottomHand(heroType) * (opponents.Length - ((heroType > villianType) ? 0 : typeTies));  // Bonus from al players (subtracting all those with the same type of hand
          winCnt++;
          break;
        case -1:
          loses += 3 + ((villianType > heroType) ? PointBottomHand(villianType) : 0); // 3 to the villain's pot plus the villain's bonus if it's a better type
          winCnt--;
          break;
        default:
          break;
      }

      if (winCnt == 3) 
        wins += 3 * opponents.Length;  // 3 from all the beaten players
      
      // return net gain
      return wins - loses;
    }

    public int PlayAgainstOld(TaiwaneseHand otherHand, BaseHand board, double duration)
    {
      // Play top hands
      int winCnt = 0;
      var boardmask = board.CardsMask;

      (var myType, var myRank) = TopHand.Evaluate(boardmask);
      (var oType, var oRank) = otherHand.TopHand.Evaluate(boardmask);
      switch (myRank.CompareTo(oRank))
      {
        case 1:
          Wins += 1 + ((myType == oType) ? 0 : PointTopHand(myType));
          winCnt++;
          break;
        case -1:
          Loses += 1 + ((myType == oType) ? 0 : PointTopHand(oType));
          winCnt--;
          break;
        default:
          break;
      };

      // Play middle hands
      (myType, myRank) = MiddleHand.Evaluate(boardmask);
      (oType, oRank) = otherHand.MiddleHand.Evaluate(boardmask);
      switch (myRank.CompareTo(oRank))
      {
        case 1:
          Wins += 2 + ((myType == oType) ? 0 : PointMiddleHand(myType));
          winCnt++;
          break;
        case -1:
          Loses += 2 + ((myType == oType) ? 0 : PointMiddleHand(oType));
          winCnt--;
          break;
        default:
          break;
      };

      // Play bottom hands
      (myType, myRank) = BottomHand.Evaluate(boardmask);
      (oType, oRank) = otherHand.BottomHand.Evaluate(boardmask);
      switch (myRank.CompareTo(oRank))
      {
        case 1:
          Wins += 3 + ((myType == oType) ? 0 : PointBottomHand(myType)) + ((winCnt == 2) ? 3 : 0);
          break;
        case -1:
          Loses += 3 + ((myType == oType) ? 0 : PointBottomHand(oType)) + ((winCnt == -2) ? 3 : 0);
          break;
        default:
          break;
      };

      return 0;

    }

    public TaiwaneseHand[] AllLayouts()
    {
      var cards = Bits.IndividualMasks(this.CardsMask);
      return new TaiwaneseHand[105]
      {
        new TaiwaneseHand(new OneCardHand(cards[0]), new HoldemHand(cards[1] | cards[2]), new OmahaHand(cards[3] | cards[4] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[0]), new HoldemHand(cards[1] | cards[3]), new OmahaHand(cards[2] | cards[4] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[0]), new HoldemHand(cards[1] | cards[4]), new OmahaHand(cards[2] | cards[3] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[0]), new HoldemHand(cards[1] | cards[5]), new OmahaHand(cards[2] | cards[3] | cards[4] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[0]), new HoldemHand(cards[1] | cards[6]), new OmahaHand(cards[2] | cards[3] | cards[4] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[0]), new HoldemHand(cards[2] | cards[3]), new OmahaHand(cards[1] | cards[4] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[0]), new HoldemHand(cards[2] | cards[4]), new OmahaHand(cards[1] | cards[3] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[0]), new HoldemHand(cards[2] | cards[5]), new OmahaHand(cards[1] | cards[3] | cards[4] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[0]), new HoldemHand(cards[2] | cards[6]), new OmahaHand(cards[1] | cards[3] | cards[4] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[0]), new HoldemHand(cards[3] | cards[4]), new OmahaHand(cards[1] | cards[2] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[0]), new HoldemHand(cards[3] | cards[5]), new OmahaHand(cards[1] | cards[2] | cards[4] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[0]), new HoldemHand(cards[3] | cards[6]), new OmahaHand(cards[1] | cards[2] | cards[4] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[0]), new HoldemHand(cards[4] | cards[5]), new OmahaHand(cards[1] | cards[2] | cards[3] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[0]), new HoldemHand(cards[4] | cards[6]), new OmahaHand(cards[1] | cards[2] | cards[3] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[0]), new HoldemHand(cards[5] | cards[6]), new OmahaHand(cards[1] | cards[2] | cards[3] | cards[4])),

        new TaiwaneseHand(new OneCardHand(cards[1]), new HoldemHand(cards[0] | cards[2]), new OmahaHand(cards[3] | cards[4] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[1]), new HoldemHand(cards[0] | cards[3]), new OmahaHand(cards[2] | cards[4] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[1]), new HoldemHand(cards[0] | cards[4]), new OmahaHand(cards[2] | cards[3] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[1]), new HoldemHand(cards[0] | cards[5]), new OmahaHand(cards[2] | cards[3] | cards[4] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[1]), new HoldemHand(cards[0] | cards[6]), new OmahaHand(cards[2] | cards[3] | cards[4] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[1]), new HoldemHand(cards[2] | cards[3]), new OmahaHand(cards[0] | cards[4] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[1]), new HoldemHand(cards[2] | cards[4]), new OmahaHand(cards[0] | cards[3] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[1]), new HoldemHand(cards[2] | cards[5]), new OmahaHand(cards[0] | cards[3] | cards[4] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[1]), new HoldemHand(cards[2] | cards[6]), new OmahaHand(cards[0] | cards[3] | cards[4] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[1]), new HoldemHand(cards[3] | cards[4]), new OmahaHand(cards[0] | cards[2] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[1]), new HoldemHand(cards[3] | cards[5]), new OmahaHand(cards[0] | cards[2] | cards[4] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[1]), new HoldemHand(cards[3] | cards[6]), new OmahaHand(cards[0] | cards[2] | cards[4] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[1]), new HoldemHand(cards[4] | cards[5]), new OmahaHand(cards[0] | cards[2] | cards[3] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[1]), new HoldemHand(cards[4] | cards[6]), new OmahaHand(cards[0] | cards[2] | cards[3] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[1]), new HoldemHand(cards[5] | cards[6]), new OmahaHand(cards[0] | cards[2] | cards[3] | cards[4])),

        new TaiwaneseHand(new OneCardHand(cards[2]), new HoldemHand(cards[1] | cards[0]), new OmahaHand(cards[3] | cards[4] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[2]), new HoldemHand(cards[1] | cards[3]), new OmahaHand(cards[0] | cards[4] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[2]), new HoldemHand(cards[1] | cards[4]), new OmahaHand(cards[0] | cards[3] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[2]), new HoldemHand(cards[1] | cards[5]), new OmahaHand(cards[0] | cards[3] | cards[4] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[2]), new HoldemHand(cards[1] | cards[6]), new OmahaHand(cards[0] | cards[3] | cards[4] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[2]), new HoldemHand(cards[0] | cards[3]), new OmahaHand(cards[1] | cards[4] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[2]), new HoldemHand(cards[0] | cards[4]), new OmahaHand(cards[1] | cards[3] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[2]), new HoldemHand(cards[0] | cards[5]), new OmahaHand(cards[1] | cards[3] | cards[4] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[2]), new HoldemHand(cards[0] | cards[6]), new OmahaHand(cards[1] | cards[3] | cards[4] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[2]), new HoldemHand(cards[3] | cards[4]), new OmahaHand(cards[1] | cards[0] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[2]), new HoldemHand(cards[3] | cards[5]), new OmahaHand(cards[1] | cards[0] | cards[4] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[2]), new HoldemHand(cards[3] | cards[6]), new OmahaHand(cards[1] | cards[0] | cards[4] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[2]), new HoldemHand(cards[4] | cards[5]), new OmahaHand(cards[1] | cards[0] | cards[3] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[2]), new HoldemHand(cards[4] | cards[6]), new OmahaHand(cards[1] | cards[0] | cards[3] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[2]), new HoldemHand(cards[5] | cards[6]), new OmahaHand(cards[1] | cards[0] | cards[3] | cards[4])),

        new TaiwaneseHand(new OneCardHand(cards[3]), new HoldemHand(cards[1] | cards[2]), new OmahaHand(cards[0] | cards[4] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[3]), new HoldemHand(cards[1] | cards[0]), new OmahaHand(cards[2] | cards[4] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[3]), new HoldemHand(cards[1] | cards[4]), new OmahaHand(cards[2] | cards[0] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[3]), new HoldemHand(cards[1] | cards[5]), new OmahaHand(cards[2] | cards[0] | cards[4] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[3]), new HoldemHand(cards[1] | cards[6]), new OmahaHand(cards[2] | cards[0] | cards[4] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[3]), new HoldemHand(cards[2] | cards[0]), new OmahaHand(cards[1] | cards[4] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[3]), new HoldemHand(cards[2] | cards[4]), new OmahaHand(cards[1] | cards[0] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[3]), new HoldemHand(cards[2] | cards[5]), new OmahaHand(cards[1] | cards[0] | cards[4] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[3]), new HoldemHand(cards[2] | cards[6]), new OmahaHand(cards[1] | cards[0] | cards[4] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[3]), new HoldemHand(cards[0] | cards[4]), new OmahaHand(cards[1] | cards[2] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[3]), new HoldemHand(cards[0] | cards[5]), new OmahaHand(cards[1] | cards[2] | cards[4] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[3]), new HoldemHand(cards[0] | cards[6]), new OmahaHand(cards[1] | cards[2] | cards[4] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[3]), new HoldemHand(cards[4] | cards[5]), new OmahaHand(cards[1] | cards[2] | cards[0] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[3]), new HoldemHand(cards[4] | cards[6]), new OmahaHand(cards[1] | cards[2] | cards[0] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[3]), new HoldemHand(cards[5] | cards[6]), new OmahaHand(cards[1] | cards[2] | cards[0] | cards[4])),

        new TaiwaneseHand(new OneCardHand(cards[4]), new HoldemHand(cards[1] | cards[2]), new OmahaHand(cards[3] | cards[0] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[4]), new HoldemHand(cards[1] | cards[3]), new OmahaHand(cards[2] | cards[0] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[4]), new HoldemHand(cards[1] | cards[0]), new OmahaHand(cards[2] | cards[3] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[4]), new HoldemHand(cards[1] | cards[5]), new OmahaHand(cards[2] | cards[3] | cards[0] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[4]), new HoldemHand(cards[1] | cards[6]), new OmahaHand(cards[2] | cards[3] | cards[0] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[4]), new HoldemHand(cards[2] | cards[3]), new OmahaHand(cards[1] | cards[0] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[4]), new HoldemHand(cards[2] | cards[0]), new OmahaHand(cards[1] | cards[3] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[4]), new HoldemHand(cards[2] | cards[5]), new OmahaHand(cards[1] | cards[3] | cards[0] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[4]), new HoldemHand(cards[2] | cards[6]), new OmahaHand(cards[1] | cards[3] | cards[0] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[4]), new HoldemHand(cards[3] | cards[0]), new OmahaHand(cards[1] | cards[2] | cards[5] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[4]), new HoldemHand(cards[3] | cards[5]), new OmahaHand(cards[1] | cards[2] | cards[0] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[4]), new HoldemHand(cards[3] | cards[6]), new OmahaHand(cards[1] | cards[2] | cards[0] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[4]), new HoldemHand(cards[0] | cards[5]), new OmahaHand(cards[1] | cards[2] | cards[3] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[4]), new HoldemHand(cards[0] | cards[6]), new OmahaHand(cards[1] | cards[2] | cards[3] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[4]), new HoldemHand(cards[5] | cards[6]), new OmahaHand(cards[1] | cards[2] | cards[3] | cards[0])),

        new TaiwaneseHand(new OneCardHand(cards[5]), new HoldemHand(cards[1] | cards[2]), new OmahaHand(cards[3] | cards[4] | cards[0] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[5]), new HoldemHand(cards[1] | cards[3]), new OmahaHand(cards[2] | cards[4] | cards[0] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[5]), new HoldemHand(cards[1] | cards[4]), new OmahaHand(cards[2] | cards[3] | cards[0] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[5]), new HoldemHand(cards[1] | cards[0]), new OmahaHand(cards[2] | cards[3] | cards[4] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[5]), new HoldemHand(cards[1] | cards[6]), new OmahaHand(cards[2] | cards[3] | cards[4] | cards[0])),
        new TaiwaneseHand(new OneCardHand(cards[5]), new HoldemHand(cards[2] | cards[3]), new OmahaHand(cards[1] | cards[4] | cards[0] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[5]), new HoldemHand(cards[2] | cards[4]), new OmahaHand(cards[1] | cards[3] | cards[0] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[5]), new HoldemHand(cards[2] | cards[0]), new OmahaHand(cards[1] | cards[3] | cards[4] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[5]), new HoldemHand(cards[2] | cards[6]), new OmahaHand(cards[1] | cards[3] | cards[4] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[5]), new HoldemHand(cards[3] | cards[4]), new OmahaHand(cards[1] | cards[2] | cards[0] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[5]), new HoldemHand(cards[3] | cards[0]), new OmahaHand(cards[1] | cards[2] | cards[4] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[5]), new HoldemHand(cards[3] | cards[6]), new OmahaHand(cards[1] | cards[2] | cards[4] | cards[0])),
        new TaiwaneseHand(new OneCardHand(cards[5]), new HoldemHand(cards[4] | cards[0]), new OmahaHand(cards[1] | cards[2] | cards[3] | cards[6])),
        new TaiwaneseHand(new OneCardHand(cards[5]), new HoldemHand(cards[4] | cards[6]), new OmahaHand(cards[1] | cards[2] | cards[3] | cards[0])),
        new TaiwaneseHand(new OneCardHand(cards[5]), new HoldemHand(cards[0] | cards[6]), new OmahaHand(cards[1] | cards[2] | cards[3] | cards[4])),

        new TaiwaneseHand(new OneCardHand(cards[6]), new HoldemHand(cards[1] | cards[2]), new OmahaHand(cards[3] | cards[4] | cards[5] | cards[0])),
        new TaiwaneseHand(new OneCardHand(cards[6]), new HoldemHand(cards[1] | cards[3]), new OmahaHand(cards[2] | cards[4] | cards[5] | cards[0])),
        new TaiwaneseHand(new OneCardHand(cards[6]), new HoldemHand(cards[1] | cards[4]), new OmahaHand(cards[2] | cards[3] | cards[5] | cards[0])),
        new TaiwaneseHand(new OneCardHand(cards[6]), new HoldemHand(cards[1] | cards[5]), new OmahaHand(cards[2] | cards[3] | cards[4] | cards[0])),
        new TaiwaneseHand(new OneCardHand(cards[6]), new HoldemHand(cards[1] | cards[0]), new OmahaHand(cards[2] | cards[3] | cards[4] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[6]), new HoldemHand(cards[2] | cards[3]), new OmahaHand(cards[1] | cards[4] | cards[5] | cards[0])),
        new TaiwaneseHand(new OneCardHand(cards[6]), new HoldemHand(cards[2] | cards[4]), new OmahaHand(cards[1] | cards[3] | cards[5] | cards[0])),
        new TaiwaneseHand(new OneCardHand(cards[6]), new HoldemHand(cards[2] | cards[5]), new OmahaHand(cards[1] | cards[3] | cards[4] | cards[0])),
        new TaiwaneseHand(new OneCardHand(cards[6]), new HoldemHand(cards[2] | cards[0]), new OmahaHand(cards[1] | cards[3] | cards[4] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[6]), new HoldemHand(cards[3] | cards[4]), new OmahaHand(cards[1] | cards[2] | cards[5] | cards[0])),
        new TaiwaneseHand(new OneCardHand(cards[6]), new HoldemHand(cards[3] | cards[5]), new OmahaHand(cards[1] | cards[2] | cards[4] | cards[0])),
        new TaiwaneseHand(new OneCardHand(cards[6]), new HoldemHand(cards[3] | cards[0]), new OmahaHand(cards[1] | cards[2] | cards[4] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[6]), new HoldemHand(cards[4] | cards[5]), new OmahaHand(cards[1] | cards[2] | cards[3] | cards[0])),
        new TaiwaneseHand(new OneCardHand(cards[6]), new HoldemHand(cards[4] | cards[0]), new OmahaHand(cards[1] | cards[2] | cards[3] | cards[5])),
        new TaiwaneseHand(new OneCardHand(cards[6]), new HoldemHand(cards[5] | cards[0]), new OmahaHand(cards[1] | cards[2] | cards[3] | cards[4])),
      };
    }

  }
}
