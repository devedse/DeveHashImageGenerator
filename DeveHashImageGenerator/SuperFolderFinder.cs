using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DeveHashImageGenerator
{
    public static class SuperFolderFinder
    {
        public static string RoboFlowDirectory => _roboflowDirectoryLazy.Value;
        private static Lazy<string> _roboflowDirectoryLazy => new Lazy<string>(GenerateRoboFlowDirectory);

        private static string GenerateRoboFlowDirectory()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var possibleDirectory = Path.Combine(currentDirectory, "DeveHashImageGeneratorContent", "Robohash");
            if (Directory.Exists(Path.Combine(possibleDirectory, "sets")))
            {
                return possibleDirectory;
            }

            var assemblyPath = typeof(SuperFolderFinder).Assembly.Location;
            var assemblyFolderPath = Path.GetDirectoryName(assemblyPath);
            possibleDirectory = Path.Combine(assemblyFolderPath, "DeveHashImageGeneratorContent", "Robohash");
            if (assemblyFolderPath != null)
            {
                if (Directory.Exists(Path.Combine(possibleDirectory, "sets")))
                {
                    return possibleDirectory;
                }
            }

            return Path.Join(SubmodulesDirectory, "Robohash", "robohash");
        }

        public static string SubmodulesDirectory => _submodulesDirectoryLazy.Value;
        private static Lazy<string> _submodulesDirectoryLazy => new Lazy<string>(GenerateSubmodulesDirectory);

        private static string GenerateSubmodulesDirectory()
        {
            var cur = Directory.GetCurrentDirectory();

            while (true)
            {
                var submodulesPath = Path.Combine(cur, "submodules");

                if (Directory.Exists(submodulesPath))
                {
                    return submodulesPath;
                }

                if (cur == Directory.GetDirectoryRoot(cur))
                {
                    throw new DirectoryNotFoundException($"No 'submodules' directory found up to the root directory");
                }

                cur = Directory.GetParent(cur).FullName;
            }
        }

    }
}
