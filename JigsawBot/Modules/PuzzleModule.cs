using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace JigsawBot
{
    public class PuzzleModule : ModuleBase<SocketCommandContext>
    {
        private readonly string _prefix = BotClient.Instance.Configuration["prefix"];

        [Command("answer"), Alias("a")]
        [Summary("Answer a puzzle question.")]
        public async Task Answer([Remainder] string content)
        {
            var user    = Context.User;
            var message = Context.Message;

            await message.DeleteAsync();

            var regex = new Regex(@"<#(?<code>\d+)>\s*(?<answer>.*)");
            var match = regex.Match(content.Replace("|", ""));

            if (!match.Success)
            {
                await ReplyAsync($"{user.Mention} Wrong command format. It should be " +
                                 $"`{_prefix}answer #channel-name answer`");
                return;
            }

            var code   = match.Groups["code"].Value;
            var answer = match.Groups["answer"].Value;

            if (SqliteDataAccess.HasUserCompletedPuzzle(user.Id.ToString(), code))
            {
                await ReplyAsync($"{user.Mention} you have already solved this puzzle. " +
                                 "Try to move on with your life.");
                return;
            }

            var answers      = SqliteDataAccess.GetPuzzleData(code, PuzzleDataType.Answer);
            var closeAnswers = SqliteDataAccess.GetPuzzleData(code, PuzzleDataType.CloseAnswer);

            if (Contains(closeAnswers, answer))
            {
                await ReplyAsync($"You're on the right track, {user.Mention}, keep thinking!");
            }
            else if (Contains(answers, answer))
            {
                await ReplyAsync(Utility.GetCorrectAnswerMessage(user.Mention));

                var p = SqliteDataAccess.GetPuzzle(code);
                var u = SqliteDataAccess.GetUserById(user.Id.ToString());

                u.Score += p.Points;
                u.Solved++;
                SqliteDataAccess.AddOrUpdateUser(u);

                if (p.Points != 1)
                {
                    var usersWhoSolvedPuzzle = SqliteDataAccess.GetUsersWhoCompletedPuzzle(code);

                    foreach (var userWhoSolvedPuzzle in usersWhoSolvedPuzzle)
                    {
                        userWhoSolvedPuzzle.Score -= p.Points;
                        SqliteDataAccess.AddOrUpdateUser(userWhoSolvedPuzzle);
                    }

                    SqliteDataAccess.UpdatePuzzlePoints(code);
                }

                var cpm = new CompletedPuzzleModel
                          {
                              UserId        = user.Id.ToString(),
                              PuzzleCode    = code,
                              DateCompleted = DateTime.Now.ToUniversalTime()
                          };

                SqliteDataAccess.NewCompletedPuzzle(cpm);

                await BotActions.UpdateLeaderboard();
            }
            else
            {
                await ReplyAsync(Utility.GetWrongAnswerMessage(user.Mention));
            }
        }

        [Command("hint")]
        [Summary("Gets a hint for the selected puzzle.")]
        public async Task Hint([Remainder] string content)
        {
            var user    = Context.User;
            var message = Context.Message;

            await message.DeleteAsync();

            var regex = new Regex(@"<#(?<code>\d+)>");
            var match = regex.Match(content);

            if (!match.Success)
            {
                await ReplyAsync($"{user.Mention} Wrong command format. It should be " +
                                 $"`{_prefix}hint #channel-name`");
                return;
            }

            var code  = match.Groups["code"].Value;
            var hints = SqliteDataAccess.GetPuzzleData(code, PuzzleDataType.Hint);

            if (hints.Any())
            {
                var channel = await Context.Message.Author.GetOrCreateDMChannelAsync();
                await channel.SendMessageAsync(string.Join(", ", hints));
                await channel.CloseAsync();
            }
        }

        [Command("stats"), Alias("s")]
        [Summary("Gets user specific stats.")]
        public async Task Stats([Remainder] string content = null)
        {
            var userId  = Context.User.Id.ToString();
            var message = Context.Message;

            await message.DeleteAsync();

            if (content != null)
            {
                var regex = new Regex(@"<@(?<code>\d+)>");
                var match = regex.Match(content);

                userId = match.Success
                             ? match.Groups["code"].Value
                             : SqliteDataAccess.GetUserByName(content)?.Id;

                if (userId == null)
                {
                    await ReplyAsync($"404: Username '{content}' not found.");
                    return;
                }
            }

            var channel = await Context.Message.Author.GetOrCreateDMChannelAsync();
            var stats   = SqliteDataAccess.GetUsersCompletedPuzzles(userId);
            string name = content == null
                              ? "You"
                              : SqliteDataAccess.GetUserById(userId).Name;
            var msg = new EmbedBuilder()
                     .WithTitle("Statistics")
                     .WithColor(Color.Blue)
                     .WithFooter($"{name} solved {stats.Count} puzzles.");

            msg.Description += ":white_check_mark: Solved\n\n";
            foreach (var s in stats)
            {
                msg.Description += $"<#{s.PuzzleCode}> {s.DateCompleted:yyyy-MM-dd HH:mm}\n";
            }

            if (content == null)
            {
                var notSolved = SqliteDataAccess.GetPuzzlesNotSolvedByUser(userId);
                
                msg.Description += "\n:x: Not Solved\n\n";
                foreach (var notSolvedPuzzle in notSolved)
                {
                    msg.Description += $"<#{notSolvedPuzzle.Code}>\n";
                }
            }

            await channel.SendMessageAsync(embed: msg.Build());
            await channel.CloseAsync();
        }

        [Command("puzzle"), Alias("p")]
        [Summary("Gets puzzle specific stats.")]
        public async Task Puzzle([Remainder] string content = null)
        {
            var user    = Context.User;
            var message = Context.Message;
            await message.DeleteAsync();

            if (content == null || 
                content.Equals("all", StringComparison.InvariantCultureIgnoreCase))
            {
                var puzzles = SqliteDataAccess.GetPuzzles();
                
                var m = new EmbedBuilder()
                       .WithTitle("All Puzzles")
                       .WithColor(Color.Blue);

                foreach (var p in puzzles.OrderByDescending(p => p.Points))
                {
                    m.Description += $"<#{p.Code}> :  {p.Points}\n";
                }

                await ReplyAsync(embed: m.Build());
                return;
            }

            var regex = new Regex(@"<#(?<code>\d+)>");
            var match = regex.Match(content);

            if (!match.Success)
            {
                await ReplyAsync($"{user.Mention} Wrong command format. It should be " +
                                 $"`{_prefix}puzzle #channel-name`");
                return;
            }

            string code = match.Groups["code"].Value;

            var puzzle = SqliteDataAccess.GetPuzzle(code);
            var puzzleInfo = SqliteDataAccess.GetPuzzleInfo(code);
            var msg = new EmbedBuilder()
                     .WithTitle("Info")
                     .WithColor(Color.Blue)
                     .WithFooter($"Solved {puzzleInfo.Count} times.");

            msg.Description = $"This puzzle is worth {puzzle.Points} points." +
                              $"```{"Username",-25}  Date Completed\n\n";
            foreach (var info in puzzleInfo)
            {
                msg.Description += $"{SqliteDataAccess.GetUserById(info.UserId).Name,-25}" +
                                   $"{info.DateCompleted,16:yyyy-MM-dd HH:mm}\n";
            }

            msg.Description += "```";

            await ReplyAsync($"<#{code}>", embed: msg.Build());
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
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetCloseAnswer([Remainder] string content)
        {
            await SetPuzzleDataAsync(content, PuzzleDataType.CloseAnswer);
        }

        [Command("sethint"), Alias("sh")]
        [Summary("Adds a hint to the DataBase.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetHint([Remainder] string content)
        {
            await SetPuzzleDataAsync(content, PuzzleDataType.Hint);
        }

        [Command("getanswer"), Alias("ga")]
        [Summary("Gets answers from the DataBase.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task GetAnswer([Remainder] string content = null)
        {
            await GetPuzzleDataAsync(content, PuzzleDataType.Answer);
        }

        [Command("getcloseanswer"), Alias("gca")]
        [Summary("Gets close answers from the DataBase.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task GetCloseAnswer([Remainder] string content = null)
        {
            await GetPuzzleDataAsync(content, PuzzleDataType.CloseAnswer);
        }

        [Command("gethint"), Alias("gh")]
        [Summary("Gets hints from the DataBase.")]
        [RequireUserPermission(GuildPermission.Administrator)]
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
                string command = type == PuzzleDataType.Answer      ? "setanswer" :
                                 type == PuzzleDataType.CloseAnswer ? "setcloseanswer" :
                                 type == PuzzleDataType.Hint        ? "sethint" :
                                                                      throw new Exception($"Unknown PuzzleDataType: {type}");

                await ReplyAsync($"{Context.User.Mention} Wrong command format. It should be " +
                                 $"`{_prefix}{command} #channel-name answer`");
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
                string command = type == PuzzleDataType.Answer      ? "setanswer" :
                                 type == PuzzleDataType.CloseAnswer ? "setcloseanswer" :
                                 type == PuzzleDataType.Hint        ? "sethint" :
                                                                      throw new Exception($"Unknown PuzzleDataType: {type}");

                await ReplyAsync($"{user.Mention} Wrong command format. It should be" +
                                 $"`{_prefix}{command} #channel-name` or `{_prefix}{command}`");
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

        private static bool Contains(List<string> list, string item)
        {
            return list.Any(s => s.Equals(item, StringComparison.InvariantCultureIgnoreCase));
        }

        #endregion
    }
}
