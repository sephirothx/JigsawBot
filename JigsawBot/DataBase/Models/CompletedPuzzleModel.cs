using System;

namespace JigsawBot
{
    public class CompletedPuzzleModel
    {
        public string UserId { get; set; }
        public string PuzzleCode { get; set; }
        public DateTime DateCompleted { get; set; }
    }
}