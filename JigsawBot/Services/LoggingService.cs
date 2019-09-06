using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace JigsawBot
{
    public class LoggingService
    {
        private readonly IConfigurationRoot _config;

        private string LogDirectory => Path.Combine(_config["logs_path"], $"{DateTime.UtcNow:yyyy-MM}");
        private string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

        public LoggingService(DiscordSocketClient discord,
                              CommandService commands,
                              IConfigurationRoot config)
        {
            _config = config;

            discord.Log  += LogAsync;
            commands.Log += LogAsync;
        }

        public Task LogAsync(LogMessage msg)
        {
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            if (!File.Exists(LogFile))
            {
                File.Create(LogFile).Dispose();
            }

            string logText = $"{DateTime.UtcNow:HH:mm:ss} " +
                             $"[{msg.Severity}] "           +
                             $"{msg.Source}: "              +
                             $"{msg.Exception?.ToString() ?? msg.Message}";
            File.AppendAllText(LogFile, logText + Environment.NewLine);

            return Console.Out.WriteLineAsync(logText);
        }
    }
}
