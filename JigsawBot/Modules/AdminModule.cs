using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace JigsawBot
{
    [RequireUserPermission(GuildPermission.Administrator)]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
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
            SqliteDataAccess.AddQuote(qm);

            await ReplyAsync($"Added new {type} quote.");
        }

        [Command("setanswer"), Alias("sa")]
        [Summary("Adds an answer to the DataBase.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetAnswer([Remainder] string content)
        {
            await SetPuzzleDataAsync(content, PuzzleDataType.Answer);
        }

        [Command("setcloseanswer"), Alias("sca")]
        [Summary("Adds a close answer to the DataBase.")]
        public async Task SetCloseAnswer([Remainder] string content)
        {
            await SetPuzzleDataAsync(content, PuzzleDataType.CloseAnswer);
        }

        [Command("sethint"), Alias("sh")]
        [Summary("Adds a hint to the DataBase.")]
        public async Task SetHint([Remainder] string content)
        {
            await SetPuzzleDataAsync(content, PuzzleDataType.Hint);
        }

        [Command("getanswer"), Alias("ga")]
        [Summary("Gets answers from the DataBase.")]
        public async Task GetAnswer([Remainder] string content = null)
        {
            await GetPuzzleDataAsync(content, PuzzleDataType.Answer);
        }

        [Command("getcloseanswer"), Alias("gca")]
        [Summary("Gets close answers from the DataBase.")]
        public async Task GetCloseAnswer([Remainder] string content = null)
        {
            await GetPuzzleDataAsync(content, PuzzleDataType.CloseAnswer);
        }

        [Command("gethint"), Alias("gh")]
        [Summary("Gets hints from the DataBase.")]
        public async Task GetHint([Remainder] string content = null)
        {
            await GetPuzzleDataAsync(content, PuzzleDataType.Hint);
        }

        #region Private

        private async Task SetPuzzleDataAsync(string content, PuzzleDataType type)
        {
            var message = Context.Message;
            await message.DeleteAsync();

            var regex = new Regex(@"<#(?<code>\d+)>\s*(?<answer>.*)");
            var match = regex.Match(content);

            if (!match.Success)
            {
                await ReplyAsync($"{Context.User.Mention} Wrong command format.");
                return;
            }

            var code   = match.Groups["code"].Value;
            var answer = match.Groups["answer"].Value;

            var puzzle = new PuzzleDataModel
                         {
                             PuzzleCode = code,
                             Type       = type,
                             Data       = answer
                         };

            await BotActions.AddPuzzleDataAsync(puzzle);

            await LoggingService.Instance.LogAsync(new LogMessage(LogSeverity.Debug, "Bot",
                                                                  $"Added answer: {code} = {answer}"));
            await ReplyAsync($"Added {type} to problem <#{code}>.");
        }

        private async Task GetPuzzleDataAsync(string content, PuzzleDataType type)
        {
            var user    = Context.User;
            var message = Context.Message;
            await message.DeleteAsync();

            var channel = await Context.Message.Author.GetOrCreateDMChannelAsync();

            if (content?.Equals("all") ?? true)
            {
                var puzzles = SqliteDataAccess.GetAllPuzzlesData(type);
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

                await channel.SendMessageAsync(embed: msg.Build());
                await channel.CloseAsync();
                return;
            }

            var regex = new Regex(@"<#(?<code>\d+)>");
            var match = regex.Match(content);

            if (!match.Success)
            {
                await ReplyAsync($"{user.Mention} Wrong command format.");
                await channel.CloseAsync();
                return;
            }

            var code = match.Groups["code"].Value;
            var data = SqliteDataAccess.GetPuzzleData(code, type);

            if (data.Any())
            {
                await channel.SendMessageAsync($"{type}: <#{code}> {string.Join(", ", data)}");
            }

            await channel.CloseAsync();
        }

        #endregion
    }
}