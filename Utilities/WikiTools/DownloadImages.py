import os
import re
import requests

# Directory where .md files live
SOURCE_DIR = "C:/GitHub/ChessForge.wiki"   # change if needed
# Directory to save downloaded images
OUTPUT_DIR = "C:/GitHub/WikiDownloadedImages"

# Make sure output directory exists
os.makedirs(OUTPUT_DIR, exist_ok=True)

# Regex to capture URLs of the form <img src="https://github.com...GUID[.png]"
IMG_URL_PATTERN = re.compile(r'<img src="(https://github\.com[^\s"]*?([0-9a-fA-F-]{36})(?:\.png)?)"')

def download_image(url, guid):
    filename = f"{guid}.png"
    filepath = os.path.join(OUTPUT_DIR, filename)

    if os.path.exists(filepath):
        print(f"Skipping {filename}, already exists.")
        return

    try:
        print(f"Downloading {url} -> {filename}")
        response = requests.get(url, stream=True)
        response.raise_for_status()

        with open(filepath, "wb") as f:
            for chunk in response.iter_content(8192):
                f.write(chunk)

    except requests.RequestException as e:
        print(f"Failed to download {url}: {e}")

def process_md_files():
    for fname in os.listdir(SOURCE_DIR):
        if fname.endswith(".md"):
            with open(os.path.join(SOURCE_DIR, fname), "r", encoding="utf-8") as f:
                for line in f:
                    match = IMG_URL_PATTERN.search(line)
                    if match:
                        url, guid = match.groups()
                        download_image(url, guid)

if __name__ == "__main__":
    process_md_files()
    print("Done.")
