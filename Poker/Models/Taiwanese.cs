using Microsoft.AspNetCore.Mvc;
using Poker.Helpers;
using Poker.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Poker.Models
{
  [Serializable]
  public class Taiwanese : PokerGame
  {
    [NonSerialized]
    private List<Func<BaseTable, DisplayStage[]>> PhaseActions;
    public Taiwanese()
    {
      SetupPhases();
    }

    public void SetupPhases()
    {
      PhaseActions = new List<Func<BaseTable, DisplayStage[]>>();
      PhaseActions.Add((table) =>
      {
        table.PhaseTitle = "Hands Dealt";
        table.PhaseMessage = "";
        table.CompleteCards();
        return new DisplayStage[] { DisplayStage.DealtCards };
      });

      //Layout Computer Hands
      PhaseActions.Add((table) =>
      {
        table.PhaseTitle = "Layout Hands";
        table.PhaseMessage = "";
        table.LayoutHands();
        return new DisplayStage[] { DisplayStage.DealtCards };
      });

      //Deal 1st Board
      PhaseActions.Add((table) =>
      {
        // Need to wait until all hands are laid out before continuing.
        if (table.OccupiedSeats().Any(s => !s.Hand.HandsLaidOut))
        {
          return null;
        }
        table.PhaseTitle = "Board One";
        table.PhaseMessage = "";
        var board = GetBoard();
        table.Deck.CompleteCards(board);
        table.SetBoard(board);
        return new DisplayStage[] { DisplayStage.DealtCards };
      });

      //Play Top Hand
      PhaseActions.Add((table) =>
      {
        table.PhaseTitle = "Top Hand";
        table.PhaseMessage = "";
        PlayTopHand(table);
        LogTable(table, "Top Hand 1");
        return new DisplayStage[] { DisplayStage.DealtCards, DisplayStage.BetsOut, DisplayStage.Scooping, DisplayStage.PotScooped, DisplayStage.Delivering, DisplayStage.DeliverPot };
      });

      //Play Middle Hand
      PhaseActions.Add((table) =>
      {
        table.PhaseTitle = "Middle Hand";
        table.PhaseMessage = "";
        PlayMiddleHand(table);
        LogTable(table, "Middle Hand 1");
        return new DisplayStage[] { DisplayStage.DealtCards, DisplayStage.BetsOut, DisplayStage.Scooping, DisplayStage.PotScooped, DisplayStage.Delivering, DisplayStage.DeliverPot };
      });

      //Play Bottom Hand
      PhaseActions.Add((table) =>
      {
        table.PhaseTitle = "Bottom Hand";
        table.PhaseMessage = "";
        PlayBottomHand(table);
        LogTable(table, "Bottom Hand 1");
        return new DisplayStage[] { DisplayStage.DealtCards, DisplayStage.BetsOut, DisplayStage.Scooping, DisplayStage.PotScooped, DisplayStage.Delivering, DisplayStage.DeliverPot };
      });

      //Play Scoop Bonus
      PhaseActions.Add((table) =>
      {
        table.PhaseTitle = "Scoop Bonus";
        table.PhaseMessage = "";
        if (PlayScoopBonus(table))
        {
          LogTable(table, "Scoop Bonus 1");
          return new DisplayStage[] { DisplayStage.DealtCards, DisplayStage.BetsOut, DisplayStage.Scooping, DisplayStage.PotScooped, DisplayStage.Delivering, DisplayStage.DeliverPot };
        }
        else
        {
          return new DisplayStage[] { };
        }
      });

      //Deal 2nd Board
      PhaseActions.Add((table) =>
      {
        table.PhaseTitle = "Board Two";
        table.PhaseMessage = "";

        table.CleanChips();

        var board = GetBoard();
        table.Deck.CompleteCards(board);
        table.SetBoard(board);
        return new DisplayStage[] { DisplayStage.DealtCards };
      });

      //Play Top Hand
      PhaseActions.Add((table) =>
      {
        table.PhaseTitle = "Top Hand";
        table.PhaseMessage = "";
        PlayTopHand(table);
        LogTable(table, "Top Hand 2");
        return new DisplayStage[] { DisplayStage.DealtCards, DisplayStage.BetsOut, DisplayStage.Scooping, DisplayStage.PotScooped, DisplayStage.Delivering, DisplayStage.DeliverPot };
      });

      //Play Middle Hand
      PhaseActions.Add((table) =>
      {
        table.PhaseTitle = "Middle Hand";
        table.PhaseMessage = "";
        PlayMiddleHand(table);
        LogTable(table, "Middle Hand 2");
        return new DisplayStage[] { DisplayStage.DealtCards, DisplayStage.BetsOut, DisplayStage.Scooping, DisplayStage.PotScooped, DisplayStage.Delivering, DisplayStage.DeliverPot };
      });

      //Play Bottom Hand
      PhaseActions.Add((table) =>
      {
        table.PhaseTitle = "Bottom Hand";
        table.PhaseMessage = "";
        PlayBottomHand(table);
        LogTable(table, "Bottom Hand 2");
        return new DisplayStage[] { DisplayStage.DealtCards, DisplayStage.BetsOut, DisplayStage.Scooping, DisplayStage.PotScooped, DisplayStage.Delivering, DisplayStage.DeliverPot };
      });

      //Play Scoop Bonus
      PhaseActions.Add((table) =>
      {
        table.PhaseTitle = "Scoop Bonus";
        table.PhaseMessage = "";
        if (PlayScoopBonus(table))
        {
          LogTable(table, "Scoop Bonus 2");
          return new DisplayStage[] { DisplayStage.DealtCards, DisplayStage.BetsOut, DisplayStage.Scooping, DisplayStage.PotScooped, DisplayStage.Delivering, DisplayStage.DeliverPot };
        }
        else
        {
          return new DisplayStage[] { };
        }
      });

      //Reset for next hand
      PhaseActions.Add((table) =>
      {
        table.CleanTableForNextHand();
        return new DisplayStage[] { };
      });
    }

    [OnDeserialized()]
    internal void OnDeserializedMethod(StreamingContext context)
    {
      SetupPhases();
    }
    public void LogTable(BaseTable table, string description)
    {
      // Logging:
      Console.WriteLine($"--- {description} ---");
      Console.WriteLine($"  Board: {table.board.CardDescriptions}");
      foreach (var seat in table.OccupiedSeats())
      {
        Console.WriteLine($"  Seat: {seat.Player.Name}  Cards: {seat.Hand.CardDescriptions}  Out: {seat.ChipsOut}  In: {seat.ChipsIn}");
      }
    }

    public override string Name { get { return "Taiwanese"; } }

    public override IDeck GetDeck(ulong dealtCards = 0)
    {
      return new StandardDeck(dealtCards);
    }

    public override IHand GetHand()
    {
      return new TaiwaneseHand();
    }

    public override DisplayStage[] ExecutePhase(int game_phase, BaseTable table)
    {
      return PhaseActions[game_phase](table);
    }

    public static int PointTopHand(int handType)
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

    public static int PointMiddleHand(int handType)
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

    public static int PointBottomHand(int handType)
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

    private void PlayTopHand(BaseTable table)
    {
      int bestType = 0;
      uint bestValue = 0;

      // Find best hand
      foreach (var seat in table.OccupiedSeats())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          seat.HandsWon = 0;
          tHand.LastEvaluation = TexasHoldem.EvaluateHand(tHand.TopHand.CardsMask | table.board.CardsMask);
          if (tHand.LastEvaluation.Item2 > bestValue)
          {
            (bestType, bestValue) = (tHand.LastEvaluation);
          }
        }
      }

      table.PhaseMessage = TexasHoldem.HandTypeDescriptions[bestType];

      int base_chips = 1;
      int bonus_chips = PointTopHand(bestType);

      int pot = 0;
      int winners = 0;
      int bestCard = 0;

      // Determine chips out and pot size and number of winners and the best card from the winners
      foreach (var seat in table.OccupiedSeats())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          if (tHand.LastEvaluation.Item2 == bestValue)
          {
            winners++;
            seat.ChipsOut = 0;
            bestCard = Math.Max(bestCard, tHand.TopHand.BestCard());
          }
          else
          {
            seat.ChipsOut = Math.Min(seat.Chips, base_chips + (tHand.LastEvaluation.Item1 == bestType ? 0 : bonus_chips));
          }
          pot += seat.ChipsOut;
        }
      }

      int cut = pot / winners;
      int remainder = pot % winners;

      // Determine chips in
      foreach (var seat in table.OccupiedSeats())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          if (tHand.LastEvaluation.Item2 == bestValue)
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

    private void PlayMiddleHand(BaseTable table)
    {
      int bestType = 0;
      uint bestValue = 0;

      // Find best hand
      foreach (var seat in table.OccupiedSeats())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          tHand.LastEvaluation = TexasHoldem.EvaluateHand(tHand.MiddleHand.CardsMask | table.board.CardsMask);
          if (tHand.LastEvaluation.Item2 > bestValue)
          {
            (bestType, bestValue) = (tHand.LastEvaluation);
          }
        }
      }

      table.PhaseMessage = TexasHoldem.HandTypeDescriptions[bestType];

      int base_chips = 2;
      int bonus_chips = PointMiddleHand(bestType);

      int pot = 0;
      int winners = 0;
      int bestCard = 0;

      // Determine chips out and pot size and number of winners and the best card from the winners
      foreach (var seat in table.OccupiedSeats())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          if (tHand.LastEvaluation.Item2 == bestValue)
          {
            winners++;
            seat.ChipsOut = 0;
            bestCard = Math.Max(bestCard, tHand.MiddleHand.BestCard());
          }
          else
          {
            seat.ChipsOut = Math.Min(seat.Chips, base_chips + (tHand.LastEvaluation.Item1 == bestType ? 0 : bonus_chips));
          }
          pot += seat.ChipsOut;
        }
      }

      int cut = pot / winners;
      int remainder = pot % winners;

      // Determine chips in
      foreach (var seat in table.OccupiedSeats())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          if (tHand.LastEvaluation.Item2 == bestValue)
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

    private void PlayBottomHand(BaseTable table)
    {
      int bestType = 0;
      uint bestValue = 0;

      // Find best hand
      foreach (var seat in table.OccupiedSeats())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          tHand.LastEvaluation = Omaha.EvaluateHand(tHand.BottomHand.CardsMask, table.board.CardsMask);
          if (tHand.LastEvaluation.Item2 > bestValue)
          {
            (bestType, bestValue) = (tHand.LastEvaluation);
          }
        }
      }

      table.PhaseMessage = Omaha.HandTypeDescriptions[bestType];

      int base_chips = 3;
      int bonus_chips = PointBottomHand(bestType);

      int pot = 0;
      int winners = 0;
      int bestCard = 0;

      // Determine chips out and pot size and number of winners and the best card from the winners
      foreach (var seat in table.OccupiedSeats())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          if (tHand.LastEvaluation.Item2 == bestValue)
          {
            winners++;
            seat.ChipsOut = 0;
            bestCard = Math.Max(bestCard, tHand.BottomHand.BestCard());
          }
          else
          {
            seat.ChipsOut = Math.Min(seat.Chips, base_chips + (tHand.LastEvaluation.Item1 == bestType ? 0 : bonus_chips));
          }
          pot += seat.ChipsOut;
        }
      }

      int cut = pot / winners;
      int remainder = pot % winners;

      // Determine chips in
      foreach (var seat in table.OccupiedSeats())
      {
        if (seat.Hand is TaiwaneseHand tHand)
        {
          if (tHand.LastEvaluation.Item2 == bestValue)
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

    private bool PlayScoopBonus(BaseTable table)
    {
      // See if someone scooped all three hands
      bool scooped = table.OccupiedSeats().Any(seat => seat.HandsWon == 3);

      if (scooped)
      {
        int pot = 0;
        // Determine chips out and pot size
        foreach (var seat in table.OccupiedSeats())
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
        foreach (var seat in table.OccupiedSeats())
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

    public override (int, uint) Evaluate(ulong cards)
    {
      throw new ArgumentException("Taiwanese Evaluation must include a hand and a board.");
    }
    public override (int, uint) Evaluate(IHand hand)
    {
      throw new ArgumentException("Taiwanese Evaluation must include a hand and a board.");
    }

    public override (int, uint) Evaluate(ulong hand, ulong board)
    {
      return EvaluateTaiwaneseHand(hand, board);
    }

    public static (int, uint) EvaluateTaiwaneseHand(ulong hand, ulong board)
    {
      var handcards = Bits.IndividualMasks(hand);
      var boardcards = Bits.IndividualMasks(board);
      int bestType, nextType;
      uint bestRank, nextRank;

      (bestType, bestRank) = EvaluateHand(handcards[0] | handcards[1] | boardcards[0] | boardcards[1] | boardcards[2]);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[1] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[1] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[1] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[1] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[1] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[1] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[1] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[1] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[1] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);



      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[2] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[2] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[2] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[2] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[2] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[2] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[2] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[2] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[2] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[2] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);



      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[3] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[3] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[0] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);



      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[2] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[2] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[2] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[2] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[2] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[2] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[2] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[2] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[2] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[2] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);



      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[3] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[3] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[1] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);



      (nextType, nextRank) = EvaluateHand(handcards[2] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[2] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[2] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[2] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[2] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[2] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[2] | handcards[3] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[2] | handcards[3] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[2] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = EvaluateHand(handcards[2] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      return (bestType, bestRank);
    }

  }
}
