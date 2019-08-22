using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using Dapper;

namespace JigsawBot
{
    public static class SqliteDataAccess
    {
        #region Puzzle

        public static List<PuzzleData> GetAllPuzzlesData(PuzzleDataType type)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
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

        public static List<string> GetPuzzleData(string code, PuzzleDataType type)
        {
            return GetPuzzleDataHelper(code, type).Select(p => p.Data).ToList();
        }

        private static List<PuzzleDataModel> GetPuzzleDataHelper(string code, PuzzleDataType type)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
            {
                var output = connection.Query<PuzzleDataModel>("SELECT PuzzleCode, Type, Data " +
                                                               "FROM PUZZLEDATA "               +
                                                               $"WHERE PuzzleCode='{code}' AND Type='{(int)type}'",
                                                               new DynamicParameters());
                return output.ToList();
            }
        }

        public static List<PuzzleModel> GetPuzzles()
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
            {
                var output = connection.Query<PuzzleModel>("SELECT * FROM PUZZLE", new DynamicParameters());
                return output.ToList();
            }
        }

        public static PuzzleModel GetPuzzle(string code)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
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

        public static void AddOrUpdatePuzzle(PuzzleModel puzzle)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
            {
                connection.Execute("INSERT OR REPLACE INTO PUZZLE (Code, Points) VALUES (@Code, @Points)", puzzle);
            }
        }

        public static void AddPuzzleData(PuzzleDataModel data)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
            {
                connection.Execute("INSERT INTO PUZZLEDATA (PuzzleCode, Type, Data) " +
                                   "VALUES (@PuzzleCode, @Type, @Data)",
                                   data);
            }
        }

        public static List<CompletedPuzzleModel> GetPuzzleInfo(string code)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
            {
                var output = connection.Query<CompletedPuzzleModel>("SELECT * FROM COMPLETEDPUZZLES " +
                                                                    $"WHERE PuzzleCode='{code}' "     +
                                                                    "ORDER BY DateCompleted ASC",
                                                                    new DynamicParameters());
                return output.ToList();
            }
        }

        public static void UpdatePuzzlePoints(string code)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
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

        public static void AddOrUpdateUser(UserModel user)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
            {
                connection.Execute("INSERT OR REPLACE INTO USER (Id, Name, Solved, Score) " +
                                   "VALUES (@Id, @Name, @Solved, @Score)",
                                   user);
            }
        }

        public static List<UserModel> GetUsers()
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
            {
                var output = connection.Query<UserModel>("SELECT * FROM USER ORDER BY Score DESC",
                                                         new DynamicParameters());
                return output.ToList();
            }
        }

        public static UserModel GetUserById(string userId)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
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

        public static UserModel GetUserByName(string username)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
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

        public static void NewCompletedPuzzle(CompletedPuzzleModel cp)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
            {
                connection.Execute("INSERT OR REPLACE INTO COMPLETEDPUZZLES (UserId, PuzzleCode, DateCompleted) " +
                                   "VALUES (@UserId, @PuzzleCode, @DateCompleted)", cp);

                connection.Execute("UPDATE USER "                                                             +
                                   "SET Solved=(SELECT COUNT(*) FROM COMPLETEDPUZZLES WHERE UserId=@UserId) " +
                                   "WHERE Id=@UserId", cp);
            }
        }

        public static List<CompletedPuzzleModel> GetUsersCompletedPuzzles(string userId)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
            {
                var output = connection.Query<CompletedPuzzleModel>("SELECT * FROM COMPLETEDPUZZLES " +
                                                                    $"WHERE UserId='{userId}' "       +
                                                                    "ORDER BY DateCompleted DESC",
                                                                    new DynamicParameters());
                return output.ToList();
            }
        }

        public static bool HasUserCompletedPuzzle(string userId, string puzzleCode)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
            {
                var output = connection.Query<CompletedPuzzleModel>("SELECT * FROM COMPLETEDPUZZLES " +
                                                                    $"WHERE UserId='{userId}' " +
                                                                    $"AND PuzzleCode='{puzzleCode}'",
                                                                    new DynamicParameters());
                return output.Any();
            }
        }

        public static List<UserModel> GetUsersWhoCompletedPuzzle(string puzzleCode)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
            {
                var output = connection.Query<UserModel>("select Id, Name, Solved, Score from User " +
                                                         "inner join CompletedPuzzles "              +
                                                         "on User.Id=CompletedPuzzles.UserId "       +
                                                         $"where CompletedPuzzles.PuzzleCode='{puzzleCode}'",
                                                         new DynamicParameters());
                return output.ToList();
            }
        }

        #endregion

        #region Private

        private static string GetConfigurationString()
        {
            return @"Data Source=.\ProjectDB.db;Version=3;";
        }

        #endregion
    }
}
