using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ChessForge
{
    public class FileUtils
    {
        /// <summary>
        /// Replaces path extension with the provided one.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static string ReplacePathExtension(string path, string extension)
        {
            string replacedPath = GetPathWithoutExtension(path);
            replacedPath = replacedPath + extension;

            return replacedPath;
        }

        /// <summary>
        /// Removes extension from a file path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetPathWithoutExtension(string path)
        {
            string pathNoExt = "";
            if (!string.IsNullOrEmpty(path))
            {
                string directory = Path.GetDirectoryName(path);

                // Get the file name without the extension
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);

                // Combine the directory and file name without extension
                pathNoExt = Path.Combine(directory, fileNameWithoutExtension);
            }

            return pathNoExt;
        }
    }
}
