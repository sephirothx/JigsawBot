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
        public static List<UserModel> GetUsers()
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
            {
                var output = connection.Query<UserModel>("SELECT * FROM USER ORDER BY Solved DESC",
                                                         new DynamicParameters());
                return output.ToList();
            }
        }

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

        public static Dictionary<string, string> GetPuzzlesDictionary()
        {
            return GetPuzzles().ToDictionary(puzzle => puzzle.Code, puzzle => puzzle.Answer);
        }

        public static List<PuzzleModel> GetPuzzles()
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
            {
                var output = connection.Query<PuzzleModel>("SELECT * FROM PUZZLE", new DynamicParameters());
                return output.ToList();
            }
        }

        public static void SetAnswerToNewPuzzle(PuzzleModel puzzle)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
            {
                connection.Execute("INSERT OR REPLACE INTO PUZZLE (Code, Data) VALUES (@Code, @Data)", puzzle);
            }
        }

        public static void AddPuzzleData(PuzzleDataModel data)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
            {
                connection.Execute("INSERT OR REPLACE INTO PUZZLE (Code) VALUES (@PuzzleCode)", data);
                connection.Execute("INSERT INTO PUZZLEDATA (PuzzleCode, Type, Data) VALUES (@PuzzleCode, @Type, @Data)",
                                   data);
            }
        }

        public static void AddNewUser(UserModel user)
        {
            using (IDbConnection connection = new SQLiteConnection(GetConfigurationString()))
            {
                connection.Execute("INSERT OR REPLACE INTO USER (Id, Name, Solved) VALUES (@Id, @Name, @Solved)", user);
            }
        }

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

        public static List<CompletedPuzzleModel> GetUsersPuzzlesStats(string userId)
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

        private static string GetConfigurationString()
        {
            return @"Data Source=.\ProjectDB.db;Version=3;";
        }
    }
}
