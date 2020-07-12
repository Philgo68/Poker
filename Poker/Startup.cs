using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
      var builder = services.AddRazorPages();
      services.AddServerSideBlazor();
      services.AddHttpClient();
      services.AddSingleton(new Helpers.SvgCards());
      SqlMapper.AddTypeHandler(new Helpers.MySqlGuidTypeHandler());
      SqlMapper.RemoveTypeMap(typeof(Guid));
      SqlMapper.RemoveTypeMap(typeof(Guid?));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }
      app.UseStaticFiles();

      var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
      // Add new mappings
      provider.Mappings[".glb"] = "application/octet-stream";
      app.UseStaticFiles(new StaticFileOptions
      {
        ContentTypeProvider = provider
      });

      app.UseHttpsRedirection();

      app.UseRouting();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapBlazorHub();
        endpoints.MapFallbackToPage("/_Host");
      });
    }
  }
}
