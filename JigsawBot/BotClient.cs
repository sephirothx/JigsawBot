using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JigsawBot
{
    public class BotClient
    {
        public IConfigurationRoot Configuration { get; }
        public DiscordSocketClient Client { get; private set; }

        private static BotClient _instance;
        public static BotClient Instance => _instance ?? (_instance = new BotClient());

        public BotClient()
        {
            var builder = new ConfigurationBuilder()
                         .SetBasePath(AppContext.BaseDirectory)
                         .AddJsonFile("config.json");

            Configuration = builder.Build();
        }

        public async Task RunAsync()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<LoggingService>();
            provider.GetRequiredService<CommandHandler>();
            provider.GetRequiredService<BackupService>();

            await provider.GetRequiredService<StartupService>().StartAsync();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig
                                             {
                                                 LogLevel         = LogSeverity.Verbose,
                                                 MessageCacheSize = 1000
                                             });
            services.AddSingleton(Client)
                    .AddSingleton(new CommandService(new CommandServiceConfig
                                                     {
                                                         LogLevel       = LogSeverity.Verbose,
                                                         DefaultRunMode = RunMode.Async,
                                                     }))
                    .AddSingleton<CommandHandler>()
                    .AddSingleton<StartupService>()
                    .AddSingleton<LoggingService>()
                    .AddSingleton<BackupService>()
                    .AddSingleton<Random>()
                    .AddSingleton(Configuration);
        }
    }
}
