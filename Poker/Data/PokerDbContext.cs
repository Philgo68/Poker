using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Poker.Models;

namespace Poker.Data
{
  public class PokerDbContext : IdentityDbContext
  {
    public DbSet<Player> Players { get; set; }
    public DbSet<Table> Tables { get; set; }
    public DbSet<Seat> Seats { get; set; }

    public PokerDbContext(DbContextOptions<PokerDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
      modelBuilder.Entity<Player>()
        .HasIndex(player => player.ScreenName)
        .IsUnique();
    }
  }
}

