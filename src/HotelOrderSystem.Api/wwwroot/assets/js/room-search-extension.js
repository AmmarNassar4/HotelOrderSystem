(() => {
  "use strict";

  const DROPDOWN_STYLE = `
    #order-room-dropdown {
      display: none;
      position: absolute;
      z-index: 999;
      top: calc(100% + 4px);
      left: 0;
      right: 0;
      background: var(--surface, #fff);
      border: 1px solid var(--line, #e4e7ec);
      border-radius: 14px;
      padding: 4px 0;
      max-height: 230px;
      overflow-y: auto;
      list-style: none;
      margin: 0;
      box-shadow: 0 4px 20px rgba(0,0,0,.1);
    }
    #order-room-dropdown li {
      padding: 10px 14px;
      cursor: pointer;
      font-size: 14px;
      border-radius: 10px;
      margin: 0 4px;
      color: var(--ink, #101828);
      transition: background .1s;
    }
    #order-room-dropdown li:hover,
    #order-room-dropdown li.active {
      background: var(--primary-2, #eff4ff);
      color: var(--primary, #155eef);
    }
    #order-room-dropdown li.no-match {
      color: var(--muted, #667085);
      font-style: italic;
      cursor: default;
    }
    #order-room-dropdown li.no-match:hover {
      background: none;
      color: var(--muted, #667085);
    }
  `;

  function injectStyles() {
    if (document.getElementById("room-search-styles")) return;
    const el = document.createElement("style");
    el.id = "room-search-styles";
    el.textContent = DROPDOWN_STYLE;
    document.head.appendChild(el);
  }

  function h(v) {
    return String(v ?? "").replace(/[&<>"']/g, c => (
      { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]
    ));
  }

  function enhanceRoomSelect() {
    const select = document.getElementById("order-room");
    if (!select || select.dataset.searchEnhanced === "true") return;

    injectStyles();

    const rooms = Array.from(select.options)
      .map(o => ({ id: o.value, label: o.textContent.trim() }))
      .filter(r => r.id);

    const selectedValue = select.value || "";
    const currentRoom = rooms.find(r => String(r.id) === String(selectedValue)) || null;

    const wrapper = document.createElement("div");
    wrapper.className = "field";
    wrapper.dataset.roomSearchWrapper = "true";
    wrapper.style.position = "relative";

    wrapper.innerHTML = `
      <label>Room</label>
      <input
        id="order-room-search"
        type="text"
        placeholder="Type to filter rooms…"
        autocomplete="off"
        required
      />
      <ul id="order-room-dropdown"></ul>
    `;

    const input = wrapper.querySelector("#order-room-search");
    const dropdown = wrapper.querySelector("#order-room-dropdown");

    if (currentRoom) {
      input.value = currentRoom.label;
      select.value = currentRoom.id;
    }

    function getFiltered(text) {
      const q = text.trim().toLowerCase();
      if (!q) return rooms;
      return rooms.filter(r =>
        r.label.toLowerCase().includes(q) ||
        r.label.replace(/^room\s+/i, "").toLowerCase().includes(q)
      );
    }

    function openDropdown(filtered, query) {
      if (filtered.length === 0) {
        dropdown.innerHTML = `<li class="no-match">No rooms match &ldquo;${h(query)}&rdquo;</li>`;
      } else {
        const activeId = select.value;
        dropdown.innerHTML = filtered.map(r =>
          `<li data-rid="${h(r.id)}" class="${String(r.id) === String(activeId) ? "active" : ""}">${h(r.label)}</li>`
        ).join("");
      }
      dropdown.style.display = "block";
    }

    function closeDropdown() {
      dropdown.style.display = "none";
    }

    function pickRoom(room) {
      input.value = room.label;
      select.value = room.id;
      input.setCustomValidity("");
      closeDropdown();
      // Notify other listeners (e.g. order total recalc)
      select.dispatchEvent(new Event("change", { bubbles: true }));
    }

    function syncValidity() {
      const text = input.value.trim().toLowerCase();
      const match = rooms.find(r => r.label.toLowerCase() === text);
      select.value = match ? match.id : "";
      input.setCustomValidity(match ? "" : "Choose a valid room from the list.");
    }

    input.addEventListener("focus", () => openDropdown(getFiltered(input.value), input.value));
    input.addEventListener("input", () => {
      const text = input.value;
      openDropdown(getFiltered(text), text);
      syncValidity();
    });

    // mousedown prevents blur firing before the click registers
    dropdown.addEventListener("mousedown", e => {
      const li = e.target.closest("li[data-rid]");
      if (!li) return;
      e.preventDefault();
      const room = rooms.find(r => String(r.id) === String(li.dataset.rid));
      if (room) pickRoom(room);
    });

    input.addEventListener("blur", () => {
      setTimeout(closeDropdown, 160);
      syncValidity();
    });

    // Close if user clicks elsewhere in the document
    document.addEventListener("click", e => {
      if (!wrapper.contains(e.target)) closeDropdown();
    });

    select.dataset.searchEnhanced = "true";
    select.style.display = "none";
    select.closest(".field")?.after(wrapper);
  }

  function route() {
    if (["#/admin/create-order", "#/staff/create-order"].includes(location.hash)) {
      setTimeout(enhanceRoomSelect, 250);
    }
  }

  window.addEventListener("hashchange", route);
  new MutationObserver(route).observe(document.body, { childList: true, subtree: true });
  setTimeout(route, 600);
})();
