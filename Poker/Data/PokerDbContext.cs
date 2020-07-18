using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Poker.Data
{
  public class PokerDbContext : IdentityDbContext
  {
    public PokerDbContext(DbContextOptions<PokerDbContext> options)
        : base(options)
    {
    }
  }
}
