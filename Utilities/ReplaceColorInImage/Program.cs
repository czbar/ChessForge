using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace ReplaceColorInImage
{
    /// <summary>
    /// Replaces colors in bitmaps.
    /// Takes the template in Green and generates images in other colors. 
    /// </summary>
    internal class Program
    {
        // default location of the template images
        private static string _originalDirectory = "C:\\Users\\rober\\Documents\\ChessForge\\ChessForge\\Resources\\Images";

        // template name of the original images ("Triangle" to be replace with appropriate part, and "Green" with the target color)
        private static string _triangleOriginal = @"ArrowTriangleGreen.png";

        // input files with colors to generate images in
        private static string _imageListFile = "ImageList.txt";

        // original Green used in the template images
        private static Color _originalColor = Color.FromArgb(34, 177, 76);

        // default output directory if none specified on the command line
        private static string _directory = ".";

        /// <summary>
        /// Reads the command line and calls the processing methods.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                _directory = args[0];
            }

            // read the requirement list line by line
            string[] lines = File.ReadAllLines(Path.Combine(_directory, _imageListFile));
            foreach (string line in lines)
            {
                string colorPartFileName;
                int red, green, blue;

                string[] tokens = line.Split(' ');
                if (tokens.Length == 2)
                {
                    colorPartFileName = tokens[0];
                    GetColors(tokens[1], out red, out green, out blue);
                    Color color = Color.FromArgb(red, green, blue);

                    SwapColorInFile("Triangle", colorPartFileName, color);
                    SwapColorInFile("Stem", colorPartFileName, color);
                    SwapColorInFile("HalfCircle", colorPartFileName, color);
                }
            }
        }

        /// <summary>
        /// Gets the path to the template image to read the bitmap from
        /// </summary>
        /// <param name="arrowPart"></param>
        /// <returns></returns>
        private static string GenerateImageFile(string arrowPart)
        {
            string fileName = _triangleOriginal.Replace("Triangle", arrowPart);
            return Path.Combine(_originalDirectory, fileName);
        }

        /// <summary>
        /// Get the bitmap from the template file, replace the color
        /// and save under a new name.
        /// </summary>
        /// <param name="arrowPart"></param>
        /// <param name="colorPart"></param>
        /// <param name="color"></param>
        private static void SwapColorInFile(string arrowPart, string colorPart, Color color)
        {
            string originalFile = GenerateImageFile(arrowPart);
            string originalFileName = _triangleOriginal.Replace("Triangle", arrowPart);
            var bmp = (Bitmap)Image.FromFile(originalFile);
            SwapColor(bmp, _originalColor, color);

            string outFileName = originalFileName.Replace("Green", colorPart);
            bmp.Save(Path.Combine(_directory, outFileName));
        }

        /// <summary>
        /// Parses the string in the format "r,g,b" e.g. "125.30,55"
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        private static void GetColors(string colors, out int red, out int green, out int blue)
        {
            red = green = blue = 0;
            string[] tokens = colors.Split(',');
            if (tokens.Length == 3)
            {
                red = int.Parse(tokens[0]);
                green = int.Parse(tokens[1]);
                blue = int.Parse(tokens[2]);
            }
        }

        /// <summary>
        /// Performs the color replacement in the passed bitmap.
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="oldColor"></param>
        /// <param name="newColor"></param>
        private static void SwapColor(Bitmap bmp, Color oldColor, Color newColor)
        {
            var lockedBitmap = new LockBitmap(bmp);
            lockedBitmap.LockBits();

            for (int y = 0; y < lockedBitmap.Height; y++)
            {
                for (int x = 0; x < lockedBitmap.Width; x++)
                {
                    if (lockedBitmap.GetPixel(x, y) == oldColor)
                    {
                        lockedBitmap.SetPixel(x, y, newColor);
                    }
                }
            }
            lockedBitmap.UnlockBits();
        }
    }
}
