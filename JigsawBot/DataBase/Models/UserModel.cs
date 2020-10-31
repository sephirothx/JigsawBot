namespace JigsawBot
{
    public class UserModel
    {
        public string Id         { get; set; }
        public string Name       { get; set; }
        public int    Solved     { get; set; }
        public int    Score      { get; set; }
        public bool   HideSolved { get; set; }

        public UserModel(string id, string name)
        {
            Id         = id;
            Name       = name;
            Solved     = 0;
            Score      = 0;
            HideSolved = true;
        }
    }
}