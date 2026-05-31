from pathlib import Path
import re
import shutil
from datetime import datetime

ROOT = Path.cwd()
CSS_FILE = ROOT / "src" / "HotelOrderSystem.Api" / "wwwroot" / "assets" / "css" / "app.css"
INDEX_FILE = ROOT / "src" / "HotelOrderSystem.Api" / "wwwroot" / "index.html"

START = "/* === WIDE_SCREEN_LAYOUT_PATCH_START === */"
END = "/* === WIDE_SCREEN_LAYOUT_PATCH_END === */"

CSS_PATCH = """/* === WIDE_SCREEN_LAYOUT_PATCH_START === */

/*
  Wide screen layout polish
  - Turns the guest dark hero panel into a top bar.
  - Centers wide layouts so content does not stretch awkwardly on large monitors.
  - Improves grids, cards, and forms across admin/staff/guest pages.
*/

:root {
  --page-max: 1680px;
  --content-max: 1540px;
  --wide-gap: 24px;
}

/* Global page rhythm */
.content {
  width: 100%;
  max-width: var(--content-max);
  margin-inline: auto;
  padding: 28px clamp(18px, 2vw, 36px);
}

.topbar {
  padding-inline: clamp(18px, 2vw, 36px);
}

.topbar > * {
  max-width: var(--content-max);
}

.card {
  overflow: hidden;
}

/* Better grids on very wide screens */
.grid-2 {
  grid-template-columns: repeat(2, minmax(320px, 1fr));
}

.grid-3 {
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
}

.grid-4 {
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
}

.form-grid {
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
}

/* Guest request redesign: top hero instead of large left dark column */
.guest-page {
  min-height: 100vh;
  display: grid;
  grid-template-columns: 1fr;
  align-content: start;
  background:
    radial-gradient(circle at 12% 10%, rgba(21, 94, 239, 0.13), transparent 30%),
    radial-gradient(circle at 90% 80%, rgba(7, 148, 85, 0.12), transparent 34%),
    var(--bg);
}

.guest-hero {
  min-height: auto;
  width: min(calc(100% - 48px), var(--page-max));
  margin: 24px auto 0;
  padding: clamp(24px, 3vw, 42px);
  border-radius: 30px;
  background:
    linear-gradient(135deg, rgba(16, 24, 40, 0.98), rgba(21, 35, 63, 0.96)),
    #101828;
  box-shadow: 0 24px 70px rgba(16, 24, 40, 0.18);
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: center;
  gap: clamp(20px, 4vw, 64px);
}

.guest-hero > div:first-child {
  display: flex;
  align-items: center;
  gap: 14px;
}

.guest-hero h1 {
  font-size: clamp(34px, 4vw, 64px);
  margin: 18px 0 10px;
  letter-spacing: -0.04em;
}

.guest-hero p {
  max-width: 900px;
  font-size: clamp(16px, 1.2vw, 20px);
  line-height: 1.65;
  margin: 0;
}

.guest-content {
  width: min(calc(100% - 48px), var(--page-max));
  margin: 0 auto;
  padding: 24px 0 40px;
  align-self: auto;
}

.guest-content > .grid.grid-2 {
  grid-template-columns: minmax(520px, 1.25fr) minmax(360px, 0.75fr);
  gap: var(--wide-gap);
  align-items: start;
}

.guest-content .card {
  border-radius: 28px;
}

.guest-sticky {
  top: 24px;
}

.guest-menu {
  gap: 14px;
}

.guest-item {
  border-radius: 22px;
  transition: transform .16s ease, box-shadow .16s ease, border-color .16s ease;
}

.guest-item:hover {
  transform: translateY(-2px);
  box-shadow: 0 12px 34px rgba(16, 24, 40, 0.08);
  border-color: rgba(21, 94, 239, 0.20);
}

/* Make guest order form breathe better */
.guest-content form[data-form="guest-line"] .form-grid,
form[data-form="auth-line"] .form-grid {
  grid-template-columns: repeat(2, minmax(220px, 1fr));
}

.guest-content form[data-form="guest-line"] .field:has([name="quantity"]),
form[data-form="auth-line"] .field:has([name="quantity"]) {
  max-width: 260px;
}

/* Admin/staff wide screen polish */
.app-shell {
  background: var(--bg);
}

.main {
  min-width: 0;
  width: 100%;
}

.table-wrap {
  max-width: 100%;
}

.toolbar {
  gap: 18px;
}

.filters {
  gap: 14px;
}

.field {
  min-width: 0;
}

input, select, textarea, .input {
  width: 100%;
}

/* Large monitors */
@media (min-width: 1600px) {
  .content {
    padding-top: 34px;
    padding-bottom: 44px;
  }

  .card {
    padding: 24px;
  }

  .grid {
    gap: 22px;
  }

  .kpi {
    min-height: 160px;
  }
}

/* Medium screens */
@media (max-width: 1180px) {
  .guest-content > .grid.grid-2 {
    grid-template-columns: 1fr;
  }

  .guest-sticky {
    position: static;
  }

  .guest-hero {
    grid-template-columns: 1fr;
  }
}

/* Tablets and phones */
@media (max-width: 920px) {
  .guest-page {
    display: grid;
  }

  .guest-hero {
    width: calc(100% - 32px);
    margin-top: 16px;
    border-radius: 24px;
    padding: 28px 22px;
  }

  .guest-content {
    width: calc(100% - 32px);
    padding-top: 16px;
  }

  .guest-content form[data-form="guest-line"] .form-grid,
  form[data-form="auth-line"] .form-grid {
    grid-template-columns: 1fr;
  }

  .content {
    max-width: 100%;
    padding: 16px;
  }
}

@media (max-width: 620px) {
  .guest-hero {
    width: calc(100% - 24px);
    margin-top: 12px;
    padding: 22px 18px;
    border-radius: 22px;
  }

  .guest-hero h1 {
    font-size: clamp(30px, 12vw, 44px);
  }

  .guest-content {
    width: calc(100% - 24px);
  }

  .topbar {
    flex-direction: column;
    align-items: flex-start;
  }
}

/* === WIDE_SCREEN_LAYOUT_PATCH_END === */
"""


