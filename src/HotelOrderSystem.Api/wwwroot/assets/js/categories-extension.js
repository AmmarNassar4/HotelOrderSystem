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
    if (options.auth !== false && token()) headers.Authorization = `Bearer ${token()}`;

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

    const link = document.createElement("a");
    link.href = "#/admin/item-categories";
    link.innerHTML = "<span class='nav-icon'>🗂️</span><span>Categories</span>";
    itemsLink.before(link);
  }

  async function openCategoriesPage() {
    if (!token() || !isAdmin()) return;
    if (currentRoute() !== "admin/item-categories") {
      history.pushState(null, "", "#/admin/item-categories");
    }
    await renderCategoriesPage();
  }

  async function renderCategoriesPage() {
    const app = document.getElementById("app");
    if (!app || !token() || !isAdmin()) return;

    app.innerHTML = `<main class="main"><header class="topbar"><div><h1>Item Categories</h1><p>Create categories first, then link items and services to them.</p></div><div class="topbar-meta"><a class="btn btn-soft" href="#/admin/dashboard">Dashboard</a><a class="btn btn-soft" href="#/admin/items">Items & Services</a></div></header><section class="card">Loading categories...</section></main>`;
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

  function normalizeItems(items) {
    return (items || [])
      .filter(x => x && x.isActive !== false)
      .map(x => ({
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

    let items;
    if (route.startsWith("guest/")) {
      const payload = route.split("/")[1] || "";
      const catalog = await api(`/api/v1/guest/rooms/${encodeURIComponent(payload)}/catalog`, { auth: false });
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

  document.addEventListener("click", event => {
    const link = event.target.closest('a[href="#/admin/item-categories"]');
    if (!link) return;
    event.preventDefault();
    event.stopImmediatePropagation();
    openCategoriesPage().catch(err => toast(err.message, "error"));
  }, true);

  document.addEventListener("submit", async event => {
    const categoryForm = event.target.closest('[data-category-form]');
    if (!categoryForm) return;

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

  function enhanceItemCategoryField() {
    if (currentRoute() !== "admin/items") return;
    const form = document.querySelector('[data-form="item"]');
    if (!form || form.dataset.categoryInlineReady === "true") return;

    const catSelect = form.querySelector('select[name="itemCategoryId"]');
    const catField = catSelect?.closest(".field");
    if (!catSelect || !catField) return;

    form.dataset.categoryInlineReady = "true";

    // Row: [select] [+ New button]
    const row = document.createElement("div");
    row.style.cssText = "display:flex;gap:8px;align-items:stretch";
    catField.insertBefore(row, catSelect);
    catSelect.style.flex = "1";
    row.appendChild(catSelect);

    const addBtn = document.createElement("button");
    addBtn.type = "button";
    addBtn.className = "btn btn-soft";
    addBtn.style.cssText = "flex-shrink:0;white-space:nowrap;font-size:13px";
    addBtn.textContent = "+ New";
    row.appendChild(addBtn);

    // Inline mini-form (hidden by default)
    const mini = document.createElement("div");
    mini.style.cssText = "display:none;border:1px solid var(--line,#e4e7ec);border-radius:14px;padding:14px;background:var(--surface-2,#f9fafb);margin-top:6px";
    mini.innerHTML = `
      <div style="font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:.05em;color:var(--muted,#667085);margin-bottom:10px">New category</div>
      <div class="field" style="margin-bottom:10px">
        <label>Name</label>
        <input data-new-cat-name required placeholder="Category name" />
      </div>
      <div style="display:flex;gap:8px">
        <button type="button" class="btn btn-primary" data-save-cat style="font-size:13px">Create</button>
        <button type="button" class="btn btn-soft" data-cancel-cat style="font-size:13px">Cancel</button>
      </div>
    `;
    catField.appendChild(mini);

    addBtn.addEventListener("click", () => {
      const open = mini.style.display !== "none";
      mini.style.display = open ? "none" : "block";
      if (!open) mini.querySelector("[data-new-cat-name]").focus();
    });

    mini.querySelector("[data-cancel-cat]").addEventListener("click", () => {
      mini.style.display = "none";
      mini.querySelector("[data-new-cat-name]").value = "";
    });

    mini.querySelector("[data-save-cat]").addEventListener("click", async () => {
      const nameInput = mini.querySelector("[data-new-cat-name]");
      const name = nameInput.value.trim();
      if (!name) { nameInput.focus(); return; }

      const saveBtn = mini.querySelector("[data-save-cat]");
      saveBtn.disabled = true;
      try {
        const created = await api("/api/v1/admin/item-categories", {
          method: "POST",
          body: { name, description: null, isActive: true }
        });
        catalogCache.clear();

        // Remove the "Create a category first" placeholder if it's the only option
        const placeholder = catSelect.querySelector('option[value=""]');
        if (placeholder && catSelect.options.length === 1) placeholder.remove();

        const opt = document.createElement("option");
        opt.value = created.itemCategoryId;
        opt.textContent = created.name;
        catSelect.appendChild(opt);
        catSelect.value = created.itemCategoryId;

        nameInput.value = "";
        mini.style.display = "none";
        toast(`Category "${name}" created`);
      } catch (err) {
        toast(err.message, "error");
      } finally {
        saveBtn.disabled = false;
      }
    });
  }

  async function route() {
    removeDemoLogins();
    ensureCategoryNav();

    if (currentRoute() === "admin/item-categories") {
      try { await renderCategoriesPage(); }
      catch (err) { toast(err.message, "error"); }
      return;
    }

    if (["admin/create-order", "staff/create-order"].includes(currentRoute()) || currentRoute().startsWith("guest/")) {
      setTimeout(() => enhanceOrderCategoryPickers().catch(e => toast(e.message, "error")), 500);
    }

    if (currentRoute() === "admin/items") {
      setTimeout(enhanceItemCategoryField, 500);
    }
  }

  window.addEventListener("hashchange", () => setTimeout(route, 50));
  window.addEventListener("popstate", () => setTimeout(route, 50));
  new MutationObserver(() => {
    removeDemoLogins();
    ensureCategoryNav();
    if (["admin/create-order", "staff/create-order"].includes(currentRoute()) || currentRoute().startsWith("guest/")) {
      enhanceOrderCategoryPickers().catch(() => {});
    }
    if (currentRoute() === "admin/items") {
      enhanceItemCategoryField();
    }
  }).observe(document.body, { childList: true, subtree: true });
  setTimeout(route, 800);
})();
