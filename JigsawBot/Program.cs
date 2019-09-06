using System;

namespace JigsawBot
{
    class Program
    {
        private static void Main()
        {
            try
            {
                BotContainer.RunAsync().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Caught exception: {e}");
                throw;
            }
        }
    }
}
