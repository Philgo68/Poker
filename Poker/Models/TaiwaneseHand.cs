using Poker.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Poker.Models
{
  public class TaiwaneseHand : BaseHand
  {
    private BaseHand topHand;
    private BaseHand middleHand;
    private BaseHand bottomHand;
    private bool manualLayoutInProgress;

    public override bool HandsLaidOut => (!manualLayoutInProgress && TopHand != null && MiddleHand != null && BottomHand != null);

    public override string Name => "Taiwanese";
    public TaiwaneseHand() : base(7)
    {
      manualLayoutInProgress = false;
    }
    public int RunningScore { get; set; }
    public BaseHand TopHand { get => manualLayoutInProgress ? null : topHand; set { topHand = value; CardsMask = (topHand?.CardsMask ?? 0) | (middleHand?.CardsMask ?? 0) | (bottomHand?.CardsMask ?? 0); } }
    public BaseHand MiddleHand { get => manualLayoutInProgress ? null : middleHand; set { middleHand = value; CardsMask = (topHand?.CardsMask ?? 0) | (middleHand?.CardsMask ?? 0) | (bottomHand?.CardsMask ?? 0); } }
    public BaseHand BottomHand { get => manualLayoutInProgress ? null : bottomHand; set { bottomHand = value; CardsMask = (topHand?.CardsMask ?? 0) | (middleHand?.CardsMask ?? 0) | (bottomHand?.CardsMask ?? 0); } }

    public override bool Revealed { get => revealed; set {
        revealed = value;
        if (TopHand != null) TopHand.Revealed = value;
        if (MiddleHand != null) MiddleHand.Revealed = value;
        if (BottomHand != null) BottomHand.Revealed = value;
      } 
    }

    public override int CompareTo(object obj)
    {
      return -RunningScore.CompareTo(((TaiwaneseHand)obj).RunningScore);
    }

    public override string CardDescriptions => $"{Deck.CardDescriptions(TopHand.CardsMask)}  -  {Deck.CardDescriptions(MiddleHand.CardsMask)}  -  {Deck.CardDescriptions(BottomHand.CardsMask)}";

    public TaiwaneseHand(ulong cardsMask) : this()
    {
      SetCards(cardsMask);
    }

    public TaiwaneseHand(string cards) : this()
    {
      SetCards(cards);
    }
    public TaiwaneseHand(BaseHand topHand, BaseHand middleHand, BaseHand bottomHand) : this()
    {
      TopHand = topHand;
      MiddleHand = middleHand;
      BottomHand = bottomHand;
    }

    public override void SetCards(string cards)
    {
      var hhs = (cards ?? "").Split("/");
      if (hhs.Length == 1) base.SetCards(hhs[0]);
      if (hhs.Length >= 2)
      {
        TopHand = new BaseHand(hhs[0], 1);
        MiddleHand = new BaseHand(hhs[1], 2);
      }
      if (hhs.Length >= 3) BottomHand = new BaseHand(hhs[2], 4);
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

    public (BaseHand, BaseHand, BaseHand) StartManualLayout()
    {
      manualLayoutInProgress = true;
      var cards = Bits.IndividualMasks(this.CardsMask);

      Random rnd = new Random();
      cards = cards.OrderBy(x => rnd.Next()).ToArray();

      TopHand = new BaseHand(cards[0], 1);
      MiddleHand = new BaseHand(cards[1] | cards[2], 2);
      BottomHand = new BaseHand(cards[3] | cards[4] | cards[5] | cards[6], 4);
      return (topHand, middleHand, bottomHand);
    }

    public void CompleteManualLayout()
    {
      manualLayoutInProgress = false;
      StateHasChanged();
    }

    public override long LayoutHand(double duration = 0.2)
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

      StateHasChanged();

      return cnt;
    }




    public int ScoreTopHand(ulong boardmask)
    {
      (var handType, _) = TexasHoldem.EvaluateHand(TopHand.CardsMask | boardmask);
      return Taiwanese.PointTopHand(handType);
    }

    public int ScoreMiddleHand(ulong boardmask)
    {
      (var handType, _) = TexasHoldem.EvaluateHand(MiddleHand.CardsMask | boardmask);
      return Taiwanese.PointMiddleHand(handType);
    }

    public int ScoreBottomHand(ulong boardmask)
    {
      (var handType, _) = Omaha.EvaluateHand(BottomHand.CardsMask, boardmask);
      return Taiwanese.PointBottomHand(handType);
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
      (int heroType, uint hero) = PokerGame.EvaluateHand(TopHand.CardsMask | heroFiller | boardMask);

      // Must check them all to get the best opponent hand
      var rankTies = 0;
      var typeTies = 0;
      (int villianType, uint villain) = PokerGame.EvaluateHand(((TaiwaneseHand)opponents[0]).TopHand.CardsMask | opsFillers[0] | boardMask);
      for (var i = 1; i < opponents.Length; i++)
      {
        (int checkType, uint check) = PokerGame.EvaluateHand(((TaiwaneseHand)opponents[i]).TopHand.CardsMask | opsFillers[i] | boardMask);
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
          wins += Taiwanese.PointTopHand(heroType) * (opponents.Length - ((heroType > villianType) ? 0 : typeTies));  // Bonus from all players (subtracting all those with the same type of hand
          winCnt++;
          break;
        case -1:
          loses += 1 + ((villianType > heroType) ? Taiwanese.PointTopHand(villianType) : 0); // one to the villain's pot plus the villain's bonus if it's a better type
          winCnt--;
          break;
        default:
          break;
      }

      // Middle hands
      (heroType, hero) = PokerGame.EvaluateHand(MiddleHand.CardsMask | heroFiller | boardMask);

      // Must check them all to get the best opponent hand
      rankTies = 0;
      typeTies = 0;
      (villianType, villain) = PokerGame.EvaluateHand(((TaiwaneseHand)opponents[0]).MiddleHand.CardsMask | opsFillers[0] | boardMask);
      for (var i = 1; i < opponents.Length; i++)
      {
        (int checkType, uint check) = PokerGame.EvaluateHand(((TaiwaneseHand)opponents[i]).MiddleHand.CardsMask | opsFillers[i] | boardMask);
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
          wins += Taiwanese.PointMiddleHand(heroType) * (opponents.Length - ((heroType > villianType) ? 0 : typeTies));  // Bonus from all players (subtracting all those with the same type of hand
          winCnt++;
          break;
        case -1:
          loses += 2 + ((villianType > heroType) ? Taiwanese.PointMiddleHand(villianType) : 0); // 2 to the villain's pot plus the villain's bonus if it's a better type
          winCnt--;
          break;
        default:
          break;
      }


      //TODO switch to Omaha evaluation
      // Bottom hands
      (heroType, hero) = PokerGame.EvaluateHand(BottomHand.CardsMask | heroFiller | boardMask);

      // Must check them all to get the best opponent hand
      rankTies = 0;
      typeTies = 0;
      (villianType, villain) = PokerGame.EvaluateHand(((TaiwaneseHand)opponents[0]).BottomHand.CardsMask | opsFillers[0] | boardMask);
      for (var i = 1; i < opponents.Length; i++)
      {
        (int checkType, uint check) = PokerGame.EvaluateHand(((TaiwaneseHand)opponents[i]).BottomHand.CardsMask | opsFillers[i] | boardMask);
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
          wins += Taiwanese.PointBottomHand(heroType) * (opponents.Length - ((heroType > villianType) ? 0 : typeTies));  // Bonus from al players (subtracting all those with the same type of hand
          winCnt++;
          break;
        case -1:
          loses += 3 + ((villianType > heroType) ? Taiwanese.PointBottomHand(villianType) : 0); // 3 to the villain's pot plus the villain's bonus if it's a better type
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

    public TaiwaneseHand[] AllLayouts()
    {
      var cards = Bits.IndividualMasks(this.CardsMask);
      return new TaiwaneseHand[105]
      {
        new TaiwaneseHand(new BaseHand(cards[0], 1), new BaseHand(cards[1] | cards[2], 2), new BaseHand(cards[3] | cards[4] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[0], 1), new BaseHand(cards[1] | cards[3], 2), new BaseHand(cards[2] | cards[4] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[0], 1), new BaseHand(cards[1] | cards[4], 2), new BaseHand(cards[2] | cards[3] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[0], 1), new BaseHand(cards[1] | cards[5], 2), new BaseHand(cards[2] | cards[3] | cards[4] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[0], 1), new BaseHand(cards[1] | cards[6], 2), new BaseHand(cards[2] | cards[3] | cards[4] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[0], 1), new BaseHand(cards[2] | cards[3], 2), new BaseHand(cards[1] | cards[4] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[0], 1), new BaseHand(cards[2] | cards[4], 2), new BaseHand(cards[1] | cards[3] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[0], 1), new BaseHand(cards[2] | cards[5], 2), new BaseHand(cards[1] | cards[3] | cards[4] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[0], 1), new BaseHand(cards[2] | cards[6], 2), new BaseHand(cards[1] | cards[3] | cards[4] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[0], 1), new BaseHand(cards[3] | cards[4], 2), new BaseHand(cards[1] | cards[2] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[0], 1), new BaseHand(cards[3] | cards[5], 2), new BaseHand(cards[1] | cards[2] | cards[4] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[0], 1), new BaseHand(cards[3] | cards[6], 2), new BaseHand(cards[1] | cards[2] | cards[4] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[0], 1), new BaseHand(cards[4] | cards[5], 2), new BaseHand(cards[1] | cards[2] | cards[3] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[0], 1), new BaseHand(cards[4] | cards[6], 2), new BaseHand(cards[1] | cards[2] | cards[3] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[0], 1), new BaseHand(cards[5] | cards[6], 2), new BaseHand(cards[1] | cards[2] | cards[3] | cards[4], 4)),

        new TaiwaneseHand(new BaseHand(cards[1], 1), new BaseHand(cards[0] | cards[2], 2), new BaseHand(cards[3] | cards[4] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[1], 1), new BaseHand(cards[0] | cards[3], 2), new BaseHand(cards[2] | cards[4] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[1], 1), new BaseHand(cards[0] | cards[4], 2), new BaseHand(cards[2] | cards[3] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[1], 1), new BaseHand(cards[0] | cards[5], 2), new BaseHand(cards[2] | cards[3] | cards[4] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[1], 1), new BaseHand(cards[0] | cards[6], 2), new BaseHand(cards[2] | cards[3] | cards[4] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[1], 1), new BaseHand(cards[2] | cards[3], 2), new BaseHand(cards[0] | cards[4] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[1], 1), new BaseHand(cards[2] | cards[4], 2), new BaseHand(cards[0] | cards[3] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[1], 1), new BaseHand(cards[2] | cards[5], 2), new BaseHand(cards[0] | cards[3] | cards[4] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[1], 1), new BaseHand(cards[2] | cards[6], 2), new BaseHand(cards[0] | cards[3] | cards[4] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[1], 1), new BaseHand(cards[3] | cards[4], 2), new BaseHand(cards[0] | cards[2] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[1], 1), new BaseHand(cards[3] | cards[5], 2), new BaseHand(cards[0] | cards[2] | cards[4] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[1], 1), new BaseHand(cards[3] | cards[6], 2), new BaseHand(cards[0] | cards[2] | cards[4] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[1], 1), new BaseHand(cards[4] | cards[5], 2), new BaseHand(cards[0] | cards[2] | cards[3] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[1], 1), new BaseHand(cards[4] | cards[6], 2), new BaseHand(cards[0] | cards[2] | cards[3] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[1], 1), new BaseHand(cards[5] | cards[6], 2), new BaseHand(cards[0] | cards[2] | cards[3] | cards[4], 4)),

        new TaiwaneseHand(new BaseHand(cards[2], 1), new BaseHand(cards[1] | cards[0], 2), new BaseHand(cards[3] | cards[4] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[2], 1), new BaseHand(cards[1] | cards[3], 2), new BaseHand(cards[0] | cards[4] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[2], 1), new BaseHand(cards[1] | cards[4], 2), new BaseHand(cards[0] | cards[3] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[2], 1), new BaseHand(cards[1] | cards[5], 2), new BaseHand(cards[0] | cards[3] | cards[4] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[2], 1), new BaseHand(cards[1] | cards[6], 2), new BaseHand(cards[0] | cards[3] | cards[4] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[2], 1), new BaseHand(cards[0] | cards[3], 2), new BaseHand(cards[1] | cards[4] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[2], 1), new BaseHand(cards[0] | cards[4], 2), new BaseHand(cards[1] | cards[3] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[2], 1), new BaseHand(cards[0] | cards[5], 2), new BaseHand(cards[1] | cards[3] | cards[4] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[2], 1), new BaseHand(cards[0] | cards[6], 2), new BaseHand(cards[1] | cards[3] | cards[4] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[2], 1), new BaseHand(cards[3] | cards[4], 2), new BaseHand(cards[1] | cards[0] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[2], 1), new BaseHand(cards[3] | cards[5], 2), new BaseHand(cards[1] | cards[0] | cards[4] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[2], 1), new BaseHand(cards[3] | cards[6], 2), new BaseHand(cards[1] | cards[0] | cards[4] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[2], 1), new BaseHand(cards[4] | cards[5], 2), new BaseHand(cards[1] | cards[0] | cards[3] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[2], 1), new BaseHand(cards[4] | cards[6], 2), new BaseHand(cards[1] | cards[0] | cards[3] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[2], 1), new BaseHand(cards[5] | cards[6], 2), new BaseHand(cards[1] | cards[0] | cards[3] | cards[4], 4)),

        new TaiwaneseHand(new BaseHand(cards[3], 1), new BaseHand(cards[1] | cards[2], 2), new BaseHand(cards[0] | cards[4] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[3], 1), new BaseHand(cards[1] | cards[0], 2), new BaseHand(cards[2] | cards[4] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[3], 1), new BaseHand(cards[1] | cards[4], 2), new BaseHand(cards[2] | cards[0] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[3], 1), new BaseHand(cards[1] | cards[5], 2), new BaseHand(cards[2] | cards[0] | cards[4] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[3], 1), new BaseHand(cards[1] | cards[6], 2), new BaseHand(cards[2] | cards[0] | cards[4] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[3], 1), new BaseHand(cards[2] | cards[0], 2), new BaseHand(cards[1] | cards[4] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[3], 1), new BaseHand(cards[2] | cards[4], 2), new BaseHand(cards[1] | cards[0] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[3], 1), new BaseHand(cards[2] | cards[5], 2), new BaseHand(cards[1] | cards[0] | cards[4] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[3], 1), new BaseHand(cards[2] | cards[6], 2), new BaseHand(cards[1] | cards[0] | cards[4] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[3], 1), new BaseHand(cards[0] | cards[4], 2), new BaseHand(cards[1] | cards[2] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[3], 1), new BaseHand(cards[0] | cards[5], 2), new BaseHand(cards[1] | cards[2] | cards[4] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[3], 1), new BaseHand(cards[0] | cards[6], 2), new BaseHand(cards[1] | cards[2] | cards[4] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[3], 1), new BaseHand(cards[4] | cards[5], 2), new BaseHand(cards[1] | cards[2] | cards[0] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[3], 1), new BaseHand(cards[4] | cards[6], 2), new BaseHand(cards[1] | cards[2] | cards[0] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[3], 1), new BaseHand(cards[5] | cards[6], 2), new BaseHand(cards[1] | cards[2] | cards[0] | cards[4], 4)),

        new TaiwaneseHand(new BaseHand(cards[4], 1), new BaseHand(cards[1] | cards[2], 2), new BaseHand(cards[3] | cards[0] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[4], 1), new BaseHand(cards[1] | cards[3], 2), new BaseHand(cards[2] | cards[0] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[4], 1), new BaseHand(cards[1] | cards[0], 2), new BaseHand(cards[2] | cards[3] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[4], 1), new BaseHand(cards[1] | cards[5], 2), new BaseHand(cards[2] | cards[3] | cards[0] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[4], 1), new BaseHand(cards[1] | cards[6], 2), new BaseHand(cards[2] | cards[3] | cards[0] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[4], 1), new BaseHand(cards[2] | cards[3], 2), new BaseHand(cards[1] | cards[0] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[4], 1), new BaseHand(cards[2] | cards[0], 2), new BaseHand(cards[1] | cards[3] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[4], 1), new BaseHand(cards[2] | cards[5], 2), new BaseHand(cards[1] | cards[3] | cards[0] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[4], 1), new BaseHand(cards[2] | cards[6], 2), new BaseHand(cards[1] | cards[3] | cards[0] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[4], 1), new BaseHand(cards[3] | cards[0], 2), new BaseHand(cards[1] | cards[2] | cards[5] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[4], 1), new BaseHand(cards[3] | cards[5], 2), new BaseHand(cards[1] | cards[2] | cards[0] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[4], 1), new BaseHand(cards[3] | cards[6], 2), new BaseHand(cards[1] | cards[2] | cards[0] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[4], 1), new BaseHand(cards[0] | cards[5], 2), new BaseHand(cards[1] | cards[2] | cards[3] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[4], 1), new BaseHand(cards[0] | cards[6], 2), new BaseHand(cards[1] | cards[2] | cards[3] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[4], 1), new BaseHand(cards[5] | cards[6], 2), new BaseHand(cards[1] | cards[2] | cards[3] | cards[0], 4)),

        new TaiwaneseHand(new BaseHand(cards[5], 1), new BaseHand(cards[1] | cards[2], 2), new BaseHand(cards[3] | cards[4] | cards[0] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[5], 1), new BaseHand(cards[1] | cards[3], 2), new BaseHand(cards[2] | cards[4] | cards[0] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[5], 1), new BaseHand(cards[1] | cards[4], 2), new BaseHand(cards[2] | cards[3] | cards[0] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[5], 1), new BaseHand(cards[1] | cards[0], 2), new BaseHand(cards[2] | cards[3] | cards[4] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[5], 1), new BaseHand(cards[1] | cards[6], 2), new BaseHand(cards[2] | cards[3] | cards[4] | cards[0], 4)),
        new TaiwaneseHand(new BaseHand(cards[5], 1), new BaseHand(cards[2] | cards[3], 2), new BaseHand(cards[1] | cards[4] | cards[0] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[5], 1), new BaseHand(cards[2] | cards[4], 2), new BaseHand(cards[1] | cards[3] | cards[0] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[5], 1), new BaseHand(cards[2] | cards[0], 2), new BaseHand(cards[1] | cards[3] | cards[4] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[5], 1), new BaseHand(cards[2] | cards[6], 2), new BaseHand(cards[1] | cards[3] | cards[4] | cards[0], 4)),
        new TaiwaneseHand(new BaseHand(cards[5], 1), new BaseHand(cards[3] | cards[4], 2), new BaseHand(cards[1] | cards[2] | cards[0] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[5], 1), new BaseHand(cards[3] | cards[0], 2), new BaseHand(cards[1] | cards[2] | cards[4] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[5], 1), new BaseHand(cards[3] | cards[6], 2), new BaseHand(cards[1] | cards[2] | cards[4] | cards[0], 4)),
        new TaiwaneseHand(new BaseHand(cards[5], 1), new BaseHand(cards[4] | cards[0], 2), new BaseHand(cards[1] | cards[2] | cards[3] | cards[6], 4)),
        new TaiwaneseHand(new BaseHand(cards[5], 1), new BaseHand(cards[4] | cards[6], 2), new BaseHand(cards[1] | cards[2] | cards[3] | cards[0], 4)),
        new TaiwaneseHand(new BaseHand(cards[5], 1), new BaseHand(cards[0] | cards[6], 2), new BaseHand(cards[1] | cards[2] | cards[3] | cards[4], 4)),

        new TaiwaneseHand(new BaseHand(cards[6], 1), new BaseHand(cards[1] | cards[2], 2), new BaseHand(cards[3] | cards[4] | cards[5] | cards[0], 4)),
        new TaiwaneseHand(new BaseHand(cards[6], 1), new BaseHand(cards[1] | cards[3], 2), new BaseHand(cards[2] | cards[4] | cards[5] | cards[0], 4)),
        new TaiwaneseHand(new BaseHand(cards[6], 1), new BaseHand(cards[1] | cards[4], 2), new BaseHand(cards[2] | cards[3] | cards[5] | cards[0], 4)),
        new TaiwaneseHand(new BaseHand(cards[6], 1), new BaseHand(cards[1] | cards[5], 2), new BaseHand(cards[2] | cards[3] | cards[4] | cards[0], 4)),
        new TaiwaneseHand(new BaseHand(cards[6], 1), new BaseHand(cards[1] | cards[0], 2), new BaseHand(cards[2] | cards[3] | cards[4] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[6], 1), new BaseHand(cards[2] | cards[3], 2), new BaseHand(cards[1] | cards[4] | cards[5] | cards[0], 4)),
        new TaiwaneseHand(new BaseHand(cards[6], 1), new BaseHand(cards[2] | cards[4], 2), new BaseHand(cards[1] | cards[3] | cards[5] | cards[0], 4)),
        new TaiwaneseHand(new BaseHand(cards[6], 1), new BaseHand(cards[2] | cards[5], 2), new BaseHand(cards[1] | cards[3] | cards[4] | cards[0], 4)),
        new TaiwaneseHand(new BaseHand(cards[6], 1), new BaseHand(cards[2] | cards[0], 2), new BaseHand(cards[1] | cards[3] | cards[4] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[6], 1), new BaseHand(cards[3] | cards[4], 2), new BaseHand(cards[1] | cards[2] | cards[5] | cards[0], 4)),
        new TaiwaneseHand(new BaseHand(cards[6], 1), new BaseHand(cards[3] | cards[5], 2), new BaseHand(cards[1] | cards[2] | cards[4] | cards[0], 4)),
        new TaiwaneseHand(new BaseHand(cards[6], 1), new BaseHand(cards[3] | cards[0], 2), new BaseHand(cards[1] | cards[2] | cards[4] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[6], 1), new BaseHand(cards[4] | cards[5], 2), new BaseHand(cards[1] | cards[2] | cards[3] | cards[0], 4)),
        new TaiwaneseHand(new BaseHand(cards[6], 1), new BaseHand(cards[4] | cards[0], 2), new BaseHand(cards[1] | cards[2] | cards[3] | cards[5], 4)),
        new TaiwaneseHand(new BaseHand(cards[6], 1), new BaseHand(cards[5] | cards[0], 2), new BaseHand(cards[1] | cards[2] | cards[3] | cards[4], 4)),
      };
    }

  }
}
