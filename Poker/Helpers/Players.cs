using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Poker.Data;
using Poker.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Poker.Helpers
{
  public class Players
  {
    private readonly ConcurrentDictionary<string, Player> _players;
    private IApplicationBuilder _provider;
    private PokerDbContext _dbContext;

    public Players()
    {
      _players = new ConcurrentDictionary<string, Player>();
    }

    public void SetProvider(IApplicationBuilder provider)
    {
      _provider = provider;
      _dbContext = _provider.ApplicationServices.CreateScope().ServiceProvider.GetService<PokerDbContext>();
    }

    public Player SingularPlayer(string playerId)
    {
      return _players.GetOrAdd(playerId, (key) =>
      {
        var player = _dbContext.Players
          .SingleOrDefault(p => p.Id == playerId);
        player.dbContext = _dbContext;
        return player;
      });
    }

  }
}

