using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JigsawBot
{
    public class BotContainer
    {
        public IConfigurationRoot Configuration { get; }

        public BotContainer()
        {
            var builder = new ConfigurationBuilder()
                         .SetBasePath(AppContext.BaseDirectory)
                         .AddJsonFile("config.json");

            Configuration = builder.Build();
        }

        public static async Task RunAsync()
        {
            var startup = new BotContainer();
            await startup.RunHelperAsync();
        }


        public async Task RunHelperAsync()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IDataAccess>();
            provider.GetRequiredService<BotActions>();
            provider.GetRequiredService<QuotesService>();
            provider.GetRequiredService<LoggingService>();
            provider.GetRequiredService<CommandHandler>();

            await provider.GetRequiredService<StartupService>().StartAsync();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            var client = new DiscordSocketClient(new DiscordSocketConfig
                                                 {
                                                     LogLevel            = LogSeverity.Verbose,
                                                     MessageCacheSize    = 100,
                                                     ExclusiveBulkDelete = true
                                                 });

            services.AddSingleton(client)
                    .AddSingleton(new CommandService(new CommandServiceConfig
                                                     {
                                                         LogLevel       = LogSeverity.Verbose,
                                                         DefaultRunMode = RunMode.Async
                                                     }))
                    .AddSingleton<CommandHandler>()
                    .AddSingleton<StartupService>()
                    .AddSingleton<LoggingService>()
                    .AddSingleton<QuotesService>()
                    .AddSingleton<Random>()
                    .AddSingleton<IDataAccess>(new SqliteDataAccess())
                    .AddSingleton<BotActions>()
                    .AddSingleton(Configuration);
        }
    }
}
