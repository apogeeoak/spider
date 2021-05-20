using System;

namespace Apogee.Bot
{
    class Program
    {
        static void Main(string[] args)
        {
            string error = Config.TryCreate(args, out Config? config);

            // Print usage if configuration is invalid.
            if (!string.IsNullOrWhiteSpace(error) || config is null)
            {
                Console.WriteLine($"{error}\n\n{Config.Usage()}");
                return;
            }

            Console.WriteLine($"Uri: {config.Uri}");
        }
    }
}