def backup_once(path: Path) -> None:
    if not path.exists():
        raise FileNotFoundError(f"File not found: {path}")
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    backup = path.with_suffix(path.suffix + f".bak-{stamp}")
    shutil.copy2(path, backup)
    print(f"Backup created: {backup}")


def patch_css() -> None:
    backup_once(CSS_FILE)
    css = CSS_FILE.read_text(encoding="utf-8")

    pattern = re.compile(
        re.escape(START) + r".*?" + re.escape(END),
        flags=re.DOTALL,
    )

    if pattern.search(css):
        css = pattern.sub(CSS_PATCH, css)
        print("Replaced existing wide screen layout patch in app.css.")
    else:
        css = css.rstrip() + "\n\n" + CSS_PATCH + "\n"
        print("Appended wide screen layout patch to app.css.")

    CSS_FILE.write_text(css, encoding="utf-8")


def bump_css_version() -> None:
    if not INDEX_FILE.exists():
        print(f"Skipping cache-bust update. Missing file: {INDEX_FILE}")
        return

    backup_once(INDEX_FILE)
    html = INDEX_FILE.read_text(encoding="utf-8")

    def repl(match: re.Match) -> str:
        prefix = match.group(1)
        version = int(match.group(2))
        suffix = match.group(3)
        return f"{prefix}{version + 1}{suffix}"

    updated = re.sub(
        r'(<link\s+rel="stylesheet"\s+href="/assets/css/app\.css\?v=)(\d+)(")',
        repl,
        html,
        count=1,
    )

    if updated == html:
        updated = html.replace(
            '<link rel="stylesheet" href="/assets/css/app.css" />',
            '<link rel="stylesheet" href="/assets/css/app.css?v=5" />',
        )

    if updated != html:
        INDEX_FILE.write_text(updated, encoding="utf-8")
        print("Updated app.css cache-busting version in index.html.")
    else:
        print("Could not find stylesheet link to update in index.html.")


def main() -> None:
    print("Patching wide screen layout...")
    patch_css()
    bump_css_version()
    print("\nDone.")
    print("Next commands:")
    print("  git diff -- src/HotelOrderSystem.Api/wwwroot/assets/css/app.css src/HotelOrderSystem.Api/wwwroot/index.html")
    print("  dotnet build src/HotelOrderSystem.Api/HotelOrderSystem.Api.csproj")
    print("  git add src/HotelOrderSystem.Api/wwwroot/assets/css/app.css src/HotelOrderSystem.Api/wwwroot/index.html")
    print('  git commit -m "Improve wide screen layout"')


if __name__ == "__main__":
    main()
