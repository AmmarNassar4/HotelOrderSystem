from pathlib import Path

APP_JS = Path("src/HotelOrderSystem.Api/wwwroot/assets/js/app.js")


def main() -> None:
    text = APP_JS.read_text(encoding="utf-8")
    original = text

    marker = "\n  function startHeartbeat() {\n"
    helper = """
  function shouldAutoRefresh() {
    const route = normalizeHash();
    return [
      "admin/dashboard",
      "admin/orders",
      "admin/performance",
      "admin/presence",
      "staff/tasks"
    ].includes(route);
  }
"""

    if "function shouldAutoRefresh()" not in text:
        if marker not in text:
            raise RuntimeError("Could not find startHeartbeat() marker in app.js")
        text = text.replace(marker, "\n" + helper + marker.lstrip("\n"), 1)

    old_poll_condition = "if (document.hidden || !isLoggedIn()) return;"
    new_poll_condition = "if (document.hidden || !isLoggedIn() || !shouldAutoRefresh()) return;"

    if new_poll_condition not in text:
        if old_poll_condition not in text:
            raise RuntimeError("Could not find polling condition in app.js")
        text = text.replace(old_poll_condition, new_poll_condition, 1)

    if text == original:
        print("app.js already patched. No changes made.")
        return

    APP_JS.write_text(text, encoding="utf-8")
    print("Patched app.js: auto refresh now runs only on dashboard/orders/presence/performance/tasks pages.")


if __name__ == "__main__":
    main()
