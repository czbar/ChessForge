using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Represents the version of a Workbook.
    /// Version consists of the Major number that is greater equal 1
    /// and Minor number that is greater equal 0.
    /// </summary>
    public class WorkbookVersion
    {
        /// <summary>
        /// Version's major number
        /// </summary>
        public uint Major { get => _major; set => _major = value; }

        /// <summary>
        /// Version's minor number
        /// </summary>
        public uint Minor { get => _minor; set => _minor = value; }

        // major number
        private uint _major;

        // minor number
        private uint _minor;

        /// <summary>
        /// Creates a WorkbookVersion object with deafult value of 1.0. 
        /// </summary>
        public WorkbookVersion()
        {
            _major = 1;
            _minor = 0;
        }

        /// <summary>
        /// Creates a WorkbookVersion object with the value obtained from the passed string.
        /// </summary>
        /// <param name="sVer"></param>
        public WorkbookVersion(string sVer)
        {
            if (sVer != null)
            {
                try
                {
                    string[] tokens = sVer.Split('.');
                    _major = uint.Parse(tokens[0]);
                    _minor = uint.Parse(tokens[1]);
                }
                catch
                {
                    _major = 1;
                    _minor = 0;
                }
            }
            else
            {
                _major = 1;
                _minor = 0;
            }
        }

        /// <summary>
        /// Override ToString() method.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _major.ToString() + "." + _minor.ToString();
        }

        /// <summary>
        /// Increments the version number.
        /// </summary>
        /// <param name="major"></param>
        public void IncrementVersion(bool major = false)
        {
            if (major)
            {
                _major++;
            }
            else
            {
                _minor++;
            }
        }
    }
}
