using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(Poker.Areas.Identity.IdentityHostingStartup))]
namespace Poker.Areas.Identity
{
  public class IdentityHostingStartup : IHostingStartup
  {
    public void Configure(IWebHostBuilder builder)
    {
      builder.ConfigureServices((context, services) =>
      {
      });
    }
  }
}