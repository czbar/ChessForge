using ChessForge.LibraryContentEditor;
using System;
using System.IO;
using System.Linq;

namespace LibraryContentEditor.FormatTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string samplePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LibraryContent.sample.txt");
                string sample = File.ReadAllText(samplePath);
                LibraryCatalog catalog = LibraryContentSerializer.Parse(sample);

                Assert(catalog.Bookcases.Count == 5, "Expected 5 bookcases.");
                Assert(catalog.Bookcases.Sum(bookcase => bookcase.Shelves.Count) == 9, "Expected 9 shelves.");
                Assert(catalog.Bookcases.Sum(bookcase => bookcase.Shelves.Sum(shelf => shelf.Books.Count)) == 36, "Expected 36 books.");
                Assert(catalog.Bookcases.SelectMany(bookcase => bookcase.Shelves).Any(shelf => shelf.Name == string.Empty), "Expected the sample's unnamed shelf.");

                string serialized = LibraryContentSerializer.Serialize(catalog);
                Assert(!serialized.Contains("Redirect:"), "Obsolete Redirect header must not be written.");
                Assert(!serialized.Contains("Message:"), "Obsolete Message header must not be written.");
                Assert(serialized.Contains("Sämisch"), "UTF-8 text must survive serialization.");

                LibraryCatalog reparsed = LibraryContentSerializer.Parse(serialized);
                Assert(reparsed.Bookcases.Count == 5, "Round-trip bookcase count changed.");
                Assert(reparsed.Bookcases.Sum(bookcase => bookcase.Shelves.Sum(shelf => shelf.Books.Count)) == 36, "Round-trip book count changed.");

                AssertInvalid("Shelf: Orphan", "Shelf must follow a Bookcase");
                AssertInvalid("Bookcase: Test\nDescription:\n    Shelf:\n    Description:\n        Book: Missing file\n        Description:", "needs a file path");

                string alternateHeader = sample.Replace("Message:", "Messsage:");
                LibraryCatalog alternateHeaderCatalog = LibraryContentSerializer.Parse(alternateHeader);
                Assert(alternateHeaderCatalog.Bookcases.Count == 5, "The alternate obsolete Messsage header should be ignored.");

                Console.WriteLine("All format tests passed (5 bookcases, 9 shelves, 36 books).");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void AssertInvalid(string text, string expectedMessagePart)
        {
            try
            {
                LibraryContentSerializer.Parse(text);
                throw new InvalidOperationException("Expected invalid input to be rejected.");
            }
            catch (LibraryContentFormatException exception)
            {
                Assert(exception.Message.Contains(expectedMessagePart), "Unexpected validation message: " + exception.Message);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

