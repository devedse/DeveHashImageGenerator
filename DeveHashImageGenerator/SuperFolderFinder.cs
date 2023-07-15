using System;
using System.Collections.Generic;
using System.Linq;
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
            var potentialSetsDirectory = Path.Combine(currentDirectory, "sets");
            if (Directory.Exists(potentialSetsDirectory))
            {
                return potentialSetsDirectory;
            }

            //For tests
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
