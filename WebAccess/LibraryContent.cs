using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAccess
{
    /// <summary>
    /// Structure for the Online ChessForge Library's json file.
    /// </summary>
    public class LibraryContent
    {
        /// <summary>
        /// Target URL if not at the default location
        /// </summary>
        public string Redirect;

        /// <summary>
        /// Any text messages relevant to the library overall.  
        /// </summary>
        public List<string> Messages;

        /// <summary>
        /// Library Sections.
        /// </summary>
        public List<Section> Sections;
    }

    /// <summary>
    /// Represents a Section of the Library.
    /// </summary>
    public class Section
    {
        /// <summary>
        /// Title of the section.
        /// </summary>
        public string Title;

        /// <summary>
        /// Description of the section.
        /// </summary>
        public string Description;

        /// <summary>
        /// The list of subsections.
        /// </summary>
        public List<SubSection> SubSections;
    }

    /// <summary>
    /// Represents a SubSection in a Section of the Library.  
    /// </summary>
    public class SubSection
    {
        /// <summary>
        /// Title of the SubSection
        /// </summary>
        public string Title;

        /// <summary>
        /// Description of the SubSection.
        /// </summary>
        public string Description;

        /// <summary>
        /// The list of Books in the SubsSection.
        /// </summary>
        public List<Book> Books;
    }

    /// <summary>
    /// Represents a single Workbook in the Library.
    /// </summary>
    public class Book
    {
        /// <summary>
        /// Title of the workbook.
        /// </summary>
        public string Title;

        /// <summary>
        /// Description of the Workbook.
        /// </summary>
        public string Description;

        /// <summary>
        /// Location of the workbook in the online library.
        /// </summary>
        public string File;
    }
}
