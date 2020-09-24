using Poker.Helpers;
using Poker.Interfaces;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Poker.Models
{
  public class Entity
  {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
  }

  public class Seat : Entity, IHandHolder
  {
    [NotMapped]
    private BaseHand hand;

    private int startingChips;

    public Table Table;
    public string PlayerId { get; set; }
    public int Chips { get; set; }
    public int Position { get; set; }
    
    private SeatStatus status;
    public SeatStatus Status { get => status; set { status = value; StateHasChanged(); } }

    [NotMapped]
    public Player Player;

    public int ChipDelta => Chips - startingChips;

    [NotMapped]
    public BaseHand Hand
    {
      get
      {
        return hand;
      }
      set
      {
        hand = value;
        // reset staringChips when a hand starts
        startingChips = Chips;
        // Ask the hand to tell the seat when it has changed.
        if (hand != null)
          hand.StateHasChangedDelegate += StateHasChanged;
      }
    }

    [NotMapped]
    public bool ActionOn { get; set; }

    [NotMapped]
    public int ChipsMoving { get; set; }

    // Pre - calculated from display actions
    [NotMapped]
    public int ChipsOut { get; set; }
    [NotMapped]
    public int ChipsIn { get; set; }
    [NotMapped]
    public int HandsWon { get; set; }
    [NotMapped]
    public bool Button { get; set; }

    public Seat()
    {
      Position = -1;
      Hand = null;
      PlayerId = null;
      Chips = 0;
      Button = false;
      status = SeatStatus.PlayOn;
    }

    public Seat(int position, Player player, int chips)
    {
      Position = position;
      Hand = null;
      PlayerId = player.Id;
      Player = player;
      Chips = chips;
      Button = false;
      status = (chips == 0) ? SeatStatus.SittingOut : SeatStatus.PlayOn;
    }

    public string StatusClass()
    {
      var classes = "";
      if (ChipsIn > 0) classes += "winner";
      if (ActionOn) classes += " action";
      return classes;
    }

    public event Action StateHasChangedDelegate;

    protected void StateHasChanged()
    {
      StateHasChangedDelegate?.Invoke();
    }
  }
}
