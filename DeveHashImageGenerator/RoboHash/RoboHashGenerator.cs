using System.Security.Cryptography;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using Image = SixLabors.ImageSharp.Image;

namespace DeveHashImageGenerator.RoboHash
{
    public class RoboHashGenerator
    {
        private string _hexdigest;
        private List<long> _hashArray = new List<long>();
        private int _iter = 4;
        private string _resourceDir = SuperFolderFinder.RoboFlowDirectory;
        private List<string> _sets;
        private List<string> _bgSets;
        private List<string> _colors;

        public RoboHashGenerator(string inputString, int hashCount = 11, bool ignoreExt = true)
        {
            // Remove image extensions before hashing
            if (ignoreExt)
            {
                inputString = RemoveExtensions(inputString);
            }

            // Hash the string
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(inputString);
                byte[] hash = sha512.ComputeHash(bytes);
                _hexdigest = BitConverter.ToString(hash).Replace("-", string.Empty);
            }

            CreateHashes(hashCount);

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

        private void CreateHashes(int count)
        {
            int blockSize = _hexdigest.Length / count;

            for (int i = 0; i < count; i++)
            {
                int currentStart = (1 + i) * blockSize - blockSize;
                int currentEnd = (1 + i) * blockSize;
                string sub = _hexdigest.Substring(currentStart, blockSize);
                _hashArray.Add(Convert.ToInt64(sub, 16));
            }

            _hashArray.AddRange(_hashArray);
        }

        private List<string> ListDirs(string path)
        {
            IEnumerable<string> allItems = Directory
                .GetDirectories(path)
                .Select(t => Path.GetFileName(t))
                .Where(t => !string.IsNullOrWhiteSpace(t) && !t.StartsWith("."))
                .Select(t => t!);

            return allItems.ToList();
        }

        private List<string> GetListOfFiles(string path)
        {
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
                    var elementInList = _hashArray[_iter] % filesInDir.Count;
                    chosenFiles.Add(filesInDir[(int)elementInList]);
                    _iter++;
                }
            }

            return chosenFiles;
        }


        public Image<Rgba32> Assemble(string roboSet = null, string color = null, string format = null, string bgSet = null, int sizeX = 300, int sizeY = 300)
        {
            if (roboSet == "any")
            {
                roboSet = _sets[(int)(_hashArray[1] % _sets.Count)];
            }
            else if (_sets.Contains(roboSet))
            {
                roboSet = roboSet;
            }
            else
            {
                roboSet = _sets[0];
            }

            if (roboSet == "set1")
            {
                if (_colors.Contains(color))
                {
                    roboSet = "set1/" + color;
                }
                else
                {
                    string randomColor = _colors[(int)(_hashArray[0] % _colors.Count)];
                    roboSet = "set1/" + randomColor;
                }
            }

            if (_bgSets.Contains(bgSet))
            {
                bgSet = bgSet;
            }
            else if (bgSet == "any")
            {
                bgSet = _bgSets[(int)(_hashArray[2] % _bgSets.Count)];
            }

            List<string> roboParts = GetListOfFiles(Path.Combine(_resourceDir, "sets", roboSet));
            roboParts = roboParts.OrderBy(x => x.Split("#")[1]).ToList();

            string background = null;
            if (bgSet != null)
            {
                List<string> bgList = Directory.GetFiles(Path.Combine(_resourceDir, "backgrounds", bgSet)).ToList();
                background = bgList[(int)(_hashArray[3] % bgList.Count)];
            }

            Image<Rgba32> roboImg = Image.Load<Rgba32>(roboParts[0]);
            roboImg.Mutate(x => x.Resize(1024, 1024));

            foreach (string png in roboParts)
            {
                using (Image<Rgba32> img = Image.Load<Rgba32>(png))
                {
                    img.Mutate(x => x.Resize(1024, 1024));
                    roboImg.Mutate(context => context.DrawImage(img, new Point(0, 0), 1));
                }
            }

            if (bgSet != null)
            {
                using (Image<Rgba32> bg = Image.Load<Rgba32>(background))
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
