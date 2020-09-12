using Poker.Helpers;
using System;
using System.Collections.Generic;

namespace Poker.Models
{


  public abstract class PokerGame : BaseGame
  {
    public PokerGame()
    {
    }

    protected List<Func<TableDealer, DisplayStage[]>> PhaseActions;
    public override int PhaseCount => PhaseActions.Count;

    public override string Name { get { return "Generic Base Poker Evaluation"; } }
    public override HandEvaluation Evaluate(ulong cards)
    {
      return EvaluateHand(cards);
    }

    public static HandEvaluation EvaluateHand(ulong cards)
    {
      int numberOfCards = Bits.BitCount(cards);
      uint four_mask, three_mask, two_mask;
      HandEvaluation ret = new HandEvaluation(cards, 0, 0);

#if DEBUG
      // This functions supports 1-7 cards
      if (numberOfCards < 1 || numberOfCards > 7)
        throw new ArgumentOutOfRangeException("numberOfCards");
#endif
      // Seperate out by suit
      uint sc = (uint)((cards >> (0)) & 0x1fffUL);
      uint sd = (uint)((cards >> (13)) & 0x1fffUL);
      uint sh = (uint)((cards >> (26)) & 0x1fffUL);
      uint ss = (uint)((cards >> (39)) & 0x1fffUL);

      uint ranks = sc | sd | sh | ss;
      uint n_ranks = Bits.nBitsTable[ranks];
      uint n_dups = ((uint)(numberOfCards - n_ranks));

      /* Check for straight, flush, or straight flush, and return if we can
         determine immediately that this is the best possible hand 
      */
      if (n_ranks >= 5)
      {
        if (Bits.nBitsTable[ss] >= 5)
        {
          uint st = Bits.straightTable[ss];
          if (st != 0)
            return new HandEvaluation(cards, HandTypes.StraightFlush, HANDTYPE_VALUE_STRAIGHTFLUSH + (st << TOP_CARD_SHIFT));
          else
            ret = new HandEvaluation(cards, HandTypes.Flush, HANDTYPE_VALUE_FLUSH + Bits.topFiveCardsTable[ss]);
        }
        else if (Bits.nBitsTable[sc] >= 5)
        {
          uint st = Bits.straightTable[sh];
          if (st != 0)
            return new HandEvaluation(cards, HandTypes.StraightFlush, HANDTYPE_VALUE_STRAIGHTFLUSH + (st << TOP_CARD_SHIFT));
          else
            ret = new HandEvaluation(cards, HandTypes.Flush, HANDTYPE_VALUE_FLUSH + Bits.topFiveCardsTable[sc]);
        }
        else if (Bits.nBitsTable[sd] >= 5)
        {
          uint st = Bits.straightTable[sd];
          if (st != 0)
            return new HandEvaluation(cards, HandTypes.StraightFlush, HANDTYPE_VALUE_STRAIGHTFLUSH + (st << TOP_CARD_SHIFT));
          else
            ret = new HandEvaluation(cards, HandTypes.Flush, HANDTYPE_VALUE_FLUSH + Bits.topFiveCardsTable[sd]);
        }
        else if (Bits.nBitsTable[sh] >= 5)
        {
          uint st = Bits.straightTable[sc];
          if (st != 0)
            return new HandEvaluation(cards,HandTypes.StraightFlush, HANDTYPE_VALUE_STRAIGHTFLUSH + (st << TOP_CARD_SHIFT));
          else
            ret = new HandEvaluation(cards,HandTypes.Flush, HANDTYPE_VALUE_FLUSH + Bits.topFiveCardsTable[sh]);
        }
        else
        {
          uint st = Bits.straightTable[ranks];
          if (st != 0)
            ret = new HandEvaluation(cards, HandTypes.Straight, HANDTYPE_VALUE_STRAIGHT + (st << TOP_CARD_SHIFT));
        };

        /* 
           Another win -- since there can't be a FH/Quads with 5 ranks present, 
           which is true most of the time when there is a made hand, then if we've
           found a five card hand, just return.  This skips the whole process of
           computing two_mask/three_mask/etc.
        */
        if (ret.Value != 0)
          return ret;
      }

      /*
       * By the time we're here, either: 
         1) there's no five-card hand possible (flush or straight), or
         2) there's a flush or straight, but we know that there are enough
            duplicates to make a full house / quads possible.  
       */
      switch (n_dups)
      {
        case 0:
          /* It's a no-pair hand */
          return new HandEvaluation(cards, HandTypes.HighCard, HANDTYPE_VALUE_HIGHCARD + Bits.topFiveCardsTable[ranks]);
        case 1:
          {
            /* It's a one-pair hand */
            ret.Type = HandTypes.Pair;
            uint t, kickers;

            two_mask = ranks ^ (sc ^ sd ^ sh ^ ss);

            ret.Value = (uint)(HANDTYPE_VALUE_PAIR + (Bits.topCardTable[two_mask] << TOP_CARD_SHIFT));
            t = ranks ^ two_mask;      /* Only one bit set in two_mask */
            /* Get the top five cards in what is left, drop all but the top three 
             * cards, and shift them by one to get the three desired kickers */
            kickers = (Bits.topFiveCardsTable[t] >> CARD_WIDTH) & ~FIFTH_CARD_MASK;
            ret.Value += kickers;
            return ret;
          }

        case 2:
          /* Either two pair or trips */
          two_mask = ranks ^ (sc ^ sd ^ sh ^ ss);
          if (two_mask != 0)
          {
            uint t = ranks ^ two_mask; /* Exactly two bits set in two_mask */
            ret.Type = HandTypes.TwoPair;
            ret.Value = (uint)(HANDTYPE_VALUE_TWOPAIR
                + (Bits.topFiveCardsTable[two_mask] & (TOP_CARD_MASK | SECOND_CARD_MASK))
                + (Bits.topCardTable[t] << THIRD_CARD_SHIFT));

            return ret;
          }
          else
          {
            ret.Type = HandTypes.Trips;
            uint t, second;
            three_mask = ((sc & sd) | (sh & ss)) & ((sc & sh) | (sd & ss));
            ret.Value = (uint)(HANDTYPE_VALUE_TRIPS + (Bits.topCardTable[three_mask] << TOP_CARD_SHIFT));
            t = ranks ^ three_mask; /* Only one bit set in three_mask */
            second = Bits.topCardTable[t];
            ret.Value += (second << SECOND_CARD_SHIFT);
            t ^= (1U << (int)second);
            ret.Value += (uint)(Bits.topCardTable[t] << THIRD_CARD_SHIFT);
            return ret;
          }

        default:
          /* Possible quads, fullhouse, straight or flush, or two pair */
          four_mask = sh & sd & sc & ss;
          if (four_mask != 0)
          {
            ret.Type = HandTypes.FourOfAKind;
            uint tc = Bits.topCardTable[four_mask];
            ret.Value = (uint)(HANDTYPE_VALUE_FOUR_OF_A_KIND
                + (tc << TOP_CARD_SHIFT)
                + ((Bits.topCardTable[ranks ^ (1U << (int)tc)]) << SECOND_CARD_SHIFT));
            return ret;
          };

          /* Technically, three_mask as defined below is really the set of
             bits which are set in three or four of the suits, but since
             we've already eliminated quads, this is OK */
          /* Similarly, two_mask is really two_or_four_mask, but since we've
             already eliminated quads, we can use this shortcut */

          two_mask = ranks ^ (sc ^ sd ^ sh ^ ss);
          if (Bits.nBitsTable[two_mask] != n_dups)
          {
            /* Must be some trips then, which really means there is a 
               full house since n_dups >= 3 */
            ret.Type = HandTypes.FullHouse;
            uint tc, t;
            three_mask = ((sc & sd) | (sh & ss)) & ((sc & sh) | (sd & ss));
            ret.Value = HANDTYPE_VALUE_FULLHOUSE;
            tc = Bits.topCardTable[three_mask];
            ret.Value += (tc << TOP_CARD_SHIFT);
            t = (two_mask | three_mask) ^ (1U << (int)tc);
            ret.Value += (uint)(Bits.topCardTable[t] << SECOND_CARD_SHIFT);
            return ret;
          };

          if (ret.Value != 0) /* flush and straight */
            return ret;
          else
          {
            /* Must be two pair */
            ret.Type = HandTypes.TwoPair;
            uint top, second;

            ret.Value = HANDTYPE_VALUE_TWOPAIR;
            top = Bits.topCardTable[two_mask];
            ret.Value += (top << TOP_CARD_SHIFT);
            second = Bits.topCardTable[two_mask ^ (1 << (int)top)];
            ret.Value += (second << SECOND_CARD_SHIFT);
            ret.Value += (uint)((Bits.topCardTable[ranks ^ (1U << (int)top) ^ (1 << (int)second)]) << THIRD_CARD_SHIFT);
            return ret;
          }
      }
    }

