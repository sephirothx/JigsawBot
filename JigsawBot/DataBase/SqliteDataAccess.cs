using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using Dapper;

namespace JigsawBot
{
    public class SqliteDataAccess : IDataAccess
    {
        private static string ConfigurationString => ConfigurationManager.ConnectionStrings["ProjectDB"].ConnectionString;
        
        #region Puzzle

        public List<PuzzleData> GetAllPuzzlesData(PuzzleDataType type)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                var output = connection.Query<PuzzleDataModel>("SELECT PuzzleCode, Type, Data " +
                                                               "FROM PUZZLEDATA "               +
                                                               $"WHERE Type='{(int)type}'",
                                                               new DynamicParameters());
                return (from p in output
                        group p.Data by p.PuzzleCode into g
                        select new PuzzleData {Code = g.Key, Data = g.ToList()}).ToList();
            }
        }

        public List<string> GetPuzzleData(string code, PuzzleDataType type)
        {
            return GetPuzzleDataHelper(code, type).Select(p => p.Data).ToList();
        }

        private List<PuzzleDataModel> GetPuzzleDataHelper(string code, PuzzleDataType type)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                var output = connection.Query<PuzzleDataModel>("SELECT PuzzleCode, Type, Data " +
                                                               "FROM PUZZLEDATA "               +
                                                               $"WHERE PuzzleCode='{code}' AND Type='{(int)type}'",
                                                               new DynamicParameters());
                return output.ToList();
            }
        }

        public List<PuzzleModel> GetPuzzles()
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                var output = connection.Query<PuzzleModel>("SELECT * FROM PUZZLE", new DynamicParameters());
                return output.ToList();
            }
        }

        public PuzzleModel GetPuzzle(string code)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                var output = connection.Query<PuzzleModel>("SELECT * FROM PUZZLE " +
                                                           $"WHERE Code={code}", new DynamicParameters());
                try
                {
                    return output.First();
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public void AddOrUpdatePuzzle(PuzzleModel puzzle)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                connection.Execute("INSERT OR REPLACE INTO PUZZLE (Code, Points) VALUES (@Code, @Points)", puzzle);
            }
        }

        public void AddPuzzleData(PuzzleDataModel data)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                connection.Execute("INSERT INTO PUZZLEDATA (PuzzleCode, Type, Data) " +
                                   "VALUES (@PuzzleCode, @Type, @Data)",
                                   data);
            }
        }

        public List<CompletedPuzzleModel> GetPuzzleInfo(string code)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                var output = connection.Query<CompletedPuzzleModel>("SELECT * FROM COMPLETEDPUZZLES " +
                                                                    $"WHERE PuzzleCode='{code}' "     +
                                                                    "ORDER BY DateCompleted ASC",
                                                                    new DynamicParameters());
                return output.ToList();
            }
        }

        public void UpdatePuzzlePoints(string code)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                var puzzle = connection.Query<PuzzleModel>("SELECT * FROM PUZZLE " +
                                                           $"WHERE Code={code}",
                                                           new DynamicParameters())
                                       .First();

                puzzle.Points = puzzle.Points == 1
                                    ? 1
                                    : puzzle.Points / 2;

                connection.Execute("UPDATE PUZZLE "      +
                                   "SET Points=@Points " +
                                   "WHERE Code=@Code",
                                   puzzle);
            }
        }

        #endregion

        #region User

        public void AddOrUpdateUser(UserModel user)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                connection.Execute("INSERT OR REPLACE INTO USER (Id, Name, Solved, Score, HideSolved) " +
                                   "VALUES (@Id, @Name, @Solved, @Score, @HideSolved)",
                                   user);
            }
        }

        public void UpdateUserPreference_Hide(string userId, bool value)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                connection.Execute("UPDATE USER "                       +
                                   $"SET HideSolved={(value ? 1 : 0)} " +
                                   $"WHERE Id='{userId}'");
            }
        }

        public List<UserModel> GetUsers()
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                var output = connection.Query<UserModel>("SELECT * FROM USER ORDER BY Score DESC, Solved ASC",
                                                         new DynamicParameters());
                return output.ToList();
            }
        }

        public UserModel GetUserById(string userId)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                var output = connection.Query<UserModel>($"SELECT * FROM USER WHERE Id='{userId}'",
                                                         new DynamicParameters());
                try
                {
                    return output.First();
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public UserModel GetUserByName(string username)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                var output = connection.Query<UserModel>($"SELECT * FROM USER WHERE Name='{username}'",
                                                         new DynamicParameters());
                try
                {
                    return output.First();
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        #endregion

        #region Completed Puzzles

        public void NewCompletedPuzzle(CompletedPuzzleModel cp)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                connection.Execute("INSERT OR REPLACE INTO COMPLETEDPUZZLES (UserId, PuzzleCode, DateCompleted) " +
                                   "VALUES (@UserId, @PuzzleCode, @DateCompleted)", cp);

                connection.Execute("UPDATE USER "                                                             +
                                   "SET Solved=(SELECT COUNT(*) FROM COMPLETEDPUZZLES WHERE UserId=@UserId) " +
                                   "WHERE Id=@UserId", cp);
            }
        }

        public List<CompletedPuzzleModel> GetUsersCompletedPuzzles(string userId)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                var output = connection.Query<CompletedPuzzleModel>("SELECT * FROM COMPLETEDPUZZLES " +
                                                                    $"WHERE UserId='{userId}' "       +
                                                                    "ORDER BY DateCompleted DESC",
                                                                    new DynamicParameters());
                return output.ToList();
            }
        }

        public bool HasUserCompletedPuzzle(string userId, string puzzleCode)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                var output = connection.Query<CompletedPuzzleModel>("SELECT * FROM COMPLETEDPUZZLES " +
                                                                    $"WHERE UserId='{userId}' "       +
                                                                    $"AND PuzzleCode='{puzzleCode}'",
                                                                    new DynamicParameters());
                return output.Any();
            }
        }

        public List<UserModel> GetUsersWhoCompletedPuzzle(string puzzleCode)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                var output = connection.Query<UserModel>("select Id, Name, Solved, Score from User " +
                                                         "inner join CompletedPuzzles "              +
                                                         "on User.Id=CompletedPuzzles.UserId "       +
                                                         $"where CompletedPuzzles.PuzzleCode='{puzzleCode}'",
                                                         new DynamicParameters());
                return output.ToList();
            }
        }

        public List<PuzzleModel> GetPuzzlesNotSolvedByUser(string userId)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                var output = connection.Query<PuzzleModel>("SELECT * FROM Puzzle "                       +
                                                           "WHERE Code NOT IN ("                         +
                                                           "SELECT Puzzle.Code FROM Puzzle "             +
                                                           "INNER JOIN CompletedPuzzles "                +
                                                           "ON puzzle.code=completedpuzzles.PuzzleCode " +
                                                           $"WHERE completedPuzzles.UserId='{userId}')",
                                                           new DynamicParameters());
                return output.ToList();
            }
        }

        #endregion

        #region Quotes

        public void AddQuote(QuoteModel quote)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                connection.Execute("INSERT INTO QUOTES (Quote, Type) " +
                                   "VALUES (@Quote, @Type)",
                                   quote);
            }
        }

        public List<string> GetQuotes(QuoteType type)
        {
            return GetQuotesHelper(type).Select(p => p.Quote).ToList();
        }

        private List<QuoteModel> GetQuotesHelper(QuoteType type)
        {
            using (IDbConnection connection = new SQLiteConnection(ConfigurationString))
            {
                var output = connection.Query<QuoteModel>("SELECT Quote, Type " +
                                                          "FROM QUOTES "        +
                                                          $"WHERE Type='{(int)type}'",
                                                          new DynamicParameters());
                return output.ToList();
            }
        }

        #endregion

        #region Backup

        public void LocalBackup(string path)
        {
            var destConfig = $@"Data Source={path};Version=3;";

            using (var local = new SQLiteConnection(ConfigurationString))
            using (var dest = new SQLiteConnection(destConfig))
            {
                local.Open();
                dest.Open();
                local.BackupDatabase(dest, "main", "main", -1, null, 0);
            }
        }

        #endregion
    }
}