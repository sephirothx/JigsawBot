using System.Threading.Tasks;
using Discord.Commands;

namespace JigsawBot
{
    [RequireOwner]
    public class DebugModule : ModuleBase<SocketCommandContext>
    {
        [Command("leaderboard"), Alias("l")]
        [Summary("Updates the leaderboard.")]
        public async Task Leaderboard()
        {
            var message = Context.Message;
            await message.DeleteAsync();

            await BotActions.UpdateLeaderboard();
        }

        [Command("updateusers"), Alias("upu")]
        [Summary("Updates the user database.")]
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
        public async Task UpdatePuzzles()
        {
            var puzzles = SqliteDataAccess.GetPuzzles();

            foreach (var puzzle in puzzles)
            {
                var puzzleInfo = SqliteDataAccess.GetPuzzleInfo(puzzle.Code);
                int n          = puzzleInfo.Count;

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
    }
}