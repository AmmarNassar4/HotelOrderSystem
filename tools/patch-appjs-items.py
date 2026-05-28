from pathlib import Path

path = Path('src/HotelOrderSystem.Api/wwwroot/assets/js/app.js')
text = path.read_text(encoding='utf-8')

text = text.replace(
    'const rows = fields.length ? fields.map(attributeFieldRow).join("") : attributeFieldRow({ label: "", key: "", type: "text" }, 0);',
    'const rows = fields.length ? fields.map(attributeFieldRow).join("") : `<div class="empty compact-empty" data-empty-dynamic-fields>No dynamic fields. Click Add field only when needed.</div>`;'
)

text = text.replace(
    'return JSON.stringify({ fields });',
    'return fields.length ? JSON.stringify({ fields }) : null;',
    1
)

text = text.replace(
    'const [items, teams] = await Promise.all([loadItems(), loadTeams()]);\n    const editing = state.editing.itemId ? items.find(x => String(x.itemId) === String(state.editing.itemId)) : null;',
    'const [items, teams, categories] = await Promise.all([loadItems(), loadTeams(), api("/api/v1/admin/item-categories")]);\n    const editing = state.editing.itemId ? items.find(x => String(x.itemId) === String(state.editing.itemId)) : null;\n    const activeCategories = (categories || []).filter(c => c.isActive !== false);'
)

text = text.replace(
'''          <div class="field">
            <label>Name</label>
            <input name="name" value="${attr(editing?.name || "")}" required />
          </div>
          <div class="field">
            <label>Type</label>''',
'''          <div class="field">
            <label>Name</label>
            <input name="name" value="${attr(editing?.name || "")}" required />
          </div>
          <div class="field">
            <label>Category</label>
            <select name="itemCategoryId" required>
              ${option("", activeCategories.length ? "Choose category" : "Create a category first", editing?.itemCategoryId ?? "")}
              ${activeCategories.map(c => option(c.itemCategoryId, c.name, editing?.itemCategoryId ?? "")).join("")}
            </select>
          </div>
          <div class="field">
            <label>Type</label>'''
)

text = text.replace(
    'const rows = builder?.querySelector("[data-attribute-rows]");\n        if (rows) {\n          rows.insertAdjacentHTML("beforeend", attributeFieldRow({ label: "", key: "", type: "text" }, rows.querySelectorAll("[data-attribute-row]").length));',
    'const rows = builder?.querySelector("[data-attribute-rows]");\n        if (rows) {\n          rows.querySelector("[data-empty-dynamic-fields]")?.remove();\n          rows.insertAdjacentHTML("beforeend", attributeFieldRow({ label: "", key: "", type: "text" }, rows.querySelectorAll("[data-attribute-row]").length));'
)

text = text.replace(
    '          targetTeamId: data.targetTeamId ? Number(data.targetTeamId) : null,\n          baseProperties,',
    '          itemCategoryId: data.itemCategoryId ? Number(data.itemCategoryId) : 0,\n          targetTeamId: data.targetTeamId ? Number(data.targetTeamId) : null,\n          baseProperties,'
)

path.write_text(text, encoding='utf-8')
