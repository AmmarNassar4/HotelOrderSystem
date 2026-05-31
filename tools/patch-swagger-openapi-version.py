from pathlib import Path
import shutil

PROJECT_FILE = Path("src/HotelOrderSystem.Api/HotelOrderSystem.Api.csproj")

def main() -> None:
    if not PROJECT_FILE.exists():
        raise FileNotFoundError(f"Project file not found: {PROJECT_FILE}")

    text = PROJECT_FILE.read_text(encoding="utf-8")
    original = text

    lines = text.splitlines()
    new_lines = []

    removed = False
    for line in lines:
        if 'PackageReference Include="Microsoft.OpenApi"' in line:
            removed = True
            continue
        new_lines.append(line)

    text = "\n".join(new_lines) + "\n"

    if text == original:
        print("No Microsoft.OpenApi PackageReference found, or file already patched.")
    else:
        PROJECT_FILE.write_text(text, encoding="utf-8")
        print("Removed explicit Microsoft.OpenApi PackageReference from csproj.")

    for folder in [
        Path("src/HotelOrderSystem.Api/bin"),
        Path("src/HotelOrderSystem.Api/obj"),
    ]:
        if folder.exists():
            shutil.rmtree(folder)
            print(f"Deleted: {folder}")

    print("Done. Now run:")
    print("  dotnet restore")
    print("  dotnet clean")
    print("  dotnet build")

if __name__ == "__main__":
    main()