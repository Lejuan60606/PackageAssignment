using System;
using System.Threading;
using System.Threading.Tasks;
using Assignment.Data;
using Assignment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestSharp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                config.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var apiSettings = context.Configuration.GetSection("ApiSettings").Get<ApiSettings>()
                                    ?? throw new InvalidOperationException($"Settings {nameof(ApiSettings)} not found");

                services.AddSingleton(apiSettings);

                services.AddSingleton<IRestClient>(sp => new RestClient(apiSettings.BaseUrl));

                services.AddSingleton<LeeegoooService>();

                services.AddLogging(configure => configure.AddConsole());
            })
            .Build();

        var leeegoooService = host.Services.GetRequiredService<LeeegoooService>();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var setChecker = new SetChecker();
        var cancellationTokenSource = new CancellationTokenSource();

        try
        {
            var user = await leeegoooService.GetUserByUsernameAsync("brickfan35", cancellationTokenSource.Token);
            if (user == null)
            {
                logger.LogError("Failed to fetch user data for 'brickfan35'");
                return;
            }

            var sets = await leeegoooService.GetAllSetsAsync(cancellationTokenSource.Token);
            if (sets == null)
            {
                logger.LogError("Failed to fetch sets data");
                return;
            }

            var buildableSets = setChecker.GetBuildableSets(user, sets);

            Console.WriteLine("Sets that brickfan35 can build:");
            foreach (var buildableSet in buildableSets)
            {
                Console.WriteLine($"{buildableSet.Name}");
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Operation was canceled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while processing the sets for 'brickfan35'");
        }

        await host.RunAsync();
    }
}
