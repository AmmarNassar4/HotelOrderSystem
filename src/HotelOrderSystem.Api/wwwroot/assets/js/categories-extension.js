(() => {
  "use strict";

  const SESSION_KEY = "hotel.ops.session";

  function session() {
    try { return JSON.parse(localStorage.getItem(SESSION_KEY) || "null"); }
    catch { return null; }
  }

  function token() {
    return session()?.token || "";
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

  function toast(message, type = "success") {
    const host = document.getElementById("toast");
    if (!host) return alert(message);
    const el = document.createElement("div");
    el.className = `toast ${type}`;
    el.textContent = message;
    host.appendChild(el);
    setTimeout(() => el.remove(), 3500);
  }

  function ensureCategoryNav() {
    if (!token()) return;
    const sidebar = document.querySelector(".sidebar-nav, nav");
    if (!sidebar || sidebar.querySelector('[href="#/admin/item-categories"]')) return;
    const itemsLink = sidebar.querySelector('[href="#/admin/items"]');
    if (!itemsLink) return;
    const a = document.createElement("a");
    a.href = "#/admin/item-categories";
    a.innerHTML = "<span>🗂️</span><span>Item Categories</span>";
    itemsLink.before(a);
  }

  async function renderCategoriesPage() {
    const app = document.getElementById("app");
    if (!app || !token()) return;
    app.innerHTML = `<main class="main"><header class="topbar"><div><h1>Item Categories</h1><p>Every item must be linked to an active category before it can be created.</p></div></header><section class="card">Loading categories...</section></main>`;
    const categories = await api("/api/v1/admin/item-categories");
    app.innerHTML = `
      <main class="main">
        <header class="topbar"><div><h1>Item Categories</h1><p>Create categories first, then link items and services to them.</p></div></header>
        <div class="grid grid-2">
          <form class="card grid" data-category-form>
            <div class="card-header"><div><h2 class="card-title">Add category</h2><p class="card-subtitle">Categories are required for item creation.</p></div></div>
            <input type="hidden" name="id" />
            <div class="field"><label>Category name</label><input name="name" required /></div>
            <div class="field"><label>Description</label><textarea name="description" rows="3"></textarea></div>
            <div class="field"><label>Status</label><select name="isActive"><option value="true">Active</option><option value="false">Inactive</option></select></div>
            <button class="btn btn-primary" type="submit">Save category</button>
          </form>
          <section class="card">
            <div class="card-header"><h2 class="card-title">Category list</h2><span class="pill primary">${categories.length}</span></div>
            <div class="table-wrap"><table><thead><tr><th>Category</th><th>Status</th><th>Actions</th></tr></thead><tbody>
              ${categories.map(c => `<tr><td><strong>${h(c.name)}</strong><br><small>${h(c.description || "No description")}</small></td><td>${c.isActive ? '<span class="pill success">Active</span>' : '<span class="pill danger">Inactive</span>'}</td><td><div class="actions"><button class="btn btn-soft" data-edit-category='${h(JSON.stringify(c))}'>Edit</button><button class="btn btn-danger" data-delete-category="${c.itemCategoryId}">Delete</button></div></td></tr>`).join("")}
            </tbody></table></div>
          </section>
        </div>
      </main>`;
  }

  async function enhanceItemsPage() {
    const form = document.querySelector('form[data-form="item"]');
    if (!form || form.querySelector('[name="itemCategoryId"]')) return;
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

  function collectSchema(form) {
    const fields = [];
    form.querySelectorAll('[data-attribute-row]').forEach((row, index) => {
      const label = row.querySelector('[data-schema-label]')?.value?.trim();
      if (!label) return;
      let key = row.querySelector('[data-schema-key]')?.value?.trim() || label.toLowerCase().replace(/[^a-z0-9]+/g, "_").replace(/^_+|_+$/g, "");
      if (!key) key = `field_${index + 1}`;
      const type = row.querySelector('[data-schema-type]')?.value || "text";
      const options = (row.querySelector('[data-schema-options]')?.value || "").split(",").map(x => x.trim()).filter(Boolean);
      fields.push({
        key,
        label,
        type,
        required: Boolean(row.querySelector('[data-schema-required]')?.checked),
        defaultValue: row.querySelector('[data-schema-default]')?.value || null,
        options
      });
    });
    return JSON.stringify({ fields });
  }

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
      toast("Category saved");
      renderCategoriesPage().catch(e => toast(e.message, "error"));
      return;
    }

    const itemForm = event.target.closest('form[data-form="item"]');
    if (itemForm) {
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
        body: {
          name: data.name.trim(),
          type: data.type,
          itemCategoryId: categoryId,
          targetTeamId: data.targetTeamId ? Number(data.targetTeamId) : null,
          baseProperties: collectSchema(itemForm),
          isActive: data.isActive === "true"
        }
      });
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
      toast("Category deleted");
      renderCategoriesPage().catch(e => toast(e.message, "error"));
    }
  });

  async function route() {
    ensureCategoryNav();
    if (location.hash.replace(/^#\/?/, "") === "admin/item-categories") {
      try { await renderCategoriesPage(); }
      catch (err) { toast(err.message, "error"); }
      return;
    }
    if (location.hash.includes("admin/items")) {
      setTimeout(() => enhanceItemsPage().catch(e => toast(e.message, "error")), 500);
    }
  }

  window.addEventListener("hashchange", () => setTimeout(route, 50));
  new MutationObserver(() => { ensureCategoryNav(); if (location.hash.includes("admin/items")) enhanceItemsPage().catch(() => {}); }).observe(document.body, { childList: true, subtree: true });
  setTimeout(route, 800);
})();
