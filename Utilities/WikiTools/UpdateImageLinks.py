import os
import re
import shutil
import sys

# Directory with .md files
SOURCE_DIR = "C:/GitHub/ChessForge.wiki"

OUTPUT_DIR_WEB = "C:/GitHub/Wiki/WikiForWeb"
OUTPUT_DIR_PANDOC = "C:/GitHub/Wiki/WikiForPandoc"

# Regex to capture the URL and GUID
IMG_URL_PATTERN = re.compile(r'(.*)https://[^\s"]*?([0-9a-fA-F-]{36})(.*)')

IMG_LONG_URL_PATTERN = re.compile(r'(.*)https://[^\s"]*?([0-9a-fA-F-]{46})(?:\.png)?(.*)')

# Regex to capture Width=<N>
WIDTH_PATTERN = re.compile(r'Width\s*=\s*(\d+)', re.IGNORECASE)

def update_md_files():
    """Update .md files with new image URLs."""
    for fname in os.listdir(SOURCE_DIR):
        if fname.endswith(".md"):
            filepath = os.path.join(SOURCE_DIR, fname)
            outpath_web = os.path.join(OUTPUT_DIR_WEB, fname)
            outpath_pandoc = os.path.join(OUTPUT_DIR_PANDOC, fname)

            updated_lines = []
            updated_lines_pandoc = []
            changed = False

            # Get the filename without extension
            name_without_ext = os.path.splitext(fname)[0]

            # Replace hyphens with spaces
            title = name_without_ext.replace("-", " ")

            updated_lines_pandoc.append(f"# {title}")
            updated_lines_pandoc.append("\n\n")

            with open(filepath, "r", encoding="utf-8") as f:
                for line in f:
                    original_line = line
                    line_pandoc = line

                    # Get Width if found in the line before we process the line.
                    width_match = WIDTH_PATTERN.search(line)
                    if width_match:
                        width_value = width_match.group(1)

                    # Replace the GitHub image URL with the new format
                    line = IMG_LONG_URL_PATTERN.sub(
                        lambda m: f'{m.group(1)}https://github.com/czbar/ChessForge/wiki/images/{m.group(2)}.png',
                        line,
                    )

                    if line != original_line:
                        changed = True
                        print(line)
                        line_pandoc = IMG_LONG_URL_PATTERN.sub(
                            lambda m: f'![](C:/GitHub/Wiki/DownloadedImages/{m.group(2)}.png)',
                            line_pandoc,
                    )
                    else:
                        line = IMG_URL_PATTERN.sub(
                            lambda m: f'{m.group(1)}https://github.com/czbar/ChessForge/wiki/images/{m.group(2)}.png{m.group(3)}',
                            line,
                        )
                        if line != original_line:
                            changed = True
                            line_pandoc = IMG_URL_PATTERN.sub(
                                lambda m: f'![](C:/GitHub/Wiki/DownloadedImages/{m.group(2)}.png)',
                                line_pandoc,
                            )
                    
                    if width_match:
                        # remove the Width=<N> part
                        line_pandoc = WIDTH_PATTERN.sub("", line_pandoc)
                        line_pandoc = line_pandoc.rstrip() + f'{{ width={width_value}px }}\n'

                    updated_lines.append(line)
                    updated_lines_pandoc.append(line_pandoc)

                    if line != original_line:
                        changed = True
                    
            if changed:
                with open(outpath_web, "w", encoding="utf-8") as f:
                    f.writelines(updated_lines)
                print(f"Updated: {fname}")
            else:
                print(f"No changes in: {fname}")
            
            changed = True  # Force update for pandoc files
            if changed:
                with open(outpath_pandoc, "w", encoding="utf-8") as f:
                    f.writelines(updated_lines_pandoc)
                        

if __name__ == "__main__":
    update_md_files()
    print("Update complete.")