    public static ulong FindCards(HandEvaluation eval)
    {
      int numberOfCards = Bits.BitCount(eval.Cards);
      int remove = numberOfCards - 5;
      if (remove <= 0) return eval.Cards;

      uint sc = (uint)((eval.Cards >> (0)) & 0x1fffUL);
      uint sd = (uint)((eval.Cards >> (13)) & 0x1fffUL);
      uint sh = (uint)((eval.Cards >> (26)) & 0x1fffUL);
      uint ss = (uint)((eval.Cards >> (39)) & 0x1fffUL);

      uint ranks = sc | sd | sh | ss;
      uint n_ranks = Bits.nBitsTable[ranks];

      // Flush, Straight Flush, Straight
      if (n_ranks >= 5)
      {
        if (Bits.nBitsTable[ss] >= 5)
          if (Bits.straightTable[ss] != 0) return 0x1FUL << (Bits.straightTable[ss] - 4 + 39); else return (ulong)Bits.LeftBits(ss, 5) << 39;

        if (Bits.nBitsTable[sh] >= 5)
          if (Bits.straightTable[sh] != 0) return 0x1FUL << (Bits.straightTable[sh] - 4 + 26); else return (ulong)Bits.LeftBits(sh, 5) << 26;

        if (Bits.nBitsTable[sd] >= 5)
          if (Bits.straightTable[sd] != 0) return 0x1FUL << (Bits.straightTable[sd] - 4 + 13); else return (ulong)Bits.LeftBits(sd, 5) << 13;

        if (Bits.nBitsTable[sc] >= 5)
          if (Bits.straightTable[sc] != 0) return 0x1FUL << (Bits.straightTable[sc] - 4 + 0); else return (ulong)Bits.LeftBits(sc, 5) << 0;

        if (Bits.straightTable[ranks] != 0)
        {
          uint stBits = (uint)0x1F << (Bits.straightTable[ranks] - 4);
          ss &= stBits; stBits ^= ss;
          sh &= stBits; stBits ^= sh;
          sd &= stBits; stBits ^= sd;
          sc &= stBits; 
          return ((ulong)sc << 0) | ((ulong)sd << 13) | ((ulong)sh << 26) | ((ulong)ss << 39);
        }
      }

      uint n_dups = ((uint)(numberOfCards - n_ranks));
      switch (n_dups)
      {
        case 0:
          // High Card
          uint hcBits = Bits.LeftBits(ranks, 5);
          ss &= hcBits; hcBits ^= ss;
          sh &= hcBits; hcBits ^= sh;
          sd &= hcBits; hcBits ^= sd;
          sc &= hcBits; 
          return ((ulong)sc << 0) | ((ulong)sd << 13) | ((ulong)sh << 26) | ((ulong)ss << 39);
        case 1:
          // One Pair
          uint keepBits = ranks ^ (sc ^ sd ^ sh ^ ss);
          ranks ^= keepBits;
          ranks = Bits.LeftBits(ranks, 3) | keepBits;
          ss &= ranks; ranks = (ranks ^ ss) | keepBits;
          sh &= ranks; ranks = (ranks ^ sh) | keepBits;
          sd &= ranks; ranks = (ranks ^ sd) | keepBits;
          sc &= ranks; 
          return ((ulong)sc << 0) | ((ulong)sd << 13) | ((ulong)sh << 26) | ((ulong)ss << 39);
        case 2:
          // Two Pair or Trips
          keepBits = ranks ^ (sc ^ sd ^ sh ^ ss);
          int otherCards = 1;
          if (keepBits == 0)
          {
            // Or Trips
            keepBits = ((sc & sd) | (sh & ss)) & ((sc & sh) | (sd & ss));
            otherCards = 2;
          }
          ranks ^= keepBits;
          ranks = Bits.LeftBits(ranks, otherCards) | keepBits;
          ss &= ranks; ranks = (ranks ^ ss) | keepBits;
          sh &= ranks; ranks = (ranks ^ sh) | keepBits;
          sd &= ranks; ranks = (ranks ^ sd) | keepBits;
          sc &= ranks; 
          return ((ulong)sc << 0) | ((ulong)sd << 13) | ((ulong)sh << 26) | ((ulong)ss << 39);
        default:
          /* Possible quads, fullhouse, or three pair */
          keepBits = sc & sd & sh & ss;
          otherCards = 1;
          if (keepBits == 0)
          {
            keepBits = ranks ^ (sc ^ sd ^ sh ^ ss);
            if (Bits.nBitsTable[keepBits] == n_dups)
            {
              // three pair, so keep the best 2 pair
              keepBits = Bits.LeftBits(keepBits, 2);
            }
            else
            {
              // Must be trips somewhere, so fullhouse
              keepBits |= ((sc & sd) | (sh & ss)) & ((sc & sh) | (sd & ss));
              otherCards = 0;
            }
          }
          ranks ^= keepBits;
          ranks = Bits.LeftBits(ranks, otherCards) | keepBits;
          ss &= ranks; ranks = (ranks ^ ss) | keepBits;
          sh &= ranks; ranks = (ranks ^ sh) | keepBits;
          sd &= ranks; ranks = (ranks ^ sd) | keepBits;
          sc &= ranks; 
          return ((ulong)sc << 0) | ((ulong)sd << 13) | ((ulong)sh << 26) | ((ulong)ss << 39);
      }
    }

