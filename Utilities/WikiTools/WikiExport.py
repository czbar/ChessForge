
import os
import subprocess

# Path to the local clone of your Wiki repo
# Example: git clone https://github.com/czbar/ChessForge.wiki.git
WIKI_PATH = r"C:/GitHub/Outputs/WikiForPandoc"

# Output files
OUTPUT_PDF = "C:/GitHub/Wiki/Generated/ChessForge_Manual.pdf"
OUTPUT_DOCX = "C:/GitHub/Wiki/Generated/ChessForge_Manual.docx"

# Optional: define order of pages (filenames in wiki repo).
# If left empty, all .md files will be included alphabetically.
PAGE_ORDER = [
    "User's-Manual.md",
    "Installation.md",
    "Graphical-User-Interface.md",
]

def collect_markdown_files():
    if PAGE_ORDER:
        files = [os.path.join(WIKI_PATH, f) for f in PAGE_ORDER if os.path.exists(os.path.join(WIKI_PATH, f))]
    else:
        files = [os.path.join(WIKI_PATH, f) for f in sorted(os.listdir(WIKI_PATH)) if f.endswith(".md")]
    return files

def merge_markdown(files):
    merged_file = os.path.join(WIKI_PATH, "_merged.md")
    with open(merged_file, "w", encoding="utf-8") as outfile:
        for fname in files:
            with open(fname, "r", encoding="utf-8") as infile:
                outfile.write(infile.read())
                outfile.write("\n\n\\newpage\n\n")  # page break between sections
    return merged_file

def export_to_pdf(merged_file):
    try:
        subprocess.run(
            ["C:/Program Files/Pandoc/pandoc.exe", merged_file, "-o", OUTPUT_PDF, "--pdf-engine=xelatex"],
            check=True
        )
        print(f"PDF created: {OUTPUT_PDF}")
    except subprocess.CalledProcessError as e:
        print("Error during Pandoc PDF export:", e)

def export_to_docx(merged_file):
    try:
        subprocess.run(
            ["C:/Program Files/Pandoc/pandoc.exe", merged_file, "-o", OUTPUT_DOCX],
            check=True
        )
        print(f"Word DOCX created: {OUTPUT_DOCX}")
    except subprocess.CalledProcessError as e:
        print("Error during Pandoc DOCX export:", e)

if __name__ == "__main__":
    md_files = collect_markdown_files()
    if not md_files:
        print("No Markdown files found in wiki path.")
    else:
        merged = merge_markdown(md_files)
        # Choose which outputs you want:
        export_to_pdf(merged)
        export_to_docx(merged)
