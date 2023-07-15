using System.Security.Cryptography;
using System.Text;
using Image = SixLabors.ImageSharp.Image;

namespace DeveHashImageGenerator.RoboHash
{
    public class RoboHashGenerator
    {
        private readonly int _hashCount;
        private readonly bool _ignoreExt;

        private string _resourceDir = SuperFolderFinder.RoboFlowDirectory;
        private List<string> _sets;
        private List<string> _bgSets;
        private List<string> _colors;

        public RoboHashGenerator(int hashCount = 11, bool ignoreExt = true)
        {
            _hashCount = hashCount;
            _ignoreExt = ignoreExt;

            _sets = ListDirs(Path.Combine(_resourceDir, "sets"));
            _bgSets = ListDirs(Path.Combine(_resourceDir, "backgrounds"));
            _colors = ListDirs(Path.Combine(_resourceDir, "sets", "set1"));
        }

        private string RemoveExtensions(string input)
        {
            string[] extensions = { ".png", ".gif", ".jpg", ".bmp", ".jpeg", ".ppm", ".datauri" };

            foreach (var ext in extensions)
            {
                if (input.ToLower().EndsWith(ext))
                {
                    // remove extension
                    input = input.Substring(0, input.Length - ext.Length);
                    break;
                }
            }

            return input;
        }

        private List<long> CreateHashes(string hexdigest, int count)
        {
            List<long> hashArray = new List<long>();
            int blockSize = hexdigest.Length / count;

            for (int i = 0; i < count; i++)
            {
                int currentStart = (1 + i) * blockSize - blockSize;
                int currentEnd = (1 + i) * blockSize;
                string sub = hexdigest.Substring(currentStart, blockSize);
                hashArray.Add(Convert.ToInt64(sub, 16));
            }

            hashArray.AddRange(hashArray);
            return hashArray;
        }

        private List<string> ListDirs(string path)
        {
            IEnumerable<string> allItems = Directory
                .GetDirectories(path)
                .Select(t => Path.GetFileName(t))
                .Where(t => !string.IsNullOrWhiteSpace(t) && !t.StartsWith("."))
                .Select(t => t!)
                .OrderBy(t => t);

            Console.WriteLine("ListDirs:");
            Console.WriteLine(string.Join(",", allItems));

            return allItems.ToList();
        }

        private List<string> GetListOfFiles(List<long> hashArray, string path)
        {
            // Start this at 4, so earlier is reserved
            //0 = Color
            //1 = Set
            //2 = bgset
            //3 = BG
            var iter = 4;

            List<string> chosenFiles = new List<string>();
            var directories = new List<string>();

            // get all sub-directories
            foreach (var directory in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
            {
                // Ignore hidden directories
                if (!Path.GetFileName(directory).StartsWith('.'))
                {
                    directories.Add(directory);
                }
            }

            // sort directories in natural order
            directories = directories.OrderBy(s => s).ToList();

            foreach (var directory in directories)
            {
                var filesInDir = new List<string>();

                foreach (var file in Directory.GetFiles(directory))
                {
                    filesInDir.Add(file);
                }

                // sort files in natural order
                filesInDir = filesInDir.OrderBy(s => s).ToList();

                if (filesInDir.Any())
                {
                    // Use some of our hash bits to choose which file
                    var elementInList = hashArray[iter] % filesInDir.Count;
                    chosenFiles.Add(filesInDir[(int)elementInList]);
                    iter++;
                }
            }

            return chosenFiles;
        }


        public Image<Rgba32> Assemble(string inputString, string? roboSet = null, string? color = null, string? format = null, string? bgSet = null, int sizeX = 300, int sizeY = 300)
        {
            // Remove image extensions before hashing
            if (_ignoreExt)
            {
                inputString = RemoveExtensions(inputString);
            }

            // Hash the string
            string hexdigest;
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(inputString);
                byte[] hash = sha512.ComputeHash(bytes);
                hexdigest = BitConverter.ToString(hash).Replace("-", string.Empty);
            }

            var hashArray = CreateHashes(hexdigest, _hashCount);

            if (roboSet == "any")
            {
                roboSet = _sets[(int)(hashArray[1] % _sets.Count)];
            }
            else if (roboSet == null || !_sets.Contains(roboSet))
            {
                roboSet = _sets[0];
            }

            if (roboSet == "set1")
            {
                if (color != null && _colors.Contains(color))
                {
                    roboSet = "set1/" + color;
                }
                else
                {
                    string randomColor = _colors[(int)(hashArray[0] % _colors.Count)];
                    roboSet = "set1/" + randomColor;
                }
            }

            if (bgSet == "any")
            {
                bgSet = _bgSets[(int)(hashArray[2] % _bgSets.Count)];
            }
            else if (bgSet == null || !_bgSets.Contains(bgSet))
            {
                bgSet = null;
            }

            var roboParts = GetListOfFiles(hashArray, Path.Combine(_resourceDir, "sets", roboSet));
            roboParts = roboParts.OrderBy(x => x.Split("#")[1]).ToList();

            string? background = null;
            if (bgSet != null)
            {
                List<string> bgList = Directory.GetFiles(Path.Combine(_resourceDir, "backgrounds", bgSet)).ToList();
                background = bgList[(int)(hashArray[3] % bgList.Count)];
            }

            var roboImg = Image.Load<Rgba32>(roboParts[0]);
            roboImg.Mutate(x => x.Resize(1024, 1024));

            foreach (string png in roboParts)
            {
                using (var img = Image.Load<Rgba32>(png))
                {
                    img.Mutate(x => x.Resize(1024, 1024));
                    roboImg.Mutate(context => context.DrawImage(img, new Point(0, 0), 1));
                }
            }

            if (bgSet != null)
            {
                using (var bg = Image.Load<Rgba32>(background))
                {
                    bg.Mutate(x => x.Resize(1024, 1024));
                    bg.Mutate(context => context.DrawImage(roboImg, new Point(0, 0), 1));
                    roboImg.Dispose();
                    roboImg = bg.Clone();
                }
            }

            if (format == "bmp" || format == "jpeg")
            {
                roboImg.Mutate(x => x.Grayscale());
            }

            roboImg.Mutate(x => x.Resize(sizeX, sizeY));

            return roboImg;
        }

    }
}