    public static string[] HandTypeDescriptions = new string[]
    {
      "High Card",
      "Pair",
      "Two Pair",
      "Trips",
      "Straight",
      "Flush",
      "Full House",
      "Four of a Kind",
      "Straight Flush"
    };

    private static readonly int CARD_WIDTH = 4;

    private static readonly int TOP_CARD_SHIFT = 16;

    private static readonly int SECOND_CARD_SHIFT = 12;

    private static readonly int THIRD_CARD_SHIFT = 8;

    private static readonly int FOURTH_CARD_SHIFT = 4;

    private static readonly int FIFTH_CARD_SHIFT = 0;

    private static readonly System.UInt32 TOP_CARD_MASK = 0x000F0000;

    private static readonly System.UInt32 SECOND_CARD_MASK = 0x0000F000;
    
    private static readonly System.UInt32 THIRD_CARD_MASK = 0x00000F00;
    
    private static readonly System.UInt32 FOURTH_CARD_MASK = 0x000000F0;

    private static readonly System.UInt32 FIFTH_CARD_MASK = 0x0000000F;

    private static readonly int HANDTYPE_SHIFT = 24;

    private static readonly uint HANDTYPE_VALUE_STRAIGHTFLUSH = (((uint)HandTypes.StraightFlush) << HANDTYPE_SHIFT);

    private static readonly uint HANDTYPE_VALUE_STRAIGHT = (((uint)HandTypes.Straight) << HANDTYPE_SHIFT);

    private static readonly uint HANDTYPE_VALUE_FLUSH = (((uint)HandTypes.Flush) << HANDTYPE_SHIFT);

    private static readonly uint HANDTYPE_VALUE_FULLHOUSE = (((uint)HandTypes.FullHouse) << HANDTYPE_SHIFT);

    private static readonly uint HANDTYPE_VALUE_FOUR_OF_A_KIND = (((uint)HandTypes.FourOfAKind) << HANDTYPE_SHIFT);

    private static readonly uint HANDTYPE_VALUE_TRIPS = (((uint)HandTypes.Trips) << HANDTYPE_SHIFT);

    private static readonly uint HANDTYPE_VALUE_TWOPAIR = (((uint)HandTypes.TwoPair) << HANDTYPE_SHIFT);

    private static readonly uint HANDTYPE_VALUE_PAIR = (((uint)HandTypes.Pair) << HANDTYPE_SHIFT);

    private static readonly uint HANDTYPE_VALUE_HIGHCARD = (((uint)HandTypes.HighCard) << HANDTYPE_SHIFT);
  }
}
