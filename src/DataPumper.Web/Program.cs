using DataPumper.Core;
using DataPumper.PostgreSql;
using DataPumper.Sql;
using DataPumper.Web.DataLayer;
using DataPumper.Web.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Linq;

namespace DataPumper.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();

            var contextDirectory = "context";
            Directory.CreateDirectory(contextDirectory);
            var sqliteBuilder = new SqliteConnectionStringBuilder();
            sqliteBuilder.DataSource = Path.Combine(contextDirectory, "DataPumper.db");
            builder.Services.AddDbContext<DataPumperContext>(opts => { opts.UseSqlite(sqliteBuilder.ToString()); });

            builder.Services.AddTransient<IDataPumperSource, SqlDataPumperSourceTarget>();
            builder.Services.AddTransient<IDataPumperTarget, SqlDataPumperSourceTarget>();
            builder.Services.AddTransient<IDataPumperSource, PostgreSqlDataPumperSource>();
            builder.Services.AddTransient<IDataPumperTarget, PostgreSqlDataPumperTarget>();
            
            builder.Services.AddTransient<Core.DataPumper>();
            builder.Services.AddTransient<DataPumpService>();
            
            builder.Services.AddHangfire(x => x.UseMemoryStorage());
            builder.Services.AddHangfireServer();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseHangfireDashboard("/jobs", new DashboardOptions
            {
                Authorization = new[] { new AllRequestsAuthorizationFilter() }
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetService<DataPumperContext>();
            context.Database.Migrate();
            context.Seed();
            var cron = context.Settings.FirstOrDefault(s => s.Key == Setting.Cron)?.Value ?? "0 30 3 ? * *";
            RecurringJob.AddOrUpdate<DataPumpService>(DataPumpService.JobId, s => s.Process(false), cron);
            RecurringJob.AddOrUpdate<DataPumpService>(DataPumpService.FullReloadJobId, s=>s.Process(true), Cron.Never);

            app.Run();
        }
    }
}