# Library Content Editor

A standalone WPF utility for editing ChessForge's three-level `LibraryContent.txt` format:

```text
Bookcase: Opening books
Description: Opening material.

    Shelf: White repertoire
    Description: Lines for White.

        Book: The London System
        Description: A complete overview.
        File: Openings/White/London System.pgn
```

The editor targets **.NET Framework 4.8** and uses only WPF/framework assemblies. It reads the inconsistent spaces/tabs in the published sample because hierarchy is determined from the keys, not indentation. The obsolete leading `Redirect:` and `Message:` lines (including the `Messsage:` spelling from the request) are ignored when opening a file and are never written when saving.

## Build and test

Open `LibraryContentEditor.sln` in Visual Studio 2022, or build it from a Developer PowerShell:

```powershell
dotnet build .\LibraryContentEditor.sln -c Release
```

Run the standalone utility:

```powershell
.\ChessForge.LibraryContentEditor\bin\Release\net48\LibraryContentEditor.exe
```

With no command-line argument it opens a new blank document. You can open a file from the application's **File → Open** command, or pass a local content file on the command line:

```powershell
.\ChessForge.LibraryContentEditor\bin\Release\net48\LibraryContentEditor.exe C:\path\to\LibraryContent.txt
```

The automated parser/serializer checks run with:

```powershell
dotnet run --project .\LibraryContentEditor.FormatTests\LibraryContentEditor.FormatTests.csproj -c Release
```

## Editor behavior

- Tree-based Bookcase → Shelf → Book editing.
- Add, delete, and reorder items at each level.
- Edit names, descriptions, and a book's file path.
- New, Open, Save, Save As, and unsaved-change prompts.
- Validation before save (bookcase/book names, book file paths, and one-line field values).
- Empty shelf names remain valid because the published sample contains one.
- UTF-8 output without a BOM, Windows line endings, and forward slashes in saved book paths.

