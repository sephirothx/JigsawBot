namespace JigsawBot
{
    public enum PuzzleDataType
    {
        Answer      = 0,
        CloseAnswer = 1,
        Hint        = 2
    }

    public enum QuoteType
    {
        CorrectAnswer = 0,
        WrongAnswer   = 1,
        Greeting      = 2,
        CloseAnswer   = 3,
        AlreadySolved = 4,
        NotAPuzzle    = 5
    }
}
