using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebAccess
{
    /// <summary>
    /// Structure for the Online ChessForge Library file.
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
        public List<string> Messages = new List<string>();

        /// <summary>
        /// Bookcases (sections of the library).
        /// </summary>
        public List<Bookcase> Bookcases = new List<Bookcase>();
    }

    /// <summary>
    /// Represents a Bookcase (section) of the Library.
    /// </summary>
    public class Bookcase
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Bookcase() { }

        /// <summary>
        /// Constructor setting the title.
        /// </summary>
        /// <param name="title"></param>
        public Bookcase(string title) 
        {
            Title = title;
        }

        /// <summary>
        /// Unique Id of the bookcase
        /// </summary>
        public string Id;

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
        public List<Shelf> Shelves = new List<Shelf>();

        /// <summary>
        /// Whether to show expanded or collapsed
        /// </summary>
        public bool IsExpanded = true;
    }

    /// <summary>
    /// Represents a Shelf (sub-section) in a Bookcase of the Library.  
    /// </summary>
    public class Shelf
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Shelf() { }

        /// <summary>
        /// Constructor setting the title.
        /// </summary>
        /// <param name="title"></param>
        public Shelf(string title)
        {
            Title = title;
        }

        /// <summary>
        /// Id of the shelf
        /// </summary>
        public string Id;

        /// <summary>
        /// An Id combining the parent bookcaseId with this shelf's id
        /// making it unique across the library.
        /// </summary>
        public string ShelfPathId;

        /// <summary>
        /// Id of the parent bookcase
        /// </summary>
        public string ParentBookcaseId;

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
        public List<Book> Books = new List<Book>();

        /// <summary>
        /// Whether to show expanded or collapsed
        /// </summary>
        public bool IsExpanded = true;
    }

    /// <summary>
    /// Represents a single Book (Workbook) in the Library.
    /// </summary>
    public class Book
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Book() { }

        /// <summary>
        /// Constructor setting the title.
        /// </summary>
        /// <param name="title"></param>
        public Book(string title)
        {
            Title = title;
        }

        /// <summary>
        /// Id of the shelf
        /// </summary>
        public string Id;

        /// <summary>
        /// An Id combining the grandparent bookcaseId with the parent shelf id
        /// and this book's id making it unique across the library.
        /// </summary>
        public string BookPathId;

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
