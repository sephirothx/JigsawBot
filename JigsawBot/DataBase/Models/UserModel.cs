namespace JigsawBot
{
    public class UserModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Solved { get; set; }
        public int Score { get; set; }
        public bool HideSolved { get; set; }
    }
}