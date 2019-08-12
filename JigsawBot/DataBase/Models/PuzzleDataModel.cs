namespace JigsawBot
{
    public enum PuzzleDataType
    {
        Answer = 0,
        CloseAnswer = 1,
        Hint = 2
    }

    public class PuzzleDataModel
    {
        public string PuzzleCode { get; set; }
        public PuzzleDataType Type { get; set; }
        public string Data { get; set; }
    }
}