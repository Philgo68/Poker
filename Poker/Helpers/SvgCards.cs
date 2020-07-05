using Microsoft.AspNetCore.Components;
using Poker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Poker.Helpers
{
  public class SvgCards
  {
    public MarkupString BackRed { get; set; }
    public MarkupString BackBlack { get; set; }
    public MarkupString[] Faces { get; set; }

    public SvgCards()
    {
      LoadCards();
    }

    private MarkupString LoadFile(string name)
    {
      string svg = System.IO.File.ReadAllText($"wwwroot/images/cards/{name}.svg");

      // <svg xmlns="http://www.w3.org/2000/svg" class="        card" face="2B" height="3.5in" preserveAspectRatio="none" viewBox="-120 -168 240 336" width="2.5in"><defs><pattern id="B2" width="6" height="6" patternUnits="userSpaceOnUse"><path d="M3 0L6 3L3 6L0 3Z" fill="red"></path></pattern></defs><rect width="239" height="335" x="-119.5" y="-167.5" rx="12" ry="12" fill="white" stroke="black"></rect><rect fill="url(#B2)" width="216" height="312" x="-108" y="-156" rx="12" ry="12"></rect></svg>
      // <svg xmlns="http://www.w3.org/2000/svg" class="playing-card" face="2B" viewBox="-120 -168 240 336"><defs><pattern id = "B2" width="6" height="6" patternUnits="userSpaceOnUse"><path d = "M3 0L6 3L3 6L0 3Z" fill="red"></path></pattern></defs><rect width = "239" height="335" x="-119.5" y="-167.5" rx="12" ry="12" fill="white" stroke="black"></rect><rect fill = "url(#B2)" width="216" height="312" x="-108" y="-156" rx="12" ry="12"></rect></svg>

      //svg = svg.Replace("class=\"card\"", "class=\"playing-card\"");
      //svg = svg.Replace("height=\"3.5in\"", "");
      //svg = svg.Replace("width=\"2.5in\"", "");
      //svg = svg.Replace("preserveAspectRatio=\"none\"", "");

      //System.IO.File.WriteAllText($"wwwroot/images/cards/{name}.svg", svg);

      return new MarkupString(svg);
    }

    public void LoadCards()
    {
      //BackRed = new MarkupString(await httpClient.GetStringAsync("images/BackRed.svg"));
      //BackBlack = new MarkupString(await httpClient.GetStringAsync("images/BackBlack.svg"));

      BackRed = LoadFile("BackRed");
      BackBlack = LoadFile("BackBlack");
      var deck = new StandardDeck();
      int i = 0;
      Faces = new MarkupString[deck.Suits * deck.Ranks];
      foreach (var suit in deck.SuitDescriptions)
      {
        foreach (var rank in deck.RankDescriptions)
        {
          Faces[i++] = LoadFile($"{rank}{suit}");
        }
      }
    }
  }
}

