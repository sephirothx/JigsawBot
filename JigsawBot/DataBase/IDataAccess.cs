using System.Collections.Generic;

namespace JigsawBot
{
    public interface IDataAccess
    {
        #region Puzzle

        List<PuzzleModel> GetPuzzles();
        PuzzleModel GetPuzzle(string code);
        List<string> GetPuzzleData(string code, PuzzleDataType type);
        List<PuzzleData> GetAllPuzzlesData(PuzzleDataType type);
        List<CompletedPuzzleModel> GetPuzzleInfo(string code);
        void AddOrUpdatePuzzle(PuzzleModel puzzle);
        void AddPuzzleData(PuzzleDataModel data);

        #endregion

        #region User

        List<UserModel> GetUsers();
        UserModel GetUserById(string userId);
        UserModel GetUserByName(string username);
        void AddOrUpdateUser(UserModel user);
        void UpdateUserPreference_Hide(string userId, bool value);

        #endregion

        #region CompletedPuzzles

        void NewCompletedPuzzle(CompletedPuzzleModel cp);
        bool HasUserCompletedPuzzle(string userId, string puzzleCode);
        List<CompletedPuzzleModel> GetUsersCompletedPuzzles(string userId);
        List<UserModel> GetUsersWhoCompletedPuzzle(string puzzleCode);
        List<PuzzleModel> GetPuzzlesNotSolvedByUser(string userId);

        #endregion

        #region Quotes

        void AddQuote(QuoteModel quote);
        List<string> GetQuotes(QuoteType type);

        #endregion

        #region Backup

        void LocalBackup(string path);

        #endregion
    }
}