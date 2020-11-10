using System;

namespace JigsawBot
{
    public class QuotesService
    {
        private readonly Random      _random;
        private readonly IDataAccess _data;

        public QuotesService(Random random, IDataAccess data)
        {
            _random = random;
            _data   = data;
        }

        public string GetWrongAnswerMessage(string name)
        {
            return ":x: " + GetQuoteWithName(QuoteType.WrongAnswer, name);
        }

        public string GetCorrectAnswerMessage(string name)
        {
            return ":white_check_mark: " + GetQuoteWithName(QuoteType.CorrectAnswer, name);
        }

        public string GetGreetingMessage(string name)
        {
            return GetQuoteWithName(QuoteType.Greeting, name);
        }

        public string GetCloseAnswerMessage(string name)
        {
            return ":radio_button: " + GetQuoteWithName(QuoteType.CloseAnswer, name);
        }

        public string GetAlreadySolvedMessage(string name)
        {
            return ":warning: " + GetQuoteWithName(QuoteType.AlreadySolved, name);
        }

        public string GetNotAPuzzleMessage(string name)
        {
            return GetQuoteWithName(QuoteType.NotAPuzzle, name);
        }

        private string GetQuoteWithName(QuoteType type, string name)
        {
            var quotes = _data.GetQuotes(type);
            return quotes[_random.Next(quotes.Count)].Replace("_NAME_", name);
        }
    }
}
