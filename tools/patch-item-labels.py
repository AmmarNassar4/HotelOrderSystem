from pathlib import Path

FILES = [
    Path("src/HotelOrderSystem.Api/wwwroot/assets/js/app.js"),
    Path("src/HotelOrderSystem.Api/wwwroot/assets/js/categories-extension.js"),
]

REPLACEMENTS = [
    (
        '${h(item.name)}${item.targetTeamName ? " - " + h(item.targetTeamName) : ""}',
        '${h(item.name)}',
    ),
    (
        '${h(item.name)}${item.targetTeamName ? " - " + h(item.targetTeamName) : ""}',
        '${h(item.name)}',
    ),
    (
        '${escapeHtml(item.name)}${item.targetTeamName ? " - " + escapeHtml(item.targetTeamName) : ""}',
        '${escapeHtml(item.name)}',
    ),
    (
        '${item.name}${item.targetTeamName ? " - " + item.targetTeamName : ""}',
        '${item.name}',
    ),
    (
        "${h(item.name)} - ${h(item.targetTeamName)}",
        "${h(item.name)}",
    ),
    (
        "${escapeHtml(item.name)} - ${escapeHtml(item.targetTeamName)}",
        "${escapeHtml(item.name)}",
    ),
]


def patch_file(path: Path) -> bool:
    if not path.exists():
        print(f"Skipping missing file: {path}")
        return False

    text = path.read_text(encoding="utf-8")
    original = text

    for old, new in REPLACEMENTS:
        text = text.replace(old, new)

    if text == original:
        print(f"No item selector label changes found in: {path}")
        return False

    path.write_text(text, encoding="utf-8")
    print(f"Patched: {path}")
    return True


def main() -> None:
    changed = False
    for file_path in FILES:
        changed = patch_file(file_path) or changed

    if changed:
        print("Done. Item selectors now show item/service name only, without team name.")
    else:
        print("No changes made. The selectors may already be patched, or the label format is different.")


if __name__ == "__main__":
    main()
