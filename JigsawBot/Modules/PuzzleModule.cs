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
        private readonly List<string> _wrongAnswers = new List<string>
                                                      {
                                                          "You know nothing, _NAME_.",
                                                          "I wouldn't type this answer unless I was you, _NAME_."
                                                      };

        private readonly List<string> _correctAnswers = new List<string>
                                                        {
                                                            "Congratulations _NAME_, you are still alive.",
                                                            "You're goddamn right, _NAME_."
                                                        };

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

            var answers      = SqliteDataAccess.GetPuzzleData(code, PuzzleDataType.Answer);
            var closeAnswers = SqliteDataAccess.GetPuzzleData(code, PuzzleDataType.CloseAnswer);

            if (Contains(closeAnswers, answer))
            {
                await ReplyAsync($"You're on the right track, {user.Mention}, keep thinking!");
            }
            else if (Contains(answers, answer))
            {
                await ReplyAsync(GetCorrectAnswerMessage(user.Mention));

                var cpm = new CompletedPuzzleModel
                          {
                              UserId        = user.Id.ToString(),
                              PuzzleCode    = code,
                              DateCompleted = DateTime.Now.ToUniversalTime()
                          };

                SqliteDataAccess.NewCompletedPuzzle(cpm);
            }
            else
            {
                await ReplyAsync(GetWrongAnswerMessage(user.Mention));
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
            var userId = Context.User.Id.ToString();
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
            var stats   = SqliteDataAccess.GetUsersPuzzlesStats(userId);
            string name = content == null
                              ? "You"
                              : SqliteDataAccess.GetUserById(userId).Name;
            var msg = new EmbedBuilder()
                     .WithTitle("Statistics")
                     .WithColor(Color.Blue)
                     .WithFooter($"{name} solved {stats.Count} puzzles.");

            foreach (var s in stats)
            {
                msg.Description += $"<#{s.PuzzleCode}> {s.DateCompleted:yyyy-MM-dd HH:mm}\n";
            }

            await channel.SendMessageAsync(embed: msg.Build());
            await channel.CloseAsync();
        }

        [Command("leaderboard"), Alias("l")]
        [Summary("Gets user specific stats.")]
        public async Task Leaderboard()
        {
            var message = Context.Message;
            await message.DeleteAsync();

            var users = SqliteDataAccess.GetUsers();
            var msg = new EmbedBuilder()
                     .WithTitle("Leaderboard")
                     .WithColor(Color.Blue);

            msg.Description = $"```{"Username",-25}Solved\n\n";
            foreach (var user in users)
            {
                if (user.Solved == 0) break;
                msg.Description += $"{user.Name,-25}{user.Solved,6}\n";
            }

            msg.Description += "```";

            await ReplyAsync(embed: msg.Build());
        }

        [Command("puzzle"), Alias("p")]
        [Summary("Gets user specific stats.")]
        public async Task Puzzle([Remainder] string content)
        {
            var user    = Context.User;
            var message = Context.Message;
            await message.DeleteAsync();

            var regex = new Regex(@"<#(?<code>\d+)>");
            var match = regex.Match(content);

            if (!match.Success)
            {
                await ReplyAsync($"{user.Mention} Wrong command format. It should be " +
                                 $"`{_prefix}puzzle #channel-name`");
                return;
            }

            string code = match.Groups["code"].Value;

            var puzzleInfo = SqliteDataAccess.GetPuzzleInfo(code);
            var msg = new EmbedBuilder()
                     .WithTitle("Info")
                     .WithColor(Color.Blue)
                     .WithFooter($"Solved {puzzleInfo.Count} times.");

            msg.Description = $"```{"Username",-25}  Date Completed\n\n";
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
            var message = Context.Message;
            await message.DeleteAsync();

            await SetPuzzleData(content, PuzzleDataType.Answer);
        }

        [Command("setcloseanswer"), Alias("sca")]
        [Summary("Adds a close answer to the DataBase.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetCloseAnswer([Remainder] string content)
        {
            var message = Context.Message;
            await message.DeleteAsync();

            await SetPuzzleData(content, PuzzleDataType.CloseAnswer);
        }

        [Command("sethint"), Alias("sh")]
        [Summary("Adds a hint to the DataBase.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetHint([Remainder] string content)
        {
            var message = Context.Message;
            await message.DeleteAsync();

            await SetPuzzleData(content, PuzzleDataType.Hint);
        }

        [Command("getanswer"), Alias("ga")]
        [Summary("Gets an answer from the DataBase.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task GetAnswer([Remainder] string content = null)
        {
            var user    = Context.User;
            var message = Context.Message;
            await message.DeleteAsync();

            var channel = await Context.Message.Author.GetOrCreateDMChannelAsync();

            if (content?.Equals("all") ?? true)
            {
                var puzzles = SqliteDataAccess.GetAllPuzzlesData(PuzzleDataType.Answer);
                var msg = new EmbedBuilder
                          {
                              Title = "All Answers",
                              Color = Color.DarkBlue,
                              Footer = new EmbedFooterBuilder
                                       {
                                           Text = $"Total number of puzzles: {puzzles.Count.ToString()}."
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
                await ReplyAsync($"{user.Mention} Wrong command format. It should be" +
                                 $"`{_prefix}getanswer #channel-name or .getanswer`");
                return;
            }

            var code    = match.Groups["code"].Value;
            var answers = SqliteDataAccess.GetPuzzleData(code, PuzzleDataType.Answer);

            await channel.SendMessageAsync($"<#{code}> {string.Join(", ", answers)}");
            await channel.CloseAsync();
        }

        #region Private

        private string GetWrongAnswerMessage(string name)
        {
            var random = new Random();
            return ":x: " + _wrongAnswers[random.Next(_wrongAnswers.Count)].Replace("_NAME_", name);
        }

        private string GetCorrectAnswerMessage(string name)
        {
            var random = new Random();
            return ":white_check_mark: " + _correctAnswers[random.Next(_correctAnswers.Count)].Replace("_NAME_", name);
        }

        private async Task SetPuzzleData(string content, PuzzleDataType type)
        {
            var regex = new Regex(@"<#(?<code>\d+)>\s*(?<answer>.*)");
            var match = regex.Match(content);

            if (!match.Success)
            {
                string command = type == PuzzleDataType.Answer      ? "setanswer" :
                                 type == PuzzleDataType.CloseAnswer ? "setcloseanswer" :
                                                                      "sethint";

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

            SqliteDataAccess.AddPuzzleData(puzzle);

            await LoggingService.Instance.LogAsync(new LogMessage(LogSeverity.Debug,
                                                                  "Bot",
                                                                  $"Added answer: {code} = {answer}"));
            await ReplyAsync($"Added {type} to problem <#{code}>.");
        }

        private bool Contains(List<string> list, string item)
        {
            return list.Any(s => s.Equals(item, StringComparison.InvariantCultureIgnoreCase));
        }

        #endregion
    }
}
