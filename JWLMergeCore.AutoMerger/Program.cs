namespace JWLMergeCore.AutoMerger
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Hosting;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;

    public static class Program
    {
        private static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<Worker>();
            });
    }
}