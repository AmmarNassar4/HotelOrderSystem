
(() => {
  "use strict";

  const app = document.getElementById("app");
  const toastHost = document.getElementById("toast");
  const SESSION_KEY = "hotel.ops.session";
  const DEVICE_KEY = "hotel.ops.deviceId";

  const state = {
    session: loadSession(),
    hub: null,
    hubMode: null,
    heartbeatTimer: null,
    isReady: null,
    pollingTimer: null,
    isRendering: false,
    rooms: [],
    items: [],
    teams: [],
    users: [],
    orderDraft: [],
    guestDrafts: {},
    editing: {},
    orderFilters: { status: "", teamId: "", roomId: "" },
    performanceFilter: {},
    lastRefreshAt: 0
  };

  function loadSession() {
    try {
      return JSON.parse(localStorage.getItem(SESSION_KEY) || "null");
    } catch {
      return null;
    }
  }

  function saveSession(session) {
    state.session = session;
    if (session) localStorage.setItem(SESSION_KEY, JSON.stringify(session));
    else localStorage.removeItem(SESSION_KEY);
  }

  function deviceId() {
    let id = localStorage.getItem(DEVICE_KEY);
    if (!id) {
      id = "web-" + cryptoRandom();
      localStorage.setItem(DEVICE_KEY, id);
    }
    return id;
  }

  function cryptoRandom() {
    if (window.crypto?.getRandomValues) {
      const data = new Uint32Array(4);
      window.crypto.getRandomValues(data);
      return Array.from(data, x => x.toString(16)).join("");
    }
    return Math.random().toString(16).slice(2) + Date.now().toString(16);
  }

  function isLoggedIn() {
    return Boolean(state.session?.token);
  }

  function user() {
    return state.session?.user || null;
  }

  function role() {
    return user()?.role || "";
  }

  function isAdmin() {
    return role() === "Admin";
  }

  function isSupervisor() {
    return role() === "Supervisor";
  }

  function normalizeHash() {
    const raw = location.hash.replace(/^#\/?/, "");
    return raw || "";
  }

  function segments() {
    return normalizeHash().split("/").filter(Boolean).map(decodeURIComponent);
  }

  function go(path) {
    location.hash = path.startsWith("#") ? path : "#/" + path.replace(/^\/+/, "");
  }

  function redirect(path) {
    go(path);
    setTimeout(() => render(), 0);
  }

  function apiBase() {
    return localStorage.getItem("hotel.ops.apiBase") || "";
  }

  async function api(path, options = {}) {
    const method = options.method || "GET";
    const auth = options.auth !== false;
    const headers = { Accept: "application/json" };

    let body;
    if (options.body !== undefined) {
      headers["Content-Type"] = "application/json";
      body = JSON.stringify(options.body);
    }

    if (auth && state.session?.token) {
      headers.Authorization = `Bearer ${state.session.token}`;
    }

    const response = await fetch(apiBase() + path, { method, headers, body });
    const text = await response.text();
    let payload = null;

    if (text) {
      try { payload = JSON.parse(text); }
      catch { payload = { isSuccess: false, errorMessage: text }; }
    }

    if (response.status === 401 && auth) {
      logout(true);
      throw new Error("Your session expired. Please sign in again.");
    }

    if (!response.ok || payload?.isSuccess === false) {
      throw new Error(payload?.errorMessage || `HTTP ${response.status}`);
    }

    return payload?.data ?? payload;
  }

  function html(value) {
    return String(value ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#039;");
  }

  function attr(value) {
    return html(value).replaceAll("\n", " ");
  }

  function toast(message, type = "success") {
    const node = document.createElement("div");
    node.className = `toast ${type}`;
    node.textContent = message;
    toastHost.appendChild(node);
    setTimeout(() => node.remove(), 4200);
  }

  function loadingBlock(label = "Loading...") {
    return `<div class="loading"><div><div class="spinner"></div><div>${html(label)}</div></div></div>`;
  }

  function emptyBlock(message) {
    return `<div class="empty">${html(message)}</div>`;
  }

  function formatDate(value) {
    if (!value) return "-";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "-";
    return date.toLocaleString("en-US", { dateStyle: "medium", timeStyle: "short" });
  }

  function formatMinutes(value) {
    if (value === null || value === undefined || Number.isNaN(Number(value))) return "-";
    const n = Number(value);
    if (n < 1) return "Less than a minute";
    return `${Math.round(n)} min`;
  }

  function toLocalDateInput(value) {
    const date = value ? new Date(value) : new Date();
    if (Number.isNaN(date.getTime())) return "";
    const pad = n => String(n).padStart(2, "0");
    return `${date.getFullYear()}-${pad(date.getMonth()+1)}-${pad(date.getDate())}`;
  }

  function toDateTimeLocal(date) {
    const d = date ? new Date(date) : new Date();
    const pad = n => String(n).padStart(2, "0");
    return `${d.getFullYear()}-${pad(d.getMonth()+1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  function parseMaybeJson(text, fallback = {}) {
    if (!text || !String(text).trim()) return fallback;
    return JSON.parse(text);
  }

  function safeJson(value, fallback = {}) {
    if (value && typeof value === "object") return value;
    try { return JSON.parse(value || "{}"); }
    catch { return fallback; }
  }

  function sanitizeKey(value) {
    const raw = String(value || "").trim().toLowerCase();
    const cleaned = raw
      .replace(/[^a-z0-9]+/g, "_")
      .replace(/^_+|_+$/g, "");
    return cleaned || "field";
  }

  function titleFromKey(key) {
    return String(key || "field")
      .replace(/[_-]+/g, " ")
      .replace(/\b\w/g, ch => ch.toUpperCase());
  }

  function inferFieldType(value) {
    if (typeof value === "boolean") return "boolean";
    if (typeof value === "number") return "number";
    if (Array.isArray(value)) return "multiSelect";
    return "text";
  }

  function parseAttributeSchema(baseProperties) {
    const obj = typeof baseProperties === "string" ? safeJson(baseProperties) : (baseProperties || {});
    const sourceFields = Array.isArray(obj?.fields)
      ? obj.fields
      : Object.entries(obj || {}).map(([key, value]) => ({
          key,
          label: titleFromKey(key),
          type: inferFieldType(value),
          required: false,
          defaultValue: Array.isArray(value) ? value : value,
          options: Array.isArray(value) ? value : []
        }));

    return sourceFields
      .filter(Boolean)
      .map((field, index) => {
        const key = sanitizeKey(field.key || field.name || field.label || `field_${index + 1}`);
        const rawType = String(field.type || "text");
        const mappedType = { checkbox: "boolean", bool: "boolean", textarea: "notes", multiselect: "multiSelect", multi_select: "multiSelect" }[rawType] || rawType;
        const type = ["text", "number", "boolean", "select", "multiSelect", "date", "notes"].includes(mappedType)
          ? mappedType
          : "text";
        const options = Array.isArray(field.options)
          ? field.options.map(x => String(x).trim()).filter(Boolean)
          : String(field.options || "").split(",").map(x => x.trim()).filter(Boolean);
        return {
          key,
          label: String(field.label || titleFromKey(key)).trim(),
          type,
          required: Boolean(field.required),
          defaultValue: field.defaultValue ?? field.default ?? (type === "boolean" ? false : type === "multiSelect" ? [] : ""),
          options
        };
      });
  }

  function fieldDefaultText(value) {
    if (Array.isArray(value)) return value.join(", ");
    if (value === true) return "true";
    if (value === false) return "false";
    return String(value ?? "");
  }

  function attributeFieldRow(field = {}, index = 0) {
    const hasValue = Boolean(field.label || field.key || field.name || field.options || field.defaultValue || field.default);
    const normalized = hasValue ? (parseAttributeSchema({ fields: [field] })[0] || {
      key: `field_${index + 1}`,
      label: "",
      type: "text",
      required: false,
      defaultValue: "",
      options: []
    }) : {
      key: "",
      label: "",
      type: field.type || "text",
      required: false,
      defaultValue: "",
      options: []
    };

    return `
      <div class="attribute-row" data-attribute-row>
        <div class="field">
          <label>Field label</label>
          <input data-schema-label value="${attr(normalized.label)}" placeholder="Example: Size" required />
        </div>
        <div class="field">
          <label>Field key</label>
          <input data-schema-key value="${attr(normalized.key)}" placeholder="example: size" />
        </div>
        <div class="field">
          <label>Input type</label>
          <select data-schema-type>
            ${option("text", "Text", normalized.type)}
            ${option("number", "Number", normalized.type)}
            ${option("boolean", "Yes / No", normalized.type)}
            ${option("select", "Single choice", normalized.type)}
            ${option("multiSelect", "Multiple choices", normalized.type)}
            ${option("date", "Date", normalized.type)}
            ${option("notes", "Long note", normalized.type)}
          </select>
        </div>
        <label class="check-field">
          <input type="checkbox" data-schema-required ${normalized.required ? "checked" : ""} />
          <span>Required</span>
        </label>
        <div class="field">
          <label>Default value</label>
          <input data-schema-default value="${attr(fieldDefaultText(normalized.defaultValue))}" placeholder="Optional" />
        </div>
        <div class="field">
          <label>Choices</label>
          <input data-schema-options value="${attr((normalized.options || []).join(", "))}" placeholder="Small, Medium, Large" />
        </div>
        <button class="btn btn-danger" type="button" data-action="remove-attribute-field">Remove</button>
      </div>
    `;
  }

  function attributeBuilderMarkup(baseProperties) {
    const fields = parseAttributeSchema(baseProperties);
    const rows = fields.length ? fields.map(attributeFieldRow).join("") : attributeFieldRow({ label: "", key: "", type: "text" }, 0);
    return `
      <div class="field span-2">
        <label>Dynamic item fields</label>
        <div class="attribute-builder" data-attribute-builder>
          <div class="attribute-builder-head">
            <div>
              <strong>Field Builder</strong>
              <small>Define the fields the guest or staff member will fill in when ordering this item. The technical configuration stays hidden.</small>
            </div>
            <button class="btn btn-soft" type="button" data-action="add-attribute-field">Add field</button>
          </div>
          <div class="attribute-rows" data-attribute-rows>${rows}</div>
        </div>
      </div>
    `;
  }

  function parseOptionsText(text) {
    return String(text || "")
      .split(",")
      .map(x => x.trim())
      .filter(Boolean);
  }

  function parseDefaultValue(type, raw, options) {
    const value = String(raw ?? "").trim();
    if (type === "boolean") return ["true", "yes", "1", "on"].includes(value.toLowerCase());
    if (type === "number") return value ? Number(value) : null;
    if (type === "multiSelect") {
      const selected = parseOptionsText(value);
      return selected.length ? selected : [];
    }
    if (type === "select" && !value && options.length) return options[0];
    return value;
  }

  function collectAttributeSchema(form) {
    const rows = Array.from(form.querySelectorAll("[data-attribute-row]"));
    const fields = [];
    const usedKeys = new Set();

    rows.forEach((row, index) => {
      const labelInput = row.querySelector("[data-schema-label]");
      const keyInput = row.querySelector("[data-schema-key]");
      const typeInput = row.querySelector("[data-schema-type]");
      const requiredInput = row.querySelector("[data-schema-required]");
      const defaultInput = row.querySelector("[data-schema-default]");
      const optionsInput = row.querySelector("[data-schema-options]");

      const label = String(labelInput?.value || "").trim();
      if (!label) return;

      let key = sanitizeKey(keyInput?.value || label);
      const baseKey = key;
      let suffix = 2;
      while (usedKeys.has(key)) key = `${baseKey}_${suffix++}`;
      usedKeys.add(key);

      const type = typeInput?.value || "text";
      const options = parseOptionsText(optionsInput?.value || "");
      if ((type === "select" || type === "multiSelect") && !options.length) {
        throw new Error(`${label}: choices are required for choice fields.`);
      }
      const defaultValue = parseDefaultValue(type, defaultInput?.value, options);
      if (type === "number" && defaultInput?.value && Number.isNaN(defaultValue)) {
        throw new Error(`${label}: default value must be a valid number.`);
      }

      fields.push({
        key,
        label,
        type,
        required: Boolean(requiredInput?.checked),
        defaultValue,
        options
      });
    });

    return JSON.stringify({ fields });
  }

  function schemaSummary(baseProperties) {
    const fields = parseAttributeSchema(baseProperties);
    if (!fields.length) return "No dynamic fields";
    return fields.map(f => {
      const choices = (f.type === "select" || f.type === "multiSelect") && f.options?.length
        ? `: ${f.options.join(" / ")}`
        : "";
      return `${f.label} (${fieldTypeLabel(f.type)}${f.required ? ", required" : ""}${choices})`;
    }).join("; ");
  }

  function fieldTypeLabel(type) {
    return {
      text: "Text",
      number: "Number",
      boolean: "Yes/No",
      select: "Single choice",
      multiSelect: "Multiple choices",
      date: "Date",
      notes: "Long note"
    }[type] || "Text";
  }

  function buildDynamicFieldsMarkup(fields, prefix) {
    if (!fields.length) {
      return `<div class="empty compact-empty">No additional fields for this item.</div>`;
    }

    return fields.map(field => {
      const id = `${prefix}-attr-${field.key}`;
      const common = `data-dynamic-attr data-attr-key="${attr(field.key)}" data-attr-label="${attr(field.label)}" data-attr-type="${attr(field.type)}" data-required="${field.required ? "true" : "false"}"`;
      const required = field.required ? "required" : "";
      const defaultValue = field.defaultValue;
      const label = `<label for="${attr(id)}">${html(field.label)}${field.required ? " *" : ""}</label>`;

      if (field.type === "boolean") {
        const selected = defaultValue === true || String(defaultValue).toLowerCase() === "true" ? "true" : "false";
        return `<div class="field">${label}<select id="${attr(id)}" ${common}>${option("false", "No", selected)}${option("true", "Yes", selected)}</select></div>`;
      }

      if (field.type === "select") {
        return `<div class="field">${label}<select id="${attr(id)}" ${common} ${required}>${(field.options || []).map(opt => option(opt, opt, defaultValue)).join("")}</select></div>`;
      }

      if (field.type === "multiSelect") {
        const defaults = Array.isArray(defaultValue) ? defaultValue.map(String) : parseOptionsText(defaultValue);
        return `<div class="field">${label}<select id="${attr(id)}" ${common} multiple>${(field.options || []).map(opt => `<option value="${attr(opt)}" ${defaults.includes(String(opt)) ? "selected" : ""}>${html(opt)}</option>`).join("")}</select><small class="field-help">Hold Ctrl/Cmd to select multiple values.</small></div>`;
      }

      if (field.type === "notes") {
        return `<div class="field span-2">${label}<textarea id="${attr(id)}" ${common} ${required}>${html(defaultValue || "")}</textarea></div>`;
      }

      if (field.type === "date") {
        return `<div class="field">${label}<input id="${attr(id)}" type="date" value="${attr(defaultValue || "")}" ${common} ${required} /></div>`;
      }

      if (field.type === "number") {
        return `<div class="field">${label}<input id="${attr(id)}" type="number" step="any" value="${attr(defaultValue ?? "")}" ${common} ${required} /></div>`;
      }

      return `<div class="field">${label}<input id="${attr(id)}" type="text" value="${attr(defaultValue || "")}" ${common} ${required} /></div>`;
    }).join("");
  }

  function gatherDynamicAttributes(form) {
    const attrs = {};
    const controls = Array.from(form.querySelectorAll("[data-dynamic-attr]"));

    for (const control of controls) {
      const key = control.dataset.attrKey;
      const label = control.dataset.attrLabel || key;
      const type = control.dataset.attrType || "text";
      const required = control.dataset.required === "true";
      let value;

      if (type === "multiSelect") {
        value = Array.from(control.selectedOptions || []).map(opt => opt.value);
        if (required && !value.length) throw new Error(`${label} is required.`);
      } else if (type === "boolean") {
        value = control.value === "true";
      } else if (type === "number") {
        if (required && !String(control.value).trim()) throw new Error(`${label} is required.`);
        value = String(control.value).trim() ? Number(control.value) : null;
        if (value !== null && Number.isNaN(value)) throw new Error(`${label} must be a valid number.`);
      } else {
        value = String(control.value ?? "").trim();
        if (required && !value) throw new Error(`${label} is required.`);
      }

      attrs[key] = value;
    }

    return attrs;
  }

  function formatAttributes(value) {
    const obj = typeof value === "string" ? safeJson(value) : (value || {});
    const keys = Object.keys(obj || {});
    if (!keys.length) return "";
    return keys.map(key => {
      const raw = obj[key];
      const display = Array.isArray(raw) ? raw.join(", ") : (raw === true ? "Yes" : raw === false ? "No" : raw);
      return `${titleFromKey(key)}: ${display}`;
    }).join(", ");
  }

  function statusInfo(status) {
    const map = {
      Pending: ["Pending", "warning"],
      Accepted: ["Accepted", "primary"],
      InProgress: ["In progress", "info"],
      Completed: ["Completed", "success"],
      Cancelled: ["Cancelled", "danger"]
    };
    return map[status] || [status || "-", "info"];
  }

  function statusPill(status) {
    const [label, cls] = statusInfo(status);
    return `<span class="pill ${cls}">${html(label)}</span>`;
  }

  function sourceLabel(source) {
    return {
      GuestQR: "Guest QR",
      Admin: "Admin",
      StaffProxy: "Staff proxy"
    }[source] || source || "-";
  }

  function roleLabel(value) {
    return {
      Admin: "Admin",
      Supervisor: "Supervisor",
      Staff: "Staff"
    }[value] || value || "-";
  }

  function setView(markup) {
    app.innerHTML = markup;
    const sidebar = document.querySelector(".sidebar");
    if (sidebar) sidebar.classList.remove("open");
    updateConnectionPill();
  }

  function pageShell(title, body, actions = "") {
    const u = user();
    const nav = navItems().map(item => {
      const active = normalizeHash().startsWith(item.path.replace(/^\/+/, ""));
      return `<a class="${active ? "active" : ""}" href="#/${item.path}"><span class="nav-icon">${item.icon}</span><span>${item.label}</span></a>`;
    }).join("");

    const isUserReady = state.isReady ?? false;
    const readyToggle = isAdmin() ? "" : `
      <button class="btn btn-block" style="margin-top:8px;background:${isUserReady ? 'var(--color-success)' : 'var(--color-warning)'};color:#fff;border:none;cursor:pointer" data-action="toggle-ready">
        ${isUserReady ? "<span class='status-dot online'></span> Ready — click to Not Ready" : "<span class='status-dot offline'></span> Not Ready — click to Ready"}
      </button>
    `;

    return `
      <div class="app-shell">
        <aside class="sidebar">
          <div class="sidebar-brand">
            <div class="brand-mark">H</div>
            <div>
              <div class="brand-title">Hotel Ops</div>
              <div class="brand-subtitle">API-first dashboard</div>
            </div>
          </div>
          <nav class="nav">${nav}</nav>
          <div class="sidebar-footer">
            <div class="user-chip">
              <strong>${html(u?.fullName || u?.userName || "")}</strong>
              <small>${html(roleLabel(u?.role))}${u?.teamName ? " - " + html(u.teamName) : ""}</small>
            </div>
            ${readyToggle}
            <button class="btn btn-soft btn-block" style="margin-top:10px" data-action="logout">Sign out</button>
          </div>
        </aside>
        <main class="main">
          <header class="topbar">
            <div style="display:flex;align-items:center;gap:10px">
              <button class="btn btn-soft mobile-menu" data-action="toggle-menu">☰</button>
              <h1>${html(title)}</h1>
            </div>
            <div class="topbar-meta">
              <span class="pill" id="connection-pill">Offline</span>
              ${actions}
              <button class="btn btn-soft" data-action="refresh">Refresh</button>
            </div>
          </header>
          <section class="content">${body}</section>
        </main>
      </div>
    `;
  }

  function navItems() {
    if (isAdmin()) {
      return [
        { path: "admin/dashboard", label: "Dashboard", icon: "📊" },
        { path: "admin/orders", label: "Orders", icon: "🧾" },
        { path: "admin/create-order", label: "Create Order", icon: "➕" },
        { path: "admin/performance", label: "Performance", icon: "📈" },
        { path: "admin/presence", label: "Staff Presence", icon: "🟢" },
        { path: "admin/rooms", label: "Rooms & Guest Links", icon: "🚪" },
        { path: "admin/items", label: "Items & Services", icon: "🛎️" },
        { path: "admin/teams", label: "Teams", icon: "👥" },
        { path: "admin/users", label: "Users", icon: "🔐" },
        { path: "staff/tasks", label: "Staff Console", icon: "📱" }
      ];
    }

    return [
      { path: "staff/tasks", label: "My Tasks", icon: "📋" },
      { path: "staff/create-order", label: "Create Guest Order", icon: "➕" }
    ];
  }

  function renderLogin() {
    stopRealtime();
    stopHeartbeat();
    setView(`
      <div class="auth-page">
        <section class="auth-hero">
          <div>
            <div class="sidebar-brand" style="padding:0">
              <div class="brand-mark">H</div>
              <div>
                <div class="brand-title">Hotel Order System</div>
               
              </div>
            </div>
            <h1>Manage hotel requests from one place</h1>
            <p>An admin dashboard, a mobile-friendly staff console, and guest QR pages for creating requests without sign-in.</p>
            
          </div>
          
        </section>
        <section class="auth-card">
          <h2>Sign in</h2>
          <p>Use an admin or staff account. Available screens are controlled by role and team.</p>
          <form data-form="login" class="grid" style="gap:14px">
            <div class="field">
              <label>Username</label>
              <input name="userName" autocomplete="username" required />
            </div>
            <div class="field">
              <label>Password</label>
              <input name="password" type="password" autocomplete="current-password" required />
            </div>
            <button class="btn btn-primary btn-block" type="submit">Sign in</button>
          </form>
          
        </section>
      </div>
    `);
  }

  async function render() {
    if (state.isRendering) return;
    state.isRendering = true;

    try {
      const parts = segments();

      if (parts[0] === "guest") {
        await renderGuest(parts[1] || "");
        return;
      }

      if (!isLoggedIn()) {
        renderLogin();
        return;
      }

      startHeartbeat();
      startRealtime();

      if (!parts.length) {
        redirect(isAdmin() ? "admin/dashboard" : "staff/tasks");
        return;
      }

      if (parts[0] === "admin" && !isAdmin()) {
        redirect("staff/tasks");
        return;
      }

      switch (`${parts[0]}/${parts[1] || ""}`) {
        case "admin/dashboard": await renderAdminDashboard(); break;
        case "admin/orders": await renderAdminOrders(); break;
        case "admin/create-order": await renderCreateOrderPage("Admin"); break;
        case "admin/performance": await renderPerformance(); break;
        case "admin/presence": await renderPresence(); break;
        case "admin/rooms": await renderRooms(); break;
        case "admin/items": await renderItems(); break;
        case "admin/teams": await renderTeams(); break;
        case "admin/users": await renderUsers(); break;
        case "staff/tasks": await renderStaffTasks(); break;
        case "staff/create-order": await renderCreateOrderPage("StaffProxy"); break;
        default: redirect(isAdmin() ? "admin/dashboard" : "staff/tasks"); break;
      }
    } catch (err) {
      setView(pageShell("Error", `<div class="card"><h2>The page could not be loaded</h2><p class="card-subtitle">${html(err.message || err)}</p><button class="btn btn-primary" data-action="refresh">Try again</button></div>`));
    } finally {
      state.isRendering = false;
    }
  }

  async function renderAdminDashboard() {
    setView(pageShell("Dashboard", loadingBlock()));
    const [summary, orders, presence, performance] = await Promise.all([
      api("/api/v1/admin/dashboard/live-summary"),
      loadAdminOrders({ status: "Pending", take: 8 }),
      api("/api/v1/presence/users/online").catch(() => []),
      api("/api/v1/admin/performance").catch(() => null)
    ]);

    const kpis = `
      <div class="grid grid-4">
        ${kpi("Pending orders", summary.pendingOrders, "⏳", "Requests waiting to be accepted")}
        ${kpi("Active orders", summary.acceptedOrders, "🛠️", "Accepted or in progress")}
        ${kpi("Online staff", summary.onlineStaff, "🟢", "Based on heartbeat")}
        ${kpi("Active rooms", summary.activeRooms, "🚪", "Ready to receive guest requests")}
      </div>
    `;

    const perf = performance ? `
      <div class="grid grid-3">
        ${miniMetric("Completion rate", `${performance.completionRatePercent}%`, performance.completionRatePercent)}
        ${miniMetric("Average accept time", formatMinutes(performance.averageAcceptMinutes), performance.averageAcceptMinutes ? Math.min(100, 100 - performance.averageAcceptMinutes) : 0)}
        ${miniMetric("Average completion time", formatMinutes(performance.averageCompletionMinutes), performance.averageCompletionMinutes ? Math.min(100, 100 - performance.averageCompletionMinutes) : 0)}
      </div>
    ` : "";

    const body = `
      ${kpis}
      <div class="grid grid-2" style="margin-top:18px">
        <section class="card">
          <div class="card-header">
            <div>
              <h2 class="card-title">Pending orders now</h2>
              <p class="card-subtitle">Updates automatically through SignalR or manually with Refresh.</p>
            </div>
            <a class="btn btn-soft" href="#/admin/orders">All orders</a>
          </div>
          ${renderOrdersList(orders, "compact")}
        </section>
        <section class="card">
          <div class="card-header">
            <div>
              <h2 class="card-title">Staff presence</h2>
              <p class="card-subtitle">Latest online/offline state based on heartbeat.</p>
            </div>
            <a class="btn btn-soft" href="#/admin/presence">Details</a>
          </div>
          ${renderPresenceList(presence.slice(0, 8))}
        </section>
      </div>
      <section class="card" style="margin-top:18px">
        <div class="card-header">
          <div>
            <h2 class="card-title">Performance summary - last 7 days</h2>
            <p class="card-subtitle">A quick view of order acceptance and completion speed.</p>
          </div>
          <a class="btn btn-soft" href="#/admin/performance">Full report</a>
        </div>
        ${perf || emptyBlock("Not enough performance data yet.")}
      </section>
    `;

    setView(pageShell("Dashboard", body));
  }

  function kpi(label, value, icon, hint) {
    return `
      <section class="card kpi">
        <div>
          <div class="kpi-label">${html(label)}</div>
          <div class="kpi-value">${html(value)}</div>
          <div class="footer-note">${html(hint)}</div>
        </div>
        <div class="kpi-icon">${icon}</div>
      </section>
    `;
  }

  function miniMetric(label, value, percent) {
    const width = Math.max(0, Math.min(100, Number(percent) || 0));
    return `
      <div class="metric-row">
        <div class="metric-label"><span>${html(label)}</span><strong>${html(value)}</strong></div>
        <div class="bar"><span style="width:${width}%"></span></div>
      </div>
    `;
  }

  async function renderAdminOrders() {
    setView(pageShell("Order Management", loadingBlock()));
    const [rooms, teams] = await Promise.all([loadRooms(), loadTeams()]);
    const orders = await loadAdminOrders({ ...state.orderFilters, take: 300 });

    const body = `
      <div class="toolbar">
        <form class="filters" data-form="order-filter">
          <div class="field">
            <label>Status</label>
            <select name="status">
              ${option("", "All statuses", state.orderFilters.status)}
              ${["Pending","Accepted","InProgress","Completed","Cancelled"].map(s => option(s, statusInfo(s)[0], state.orderFilters.status)).join("")}
            </select>
          </div>
          <div class="field">
            <label>Team</label>
            <select name="teamId">
              ${option("", "All teams", state.orderFilters.teamId)}
              ${teams.map(t => option(t.teamId, t.name, state.orderFilters.teamId)).join("")}
            </select>
          </div>
          <div class="field">
            <label>Room</label>
            <select name="roomId">
              ${option("", "All rooms", state.orderFilters.roomId)}
              ${rooms.map(r => option(r.roomId, "Room " + r.roomNumber, state.orderFilters.roomId)).join("")}
            </select>
          </div>
          <button class="btn btn-primary" type="submit">Apply filters</button>
        </form>
        <a href="#/admin/create-order" class="btn btn-dark">Create new order</a>
      </div>
      <section class="card">
        <div class="card-header">
          <div>
            <h2 class="card-title">All orders</h2>
            <p class="card-subtitle">Latest 300 orders with accept, complete, and cancel actions where available.</p>
          </div>
          <span class="pill primary">${orders.length} orders</span>
        </div>
        ${renderOrdersList(orders, "admin")}
      </section>
    `;

    setView(pageShell("Order Management", body));
  }

  async function renderStaffTasks() {
    setView(pageShell("Staff Console", loadingBlock()));
    const [pending, active] = await Promise.all([
      api("/api/v1/orders/pending"),
      api("/api/v1/orders/my-active")
    ]);

    const body = `
      <div class="grid grid-2">
        <section class="card">
          <div class="card-header">
            <div>
              <h2 class="card-title">New requests for your team</h2>
              <p class="card-subtitle">Click Accept to claim the task and lock it for other staff members.</p>
            </div>
            <span class="pill warning">${pending.length}</span>
          </div>
          ${renderOrdersList(pending, "staff")}
        </section>
        <section class="card">
          <div class="card-header">
            <div>
              <h2 class="card-title">My active tasks</h2>
              <p class="card-subtitle">Tasks you accepted and still need to complete.</p>
            </div>
            <span class="pill primary">${active.length}</span>
          </div>
          ${renderOrdersList(active, "staff")}
        </section>
      </div>
    `;

    setView(pageShell("Staff Console", body, `<a href="#/staff/create-order" class="btn btn-primary">Create guest order</a>`));
  }

  async function renderCreateOrderPage(source) {
    const title = source === "Admin" ? "Create order as admin" : "Create order for a guest";
    setView(pageShell(title, loadingBlock()));
    const [rooms, items] = await Promise.all([loadRooms(), loadItems()]);
    const draft = state.orderDraft;

    const body = `
      <div class="grid grid-2">
        <section class="card">
          <div class="card-header">
            <div>
              <h2 class="card-title">Order details</h2>
              <p class="card-subtitle">Choose a room and add at least one item or service.</p>
            </div>
          </div>
          <form class="grid" data-form="auth-line">
            <div class="field">
              <label>Room</label>
              <select id="order-room" name="roomId" required>
                ${rooms.map(r => option(r.roomId, "Room " + r.roomNumber, document.getElementById("order-room")?.value)).join("")}
              </select>
            </div>
            ${linePicker(items, "auth")}
            <button class="btn btn-soft" type="submit">Add to my request</button>
          </form>
        </section>
        <section class="card">
          <div class="card-header">
            <div>
              <h2 class="card-title">Order cart</h2>
              <p class="card-subtitle">The order will be routed automatically by the target team for each item.</p>
            </div>
          </div>
          ${renderCart(draft, "auth")}
          <div class="actions" style="margin-top:14px">
            <button class="btn btn-primary" data-action="submit-auth-order" data-source="${attr(source)}" ${draft.length ? "" : "disabled"}>Submit order</button>
            <button class="btn btn-soft" data-action="clear-auth-draft" ${draft.length ? "" : "disabled"}>Clear cart</button>
          </div>
        </section>
      </div>
    `;

    setView(pageShell(title, body));
  }

  async function renderRooms() {
    setView(pageShell("Rooms & Guest Links", loadingBlock()));
    const rooms = await loadRooms();
    const editing = state.editing.roomId ? rooms.find(x => String(x.roomId) === String(state.editing.roomId)) : null;

    const form = `
      <form class="card grid" data-form="room">
        <div class="card-header">
          <div>
            <h2 class="card-title">${editing ? "Edit room" : "Add room"}</h2>
            <p class="card-subtitle">Guest links are used in room QR codes so guests can create requests without sign-in.</p>
          </div>
          ${editing ? `<button type="button" class="btn btn-soft" data-action="cancel-edit">Cancel edit</button>` : ""}
        </div>
        <input type="hidden" name="id" value="${attr(editing?.roomId || "")}" />
        <div class="form-grid">
          <div class="field">
            <label>Room number</label>
            <input name="roomNumber" value="${attr(editing?.roomNumber || "")}" required />
          </div>
          <div class="field">
            <label>Guest payload / QR</label>
            <input name="directLinkPayload" value="${attr(editing?.directLinkPayload || "")}" placeholder="Auto-generated if left empty" />
          </div>
          <label class="field">
            <span>Status</span>
            <select name="isActive">
              ${option("true", "Active", String(editing?.isActive ?? true))}
              ${option("false", "Inactive", String(editing?.isActive ?? true))}
            </select>
          </label>
        </div>
        <button class="btn btn-primary" type="submit">${editing ? "Save changes" : "Add room"}</button>
      </form>
    `;

    const rows = rooms.map(r => {
      const link = `${location.origin}/#/guest/${encodeURIComponent(r.directLinkPayload)}`;
      return `
        <tr>
          <td><strong>${html(r.roomNumber)}</strong></td>
          <td>${r.isActive ? `<span class="pill success">Active</span>` : `<span class="pill danger">Inactive</span>`}</td>
          <td><code>${html(r.directLinkPayload)}</code></td>
          <td>
            <div class="actions">
              <button class="btn btn-soft" data-action="copy-link" data-link="${attr(link)}">Copy guest link</button>
              <a class="btn btn-soft" href="#/guest/${encodeURIComponent(r.directLinkPayload)}" target="_blank">Open guest page</a>
              <button class="btn btn-soft" data-action="edit-room" data-id="${r.roomId}">Edit</button>
              <button class="btn btn-danger" data-action="delete-room" data-id="${r.roomId}">Delete</button>
            </div>
          </td>
        </tr>
      `;
    }).join("");

    setView(pageShell("Rooms & Guest Links", `
      <div class="grid">
        ${form}
        <section class="card">
          <div class="card-header">
            <div>
              <h2 class="card-title">Room list</h2>
              <p class="card-subtitle">Copy a room guest link and convert it into a QR code.</p>
            </div>
            <span class="pill primary">${rooms.length} rooms</span>
          </div>
          <div class="table-wrap"><table><thead><tr><th>Room</th><th>Status</th><th>Payload</th><th>Actions</th></tr></thead><tbody>${rows}</tbody></table></div>
        </section>
      </div>
    `));
  }

  async function renderItems() {
    setView(pageShell("Items & Services", loadingBlock()));
    const [items, teams] = await Promise.all([loadItems(), loadTeams()]);
    const editing = state.editing.itemId ? items.find(x => String(x.itemId) === String(state.editing.itemId)) : null;

    const form = `
      <form class="card grid" data-form="item">
        <div class="card-header">
          <div>
            <h2 class="card-title">${editing ? "Edit item / service" : "Add item / service"}</h2>
            <p class="card-subtitle">The target team receives the task notification. Dynamic fields are configured visually and saved behind the scenes.</p>
          </div>
          ${editing ? `<button type="button" class="btn btn-soft" data-action="cancel-edit">Cancel edit</button>` : ""}
        </div>
        <input type="hidden" name="id" value="${attr(editing?.itemId || "")}" />
        <div class="form-grid">
          <div class="field">
            <label>Name</label>
            <input name="name" value="${attr(editing?.name || "")}" required />
          </div>
          <div class="field">
            <label>Type</label>
            <select name="type">
              ${option("Product", "Product", editing?.type || "Product")}
              ${option("Service", "Service", editing?.type || "Product")}
            </select>
          </div>
          <div class="field">
            <label>Target team</label>
            <select name="targetTeamId">
              ${option("", "All teams / Broadcast", editing?.targetTeamId ?? "")}
              ${teams.map(t => option(t.teamId, t.name, editing?.targetTeamId ?? "")).join("")}
            </select>
          </div>
          <div class="field">
            <label>Status</label>
            <select name="isActive">
              ${option("true", "Active", String(editing?.isActive ?? true))}
              ${option("false", "Inactive", String(editing?.isActive ?? true))}
            </select>
          </div>
          ${attributeBuilderMarkup(editing?.baseProperties || '{"fields":[]}')}
        </div>
        <button class="btn btn-primary" type="submit">${editing ? "Save changes" : "Add item"}</button>
      </form>
    `;

    const rows = items.map(i => `
      <tr>
        <td><strong>${html(i.name)}</strong><br><small>${html(i.type)}</small></td>
        <td>${html(i.targetTeamName || "All teams")}</td>
        <td>${i.isActive ? `<span class="pill success">Active</span>` : `<span class="pill danger">Inactive</span>`}</td>
        <td>${html(schemaSummary(i.baseProperties))}</td>
        <td>
          <div class="actions">
            <button class="btn btn-soft" data-action="edit-item" data-id="${i.itemId}">Edit</button>
            <button class="btn btn-danger" data-action="delete-item" data-id="${i.itemId}">Delete</button>
          </div>
        </td>
      </tr>
    `).join("");

    setView(pageShell("Items & Services", `
      <div class="grid">
        ${form}
        <section class="card">
          <div class="card-header">
            <div>
              <h2 class="card-title">Item and service catalog</h2>
              <p class="card-subtitle">Each item can define its own fields without showing technical configuration to users.</p>
            </div>
            <span class="pill primary">${items.length} items</span>
          </div>
          <div class="table-wrap"><table><thead><tr><th>Item</th><th>Team</th><th>Status</th><th>Dynamic fields</th><th>Actions</th></tr></thead><tbody>${rows}</tbody></table></div>
        </section>
      </div>
    `));
  }

  async function renderTeams() {
    setView(pageShell("Teams", loadingBlock()));
    const teams = await loadTeams();
    const editing = state.editing.teamId ? teams.find(x => String(x.teamId) === String(state.editing.teamId)) : null;

    const rows = teams.map(t => `
      <tr>
        <td><strong>${html(t.name)}</strong></td>
        <td>${t.isActive ? `<span class="pill success">Active</span>` : `<span class="pill danger">Inactive</span>`}</td>
        <td>
          <div class="actions">
            <button class="btn btn-soft" data-action="edit-team" data-id="${t.teamId}">Edit</button>
            <button class="btn btn-danger" data-action="delete-team" data-id="${t.teamId}">Delete</button>
          </div>
        </td>
      </tr>
    `).join("");

    setView(pageShell("Teams", `
      <div class="grid grid-2">
        <form class="card grid" data-form="team">
          <div class="card-header">
            <div>
              <h2 class="card-title">${editing ? "Edit team" : "Add team"}</h2>
              <p class="card-subtitle">Teams isolate tasks and notifications.</p>
            </div>
            ${editing ? `<button type="button" class="btn btn-soft" data-action="cancel-edit">Cancel</button>` : ""}
          </div>
          <input type="hidden" name="id" value="${attr(editing?.teamId || "")}" />
          <div class="field"><label>Team name</label><input name="name" value="${attr(editing?.name || "")}" required /></div>
          <div class="field"><label>Status</label><select name="isActive">${option("true","Active",String(editing?.isActive ?? true))}${option("false","Inactive",String(editing?.isActive ?? true))}</select></div>
          <button class="btn btn-primary" type="submit">${editing ? "Save" : "Add"}</button>
        </form>
        <section class="card">
          <div class="card-header"><h2 class="card-title">Team list</h2><span class="pill primary">${teams.length}</span></div>
          <div class="table-wrap"><table><thead><tr><th>Team</th><th>Status</th><th>Actions</th></tr></thead><tbody>${rows}</tbody></table></div>
        </section>
      </div>
    `));
  }

  async function renderUsers() {
    setView(pageShell("Users", loadingBlock()));
    const [users, teams] = await Promise.all([loadUsers(), loadTeams()]);
    const editing = state.editing.userId ? users.find(x => String(x.userId) === String(state.editing.userId)) : null;

    const form = `
      <form class="card grid" data-form="user">
        <div class="card-header">
          <div>
            <h2 class="card-title">${editing ? "Edit user" : "Add user"}</h2>
            <p class="card-subtitle">Select the role and team so staff receive the correct tasks.</p>
          </div>
          ${editing ? `<button type="button" class="btn btn-soft" data-action="cancel-edit">Cancel</button>` : ""}
        </div>
        <input type="hidden" name="id" value="${attr(editing?.userId || "")}" />
        <div class="form-grid">
          <div class="field"><label>Full name</label><input name="fullName" value="${attr(editing?.fullName || "")}" required /></div>
          <div class="field"><label>Username</label><input name="userName" value="${attr(editing?.userName || "")}" ${editing ? "readonly" : "required"} /></div>
          <div class="field"><label>${editing ? "New password (optional)" : "Password"}</label><input name="password" type="password" ${editing ? "" : "required"} /></div>
          <div class="field"><label>Role</label><select name="role">${["Admin","Supervisor","Staff"].map(r => option(r, roleLabel(r), editing?.role || "Staff")).join("")}</select></div>
          <div class="field"><label>Team</label><select name="teamId">${option("", "No team", editing?.teamId ?? "")}${teams.map(t => option(t.teamId, t.name, editing?.teamId ?? "")).join("")}</select></div>
          <div class="field"><label>Status</label><select name="isActive">${option("true","Active",String(editing?.isActive ?? true))}${option("false","Inactive",String(editing?.isActive ?? true))}</select></div>
        </div>
        <button class="btn btn-primary" type="submit">${editing ? "Save changes" : "Add user"}</button>
      </form>
    `;

    const rows = users.map(u => `
      <tr>
        <td><strong>${html(u.fullName)}</strong><br><small>${html(u.userName)}</small></td>
        <td>${html(roleLabel(u.role))}</td>
        <td>${html(u.teamName || "-")}</td>
        <td>
          <div class="actions">
            <button class="btn btn-soft" data-action="edit-user" data-id="${u.userId}">Edit</button>
            <button class="btn btn-danger" data-action="delete-user" data-id="${u.userId}">Delete</button>
          </div>
        </td>
      </tr>
    `).join("");

    setView(pageShell("Users", `
      <div class="grid">
        ${form}
        <section class="card">
          <div class="card-header"><h2 class="card-title">User list</h2><span class="pill primary">${users.length}</span></div>
          <div class="table-wrap"><table><thead><tr><th>User</th><th>Role</th><th>Team</th><th>Actions</th></tr></thead><tbody>${rows}</tbody></table></div>
        </section>
      </div>
    `));
  }

  async function renderPresence() {
    setView(pageShell("Staff Presence", loadingBlock()));
    const presence = await api("/api/v1/presence/users/online");

    setView(pageShell("Staff Presence", `
      <section class="card">
        <div class="card-header">
          <div>
            <h2 class="card-title">Connection status</h2>
            <p class="card-subtitle">Presence is updated by the web/mobile heartbeat.</p>
          </div>
          <span class="pill success">${presence.filter(x => x.isOnline).length} online</span>
        </div>
        ${renderPresenceList(presence)}
      </section>
    `));
  }

  async function renderPerformance() {
    setView(pageShell("Performance Report", loadingBlock()));
    const filter = state.performanceFilter;
    const params = new URLSearchParams();
    if (filter.fromUtc) params.set("fromUtc", filter.fromUtc);
    if (filter.toUtc) params.set("toUtc", filter.toUtc);
    const perf = await api(`/api/v1/admin/performance${params.toString() ? "?" + params.toString() : ""}`);

    const maxStatus = Math.max(1, ...perf.byStatus.map(x => x.count));
    const statusBars = perf.byStatus.map(x => miniMetric(statusInfo(x.status)[0], x.count, x.count * 100 / maxStatus)).join("");

    const teamRows = perf.byTeam.map(x => `
      <tr>
        <td><strong>${html(x.teamName)}</strong></td>
        <td>${x.totalOrders}</td>
        <td>${x.pendingOrders}</td>
        <td>${x.completedOrders}</td>
        <td>${x.cancelledOrders}</td>
        <td>${x.escalatedOrders}</td>
        <td>${formatMinutes(x.averageAcceptMinutes)}</td>
        <td>${formatMinutes(x.averageCompletionMinutes)}</td>
      </tr>
    `).join("");

    const staffRows = perf.byStaff.map(x => `
      <tr>
        <td><strong>${html(x.fullName)}</strong><br><small>${html(x.teamName || "-")}</small></td>
        <td>${x.activeOrders}</td>
        <td>${x.completedOrders}</td>
        <td>${formatMinutes(x.averageAcceptMinutes)}</td>
        <td>${formatMinutes(x.averageCompletionMinutes)}</td>
      </tr>
    `).join("");

    const fromValue = filter.fromLocal || toDateTimeLocal(new Date(perf.fromUtc));
    const toValue = filter.toLocal || toDateTimeLocal(new Date(perf.toUtc));

    const body = `
      <section class="card">
        <form class="filters" data-form="performance-filter">
          <div class="field"><label>From</label><input name="fromLocal" type="datetime-local" value="${attr(fromValue)}" /></div>
          <div class="field"><label>To</label><input name="toLocal" type="datetime-local" value="${attr(toValue)}" /></div>
          <button class="btn btn-primary" type="submit">Update report</button>
        </form>
      </section>
      <div class="grid grid-4" style="margin-top:18px">
        ${kpi("Total orders", perf.totalOrders, "🧾", "In selected period")}
        ${kpi("Completed", perf.completedOrders, "✅", `Completion rate ${perf.completionRatePercent}%`)}
        ${kpi("Pending", perf.pendingOrders, "⏳", "Not accepted yet")}
        ${kpi("Escalated", perf.escalatedOrders, "🚨", "Exceeded SLA")}
      </div>
      <div class="grid grid-2" style="margin-top:18px">
        <section class="card">
          <div class="card-header"><h2 class="card-title">Status distribution</h2></div>
          <div class="grid">${statusBars || emptyBlock("No data")}</div>
        </section>
        <section class="card">
          <div class="card-header"><h2 class="card-title">Speed averages</h2></div>
          <div class="grid">
            ${miniMetric("Average accept time", formatMinutes(perf.averageAcceptMinutes), perf.averageAcceptMinutes ? Math.min(100, 100 - perf.averageAcceptMinutes) : 0)}
            ${miniMetric("Average completion time", formatMinutes(perf.averageCompletionMinutes), perf.averageCompletionMinutes ? Math.min(100, 100 - perf.averageCompletionMinutes) : 0)}
          </div>
        </section>
      </div>
      <section class="card" style="margin-top:18px">
        <div class="card-header"><h2 class="card-title">Performance by team</h2></div>
        <div class="table-wrap"><table><thead><tr><th>Team</th><th>Total</th><th>Pending</th><th>Completed</th><th>Cancelled</th><th>Escalated</th><th>Accept</th><th>Complete</th></tr></thead><tbody>${teamRows}</tbody></table></div>
      </section>
      <section class="card" style="margin-top:18px">
        <div class="card-header"><h2 class="card-title">Performance by staff</h2></div>
        <div class="table-wrap"><table><thead><tr><th>Staff member</th><th>Active</th><th>Completed</th><th>Accept</th><th>Complete</th></tr></thead><tbody>${staffRows || `<tr><td colspan="5">No staff data</td></tr>`}</tbody></table></div>
      </section>
    `;

    setView(pageShell("Performance Report", body));
  }

  async function renderGuest(payload) {
    stopRealtime();
    stopHeartbeat();

    if (!payload) {
      setView(guestShell("Invalid link", emptyBlock("The room link is incomplete.")));
      return;
    }

    setView(guestShell("Guest request", loadingBlock("Loading room catalog...")));

    try {
      const catalog = await api(`/api/v1/guest/rooms/${encodeURIComponent(payload)}/catalog`, { auth: false });
      const draft = state.guestDrafts[payload] || [];
      state.guestDrafts[payload] = draft;

      const body = `
        <div class="guest-content">
          <div class="grid grid-2">
            <section class="card">
              <div class="card-header">
                <div>
                  <h2 class="card-title">Room ${html(catalog.room.roomNumber)}</h2>
                  <p class="card-subtitle">Select the items or services you need, then submit your request.</p>
                </div>
              </div>
              <form class="grid" data-form="guest-line" data-payload="${attr(payload)}">
                ${linePicker(catalog.items, "guest")}
                <button class="btn btn-soft" type="submit">Add to request</button>
              </form>
              <div class="guest-menu" style="margin-top:16px">
                ${catalog.items.map(item => `
                  <div class="guest-item">
                    <h3>${html(item.name)}</h3>
                    <div class="order-meta">
                      <span class="pill">${html(item.type === "Service" ? "Service" : "Product")}</span>
                      ${item.targetTeamName ? `<span class="pill info">${html(item.targetTeamName)}</span>` : ""}
                    </div>
                    <small class="footer-note">${html(schemaSummary(item.baseProperties))}</small>
                  </div>
                `).join("")}
              </div>
            </section>
            <aside class="card guest-sticky">
              <div class="card-header">
                <div>
                  <h2 class="card-title">Your request</h2>
                  <p class="card-subtitle">No sign-in required.</p>
                </div>
              </div>
              ${renderCart(draft, "guest")}
              <div class="actions" style="margin-top:14px">
                <button class="btn btn-primary" data-action="submit-guest-order" data-payload="${attr(payload)}" ${draft.length ? "" : "disabled"}>Submit request</button>
                <button class="btn btn-soft" data-action="clear-guest-draft" data-payload="${attr(payload)}" ${draft.length ? "" : "disabled"}>Clear</button>
              </div>
            </aside>
          </div>
        </div>
      `;
      setView(guestShell(`Room ${catalog.room.roomNumber} Request`, body));
    } catch (err) {
      setView(guestShell("Invalid link", `<div class="guest-content"><div class="card"><h2>The request page could not be opened</h2><p class="card-subtitle">${html(err.message)}</p></div></div>`));
    }
  }

  function guestShell(title, body) {
    return `
      <div class="guest-page">
        <section class="guest-hero">
          <div>
            <div class="sidebar-brand" style="padding:0">
              <div class="brand-mark">H</div>
              <div>
                <div class="brand-title">Hotel Guest Request</div>
                <div class="brand-subtitle">Direct request without sign-in</div>
              </div>
            </div>
            <h1>${html(title)}</h1>
            <p>Choose what you need from the hotel services. The request will be routed automatically to the right team.</p>
          </div>
          <p class="footer-note">This guest QR page is designed for guests and does not expose internal hotel data.</p>
        </section>
        ${body}
      </div>
    `;
  }

  function linePicker(items, prefix) {
    const first = items[0];
    const defaultProps = first?.baseProperties || '{"fields":[]}';
    const defaultFields = parseAttributeSchema(defaultProps);
    return `
      <div class="form-grid">
        <div class="field">
          <label>Item / service</label>
          <select name="itemId" id="${prefix}-line-item" required>
            ${items.map(item => `<option value="${item.itemId}" data-name="${attr(item.name)}" data-props="${attr(item.baseProperties)}">${html(item.name)}${item.targetTeamName ? " - " + html(item.targetTeamName) : ""}</option>`).join("")}
          </select>
        </div>
        <div class="field">
          <label>Quantity</label>
          <input name="quantity" type="number" value="1" min="1" required />
        </div>
        <div class="field span-2">
          <label>Item details</label>
          <div class="dynamic-fields form-grid" data-dynamic-fields data-prefix="${attr(prefix)}">
            ${buildDynamicFieldsMarkup(defaultFields, prefix)}
          </div>
        </div>
      </div>
    `;
  }

  function renderCart(lines, mode) {
    if (!lines.length) return emptyBlock("The cart is empty.");
    return `
      <div class="cart-list">
        ${lines.map((line, index) => {
          const attrs = formatAttributes(line.dynamicAttributes || {});
          return `
            <div class="cart-item">
              <div>
                <strong>${html(line.itemName)}</strong>
                <div class="footer-note">Quantity: ${line.quantity}${attrs ? " - " + html(attrs) : ""}</div>
              </div>
              <button class="btn btn-danger" data-action="${mode === "guest" ? "remove-guest-line" : "remove-auth-line"}" data-index="${index}">Remove</button>
            </div>
          `;
        }).join("")}
      </div>
    `;
  }

  function renderOrdersList(orders, mode = "admin") {
    if (!orders?.length) return emptyBlock("No orders to display.");
    return `<div class="order-list">${orders.map(order => renderOrderCard(order, mode)).join("")}</div>`;
  }

  function renderOrderCard(order, mode) {
    const lines = (order.details || []).map(d => {
      const extra = formatAttributes(d.dynamicAttributes);
      return `<div class="order-line"><span><strong>${html(d.itemName)}</strong>${extra ? `<br><small>${html(extra)}</small>` : ""}</span><strong>x${d.quantity}</strong></div>`;
    }).join("");

    const canComplete = order.status === "Accepted" || order.status === "InProgress";
    const currentUserId = user()?.userId;
    const mayComplete = isAdmin() || String(order.acceptedByUserId || "") === String(currentUserId || "");
    const canCancel = isAdmin() && !["Completed", "Cancelled"].includes(order.status);

    const actions = `
      <div class="actions">
        ${order.status === "Pending" ? `<button class="btn btn-success" data-action="accept-order" data-id="${order.orderId}" data-row="${attr(order.rowVersion)}">Accept request</button>` : ""}
        ${canComplete && mayComplete ? `<button class="btn btn-primary" data-action="complete-order" data-id="${order.orderId}">Complete request</button>` : ""}
        ${canCancel ? `<button class="btn btn-warning" data-action="cancel-order" data-id="${order.orderId}">Cancel</button>` : ""}
      </div>
    `;

    return `
      <article class="order-card" data-order-id="${order.orderId}">
        <div class="order-head">
          <div>
            <div class="order-id">Order #${order.orderId} - Room ${html(order.roomNumber)}</div>
            <div class="order-meta">
              ${statusPill(order.status)}
              <span class="pill">${html(sourceLabel(order.source))}</span>
              <span class="pill info">${html(order.assignedTeamName || "All teams")}</span>
              ${order.escalatedAtUtc ? `<span class="pill danger">SLA</span>` : ""}
            </div>
          </div>
          <div class="footer-note">${formatDate(order.createdAtUtc)}</div>
        </div>
        <div class="order-lines">${lines}</div>
        <div class="order-meta">
          ${order.createdByUserName ? `<span>Created by: ${html(order.createdByUserName)}</span>` : `<span>Direct guest</span>`}
          ${order.acceptedByUserName ? `<span>Accepted by: ${html(order.acceptedByUserName)}</span>` : ""}
          ${order.slaDueAtUtc ? `<span>SLA: ${formatDate(order.slaDueAtUtc)}</span>` : ""}
        </div>
        ${mode !== "compact" ? actions : ""}
      </article>
    `;
  }

  function renderPresenceList(list) {
    if (!list?.length) return emptyBlock("No presence data yet.");
    return `
      <div class="table-wrap">
        <table>
          <thead><tr><th>Staff member</th><th>Team</th><th>Status</th><th>Last heartbeat</th><th>Screen</th></tr></thead>
          <tbody>
            ${list.map(x => `
              <tr>
                <td><strong>${html(x.fullName)}</strong></td>
                <td>${html(x.teamName || "-")}</td>
                <td><span class="status-dot ${x.isOnline ? "online" : "offline"}"></span> ${x.isOnline ? "Online" : "Offline"}</td>
                <td>${formatDate(x.lastHeartbeatAtUtc)}</td>
                <td>${html(x.lastKnownAppState || "-")}</td>
              </tr>
            `).join("")}
          </tbody>
        </table>
      </div>
    `;
  }

  function option(value, label, selected) {
    return `<option value="${attr(value)}" ${String(value) === String(selected ?? "") ? "selected" : ""}>${html(label)}</option>`;
  }

  async function loadRooms() {
    state.rooms = await api(isAdmin() ? "/api/v1/admin/rooms" : "/api/v1/rooms");
    return state.rooms;
  }

  async function loadItems() {
    state.items = await api(isAdmin() ? "/api/v1/admin/items" : "/api/v1/items");
    return state.items;
  }

  async function loadTeams() {
    if (!isAdmin()) return [];
    state.teams = await api("/api/v1/admin/teams");
    return state.teams;
  }

  async function loadUsers() {
    state.users = await api("/api/v1/admin/users");
    return state.users;
  }

  async function loadAdminOrders(filter = {}) {
    const params = new URLSearchParams();
    if (filter.status) params.set("status", filter.status);
    if (filter.teamId) params.set("teamId", filter.teamId);
    if (filter.roomId) params.set("roomId", filter.roomId);
    if (filter.take) params.set("take", filter.take);
    return await api(`/api/v1/admin/orders?${params.toString()}`);
  }

  async function login(userName, password) {
    const data = await api("/api/v1/auth/login", { method: "POST", auth: false, body: { userName, password } });
    saveSession(data);
    toast("Signed in successfully");
    if (!isAdmin()) {
      Notification.requestPermission().catch(() => {});
    }
    go(data.user?.role === "Admin" ? "admin/dashboard" : "staff/tasks");
    render();
  }

  async function logout(silent = false) {
    const id = deviceId();
    if (state.session?.token) {
      await api("/api/v1/auth/logout", { method: "POST", body: { deviceId: id } }).catch(() => {});
    }
    saveSession(null);
    stopRealtime();
    stopHeartbeat();
    if (!silent) toast("Signed out", "warning");
    go("login");
    renderLogin();
  }

  function startHeartbeat() {
    if (state.heartbeatTimer) return;
    sendHeartbeat();
    state.heartbeatTimer = setInterval(sendHeartbeat, 60000);
    if (!state.pollingTimer) {
      state.pollingTimer = setInterval(() => {
        if (document.hidden || !isLoggedIn()) return;
        const now = Date.now();
        if (now - state.lastRefreshAt > 30000) refreshCurrentPage();
      }, 30000);
    }
  }

  function stopHeartbeat() {
    if (state.heartbeatTimer) clearInterval(state.heartbeatTimer);
    if (state.pollingTimer) clearInterval(state.pollingTimer);
    state.heartbeatTimer = null;
    state.pollingTimer = null;
  }

  async function sendHeartbeat() {
    if (!isLoggedIn()) return;
    try {
      const data = await api("/api/v1/presence/heartbeat", {
        method: "PUT",
        body: {
          deviceId: deviceId(),
          appState: document.hidden ? "background" : "foreground",
          currentScreen: normalizeHash() || "home"
        }
      });
      if (data?.isReady !== undefined && state.isReady === null) state.isReady = data.isReady;
      updateConnectionPill(true);
    } catch {
      updateConnectionPill(false);
    }
  }

  async function startRealtime() {
    if (!isLoggedIn() || !window.signalR) {
      updateConnectionPill(false, "Polling");
      return;
    }

    const mode = isAdmin() || isSupervisor() ? "admin" : "staff";
    if (state.hub && state.hubMode === mode) return;
    stopRealtime();

    const hubPath = mode === "admin" ? "/hubs/admin" : "/hubs/staff";
    const hub = new signalR.HubConnectionBuilder()
      .withUrl(hubPath, { accessTokenFactory: () => state.session?.token || "" })
      .withAutomaticReconnect()
      .build();

    const refresh = debounce(() => refreshCurrentPage(), 700);

    hub.on("OrderCreated", (order) => {
      const host = document.getElementById("toast");
      if (host && !isAdmin()) {
        const node = document.createElement("div");
        node.className = "toast notification";
        node.style.cursor = "pointer";
        node.innerHTML = `<strong>🔔 New order!</strong><br>Room ${order?.roomNumber || "?"} — click to view`;
        node.onclick = () => { go("staff/tasks"); node.remove(); };
        host.appendChild(node);
        setTimeout(() => node.remove(), 8000);
      }
      refresh();
    });

    ["OrderAccepted", "OrderCompleted", "DashboardChanged", "StaffPresenceChanged"].forEach(eventName => {
      hub.on(eventName, refresh);
    });

    hub.onreconnected(() => {
      updateConnectionPill(true, "Live");
      refreshCurrentPage();
    });

    hub.onclose(() => updateConnectionPill(false, "Polling"));

    state.hub = hub;
    state.hubMode = mode;

    try {
      await hub.start();
      updateConnectionPill(true, "Live");
    } catch {
      updateConnectionPill(false, "Polling");
    }
  }

  function stopRealtime() {
    if (state.hub) state.hub.stop().catch(() => {});
    state.hub = null;
    state.hubMode = null;
  }

  function updateConnectionPill(isOnline, label) {
    const pill = document.getElementById("connection-pill");
    if (!pill) return;
    const online = isOnline ?? (state.hub?.state === "Connected");
    const text = label || (online ? "Live / Online" : "Polling / Offline");
    pill.className = `pill ${online ? "success" : "warning"}`;
    pill.textContent = text;
  }

  function debounce(fn, delay) {
    let timer;
    return (...args) => {
      clearTimeout(timer);
      timer = setTimeout(() => fn(...args), delay);
    };
  }

  function refreshCurrentPage() {
    state.lastRefreshAt = Date.now();
    if (!state.isRendering) render();
  }

  async function withButton(button, fn) {
    if (!button) return;
    const old = button.innerHTML;
    button.disabled = true;
    button.innerHTML = "Working...";
    try { await fn(); }
    finally {
      button.disabled = false;
      button.innerHTML = old;
    }
  }

  document.addEventListener("click", async event => {
    const button = event.target.closest("[data-action]");
    if (!button) return;

    const action = button.dataset.action;

    try {
      if (action === "toggle-menu") {
        document.querySelector(".sidebar")?.classList.toggle("open");
        return;
      }

      if (action === "refresh") {
        refreshCurrentPage();
        return;
      }

      if (action === "logout") {
        await logout();
        return;
      }

      if (action === "toggle-ready") {
        const next = state.isReady === null ? true : !state.isReady;
        await withButton(button, async () => {
          state.isReady = next;
          await api("/api/v1/presence/availability", { method: "PUT", body: { isReady: next, deviceId: deviceId(), source: "Web" } });
          toast(next ? "Status set to Ready" : "Status set to Not Ready", next ? "success" : "warning");
          refreshCurrentPage();
        });
        return;
      }

      if (action === "demo-login") {
        await withButton(button, () => login(button.dataset.user, button.dataset.pass));
        return;
      }

      if (action === "accept-order") {
        await withButton(button, async () => {
          await api(`/api/v1/orders/${button.dataset.id}/accept`, { method: "PUT", body: { rowVersion: button.dataset.row || null } });
          toast("Request accepted");
          refreshCurrentPage();
        });
        return;
      }

      if (action === "complete-order") {
        const notes = prompt("Completion notes - optional", "") || "";
        await withButton(button, async () => {
          await api(`/api/v1/orders/${button.dataset.id}/complete`, { method: "PUT", body: { notes } });
          toast("Request completed");
          refreshCurrentPage();
        });
        return;
      }

      if (action === "cancel-order") {
        const reason = prompt("Cancellation reason", "") || "";
        if (!confirm("Do you want to cancel this request?")) return;
        await withButton(button, async () => {
          await api(`/api/v1/orders/${button.dataset.id}/cancel`, { method: "PUT", body: { reason } });
          toast("Request cancelled", "warning");
          refreshCurrentPage();
        });
        return;
      }

      if (action === "copy-link") {
        await navigator.clipboard.writeText(button.dataset.link);
        toast("Guest link copied");
        return;
      }

      if (action === "cancel-edit") {
        state.editing = {};
        refreshCurrentPage();
        return;
      }

      if (action === "add-attribute-field") {
        const builder = button.closest("[data-attribute-builder]");
        const rows = builder?.querySelector("[data-attribute-rows]");
        if (rows) {
          rows.insertAdjacentHTML("beforeend", attributeFieldRow({ label: "", key: "", type: "text" }, rows.querySelectorAll("[data-attribute-row]").length));
        }
        return;
      }

      if (action === "remove-attribute-field") {
        const rows = button.closest("[data-attribute-rows]");
        const row = button.closest("[data-attribute-row]");
        if (rows && row && rows.querySelectorAll("[data-attribute-row]").length > 1) {
          row.remove();
        } else {
          row?.querySelectorAll("input").forEach(input => {
            if (input.type === "checkbox") input.checked = false;
            else input.value = "";
          });
          const typeSelect = row?.querySelector("[data-schema-type]");
          if (typeSelect) typeSelect.value = "text";
        }
        return;
      }


      if (action.startsWith("edit-")) {
        const type = action.replace("edit-", "");
        state.editing = { [`${type}Id`]: button.dataset.id };
        refreshCurrentPage();
        return;
      }

      if (action.startsWith("delete-")) {
        const type = action.replace("delete-", "");
        if (!confirm("Are you sure you want to delete this record? Master data uses soft delete.")) return;
        const endpoint = {
          room: "rooms",
          item: "items",
          team: "teams",
          user: "users"
        }[type];
        await withButton(button, async () => {
          await api(`/api/v1/admin/${endpoint}/${button.dataset.id}`, { method: "DELETE" });
          toast("Deleted");
          refreshCurrentPage();
        });
        return;
      }

      if (action === "remove-auth-line") {
        state.orderDraft.splice(Number(button.dataset.index), 1);
        refreshCurrentPage();
        return;
      }

      if (action === "clear-auth-draft") {
        state.orderDraft = [];
        refreshCurrentPage();
        return;
      }

      if (action === "submit-auth-order") {
        await submitAuthOrder(button);
        return;
      }

      if (action === "remove-guest-line") {
        const payload = segments()[1] || "";
        state.guestDrafts[payload]?.splice(Number(button.dataset.index), 1);
        renderGuest(payload);
        return;
      }

      if (action === "clear-guest-draft") {
        state.guestDrafts[button.dataset.payload] = [];
        renderGuest(button.dataset.payload);
        return;
      }

      if (action === "submit-guest-order") {
        await submitGuestOrder(button);
        return;
      }
    } catch (err) {
      toast(err.message || String(err), "error");
    }
  });

  document.addEventListener("submit", async event => {
    const form = event.target.closest("form[data-form]");
    if (!form) return;
    event.preventDefault();

    try {
      const type = form.dataset.form;

      if (type === "login") {
        await withButton(form.querySelector("button[type=submit]"), () => login(form.userName.value.trim(), form.password.value));
        return;
      }

      if (type === "order-filter") {
        const data = Object.fromEntries(new FormData(form));
        state.orderFilters = data;
        renderAdminOrders();
        return;
      }

      if (type === "performance-filter") {
        const fromLocal = form.fromLocal.value;
        const toLocal = form.toLocal.value;
        state.performanceFilter = {
          fromLocal,
          toLocal,
          fromUtc: fromLocal ? new Date(fromLocal).toISOString() : "",
          toUtc: toLocal ? new Date(toLocal).toISOString() : ""
        };
        renderPerformance();
        return;
      }

      if (type === "auth-line") {
        addLineToDraft(form, state.orderDraft);
        toast("Item added to cart");
        refreshCurrentPage();
        return;
      }

      if (type === "guest-line") {
        const payload = form.dataset.payload;
        state.guestDrafts[payload] = state.guestDrafts[payload] || [];
        addLineToDraft(form, state.guestDrafts[payload]);
        toast("Item added to request");
        renderGuest(payload);
        return;
      }

      if (["room", "item", "team", "user"].includes(type)) {
        await saveAdminEntity(type, form);
        return;
      }
    } catch (err) {
      toast(err.message || String(err), "error");
    }
  });

  document.addEventListener("change", event => {
    const itemSelect = event.target.closest("select[name=itemId]");
    if (itemSelect) {
      const form = itemSelect.closest("form");
      const container = form?.querySelector("[data-dynamic-fields]");
      const opt = itemSelect.selectedOptions[0];
      if (container) {
        const prefix = container.dataset.prefix || itemSelect.id || "line";
        container.innerHTML = buildDynamicFieldsMarkup(parseAttributeSchema(opt?.dataset.props || '{"fields":[]}'), prefix);
      }
      return;
    }

    const schemaType = event.target.closest("[data-schema-type]");
    if (schemaType) {
      const row = schemaType.closest("[data-attribute-row]");
      const optionsInput = row?.querySelector("[data-schema-options]");
      if (optionsInput && (schemaType.value === "select" || schemaType.value === "multiSelect") && !optionsInput.value.trim()) {
        optionsInput.placeholder = "Small, Medium, Large";
      }
    }
  });

  async function saveAdminEntity(type, form) {
    const submit = form.querySelector("button[type=submit]");
    await withButton(submit, async () => {
      const data = Object.fromEntries(new FormData(form));
      const id = data.id;
      let endpoint = `/api/v1/admin/${type}s`;
      let body = {};
      const method = id ? "PUT" : "POST";

      if (type === "room") {
        endpoint = `/api/v1/admin/rooms${id ? "/" + id : ""}`;
        body = {
          roomNumber: data.roomNumber.trim(),
          directLinkPayload: data.directLinkPayload?.trim() || null,
          isActive: data.isActive === "true"
        };
      }

      if (type === "team") {
        endpoint = `/api/v1/admin/teams${id ? "/" + id : ""}`;
        body = { name: data.name.trim(), isActive: data.isActive === "true" };
      }

      if (type === "item") {
        const baseProperties = collectAttributeSchema(form);
        endpoint = `/api/v1/admin/items${id ? "/" + id : ""}`;
        body = {
          name: data.name.trim(),
          type: data.type,
          targetTeamId: data.targetTeamId ? Number(data.targetTeamId) : null,
          baseProperties,
          isActive: data.isActive === "true"
        };
      }

      if (type === "user") {
        endpoint = `/api/v1/admin/users${id ? "/" + id : ""}`;
        if (id) {
          body = {
            fullName: data.fullName.trim(),
            teamId: data.teamId ? Number(data.teamId) : null,
            role: data.role,
            isActive: data.isActive === "true",
            newPassword: data.password || null
          };
        } else {
          body = {
            fullName: data.fullName.trim(),
            userName: data.userName.trim(),
            password: data.password,
            teamId: data.teamId ? Number(data.teamId) : null,
            role: data.role,
            isActive: data.isActive === "true"
          };
        }
      }

      await api(endpoint, { method, body });
      state.editing = {};
      toast(id ? "Changes saved" : "Created");
      refreshCurrentPage();
    });
  }

  function addLineToDraft(form, draft) {
    const data = Object.fromEntries(new FormData(form));
    const select = form.querySelector('select[name="itemId"]');
    const selectedOption = select.selectedOptions[0];
    const itemId = Number(data.itemId);
    const quantity = Number(data.quantity);
    const dynamicAttributes = gatherDynamicAttributes(form);

    if (!itemId || quantity <= 0) throw new Error("Choose an item and a valid quantity.");

    draft.push({
      itemId,
      itemName: selectedOption?.dataset.name || selectedOption?.textContent?.trim() || `Item ${itemId}`,
      quantity,
      dynamicAttributes
    });
  }

  async function submitAuthOrder(button) {
    const roomSelect = document.getElementById("order-room");
    const roomId = Number(roomSelect?.value);
    if (!roomId) {
      toast("Choose the room first", "error");
      return;
    }

    if (!state.orderDraft.length) {
      toast("Add at least one item", "error");
      return;
    }

    await withButton(button, async () => {
      await api("/api/v1/orders", {
        method: "POST",
        body: {
          roomId,
          source: button.dataset.source || (isAdmin() ? "Admin" : "StaffProxy"),
          items: state.orderDraft.map(x => ({
            itemId: x.itemId,
            quantity: x.quantity,
            dynamicAttributes: x.dynamicAttributes || {}
          }))
        }
      });
      state.orderDraft = [];
      toast("Order created and routed to the right team");
      go(isAdmin() ? "admin/orders" : "staff/tasks");
      render();
    });
  }

  async function submitGuestOrder(button) {
    const payload = button.dataset.payload;
    const draft = state.guestDrafts[payload] || [];
    if (!draft.length) return;

    await withButton(button, async () => {
      const response = await api(`/api/v1/guest/rooms/${encodeURIComponent(payload)}/orders`, {
        auth: false,
        method: "POST",
        body: {
          items: draft.map(x => ({
            itemId: x.itemId,
            quantity: x.quantity,
            dynamicAttributes: x.dynamicAttributes || {}
          }))
        }
      });
      state.guestDrafts[payload] = [];
      const orderIds = (response.orders || []).map(x => "#" + x.orderId).join(", ");
      toast(`Request sent successfully ${orderIds}`, "success");
      renderGuest(payload);
    });
  }

  window.addEventListener("hashchange", render);
  document.addEventListener("visibilitychange", () => {
    if (!document.hidden) sendHeartbeat();
  });

  if ("serviceWorker" in navigator) {
    navigator.serviceWorker.register("/sw.js").catch(() => {});
  }

  render();
})();
