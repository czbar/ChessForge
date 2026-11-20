using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ChessForge.LibraryContentEditor
{
    /// <summary>
    /// Parser and serializer for ChessForge's Bookcase/Shelf/Book text format.
    /// Indentation is accepted but not used to determine the hierarchy.
    /// </summary>
    public static class LibraryContentSerializer
    {
        public static LibraryCatalog Parse(string text)
        {
            LibraryCatalog catalog = new LibraryCatalog();
            List<string> errors = new List<string>();
            HashSet<LibraryNode> describedNodes = new HashSet<LibraryNode>();
            HashSet<BookNode> booksWithFiles = new HashSet<BookNode>();
            BookcaseNode currentBookcase = null;
            ShelfNode currentShelf = null;
            BookNode currentBook = null;
            LibraryNode currentNode = null;
            bool hasCatalogContent = false;

            using (StringReader reader = new StringReader(text ?? string.Empty))
            {
                string line;
                int lineNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    string trimmed = line.Trim();
                    if (trimmed.Length == 0)
                    {
                        continue;
                    }

                    int colonIndex = trimmed.IndexOf(':');
                    if (colonIndex < 0)
                    {
                        errors.Add("Line " + lineNumber + ": expected a 'Key: value' entry.");
                        continue;
                    }

                    string key = trimmed.Substring(0, colonIndex).Trim();
                    string value = trimmed.Substring(colonIndex + 1).Trim();

                    // The published sample contains these two obsolete header lines.
                    if (!hasCatalogContent &&
                        (key.Equals("Redirect", StringComparison.OrdinalIgnoreCase) ||
                         key.Equals("Message", StringComparison.OrdinalIgnoreCase) ||
                         key.Equals("Messsage", StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    if (key.Equals("Bookcase", StringComparison.OrdinalIgnoreCase))
                    {
                        currentBookcase = new BookcaseNode { Name = value };
                        catalog.Bookcases.Add(currentBookcase);
                        currentShelf = null;
                        currentBook = null;
                        currentNode = currentBookcase;
                        hasCatalogContent = true;
                    }
                    else if (key.Equals("Shelf", StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentBookcase == null)
                        {
                            errors.Add("Line " + lineNumber + ": Shelf must follow a Bookcase.");
                            currentShelf = null;
                            currentBook = null;
                            currentNode = null;
                            continue;
                        }

                        currentShelf = new ShelfNode { Name = value };
                        currentBookcase.Shelves.Add(currentShelf);
                        currentBook = null;
                        currentNode = currentShelf;
                        hasCatalogContent = true;
                    }
                    else if (key.Equals("Book", StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentShelf == null)
                        {
                            errors.Add("Line " + lineNumber + ": Book must follow a Shelf.");
                            currentBook = null;
                            currentNode = null;
                            continue;
                        }

                        currentBook = new BookNode { Name = value };
                        currentShelf.Books.Add(currentBook);
                        currentNode = currentBook;
                        hasCatalogContent = true;
                    }
                    else if (key.Equals("Description", StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentNode == null)
                        {
                            errors.Add("Line " + lineNumber + ": Description has no Bookcase, Shelf, or Book to describe.");
                        }
                        else if (!describedNodes.Add(currentNode))
                        {
                            errors.Add("Line " + lineNumber + ": duplicate Description for the current " + currentNode.Kind + ".");
                        }
                        else
                        {
                            currentNode.Description = value;
                        }
                    }
                    else if (key.Equals("File", StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentBook == null)
                        {
                            errors.Add("Line " + lineNumber + ": File must follow a Book.");
                        }
                        else if (!booksWithFiles.Add(currentBook))
                        {
                            errors.Add("Line " + lineNumber + ": duplicate File for the current Book.");
                        }
                        else
                        {
                            currentBook.FilePath = value;
                        }
                    }
                    else
                    {
                        errors.Add("Line " + lineNumber + ": unknown key '" + key + "'.");
                    }
                }
            }

            errors.AddRange(Validate(catalog));
            if (errors.Count > 0)
            {
                throw new LibraryContentFormatException(errors);
            }

            return catalog;
        }

        public static string Serialize(LibraryCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException("catalog");
            }

            IList<string> errors = Validate(catalog);
            if (errors.Count > 0)
            {
                throw new LibraryContentFormatException(errors);
            }

            StringBuilder output = new StringBuilder();
            foreach (BookcaseNode bookcase in catalog.Bookcases)
            {
                AppendEntry(output, string.Empty, "Bookcase", bookcase.Name);
                AppendEntry(output, string.Empty, "Description", bookcase.Description);
                output.AppendLine();

                foreach (ShelfNode shelf in bookcase.Shelves)
                {
                    AppendEntry(output, "    ", "Shelf", shelf.Name);
                    AppendEntry(output, "    ", "Description", shelf.Description);
                    output.AppendLine();

                    foreach (BookNode book in shelf.Books)
                    {
                        AppendEntry(output, "        ", "Book", book.Name);
                        AppendEntry(output, "        ", "Description", book.Description);
                        AppendEntry(output, "        ", "File", book.FilePath.Replace('\\', '/'));
                        output.AppendLine();
                    }
                }
            }

            return output.ToString();
        }

        public static IList<string> Validate(LibraryCatalog catalog)
        {
            List<string> errors = new List<string>();
            if (catalog == null)
            {
                errors.Add("The document is missing.");
                return errors;
            }

            if (catalog.Bookcases.Count == 0)
            {
                errors.Add("Add at least one Bookcase before saving.");
            }

            for (int bookcaseIndex = 0; bookcaseIndex < catalog.Bookcases.Count; bookcaseIndex++)
            {
                BookcaseNode bookcase = catalog.Bookcases[bookcaseIndex];
                string bookcasePath = "Bookcase " + (bookcaseIndex + 1);
                ValidateRequired(bookcase.Name, bookcasePath + " needs a name.", errors);
                ValidateSingleLine(bookcase.Name, bookcasePath + " name", errors);
                ValidateSingleLine(bookcase.Description, bookcasePath + " description", errors);

                for (int shelfIndex = 0; shelfIndex < bookcase.Shelves.Count; shelfIndex++)
                {
                    ShelfNode shelf = bookcase.Shelves[shelfIndex];
                    string shelfPath = bookcasePath + ", Shelf " + (shelfIndex + 1);
                    // Empty shelf names occur in the published ChessForge sample and are valid.
                    ValidateSingleLine(shelf.Name, shelfPath + " name", errors);
                    ValidateSingleLine(shelf.Description, shelfPath + " description", errors);

                    for (int bookIndex = 0; bookIndex < shelf.Books.Count; bookIndex++)
                    {
                        BookNode book = shelf.Books[bookIndex];
                        string bookPath = shelfPath + ", Book " + (bookIndex + 1);
                        ValidateRequired(book.Name, bookPath + " needs a name.", errors);
                        ValidateRequired(book.FilePath, bookPath + " needs a file path.", errors);
                        ValidateSingleLine(book.Name, bookPath + " name", errors);
                        ValidateSingleLine(book.Description, bookPath + " description", errors);
                        ValidateSingleLine(book.FilePath, bookPath + " file path", errors);
                    }
                }
            }

            return errors;
        }

        private static void AppendEntry(StringBuilder output, string indent, string key, string value)
        {
            output.Append(indent);
            output.Append(key);
            output.Append(": ");
            output.AppendLine(value ?? string.Empty);
        }

        private static void ValidateRequired(string value, string message, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(message);
            }
        }

        private static void ValidateSingleLine(string value, string fieldName, ICollection<string> errors)
        {
            if (!string.IsNullOrEmpty(value) && (value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0))
            {
                errors.Add(fieldName + " must fit on one line.");
            }
        }
    }
}

