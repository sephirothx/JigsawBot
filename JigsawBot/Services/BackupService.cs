using System;
using System.IO;
using System.Timers;
using Discord;
using Microsoft.Extensions.Configuration;

namespace JigsawBot
{
    public class BackupService
    {
        private readonly IConfigurationRoot _config;
        private readonly LoggingService     _logger;
        private readonly IDataAccess        _data;

        private readonly Timer _timer = new Timer(60 * 1000);

        public BackupService(IConfigurationRoot config,
                             LoggingService logger,
                             IDataAccess data)
        {
            _config = config;
            _logger = logger;
            _data   = data;

            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            string backup_path = Path.Combine(_config["backup_path"], $"{DateTime.UtcNow:yyyy-MM}");
            if (!Directory.Exists(backup_path))
            {
                Directory.CreateDirectory(backup_path);
            }

            string backupFile = Path.Combine(backup_path,
                                             $"backup_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.db");

            try
            {
                _data.LocalBackup(backupFile);
                _logger.LogAsync(new LogMessage(LogSeverity.Verbose,
                                                "Backup",
                                                $"{backupFile} saved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogAsync(new LogMessage(LogSeverity.Error,
                                                "Backup",
                                                "Failed to backup the Database.",
                                                ex));
            }
        }
    }
}
