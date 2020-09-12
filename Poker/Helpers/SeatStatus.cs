using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Poker.Helpers
{

  public enum SeatStatus
  {
    Leaving,
    SittingOut,
    PleasePause,
    PlayOne,
    PlayOn,
  }

  public static class Icon
  {
    public static readonly string[] seatStatusIcons = { "fas fa-running", "fas fa-ban", "far fa-pause-circle", "far fa-play-circle", null };

    public static string SeatStatus(SeatStatus ss)
    {
      return seatStatusIcons[(int)ss];
    }
  }

}
