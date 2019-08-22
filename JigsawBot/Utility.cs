using System;

namespace JigsawBot
{
    public static class Utility
    {
        private static readonly Random _random = new Random();

        public static string GetWrongAnswerMessage(string name)
        {
            var wrongAnswers = SqliteDataAccess.GetQuotes(QuoteType.WrongAnswer);
            return ":x: " + wrongAnswers[_random.Next(wrongAnswers.Count)].Replace("_NAME_", name);
        }

        public static string GetCorrectAnswerMessage(string name)
        {
            var correctAnswers = SqliteDataAccess.GetQuotes(QuoteType.CorrectAnswer);
            return ":white_check_mark: " + correctAnswers[_random.Next(correctAnswers.Count)].Replace("_NAME_", name);
        }

        public static string GetGreetingMessage(string name)
        {
            var greetings = SqliteDataAccess.GetQuotes(QuoteType.Greeting);
            return greetings[_random.Next(greetings.Count)].Replace("_NAME_", name);
        }
    }
}