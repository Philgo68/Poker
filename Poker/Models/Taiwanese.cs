using Poker.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Poker.Models
{
  public class Taiwanese : PokerGame
  {
    public Taiwanese() : base()
    {
    }

    public override void SetupPhases()
    {
      PhaseActions = new List<Func<TableDealer, DisplayStage[]>>();

      var chipMovingPhases = new DisplayStage[] { DisplayStage.DealtCards, DisplayStage.BetsOut, DisplayStage.Scooping, DisplayStage.PotScooped, DisplayStage.Delivering, DisplayStage.DeliverPot, DisplayStage.PotDelivered };

      //Reset table for new hand
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.CleanTableForNewHand();
        // See if there are enough players to start a hand
        // more than one player and at least one non-computer
        if (tableDealer.Table.OccupiedSeats().Count() > 1 && tableDealer.Table.OccupiedSeats().Any(s => !s.Player.Computer))
        {
          // Reset Pause request for all seats.
          foreach (var s in tableDealer.Table.AllSeats())
          {
            if (s.Status == SeatStatus.PlayOne) s.Status = SeatStatus.PleasePause;
          }
          return new DisplayStage[] { };
        }
        else
        {
          tableDealer.Table.PhaseTitle = "Waiting for Player(s)";
          tableDealer.Table.PhaseMessage = "";
          return null;
        }
      });

      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Hands Dealt";
        tableDealer.Table.PhaseMessage = "";
        tableDealer.DealHands();
        return new DisplayStage[] { DisplayStage.DealtCards };
      });

      //Layout Computer Hands
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Layout Hands";
        tableDealer.Table.PhaseMessage = "";
        tableDealer.LayoutHands();
        return new DisplayStage[] { DisplayStage.DealtCards };
      });

      //Hands Laid Out
      PhaseActions.Add((tableDealer) =>
      {
        // Need to wait until all hands are laid out before continuing.
        if (tableDealer.Table.SeatsWithHands().Any(s => !s.Hand.HandsLaidOut))
        {
          return null;
        }
        // Reveal all hands when everyone has finished the Layout.
        foreach (var s in tableDealer.Table.SeatsWithHands())
        {
          s.Hand.Revealed = true;
          var tHand = s.Hand as TaiwaneseHand;
        }

        tableDealer.Table.PhaseTitle = "Hands Revealed";
        tableDealer.Table.PhaseMessage = "";
        return new DisplayStage[] { DisplayStage.DealtCards, DisplayStage.DealtCards };
      });

      //Deal 1st Board
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Board One";
        tableDealer.Table.PhaseMessage = "";
        var board = GetBoard();
        tableDealer.Deck.CompleteCards(board);
        tableDealer.Table.SetBoard(board);
        return new DisplayStage[] { DisplayStage.DealtCards };
      });

      //Play Top Hand
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Top Hand";
        tableDealer.Table.PhaseMessage = "";
        PlayTopHand(tableDealer.Table);
        LogTable(tableDealer.Table, "Top Hand 1");
        return chipMovingPhases;
      });

      //Play Middle Hand
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Middle Hand";
        tableDealer.Table.PhaseMessage = "";
        PlayMiddleHand(tableDealer.Table);
        LogTable(tableDealer.Table, "Middle Hand 1");
        return chipMovingPhases;
      });

      //Play Bottom Hand
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Bottom Hand";
        tableDealer.Table.PhaseMessage = "";
        PlayBottomHand(tableDealer.Table);
        LogTable(tableDealer.Table, "Bottom Hand 1");
        return chipMovingPhases;
      });

      //Play Scoop Bonus
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Scoop Bonus";
        tableDealer.Table.PhaseMessage = "";
        if (PlayScoopBonus(tableDealer.Table))
        {
          LogTable(tableDealer.Table, "Scoop Bonus 1");
          return chipMovingPhases;
        }
        else
        {
          return new DisplayStage[] { };
        }
      });

      //Deal 2nd Board
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Board Two";
        tableDealer.Table.PhaseMessage = "";

        tableDealer.CleanChips();

        var board = GetBoard();
        tableDealer.Deck.CompleteCards(board);
        tableDealer.Table.SetBoard(board);
        return new DisplayStage[] { DisplayStage.DealtCards };
      });

      //Play Top Hand
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Top Hand";
        tableDealer.Table.PhaseMessage = "";
        PlayTopHand(tableDealer.Table);
        LogTable(tableDealer.Table, "Top Hand 2");
        return chipMovingPhases;
      });

      //Play Middle Hand
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Middle Hand";
        tableDealer.Table.PhaseMessage = "";
        PlayMiddleHand(tableDealer.Table);
        LogTable(tableDealer.Table, "Middle Hand 2");
        return chipMovingPhases;
      });

      //Play Bottom Hand
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Bottom Hand";
        tableDealer.Table.PhaseMessage = "";
        PlayBottomHand(tableDealer.Table);
        LogTable(tableDealer.Table, "Bottom Hand 2");
        return chipMovingPhases;
      });

      //Play Scoop Bonus
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Scoop Bonus";
        tableDealer.Table.PhaseMessage = "";
        if (PlayScoopBonus(tableDealer.Table))
        {
          LogTable(tableDealer.Table, "Scoop Bonus 2");
          return chipMovingPhases;
        }
        else
        {
          return new DisplayStage[] {};
        }
      });
    }

    public override string Name { get { return "Taiwanese"; } }

    public override BaseDeck GetDeck(ulong dealtCards = 0)
    {
      return new StandardDeck(dealtCards);
    }

    public override BaseHand GetHand()
    {
      return new TaiwaneseHand();
    }

    public static int PointTopHand(HandTypes handType)
    {
      return handType switch
      {
        HandTypes.HighCard => 0,
        HandTypes.Pair => 0,
        HandTypes.TwoPair  => 1,
        HandTypes.Trips => 2,
        HandTypes.Straight => 3,
        HandTypes.Flush => 3,
        HandTypes.FullHouse => 4,
        HandTypes.FourOfAKind => 6,
        HandTypes.StraightFlush => 12,
        _ => 0
      };
    }

    public static int PointMiddleHand(HandTypes handType)
    {
      return handType switch
      {
        HandTypes.HighCard => 0,
        HandTypes.Pair => 0,
        HandTypes.TwoPair => 0,
        HandTypes.Trips => 1,
        HandTypes.Straight => 2,
        HandTypes.Flush => 2,
        HandTypes.FullHouse => 3,
        HandTypes.FourOfAKind => 5,
        HandTypes.StraightFlush => 10,
        _ => 0
      };
    }

    public static int PointBottomHand(HandTypes handType)
    {
      return handType switch
      {
        HandTypes.HighCard => 0,
        HandTypes.Pair => 0,
        HandTypes.TwoPair => 0,
        HandTypes.Trips => 0,
        HandTypes.Straight => 0,
        HandTypes.Flush => 0,
        HandTypes.FullHouse => 2,
        HandTypes.FourOfAKind => 4,
        HandTypes.StraightFlush => 8,
        _ => 0
      };
    }

    private void PlayTopHand(Table table)
    {
      HandEvaluation best = new HandEvaluation();

      // Find best hand
      foreach (var seat in table.SeatsWithHands())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          seat.HandsWon = 0;
          tHand.LastEvaluation = TexasHoldem.EvaluateHand(tHand.TopHand.CardsMask | table.Board.CardsMask);
          if (tHand.LastEvaluation.Value > best.Value)
          {
            best = tHand.LastEvaluation;
          }
        }
      }

      int base_chips = 1;
      int bonus_chips = PointTopHand(best.Type);

      int pot = 0;
      int winners = 0;
      int bestCard = 0;

      table.PhaseMessage = $"{TexasHoldem.HandTypeDescriptions[(int)best.Type]} ({base_chips}{(bonus_chips > 0 ? $" + {bonus_chips}" : "")})";

      // Determine chips out and pot size and number of winners and the best card from the winners
      foreach (var seat in table.SeatsWithHands())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          if (tHand.LastEvaluation.Value == best.Value)
          {
            winners++;
            seat.ChipsOut = 0;
            bestCard = Math.Max(bestCard, tHand.TopHand.BestCard());
            table.HighlightCards |= TexasHoldem.FindCards(best);
          }
          else
          {
            seat.ChipsOut = Math.Min(seat.Chips, base_chips + (tHand.LastEvaluation.Type == best.Type ? 0 : bonus_chips));
          }
          pot += seat.ChipsOut;
        }
      }

      int cut = pot / winners;
      int remainder = pot % winners;

      // Determine chips in
      foreach (var seat in table.SeatsWithHands())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          if (tHand.LastEvaluation.Value == best.Value)
          {
            seat.ChipsIn = cut + (tHand.TopHand.BestCard() == bestCard ? remainder : 0);
            if (winners == 1)
              seat.HandsWon++;
          }
          else
          {
            seat.ChipsIn = 0;
          }
        }
      }
    }

    private void PlayMiddleHand(Table table)
    {
      HandEvaluation best = new HandEvaluation();

      // Find best hand
      foreach (var seat in table.SeatsWithHands())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          tHand.LastEvaluation = TexasHoldem.EvaluateHand(tHand.MiddleHand.CardsMask | table.Board.CardsMask);
          if (tHand.LastEvaluation.Value > best.Value)
          {
            best = tHand.LastEvaluation;
          }
        }
      }

      int base_chips = 2;
      int bonus_chips = PointMiddleHand(best.Type);

      int pot = 0;
      int winners = 0;
      int bestCard = 0;

      table.PhaseMessage = $"{TexasHoldem.HandTypeDescriptions[(int)best.Type]} ({base_chips}{(bonus_chips > 0 ? $" + {bonus_chips}" : "")})";

      // Determine chips out and pot size and number of winners and the best card from the winners
      foreach (var seat in table.SeatsWithHands())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          if (tHand.LastEvaluation.Value == best.Value)
          {
            winners++;
            seat.ChipsOut = 0;
            bestCard = Math.Max(bestCard, tHand.MiddleHand.BestCard());
            table.HighlightCards |= TexasHoldem.FindCards(best);
          }
          else
          {
            seat.ChipsOut = Math.Min(seat.Chips, base_chips + (tHand.LastEvaluation.Type == best.Type ? 0 : bonus_chips));
          }
          pot += seat.ChipsOut;
        }
      }

      int cut = pot / winners;
      int remainder = pot % winners;

      // Determine chips in
      foreach (var seat in table.SeatsWithHands())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          if (tHand.LastEvaluation.Value == best.Value)
          {
            seat.ChipsIn = cut + (tHand.MiddleHand.BestCard() == bestCard ? remainder : 0);
            if (winners == 1)
              seat.HandsWon++;
          }
          else
          {
            seat.ChipsIn = 0;
          }
        }
      }
    }

    private void PlayBottomHand(Table table)
    {
      HandEvaluation best = new HandEvaluation();

      // Find best hand
      foreach (var seat in table.SeatsWithHands())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          tHand.LastEvaluation = Omaha.EvaluateHand(tHand.BottomHand.CardsMask, table.Board.CardsMask);
          if (tHand.LastEvaluation.Value > best.Value)
          {
            best = tHand.LastEvaluation;
          }
        }
      }

      int base_chips = 3;
      int bonus_chips = PointBottomHand(best.Type);

      int pot = 0;
      int winners = 0;
      int bestCard = 0;

      table.PhaseMessage = $"{Omaha.HandTypeDescriptions[(int)best.Type]} ({base_chips}{(bonus_chips > 0 ? $" + {bonus_chips}" : "")})";

      // Determine chips out and pot size and number of winners and the best card from the winners
      foreach (var seat in table.SeatsWithHands())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          if (tHand.LastEvaluation.Value == best.Value)
          {
            winners++;
            seat.ChipsOut = 0;
            bestCard = Math.Max(bestCard, tHand.BottomHand.BestCard());
            table.HighlightCards |= Omaha.FindCards(best);
          }
          else
          {
            seat.ChipsOut = Math.Min(seat.Chips, base_chips + (tHand.LastEvaluation.Type == best.Type ? 0 : bonus_chips));
          }
          pot += seat.ChipsOut;
        }
      }

      int cut = pot / winners;
      int remainder = pot % winners;

      // Determine chips in
      foreach (var seat in table.SeatsWithHands())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          if (tHand.LastEvaluation.Value == best.Value)
          {
            seat.ChipsIn = cut + (tHand.BottomHand.BestCard() == bestCard ? remainder : 0);
            if (winners == 1)
              seat.HandsWon++;
          }
          else
          {
            seat.ChipsIn = 0;
          }
        }
      }
    }

    private bool PlayScoopBonus(Table table)
    {
      // See if someone scooped all three hands
      bool scooped = table.SeatsWithHands().Any(seat => seat.HandsWon == 3);

      if (scooped)
      {
        int pot = 0;
        // Determine chips out and pot size
        foreach (var seat in table.SeatsWithHands())
        {
          if (seat.HandsWon != 3)
          {
            seat.ChipsOut = Math.Min(seat.Chips, 3);
          }
          else
          {
            seat.ChipsOut = 0;
          }
          pot += seat.ChipsOut;
        }

        // Determine chips in 
        foreach (var seat in table.SeatsWithHands())
        {
          if (seat.HandsWon == 3)
          {
            seat.ChipsIn = pot;
          }
          else
          {
            seat.ChipsIn = 0;
          }
        }
      }

      return scooped;
    }

    public override HandEvaluation Evaluate(ulong cards)
    {
      throw new ArgumentException("Taiwanese Evaluation must include a hand and a board.");
    }
    public override HandEvaluation Evaluate(BaseHand hand)
    {
      throw new ArgumentException("Taiwanese Evaluation must include a hand and a board.");
    }

    public override HandEvaluation Evaluate(ulong hand, ulong board)
    {
      return EvaluateTaiwaneseHand(hand, board);
    }

    public static HandEvaluation EvaluateTaiwaneseHand(ulong hand, ulong board)
    {
      var handcards = Bits.IndividualMasks(hand);
      var boardcards = Bits.IndividualMasks(board);
      HandEvaluation best, next;

      best = EvaluateHand(handcards[0] | handcards[1] | boardcards[0] | boardcards[1] | boardcards[2]);

      next = EvaluateHand(handcards[0] | handcards[1] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[1] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[1] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[1] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[1] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[1] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[1] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[1] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[1] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;



      next = EvaluateHand(handcards[0] | handcards[2] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[2] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[2] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[2] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[2] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[2] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[2] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[2] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[2] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[2] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;



      next = EvaluateHand(handcards[0] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[3] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[3] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[0] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;



      next = EvaluateHand(handcards[1] | handcards[2] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[2] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[2] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[2] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[2] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[2] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[2] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[2] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[2] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[2] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;



      next = EvaluateHand(handcards[1] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[3] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[3] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[1] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;



      next = EvaluateHand(handcards[2] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[2] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[2] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[2] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[2] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[2] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[2] | handcards[3] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[2] | handcards[3] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[2] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      next = EvaluateHand(handcards[2] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (next.Value > best.Value) best = next;

      return best;
    }

  }
}
