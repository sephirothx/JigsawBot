using System;

namespace JigsawBot
{
    class Program
    {
        private static void Main()
        {
            try
            {
                BotClient.Instance.RunAsync().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Caught exception: {e}");
                throw;
            }
        }
    }
}
