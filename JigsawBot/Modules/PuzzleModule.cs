using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace JigsawBot
{
    public class PuzzleModule : ModuleBase<SocketCommandContext>
    {
        private readonly IDataAccess   _data;
        private readonly BotActions    _actions;
        private readonly QuotesService _quotes;

        public PuzzleModule(IDataAccess data,
                            BotActions actions,
                            QuotesService quotes)
        {
            _data    = data;
            _actions = actions;
            _quotes  = quotes;
        }

        [Command("answer"), Alias("a")]
        [Summary("Answer a puzzle question.")]
        public async Task Answer(IMessageChannel puzzle, [Remainder] string content)
        {
            var user    = Context.User;
            var message = Context.Message;
            await message.DeleteAsync();

            var code   = puzzle.Id.ToString();
            var userId = user.Id.ToString();
            var answer = content.Replace("|", "");

            if (_data.HasUserCompletedPuzzle(userId, code))
            {
                await ReplyAsync(_quotes.GetAlreadySolvedMessage(user.Mention));
                return;
            }

            var answers      = _data.GetPuzzleData(code, PuzzleDataType.Answer);
            var closeAnswers = _data.GetPuzzleData(code, PuzzleDataType.CloseAnswer);

            string log_msg = $"User **{user.Username}** (`{user.Id}`) - Puzzle <#{puzzle.Id}> - ||{answer}|| ";

            if (Contains(answers, answer))
            {
                await ReplyAsync(_quotes.GetCorrectAnswerMessage(user.Mention));

                await _actions.ProcessCorrectAnswer(user, code);
                await _actions.UpdateLeaderboard();

                log_msg += ":white_check_mark:";
            }
            else if (Contains(closeAnswers, answer))
            {
                await ReplyAsync(_quotes.GetCloseAnswerMessage(user.Mention));

                log_msg += ":radio_button:";
            }
            else
            {
                await ReplyAsync(_quotes.GetWrongAnswerMessage(user.Mention));

                log_msg += ":x:";
            }

            await _actions.SendMessageToChannelAsync(log_msg, Constants.LOGS_CHANNEL);
        }

        [Command("hint")]
        [Summary("Gets a hint for the selected puzzle.")]
        public async Task Hint(IMessageChannel puzzle)
        {
            var message = Context.Message;
            await message.DeleteAsync();

            var hints = _data.GetPuzzleData(puzzle.Id.ToString(),
                                                       PuzzleDataType.Hint);
            if (hints.Any())
            {
                var channel = await Context.User.GetOrCreateDMChannelAsync();
                await channel.SendMessageAsync(string.Join(", ", hints));
                await channel.CloseAsync();
            }
        }

        [Command("stats"), Alias("solved", "s")]
        [Summary("Gets the stats of a specific user.")]
        public async Task Stats(SocketUser user)
        {
            var message = Context.Message;
            await message.DeleteAsync();

            var stats = _data.GetUsersCompletedPuzzles(user.Id.ToString());
            var msg = new EmbedBuilder()
                     .WithTitle("Statistics")
                     .WithColor(Color.Blue)
                     .WithFooter($"{user.Username} solved {stats.Count} puzzles.", user.GetAvatarUrl());

            foreach (var s in stats)
            {
                msg.Description += $"<#{s.PuzzleCode}> {s.DateCompleted:yyyy-MM-dd HH:mm}\n";
            }

            var channel = await Context.User.GetOrCreateDMChannelAsync();
            await channel.SendMessageAsync(embed: msg.Build());
            await channel.CloseAsync();
        }

        [Command("stats"), Alias("solved", "s")]
        [Summary("Gets user stats.")]
        public async Task Stats()
        {
            var user    = Context.User;
            var message = Context.Message;
            await message.DeleteAsync();

            var stats = _data.GetUsersCompletedPuzzles(user.Id.ToString());
            var msg = new EmbedBuilder()
                     .WithTitle("Statistics")
                     .WithColor(Color.Blue)
                     .WithFooter($"You solved {stats.Count} puzzles.", user.GetAvatarUrl());

            msg.Description += ":white_check_mark: Solved\n\n";
            foreach (var s in stats)
            {
                msg.Description += $"<#{s.PuzzleCode}> {s.DateCompleted:yyyy-MM-dd HH:mm}\n";
            }

            var notSolved = _data.GetPuzzlesNotSolvedByUser(user.Id.ToString());

            msg.Description += "\n:x: Not Solved\n\n";
            foreach (var notSolvedPuzzle in notSolved)
            {
                msg.Description += $"<#{notSolvedPuzzle.Code}>\n";
            }

            var channel = await Context.User.GetOrCreateDMChannelAsync();
            await channel.SendMessageAsync(embed: msg.Build());
            await channel.CloseAsync();
        }

        [Command("puzzle"), Alias("p")]
        [Summary("Gets puzzle specific stats.")]
        public async Task Puzzle(IMessageChannel puzzle)
        {
            string puzzleCode = puzzle.Id.ToString();

            var puzzleModel = _data.GetPuzzle(puzzleCode);
            var puzzleInfo  = _data.GetPuzzleInfo(puzzleCode);

            var msg = new EmbedBuilder()
                     .WithTitle("Info")
                     .WithColor(Color.Blue)
                     .WithFooter($"Solved {puzzleInfo.Count} times.");

            msg.Description = $"This puzzle is worth {puzzleModel.Points} points." +
                              $"```{"Username",-25}  Date Completed\n\n";
            foreach (var info in puzzleInfo)
            {
                msg.Description += $"{_data.GetUserById(info.UserId).Name,-25}" +
                                   $"{info.DateCompleted,16:yyyy-MM-dd HH:mm}\n";
            }

            msg.Description += "```";

            await ReplyAsync($"<#{puzzle.Id}>", embed: msg.Build());
        }

        [Command("puzzle"), Alias("p")]
        [Summary("Gets stats for all the puzzles.")]
        public async Task Puzzle()
        {
            var puzzles = _data.GetPuzzles();

            var msg = new EmbedBuilder()
                     .WithTitle("All Puzzles")
                     .WithColor(Color.Blue);

            foreach (var p in puzzles.OrderByDescending(p => p.Points))
            {
                msg.Description += $"<#{p.Code}> :  {p.Points}\n";
            }

            await ReplyAsync(embed: msg.Build());
        }

        #region Private

        private static bool Contains(List<string> list, string item)
        {
            return list.Any(s => s.Equals(item, StringComparison.InvariantCultureIgnoreCase));
        }

        #endregion
    }
}
