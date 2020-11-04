using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace JigsawBot
{
    [RequireOwner]
    public class DebugModule : ModuleBase<SocketCommandContext>
    {
        private readonly IDataAccess _data;
        private readonly BotActions  _actions;

        public DebugModule(IDataAccess data,
                           BotActions actions)
        {
            _data    = data;
            _actions = actions;
        }

        [Command("leaderboard"), Alias("lb")]
        [Summary("Updates the leaderboard.")]
        public async Task Leaderboard()
        {
            var message = Context.Message;
            await message.DeleteAsync();

            await _actions.UpdateLeaderboard();
        }

        [Command("updateusers"), Alias("upu")]
        [Summary("Updates the user database.")]
        public async Task UpdateUsers()
        {
            await Context.Guild.DownloadUsersAsync();
            var users = Context.Guild.Users;
            foreach (var user in users)
            {
                if (user.IsBot || _data.GetUserById(user.Id.ToString()) != null)
                    continue;

                var u = new UserModel(user.Id.ToString(), user.Username);
                _data.AddOrUpdateUser(u);
            }

            await ReplyAsync("User database updated.");
        }

        [Command("updatepuzzles"), Alias("upp")]
        [Summary("Updates the puzzle database.")]
        public async Task UpdatePuzzles()
        {
            var puzzles = _data.GetPuzzles();

            foreach (var puzzle in puzzles)
            {
                var puzzleInfo = _data.GetPuzzleInfo(puzzle.Code);
                puzzle.Points = BotActions.CalculatePuzzlePoints(puzzleInfo.Count);

                _data.AddOrUpdatePuzzle(puzzle);
            }

            await ReplyAsync("Puzzle database updated.");
        }

        [Command("calculateuserscore"), Alias("cus")]
        [Summary("Updates the scores in the user database.")]
        public async Task CalculateUserScore()
        {
            var users = _data.GetUsers();
            foreach (var user in users)
            {
                user.Score = 0;
                var solved = _data.GetUsersCompletedPuzzles(user.Id);
                foreach (var puzzle in solved)
                {
                    var p = _data.GetPuzzle(puzzle.PuzzleCode);
                    user.Score += p.Points;
                }

                _data.AddOrUpdateUser(user);
            }

            await ReplyAsync("User scores updated.");
        }

        [Command("updatehidden"), Alias("uh")]
        [Summary("Updates the hide-solved-puzzles preferences.")]
        public async Task UpdateHidden()
        {
            var users = Context.Guild.Users;
            foreach (var user in users)
            {
                if (user.IsBot) continue;

                await _actions.SetSolvedChannelsViewPermissionAsync(user, true);
            }

            await ReplyAsync("Hidden already solved puzzles for all users.");
        }

        [Command("purge")]
        [Summary("Clears all the messages from a channel.")]
        public async Task Purge(ITextChannel channel)
        {
            await _actions.PurgeChannel(channel);
            await ReplyAsync($"Channel {channel.Mention} purged.");
        }
    }
}
