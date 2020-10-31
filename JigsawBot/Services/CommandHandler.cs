using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace JigsawBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService      _commands;
        private readonly IConfigurationRoot  _config;
        private readonly IServiceProvider    _provider;
        private readonly IDataAccess         _data;
        private readonly LoggingService      _logger;
        private readonly BotActions          _actions;
        private readonly QuotesService       _quotes;

        public CommandHandler(DiscordSocketClient client,
                              CommandService commands,
                              IConfigurationRoot config,
                              IServiceProvider provider,
                              IDataAccess data,
                              LoggingService logger,
                              BotActions actions,
                              QuotesService quotes)
        {
            _client   = client;
            _commands = commands;
            _config   = config;
            _provider = provider;
            _data     = data;
            _logger   = logger;
            _actions  = actions;
            _quotes   = quotes;

            _client.MessageReceived       += OnMessageReceivedAsync;
            _client.UserJoined            += OnUserJoined;
            _client.ReactionAdded         += OnReactionAdded;
            _client.ReactionRemoved       += OnReactionRemoved;
            _client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
            _client.Connected             += OnClientConnected;
        }

        private async Task OnClientConnected()
        {
            await _client.SetGameAsync("you.", type: ActivityType.Watching);
        }

        private async Task OnUserVoiceStateUpdated(SocketUser user,
                                                   SocketVoiceState state_1,
                                                   SocketVoiceState state_2)
        {
            if (user.IsBot) return;

            var channel = _actions.GetChannelFromConfig(Constants.VOICE_CHANNEL);

            if (state_1.VoiceChannel == null && state_2.VoiceChannel != null)
            {
                var log = new LogMessage(LogSeverity.Verbose, "Bot",
                                         $"{user.Username} joined voice channel {state_2.VoiceChannel.Name}. " +
                                         $"Users in channel: {state_2.VoiceChannel.Users.Count(u => !u.IsBot)}.");
                await _logger.LogAsync(log);

                await _actions.SetChannelViewPermissionAsync(user, channel, false);
                await _actions.SendDirectMessageAsync(user,
                                                      $"You now have access to {channel.Mention}. "                               +
                                                      "You can use it to discuss puzzles with the other users in the voice chat " +
                                                      "and it will be wiped when everyone leaves the voice chat.");
            }
            else if (state_1.VoiceChannel != null && state_2.VoiceChannel == null)
            {
                var log = new LogMessage(LogSeverity.Verbose, "Bot",
                                         $"{user.Username} left voice channel {state_1.VoiceChannel.Name}. " +
                                         $"Users in channel: {state_1.VoiceChannel.Users.Count(u => !u.IsBot)}.");
                await _logger.LogAsync(log);

                await _actions.SetChannelViewPermissionAsync(user, channel, true);
                if (state_1.VoiceChannel.Users.Count(u => !u.IsBot) == 0)
                {
                    await _actions.PurgeChannel(channel);
                }
            }
        }

        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> message,
                                           ISocketMessageChannel channel,
                                           SocketReaction reaction)
        {
            if (message.Id == ulong.Parse(_config[Constants.PREFERENCES_MESSAGE]) &&
                reaction.Emote.Name.Equals("👀"))
            {
                var user = reaction.User.Value;

                await _actions.SetSolvedChannelsViewPermissionAsync(user, false);
                await _actions.SendDirectMessageAsync(user, "Your solved puzzles are now being shown.");
            }
        }

        private async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> message,
                                             ISocketMessageChannel channel,
                                             SocketReaction reaction)
        {
            if (message.Id == ulong.Parse(_config[Constants.PREFERENCES_MESSAGE]) &&
                reaction.Emote.Name.Equals("👀"))
            {
                var user = reaction.User.Value;

                await _actions.SetSolvedChannelsViewPermissionAsync(user, true);
                await _actions.SendDirectMessageAsync(user, "Your solved puzzles are now being hidden.");
            }
        }

        private async Task OnUserJoined(SocketGuildUser u)
        {
            if (u.IsBot)
            {
                return;
            }

            if (_data.GetUserById(u.Id.ToString()) == null)
            {
                var user = new UserModel(u.Id.ToString(), u.Username);
                _data.AddOrUpdateUser(user);
            }

            await _actions.SendMessageToChannelAsync(_quotes.GetGreetingMessage(u.Mention),
                                                     Constants.GREETING_CHANNEL);
            await _actions.SendHelpMessageAsync(u);
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            if (!(s is SocketUserMessage msg))
            {
                return;
            }

            if (msg.Author.Id == _client.CurrentUser.Id)
            {
                return;
            }

            var context = new SocketCommandContext(_client, msg);

            int argPos = 0;
            if (msg.HasStringPrefix(_config["prefix"], ref argPos) ||
                msg.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                await _logger.LogAsync(new LogMessage(LogSeverity.Verbose, "Bot",
                                                      $"{msg.Author.Id} - {msg.Author.Username}: {msg.Content}"));
                var result = await _commands.ExecuteAsync(context, argPos, _provider);

                if (!result.IsSuccess)
                {
                    await _logger.LogAsync(new LogMessage(LogSeverity.Error, "Bot", result.ToString()));
                }
            }
        }
    }
}
