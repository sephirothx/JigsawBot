﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace JigsawBot
{
    public class StartupService
    {
        private readonly IServiceProvider    _provider;
        private readonly DiscordSocketClient _discord;
        private readonly CommandService      _commands;
        private readonly IConfigurationRoot  _config;

        public StartupService(IServiceProvider provider,
                              DiscordSocketClient discord,
                              CommandService commands,
                              IConfigurationRoot config)
        {
            _provider = provider;
            _config   = config;
            _discord  = discord;
            _commands = commands;
        }

        public async Task StartAsync()
        {
            string discordToken = _config["tokens:discord"];
            if (string.IsNullOrWhiteSpace(discordToken))
            {
                throw new Exception("Please enter your bot's token into the `config.json` " +
                                    "file found in the applications root directory.");
            }

            await _discord.LoginAsync(TokenType.Bot, discordToken);
            await _discord.StartAsync();

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
            await Task.Delay(-1);
        }
    }
}
