using System;

namespace MissionPatcher {
    internal class Program {
        public static void Main(string[] args) {
            if (args.Length == 0) {
                Console.WriteLine("No file path given");
                return;
            }

            Console.WriteLine($"Patching '{args[0]}'");
            Patcher patcher = new Patcher(args[0]);
            int exitCode = patcher.Patch();
            switch (exitCode) {
                case 1:
                    Console.WriteLine("Patching failed");
                    break;
                case 2:
                    Console.WriteLine("Patching stopped as mission is ignored");
                    break;
                default:
                    Console.WriteLine("Patching successful");
                    break;
            }
        }
    }
}