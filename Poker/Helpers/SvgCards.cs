using Microsoft.AspNetCore.Components;
using Poker.Models;
using System.Text.RegularExpressions;

namespace Poker.Helpers
{
  public class SvgCards
  {
    public MarkupString BackRed { get; set; }
    public MarkupString BackBlack { get; set; }
    public MarkupString[] Faces { get; set; }
    public MarkupString[] FacesAsImage { get; set; }

    public SvgCards()
    {
      LoadCards();
    }

    // Utility used to make mass adjustments to the SVG files or to imbed the SVG in the page
    private MarkupString LoadFile(string name)
    {
      string svg = System.IO.File.ReadAllText($"wwwroot/images/cards/{name}.svg");

      //svg = svg.Replace("class=\"card\"", "class=\"playing-card\"");
      //svg = svg.Replace("height=\"3.5in\"", "");
      //svg = svg.Replace("width=\"2.5in\"", ""); 
      //svg = svg.Replace("preserveAspectRatio=\"none\"", "");

      //svg = svg.Replace("class=\"playing-card\"", "");
      //svg = svg.Replace("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>", "");
      //svg = svg.Replace("\n", "");
      //svg = svg.Replace("\r", "");

      // Squeezes multiple spaces down to one
      //Regex regex = new Regex("[ ]{2,}", RegexOptions.None);
      //svg = regex.Replace(svg, " ");

      //System.IO.File.WriteAllText($"wwwroot/images/cards/{name}.svg", svg);

      svg = svg.Replace("face=\"", "class=\"playing-card\" face=\"");

      return new MarkupString(svg);
    }

    public void LoadCards()
    {
      //var test = LoadFile("BackRed");
      //test = LoadFile("BackBlack");

      BackRed = (MarkupString)$"<object class='playing-card' type='image/svg+xml' data='images/cards/BackRed.svg'></object>";
      BackBlack = (MarkupString)$"<object class='playing-card' type='image/svg+xml' data='images/cards/BackBlack.svg'></object>";

      var deck = new StandardDeck();
      int i = 0;
      Faces = new MarkupString[deck.Suits * deck.Ranks];
      FacesAsImage = new MarkupString[deck.Suits * deck.Ranks];
      foreach (var suit in deck.SuitDescriptions)
      {
        foreach (var rank in deck.RankDescriptions)
        {
          //test = LoadFile($"{rank}{suit}");
          
          // Embeded SVG too large, sends cards too often
          //Faces[i++] = LoadFile($"{rank}{suit}");
          
          // Send the SVG as an object so it can be cached 
          Faces[i] = (MarkupString)$"<object class='playing-card' type='image/svg+xml' data='images/cards/{rank}{suit}.svg'></object>";
          
          // Send the SVG as an image so it can be cached (Note: sending as an image works, but does not rotate and scale like an SVG should
          FacesAsImage[i] = (MarkupString)$"<img class='playing-card' src='images/cards/{rank}{suit}.svg'>";
          i++;
        }
      }
    }
  }
}

