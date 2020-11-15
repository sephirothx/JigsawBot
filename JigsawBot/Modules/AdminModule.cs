using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace JigsawBot
{
    [RequireUserPermission(GuildPermission.Administrator)]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        private readonly IDataAccess    _data;
        private readonly LoggingService _logger;
        private readonly BotActions     _actions;

        public AdminModule(IDataAccess data,
                           LoggingService logger,
                           BotActions actions)
        {
            _data    = data;
            _logger  = logger;
            _actions = actions;
        }

        [Command("say")]
        [Summary("Makes the bot say something.")]
        public async Task Say([Remainder] string text)
        {
            var message = Context.Message;
            await message.DeleteAsync();

            await ReplyAsync(text);
        }

        [Command("say")]
        [Summary("Makes the bot say something in a specific channel.")]
        public async Task Say(IMessageChannel channel, [Remainder] string text)
        {
            var message = Context.Message;
            await message.DeleteAsync();

            await channel.SendMessageAsync(text);
        }

        [Command("setquote"), Alias("sq")]
        [Summary("Adds a quote to the database.")]
        public async Task SetQuote(QuoteType type, [Remainder] string quote)
        {
            var qm = new QuoteModel
                     {
                         Quote = quote,
                         Type  = type
                     };
            _data.AddQuote(qm);

            await ReplyAsync($"Added new {type} quote.");
        }

        [Command("setanswer"), Alias("sa")]
        [Summary("Adds an answer to the DataBase.")]
        public async Task SetAnswer(IMessageChannel channel, [Remainder] string content)
        {
            await SetPuzzleDataAsync(channel, content, PuzzleDataType.Answer);
        }

        [Command("setcloseanswer"), Alias("sca")]
        [Summary("Adds a close answer to the DataBase.")]
        public async Task SetCloseAnswer(IMessageChannel channel, [Remainder] string content)
        {
            await SetPuzzleDataAsync(channel, content, PuzzleDataType.CloseAnswer);
        }

        [Command("sethint"), Alias("sh")]
        [Summary("Adds a hint to the DataBase.")]
        public async Task SetHint(IMessageChannel channel, [Remainder] string content)
        {
            await SetPuzzleDataAsync(channel, content, PuzzleDataType.Hint);
        }

        [Command("getanswer"), Alias("ga")]
        [Summary("Gets answers from the DataBase.")]
        public async Task GetAnswer(IMessageChannel channel = null)
        {
            await GetPuzzleDataAsync(channel, PuzzleDataType.Answer);
        }

        [Command("getcloseanswer"), Alias("gca")]
        [Summary("Gets close answers from the DataBase.")]
        public async Task GetCloseAnswer(IMessageChannel channel = null)
        {
            await GetPuzzleDataAsync(channel, PuzzleDataType.CloseAnswer);
        }

        [Command("gethint"), Alias("gh")]
        [Summary("Gets hints from the DataBase.")]
        public async Task GetHint(IMessageChannel channel = null)
        {
            await GetPuzzleDataAsync(channel, PuzzleDataType.Hint);
        }

        #region Private

        private async Task SetPuzzleDataAsync(IMessageChannel channel, string content, PuzzleDataType type)
        {
            var message = Context.Message;
            await message.DeleteAsync();

            string code = channel.Id.ToString();

            var puzzle = new PuzzleDataModel
                         {
                             PuzzleCode = code,
                             Type       = type,
                             Data       = content
                         };

            await _actions.AddPuzzleDataAsync(puzzle);

            await _logger.LogAsync(new LogMessage(LogSeverity.Debug, "Bot", $"Added answer: {code} = {content}"));
            await ReplyAsync($"Added {type} to problem <#{code}>.");
        }

        private async Task GetPuzzleDataAsync(IMessageChannel channel, PuzzleDataType type)
        {
            var message = Context.Message;
            await message.DeleteAsync();

            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();

            if (channel == null)
            {
                var puzzles = _data.GetAllPuzzlesData(type);
                var msg = new EmbedBuilder
                          {
                              Title = $"All {type}s",
                              Color = Color.Blue,
                              Footer = new EmbedFooterBuilder
                                       {
                                           Text = $"Total: {puzzles.Count.ToString()}."
                                       }
                          };

                foreach (var puzzle in puzzles)
                {
                    msg.Description += $"<#{puzzle.Code}> {string.Join(", ", puzzle.Data)}\n";
                }

                await dmChannel.SendMessageAsync(embed: msg.Build());
                await dmChannel.CloseAsync();
                return;
            }

            var code = channel.Id.ToString();
            var data = _data.GetPuzzleData(code, type);

            if (data.Any())
            {
                await dmChannel.SendMessageAsync($"{type}: <#{code}> {string.Join(", ", data)}");
            }

            await dmChannel.CloseAsync();
        }

        #endregion
    }
}
