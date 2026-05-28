(() => {
  "use strict";

  const SESSION_KEY = "hotel.ops.session";
  const catalogCache = new Map();

  function session() {
    try { return JSON.parse(localStorage.getItem(SESSION_KEY) || "null"); }
    catch { return null; }
  }

  function token() {
    return session()?.token || "";
  }

  function isAdmin() {
    return session()?.user?.role === "Admin";
  }

  function apiBase() {
    return localStorage.getItem("hotel.ops.apiBase") || "";
  }

  async function api(path, options = {}) {
    const headers = { Accept: "application/json" };
    if (options.body !== undefined) headers["Content-Type"] = "application/json";
    if (token()) headers.Authorization = `Bearer ${token()}`;
    const response = await fetch(apiBase() + path, {
      method: options.method || "GET",
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body)
    });
    const payload = await response.json().catch(() => null);
    if (!response.ok || payload?.isSuccess === false) {
      throw new Error(payload?.errorMessage || `HTTP ${response.status}`);
    }
    return payload?.data ?? payload;
  }

  function h(value) {
    return String(value ?? "").replace(/[&<>'"]/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;" }[c]));
  }

  function attr(value) {
    return h(value).replaceAll("\n", " ");
  }

  function toast(message, type = "success") {
    const host = document.getElementById("toast");
    if (!host) return alert(message);
    const el = document.createElement("div");
    el.className = `toast ${type}`;
    el.textContent = message;
    host.appendChild(el);
    setTimeout(() => el.remove(), 3500);
  }

  function currentRoute() {
    return location.hash.replace(/^#\/?/, "");
  }

  function removeDemoLogins() {
    document.querySelectorAll(".demo-logins").forEach(x => x.remove());
  }

  function ensureCategoryNav() {
    if (!token() || !isAdmin()) return;
    const sidebar = document.querySelector(".sidebar-nav, nav");
    if (!sidebar || sidebar.querySelector('[href="#/admin/item-categories"]')) return;
    const itemsLink = sidebar.querySelector('[href="#/admin/items"]');
    if (!itemsLink) return;
    const a = document.createElement("a");
    a.href = "#/admin/item-categories";
    a.innerHTML = "<span class='nav-icon'>🗂️</span><span>Categories</span>";
    itemsLink.before(a);
  }

  async function openCategoriesPage() {
    if (!token() || !isAdmin()) return;
    if (currentRoute() !== "admin/item-categories") {
      history.pushState(null, "", "#/admin/item-categories");
    }
    await renderCategoriesPage();
  }

  document.addEventListener("click", event => {
    const link = event.target.closest('a[href="#/admin/item-categories"]');
    if (!link) return;
    event.preventDefault();
    event.stopImmediatePropagation();
    openCategoriesPage().catch(err => toast(err.message, "error"));
  }, true);

  async function renderCategoriesPage() {
    const app = document.getElementById("app");
    if (!app || !token() || !isAdmin()) return;
    app.innerHTML = `<main class="main"><header class="topbar"><div><h1>Item Categories</h1><p>Every item must be linked to an active category before it can be created.</p></div><div class="topbar-meta"><a class="btn btn-soft" href="#/admin/dashboard">Dashboard</a><a class="btn btn-soft" href="#/admin/items">Items & Services</a></div></header><section class="card">Loading categories...</section></main>`;
    const categories = await api("/api/v1/admin/item-categories");
    app.innerHTML = `
      <main class="main">
        <header class="topbar">
          <div><h1>Item Categories</h1><p>Create categories first, then link items and services to them.</p></div>
          <div class="topbar-meta"><a class="btn btn-soft" href="#/admin/dashboard">Dashboard</a><a class="btn btn-soft" href="#/admin/items">Items & Services</a></div>
        </header>
        <div class="grid grid-2">
          <form class="card grid" data-category-form>
            <div class="card-header"><div><h2 class="card-title">Add category</h2><p class="card-subtitle">Categories are required for item and service creation.</p></div></div>
            <input type="hidden" name="id" />
            <div class="field"><label>Category name</label><input name="name" required /></div>
            <div class="field"><label>Description</label><textarea name="description" rows="3"></textarea></div>
            <div class="field"><label>Status</label><select name="isActive"><option value="true">Active</option><option value="false">Inactive</option></select></div>
            <button class="btn btn-primary" type="submit">Save category</button>
          </form>
          <section class="card">
            <div class="card-header"><h2 class="card-title">Category list</h2><span class="pill primary">${categories.length}</span></div>
            <div class="table-wrap"><table><thead><tr><th>Category</th><th>Status</th><th>Actions</th></tr></thead><tbody>
              ${categories.map(c => `<tr><td><strong>${h(c.name)}</strong><br><small>${h(c.description || "No description")}</small></td><td>${c.isActive ? '<span class="pill success">Active</span>' : '<span class="pill danger">Inactive</span>'}</td><td><div class="actions"><button class="btn btn-soft" data-edit-category='${h(JSON.stringify(c))}'>Edit</button><button class="btn btn-danger" data-delete-category="${c.itemCategoryId}">Delete</button></div></td></tr>`).join("") || '<tr><td colspan="3">No categories yet.</td></tr>'}
            </tbody></table></div>
          </section>
        </div>
      </main>`;
  }

  function cleanupDuplicateItemCategoryFields(form) {
    const selects = Array.from(form.querySelectorAll('select[name="itemCategoryId"]'));
    if (selects.length <= 1) return;
    selects.slice(1).forEach(select => select.closest(".field")?.remove());
  }

  function isBlankAttributeRow(row) {
    const label = row.querySelector("[data-schema-label]")?.value?.trim();
    const key = row.querySelector("[data-schema-key]")?.value?.trim();
    const def = row.querySelector("[data-schema-default]")?.value?.trim();
    const options = row.querySelector("[data-schema-options]")?.value?.trim();
    const required = row.querySelector("[data-schema-required]")?.checked;
    return !label && !key && !def && !options && !required;
  }

  function emptyDynamicFieldsMarkup() {
    return '<div class="empty compact-empty" data-empty-dynamic-fields>No dynamic fields. Click Add field only when this item needs extra information.</div>';
  }

  function cleanupDynamicItemFields(form, force = false) {
    if (!form || (!force && form.dataset.dynamicFieldsCleaned === "true")) return;
    const rowsContainer = form.querySelector("[data-attribute-rows]");
    if (!rowsContainer) return;
    const rows = Array.from(rowsContainer.querySelectorAll("[data-attribute-row]"));
    if (rows.length === 0) {
      rowsContainer.innerHTML = emptyDynamicFieldsMarkup();
    } else if (rows.every(isBlankAttributeRow)) {
      rows.forEach(row => row.remove());
      rowsContainer.innerHTML = emptyDynamicFieldsMarkup();
    }
    form.dataset.dynamicFieldsCleaned = "true";
  }

  async function enhanceItemsPage() {
    const form = document.querySelector('form[data-form="item"]');
    if (!form) return;

    cleanupDuplicateItemCategoryFields(form);
    cleanupDynamicItemFields(form);

    if (form.querySelector('[name="itemCategoryId"]')) return;
    const categories = await api("/api/v1/admin/item-categories");
    const active = categories.filter(c => c.isActive);
    const currentItemId = form.querySelector('[name="id"]')?.value;
    let currentCategoryId = "";
    if (currentItemId) {
      const items = await api("/api/v1/admin/catalog-items");
      currentCategoryId = String(items.find(i => String(i.itemId) === String(currentItemId))?.itemCategoryId || "");
    }
    const wrap = document.createElement("div");
    wrap.className = "field";
    wrap.innerHTML = `<label>Category</label><select name="itemCategoryId" required>${active.map(c => `<option value="${c.itemCategoryId}" ${String(c.itemCategoryId) === currentCategoryId ? "selected" : ""}>${h(c.name)}</option>`).join("")}</select>${active.length ? "" : '<small class="footer-note">Create an active category first.</small>'}`;
    const typeField = form.querySelector('[name="type"]')?.closest('.field');
    if (typeField) typeField.before(wrap); else form.querySelector('.form-grid')?.prepend(wrap);
  }

  function normalizeItems(items) {
    return (items || []).filter(x => x && x.isActive !== false).map(x => ({
      itemId: x.itemId,
      name: x.name || `Item ${x.itemId}`,
      targetTeamName: x.targetTeamName || "",
      baseProperties: x.baseProperties || '{"fields":[]}',
      itemCategoryId: String(x.itemCategoryId || ""),
      itemCategoryName: x.itemCategoryName || "Uncategorized"
    }));
  }

  function categoriesFromItems(items) {
    const map = new Map();
    items.forEach(item => {
      if (!item.itemCategoryId) return;
      if (!map.has(item.itemCategoryId)) map.set(item.itemCategoryId, item.itemCategoryName || "Uncategorized");
    });
    return Array.from(map.entries()).map(([id, name]) => ({ id, name })).sort((a, b) => a.name.localeCompare(b.name));
  }

  async function loadOrderCatalog() {
    const route = currentRoute();
    const key = route.startsWith("guest/") ? route : (isAdmin() ? "admin" : "staff");
    if (catalogCache.has(key)) return catalogCache.get(key);

    let items = [];
    if (route.startsWith("guest/")) {
      const payload = route.split("/")[1] || "";
      const catalog = await api(`/api/v1/guest/rooms/${encodeURIComponent(payload)}/catalog`);
      items = normalizeItems(catalog.items || []);
    } else if (isAdmin()) {
      items = normalizeItems(await api("/api/v1/admin/items"));
    } else {
      items = normalizeItems(await api("/api/v1/items"));
    }

    const result = { items, categories: categoriesFromItems(items) };
    catalogCache.set(key, result);
    return result;
  }

  async function enhanceOrderCategoryPickers() {
    const forms = Array.from(document.querySelectorAll('form[data-form="auth-line"], form[data-form="guest-line"]'));
    if (!forms.length) return;
    const catalog = await loadOrderCatalog();

    forms.forEach(form => {
      if (form.dataset.categoryPickerReady === "true") return;
      const itemSelect = form.querySelector('select[name="itemId"]');
      const itemField = itemSelect?.closest(".field");
      if (!itemSelect || !itemField) return;

      const categoryField = document.createElement("div");
      categoryField.className = "field";
      categoryField.innerHTML = `
        <label>Category</label>
        <select name="categoryId" data-order-category required>
          <option value="">Choose category</option>
          ${catalog.categories.map(c => `<option value="${attr(c.id)}">${h(c.name)}</option>`).join("")}
        </select>`;
      itemField.before(categoryField);
      itemSelect.innerHTML = '<option value="">Choose category first</option>';
      itemSelect.disabled = true;
      form.dataset.categoryPickerReady = "true";
    });
  }

  function updateItemOptions(categorySelect) {
    const form = categorySelect.closest("form");
    const itemSelect = form?.querySelector('select[name="itemId"]');
    if (!form || !itemSelect) return;

    loadOrderCatalog().then(catalog => {
      const selectedCategory = String(categorySelect.value || "");
      const filtered = catalog.items.filter(x => x.itemCategoryId === selectedCategory);
      itemSelect.disabled = filtered.length === 0;
      itemSelect.innerHTML = filtered.length
        ? filtered.map(item => `<option value="${attr(item.itemId)}" data-name="${attr(item.name)}" data-props="${attr(item.baseProperties)}">${h(item.name)}${item.targetTeamName ? " - " + h(item.targetTeamName) : ""}</option>`).join("")
        : '<option value="">No active items in this category</option>';
      itemSelect.dispatchEvent(new Event("change", { bubbles: true }));
    }).catch(err => toast(err.message, "error"));
  }

  function collectSchema(form) {
    const fields = [];
    form.querySelectorAll('[data-attribute-row]').forEach((row, index) => {
      const label = row.querySelector('[data-schema-label]')?.value?.trim();
      if (!label) return;
      let key = row.querySelector('[data-schema-key]')?.value?.trim() || label.toLowerCase().replace(/[^a-z0-9]+/g, "_").replace(/^_+|_+$/g, "");
      if (!key) key = `field_${index + 1}`;
      const type = row.querySelector('[data-schema-type]')?.value || "text";
      const options = (row.querySelector('[data-schema-options]')?.value || "").split(",").map(x => x.trim()).filter(Boolean);
      fields.push({ key, label, type, required: Boolean(row.querySelector('[data-schema-required]')?.checked), defaultValue: row.querySelector('[data-schema-default]')?.value || null, options });
    });
    return fields.length ? JSON.stringify({ fields }) : null;
  }

  document.addEventListener("click", event => {
    const addField = event.target.closest('[data-action="add-attribute-field"]');
    if (addField) {
      addField.closest("[data-attribute-builder]")?.querySelector("[data-empty-dynamic-fields]")?.remove();
      return;
    }

    const removeField = event.target.closest('[data-action="remove-attribute-field"]');
    if (removeField) {
      const form = removeField.closest('form[data-form="item"]');
      setTimeout(() => cleanupDynamicItemFields(form, true), 0);
    }
  }, true);

  document.addEventListener("submit", async event => {
    const categoryForm = event.target.closest('[data-category-form]');
    if (categoryForm) {
      event.preventDefault();
      event.stopImmediatePropagation();
      const data = Object.fromEntries(new FormData(categoryForm));
      await api(`/api/v1/admin/item-categories${data.id ? "/" + data.id : ""}`, {
        method: data.id ? "PUT" : "POST",
        body: { name: data.name.trim(), description: data.description?.trim() || null, isActive: data.isActive === "true" }
      });
      catalogCache.clear();
      toast("Category saved");
      renderCategoriesPage().catch(e => toast(e.message, "error"));
      return;
    }

    const itemForm = event.target.closest('form[data-form="item"]');
    if (itemForm) {
      cleanupDuplicateItemCategoryFields(itemForm);
      const categoryId = Number(itemForm.querySelector('[name="itemCategoryId"]')?.value || 0);
      if (!categoryId) {
        event.preventDefault();
        event.stopImmediatePropagation();
        toast("Choose a category before saving the item.", "error");
        return;
      }
      event.preventDefault();
      event.stopImmediatePropagation();
      const data = Object.fromEntries(new FormData(itemForm));
      await api(`/api/v1/admin/catalog-items${data.id ? "/" + data.id : ""}`, {
        method: data.id ? "PUT" : "POST",
        body: { name: data.name.trim(), type: data.type, itemCategoryId: categoryId, targetTeamId: data.targetTeamId ? Number(data.targetTeamId) : null, baseProperties: collectSchema(itemForm), isActive: data.isActive === "true" }
      });
      catalogCache.clear();
      toast("Item saved");
      location.hash = "#/admin/items";
      setTimeout(() => location.reload(), 300);
    }
  }, true);

  document.addEventListener("click", async event => {
    const edit = event.target.closest('[data-edit-category]');
    if (edit) {
      const c = JSON.parse(edit.dataset.editCategory);
      const form = document.querySelector('[data-category-form]');
      if (!form) return;
      form.id.value = c.itemCategoryId;
      form.name.value = c.name || "";
      form.description.value = c.description || "";
      form.isActive.value = String(c.isActive);
      form.querySelector('.card-title').textContent = "Edit category";
      return;
    }

    const del = event.target.closest('[data-delete-category]');
    if (del) {
      if (!confirm("Delete this category? Categories linked to items cannot be deleted.")) return;
      await api(`/api/v1/admin/item-categories/${del.dataset.deleteCategory}`, { method: "DELETE" });
      catalogCache.clear();
      toast("Category deleted");
      renderCategoriesPage().catch(e => toast(e.message, "error"));
    }
  });

  document.addEventListener("change", event => {
    const categorySelect = event.target.closest('[data-order-category]');
    if (categorySelect) updateItemOptions(categorySelect);
  }, true);

  async function route() {
    removeDemoLogins();
    ensureCategoryNav();
    if (currentRoute() === "admin/item-categories") {
      try { await renderCategoriesPage(); }
      catch (err) { toast(err.message, "error"); }
      return;
    }
    if (currentRoute().includes("admin/items")) {
      setTimeout(() => enhanceItemsPage().catch(e => toast(e.message, "error")), 500);
    }
    if (["admin/create-order", "staff/create-order"].includes(currentRoute()) || currentRoute().startsWith("guest/")) {
      setTimeout(() => enhanceOrderCategoryPickers().catch(e => toast(e.message, "error")), 500);
    }
  }

  window.addEventListener("hashchange", () => setTimeout(route, 50));
  window.addEventListener("popstate", () => setTimeout(route, 50));
  new MutationObserver(() => {
    removeDemoLogins();
    ensureCategoryNav();
    if (currentRoute().includes("admin/items")) enhanceItemsPage().catch(() => {});
    if (["admin/create-order", "staff/create-order"].includes(currentRoute()) || currentRoute().startsWith("guest/")) enhanceOrderCategoryPickers().catch(() => {});
  }).observe(document.body, { childList: true, subtree: true });
  setTimeout(route, 800);
})();
