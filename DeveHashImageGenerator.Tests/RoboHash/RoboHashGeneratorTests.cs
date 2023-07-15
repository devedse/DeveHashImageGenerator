using DeveHashImageGenerator.RoboHash;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace DeveHashImageGenerator.Tests.RoboHash
{
    [TestClass]
    public class RoboHashGeneratorTests
    {
        [TestMethod]
        public void GeneratesAnImageBasedOnAHash()
        {
            var roboHashGenerator = new RoboHashGenerator("10.88.10.1");
            using (var image = roboHashGenerator.Assemble())
            {
                // Save the image to "output_GeneratesAnImageBasedOnAHash.png".
                image.Save("output_GeneratesAnImageBasedOnAHash.png");

                // Assert that the image isn't empty.
                Assert.AreNotEqual(0, image.Width);
                Assert.AreNotEqual(0, image.Height);

                // Assert that the image contains colored pixels.
                bool containsColoredPixels = ContainsColoredPixels(image);
                Assert.IsTrue(containsColoredPixels);
            }
        }

        // This method checks if an image contains any colored pixels (non-white and non-black).
        private bool ContainsColoredPixels(Image<Rgba32> image)
        {
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Rgba32 pixelColor = image[x, y];

                    // Check if the pixel is not white and not black.
                    if (!(pixelColor.R == 255 && pixelColor.G == 255 && pixelColor.B == 255) &&
                        !(pixelColor.R == 0 && pixelColor.G == 0 && pixelColor.B == 0))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}