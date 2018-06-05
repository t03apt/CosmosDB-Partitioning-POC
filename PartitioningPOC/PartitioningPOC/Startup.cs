using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace PartitioningPOC
{
    internal class Startup
    {
        private static async Task Main()
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .CreateLogger();

                var collection = new ServiceCollection();
                collection
                    .AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true))
                    .AddSingleton<Application>();

                var serviceProvider = collection.BuildServiceProvider(true);

                var app = serviceProvider.GetRequiredService<Application>();
                await app.Run();
            }
            catch (Exception e)
            {
                LogException(e);
            }
            finally
            {
                Log.Logger.Information("End of demo, press any key to exit.");
                Console.ReadKey();
            }
        }

        private static void LogException(Exception e)
        {
            ConsoleColor color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            if (e is DocumentClientException)
            {
                DocumentClientException de = (DocumentClientException)e;
                Exception baseException = de.GetBaseException();
                Log.Logger.Information("{0} error occurred: {1}, Message: {2}", de.StatusCode, de.Message, baseException.Message);
            }
            else
            {
                Exception baseException = e.GetBaseException();
                Log.Logger.Information("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }

            Console.ForegroundColor = color;
        }
    }
}
