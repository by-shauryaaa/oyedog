import os
import struct
import zlib

SPRITES_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Sprites")
INSTALLER_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "installer")
os.makedirs(INSTALLER_DIR, exist_ok=True)

SOURCE_PNG = os.path.join(SPRITES_DIR, "idle_0.png")
OUTPUT_ICO = os.path.join(INSTALLER_DIR, "app_icon.ico")

def create_ico_from_png(png_path, ico_path):
    with open(png_path, "rb") as f:
        png_data = f.read()

    # ICO format header for single PNG image (Windows supports embedded PNG in ICO since Vista)
    # Header: 0 (2 bytes reserved), 1 (2 bytes image type 1=ICO), 1 (2 bytes count=1)
    header = struct.pack("<HHH", 0, 1, 1)
    
    # Directory entry:
    # Width (1 byte, 0 means 256), Height (1 byte, 0 means 256), Colors (1 byte, 0), Reserved (1 byte, 0)
    # Planes (2 bytes, 1), BitCount (2 bytes, 32), BytesInRes (4 bytes, len), ImageOffset (4 bytes, 6+16=22)
    entry = struct.pack("<BBBBHHII", 0, 0, 0, 0, 1, 32, len(png_data), 22)

    with open(ico_path, "wb") as f:
        f.write(header)
        f.write(entry)
        f.write(png_data)

    print(f"Generated ICO icon at {ico_path} ({len(png_data)} bytes)")

if __name__ == "__main__":
    create_ico_from_png(SOURCE_PNG, OUTPUT_ICO)
