using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace JigsawBot
{
    public class GeneralModule : ModuleBase<SocketCommandContext>
    {
        [Command("say")]
        [Summary("Makes the bot say something.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Say([Remainder] string text)
        {
            var message = Context.Message;
            await message.DeleteAsync();

            await ReplyAsync(text);
        }

        [Command("say")]
        [Summary("Makes the bot say something in a specific channel.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Say(IMessageChannel channel, [Remainder] string text)
        {
            var message = Context.Message;
            await message.DeleteAsync();

            await channel.SendMessageAsync(text);
        }

        [Command("updateusers"), Alias("upu")]
        [Summary("Updates the user database.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task UpdateUsers()
        {
            var users = Context.Guild.Users;
            foreach (var user in users)
            {
                if (user.IsBot || SqliteDataAccess.GetUserById(user.Id.ToString()) != null)
                    continue;

                var u = new UserModel
                        {
                            Id     = user.Id.ToString(),
                            Name   = user.Username,
                            Solved = 0,
                            Score  = 0
                        };

                SqliteDataAccess.AddOrUpdateUser(u);
            }

            await ReplyAsync("User database updated.");
        }

        [Command("updatepuzzles"), Alias("upp")]
        [Summary("Updates the puzzle database.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task UpdatePuzzles()
        {
            var puzzles = SqliteDataAccess.GetPuzzles();

            foreach (var puzzle in puzzles)
            {
                var puzzleInfo = SqliteDataAccess.GetPuzzleInfo(puzzle.Code);
                int n = puzzleInfo.Count;

                if (n > 10)
                {
                    puzzle.Points = 1;
                }
                else
                {
                    puzzle.Points = Constants.PUZZLE_STARTING_POINTS / (1 << n);
                }

                SqliteDataAccess.AddOrUpdatePuzzle(puzzle);
            }

            await ReplyAsync("Puzzle database updated.");
        }

        [Command("calculateuserscore"), Alias("cus")]
        [Summary("Updates the scores in the user database.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CalculateUserScore()
        {
            var users = SqliteDataAccess.GetUsers();
            foreach (var user in users)
            {
                user.Score = 0;
                var solved = SqliteDataAccess.GetUsersCompletedPuzzles(user.Id);
                foreach (var puzzle in solved)
                {
                    var p = SqliteDataAccess.GetPuzzle(puzzle.PuzzleCode);
                    user.Score += p.Points;
                }

                SqliteDataAccess.AddOrUpdateUser(user);
            }

            await ReplyAsync("User scores updated.");
        }

        [Command("setquote"), Alias("sq")]
        [Summary("Adds a quote to the database.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetQuote(QuoteType type, [Remainder] string quote)
        {
            var qm = new QuoteModel
                     {
                         Quote = quote,
                         Type = type
                     };
            SqliteDataAccess.AddQuote(qm);

            await ReplyAsync($"Added new {type}.");
        }

        [Command("help"), Alias("h")]
        [Summary("Posts a description of the usable commands.")]
        public async Task Help()
        {
            var user    = Context.User as SocketGuildUser;
            var message = Context.Message;
            await message.DeleteAsync();

            await BotActions.SendHelpMessageAsync(user);
        }
    }
}
