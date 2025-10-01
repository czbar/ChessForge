import os
import re
import shutil
import sys

# Directory with .md files
SOURCE_DIR = "C:/GitHub/ChessForge.wiki"   # change if needed

# Regex to capture the URL and GUID
IMG_URL_PATTERN = re.compile(r'(<img src=")https://github\.com[^\s"]*?([0-9a-fA-F-]{36})(?:\.png)?(")')

def update_md_files():
    """Backup and update .md files with new image URLs."""
    for fname in os.listdir(SOURCE_DIR):
        if fname.endswith(".md"):
            filepath = os.path.join(SOURCE_DIR, fname)
            backup_path = filepath + ".bak"

            # Create a backup if not already there
            if not os.path.exists(backup_path):
                shutil.copy(filepath, backup_path)
                print(f"Backup created: {backup_path}")

            with open(filepath, "r", encoding="utf-8") as f:
                content = f.read()

            # Replace URLs with new format
            new_content = IMG_URL_PATTERN.sub(
                lambda m: f'{m.group(1)}https://github.com/czbar/ChessForge/wiki/images/{m.group(2)}.png{m.group(3)}',
                content
            )

            if new_content != content:
                with open(filepath, "w", encoding="utf-8") as f:
                    f.write(new_content)
                print(f"Updated: {fname}")
            else:
                print(f"No changes in: {fname}")

def restore_md_files():
    """Restore all .md files from their .bak backups, then delete backups."""
    for fname in os.listdir(SOURCE_DIR):
        if fname.endswith(".md.bak"):
            backup_path = os.path.join(SOURCE_DIR, fname)
            original_path = backup_path[:-4]  # remove .bak

            shutil.copy(backup_path, original_path)
            os.remove(backup_path)
            print(f"Restored and deleted backup: {original_path}")

if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1].lower() == "restore":
        restore_md_files()
        print("All files restored and backups deleted.")
    else:
        update_md_files()
        print("Update complete.")
