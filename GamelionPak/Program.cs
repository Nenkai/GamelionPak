
namespace GamelionPak
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine($"- GamelionPak Extractor by Nenkai");
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("- https://github.com/Nenkai");
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("For IcyTower");
            Console.WriteLine("");

            if (args.Length < 1)
            {
                Console.WriteLine($"Usage: <path to pak file>");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine($"ERR: File does not exist");
                return;
            }

            var pakMount = new PakMount();
            pakMount.Init(args[0]);
            pakMount.ExtractAll();
        }
    }

}