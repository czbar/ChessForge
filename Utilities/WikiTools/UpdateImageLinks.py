import os
import re
import shutil
import sys

# Directory with .md files
SOURCE_DIR = "C:/GitHub/ChessForge.wiki"

OUTPUT_DIR_WEB = "C:/GitHub/Wiki/Outputs/WikiForWeb"
OUTPUT_DIR_PANDOC = "C:/GitHub/Wiki/Outputs/WikiForPandoc"

# Regex to capture the URL and GUID
IMG_URL_PATTERN = re.compile(r'(<img src=")https://github\.com[^\s"]*?([0-9a-fA-F-]{36})(?:\.png)?(")')

def update_md_files():
    """Update .md files with new image URLs."""
    for fname in os.listdir(SOURCE_DIR):
        if fname.endswith(".md"):
            filepath = os.path.join(SOURCE_DIR, fname)
            outpath_web = os.path.join(OUTPUT_DIR_WEB, fname)
            outpath_pandoc = os.path.join(OUTPUT_DIR_PANDOC, fname)

            with open(filepath, "r", encoding="utf-8") as f:
                content = f.read()

            # Replace URLs with new format for web
            new_content = IMG_URL_PATTERN.sub(
                lambda m: f'{m.group(1)}https://github.com/czbar/ChessForge/wiki/images/{m.group(2)}.png{m.group(3)}',
                content
            )

            with open(outpath_web, "w", encoding="utf-8") as f:
                f.write(new_content)
                print(f"Updated: {fname}")

                
            # Replace URLs with new format for Pandoc
            new_content = IMG_URL_PATTERN.sub(
                lambda m: f'{m.group(1)}C:/GitHub/Wiki/DownloadedImages/{m.group(2)}.png{m.group(3)}',
                content
            )

            with open(outpath_pandoc, "w", encoding="utf-8") as f:
                f.write(new_content)
                print(f"Updated: {fname}")

if __name__ == "__main__":
    update_md_files()
    print("Update complete.")
