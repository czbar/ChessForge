using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            // try parsing with the current culture (e.g. for cases when the decimal separator is not dot but comma)
            bool success = double.TryParse(sDouble, out dValue);

            if (!success)
            {
                // try parsing with invariant culture (the decimal separator is a comma)
                success = double.TryParse(sDouble, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out dValue);
            }

            return success;
        }
    }
}
