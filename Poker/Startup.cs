using BlazorStrap;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Poker.Areas.Identity;
using Poker.Data;
using Poker.Models;

namespace Poker
{
  public class Startup
  {
    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
      Configuration = configuration;
      Env = env;
    }

    public IConfiguration Configuration { get; }

    public IWebHostEnvironment Env { get; set; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
      var db = new PokerDbContext(new DbContextOptionsBuilder<PokerDbContext>().UseSqlite(Configuration.GetConnectionString("PokerContextConnection")).Options);
      services.AddSingleton(db);
      //services.AddDbContext<PokerDbContext>(options => options.UseSqlite(Configuration.GetConnectionString("PokerContextConnection")));
      services.AddDefaultIdentity<Player>(options => options.SignIn.RequireConfirmedAccount = true)
        .AddEntityFrameworkStores<PokerDbContext>()
        .AddUserValidator<ScreenNameValidator<Player>>();
      services.AddRazorPages();
      services.AddServerSideBlazor();
      services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<Player>>();
      services.AddHttpClient();
      services.AddSingleton(new Helpers.SvgCards());
      services.AddSingleton(new Helpers.Dealers(db));

      services.AddBootstrapCss();

      //SqlMapper.AddTypeHandler(new Helpers.MySqlGuidTypeHandler());
      //SqlMapper.RemoveTypeMap(typeof(Guid));
      //SqlMapper.RemoveTypeMap(typeof(Guid?));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, PokerDbContext db)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseDatabaseErrorPage();
      }
      else
      {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }

      app.UseHttpsRedirection();
      app.UseStaticFiles();

      var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
      // Add new mappings
      provider.Mappings[".glb"] = "application/octet-stream";
      app.UseStaticFiles(new StaticFileOptions
      {
        ContentTypeProvider = provider
      });

      db.Database.EnsureCreated();

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
        endpoints.MapBlazorHub();
        endpoints.MapFallbackToPage("/_Host");
      });
    }
  }
}
