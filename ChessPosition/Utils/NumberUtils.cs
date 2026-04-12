using System.Globalization;

namespace ChessPosition
{
    public class NumberUtils
    {
        /// <summary>
        /// Parses a double value from a string, trying first the current culture 
        /// and if that fails the invariant culture formats.
        /// </summary>
        /// <param name="sDouble"></param>
        /// <param name="dValue"></param>
        /// <returns></returns>
        public static bool ParseDouble(string sDouble, out double dValue)
        {
            bool success = false;

            if (!string.IsNullOrEmpty(sDouble))
            {
                sDouble = sDouble.Replace(',', '.');
            }

            // try parsing with invariant culture (the decimal separator is a comma)
            success = double.TryParse(sDouble, NumberStyles.Float, CultureInfo.InvariantCulture, out dValue);

            return success;
        }
    }
}
