(() => {
  "use strict";

  function enhanceRoomSelect() {
    const select = document.getElementById("order-room");
    if (!select || select.dataset.searchEnhanced === "true") return;

    const selectedValue = select.value || "";
    const rooms = Array.from(select.options).map(option => ({
      id: option.value,
      label: option.textContent.trim()
    })).filter(room => room.id);

    const wrapper = document.createElement("div");
    wrapper.className = "field";
    wrapper.dataset.roomSearchWrapper = "true";

    const listId = "order-room-list";
    wrapper.innerHTML = `
      <label>Room</label>
      <input id="order-room-search" list="${listId}" placeholder="Type room number" autocomplete="off" required />
      <datalist id="${listId}">
        ${rooms.map(room => `<option value="${escapeAttr(room.label)}" data-room-id="${escapeAttr(room.id)}"></option>`).join("")}
      </datalist>
      <small class="field-help">Start typing the room number, then select the room.</small>
    `;

    const input = wrapper.querySelector("#order-room-search");
    const currentRoom = rooms.find(room => String(room.id) === String(selectedValue)) || rooms[0];
    if (currentRoom) {
      input.value = currentRoom.label;
      select.value = currentRoom.id;
    }

    input.addEventListener("input", () => syncRoomValue(input, select, rooms));
    input.addEventListener("change", () => syncRoomValue(input, select, rooms));

    select.dataset.searchEnhanced = "true";
    select.style.display = "none";
    select.closest(".field")?.after(wrapper);
  }

  function syncRoomValue(input, select, rooms) {
    const text = String(input.value || "").trim().toLowerCase();
    const exact = rooms.find(room => room.label.toLowerCase() === text);
    const byNumber = rooms.find(room => room.label.replace(/^room\s+/i, "").toLowerCase() === text);
    const startsWith = rooms.find(room => room.label.toLowerCase().startsWith(text) || room.label.replace(/^room\s+/i, "").toLowerCase().startsWith(text));
    const match = exact || byNumber || startsWith;

    select.value = match ? match.id : "";
    input.setCustomValidity(match ? "" : "Choose a valid room from the list.");
  }

  function escapeAttr(value) {
    return String(value ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#039;");
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
