(() => {
    "use strict";

    const shell = document.getElementById("posShell");
    if (!shell) return;

    const form = document.getElementById("posCheckoutForm");
    const cartView = document.getElementById("orderCart");
    const cartCount = document.getElementById("cartCount");
    const totalView = document.getElementById("cartTotal");
    const customerSearch = document.getElementById("customerSearch");
    const customerResults = document.getElementById("customerResults");
    const customerLabel = document.getElementById("selectedCustomer");
    const message = document.getElementById("posMessage");
    const saveButton = document.getElementById("saveOrderButton");
    const newButton = document.getElementById("newOrderButton");
    const dialog = document.getElementById("productDialog");
    const dialogFields = document.getElementById("dialogFields");
    const arabic = shell.dataset.arabic === "true";
    const token = form.querySelector('input[name="__RequestVerificationToken"]').value;
    const pendingKey = "ibdaa.pending-order-request";
    const cartKey = "ibdaa.current-cart";
    let customerId = 0;
    let customerName = "";
    let items = [];
    let selectedProduct = null;
    let saving = false;
    let completed = false;
    let requestId = 0;
    let customerAbort = null;
    let productAbort = null;

    try {
        requestId = Number(sessionStorage.getItem(pendingKey)) || 0;
        const saved = JSON.parse(sessionStorage.getItem(cartKey) || "null");
        if (saved && Array.isArray(saved.items)) {
            items = saved.items;
            customerId = saved.customerId || 0;
            customerName = saved.customerName || "";
            if (customerId) {
                customerLabel.textContent = text("Selected: ", "العميل: ") + customerName;
                customerSearch.value = customerName;
            }
        }
    } catch { /* In-memory fallback when browser storage is unavailable. */ }

    const text = (en, ar) => arabic ? ar : en;
    const money = amount => Number(amount).toFixed(3) + " KWD";
    const notice = (value, error = false) => {
        message.textContent = value;
        message.className = error ? "small text-danger mb-3" : "small text-success mb-3";
    };

    async function getJson(url, options = {}) {
        const response = await fetch(url, { credentials: "same-origin", ...options });
        const body = await response.json().catch(() => ({}));
        if (!response.ok) throw new Error(body.error || text("Request failed.", "تعذر إكمال الطلب."));
        return body;
    }

    function element(tag, className, value) {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (value !== undefined) node.textContent = value;
        return node;
    }

    function persistCart() {
        try { sessionStorage.setItem(cartKey, JSON.stringify({ items, customerId, customerName })); }
        catch { /* Keep the current in-memory order. */ }
    }

    function renderCart() {
        cartView.replaceChildren();
        cartCount.textContent = String(items.length);
        if (items.length === 0) {
            cartView.append(element("p", "text-secondary small", text("No items added yet.", "لا توجد عناصر بعد.")));
        }
        items.forEach((line, index) => {
            const row = element("div", "pos-cart-item border-bottom py-2");
            const title = element("div", "d-flex justify-content-between gap-2");
            title.append(element("strong", "", (arabic ? line.nameAr || line.name : line.name) + " × " + line.quantity));
            const remove = element("button", "btn btn-sm btn-outline-secondary", text("Remove", "حذف"));
            remove.type = "button";
            remove.addEventListener("click", () => {
                if (saving || completed) return;
                items.splice(index, 1);
                renderCart();
            });
            title.append(remove);
            row.append(title);
            const details = [line.sizeName, ...line.measurementLabels].filter(Boolean).join(" · ");
            if (details) row.append(element("small", "d-block text-secondary", details));
            row.append(element("small", "d-block product-pricing", money(line.price * line.quantity)));
            cartView.append(row);
        });
        totalView.textContent = money(items.reduce((sum, line) => sum + line.price * line.quantity, 0));
        persistCart();
    }

    customerSearch.addEventListener("input", async () => {
        if (saving || completed) return;
        if (customerId && items.length > 0) {
            if (!window.confirm(text("Changing customer clears the cart. Continue?",
                "تغيير العميل يمسح عناصر الطلب. متابعة؟"))) {
                customerSearch.value = customerName;
                return;
            }
            items = [];
            renderCart();
        }
        customerId = 0;
        customerName = "";
        persistCart();
        customerLabel.textContent = text("No customer selected", "لم يتم اختيار عميل");
        customerResults.replaceChildren();
        customerAbort?.abort();
        const search = customerSearch.value.trim();
        if (search.length < 2) return;
        const controller = new AbortController();
        customerAbort = controller;
        try {
            const customers = await getJson(shell.dataset.customerUrl + "?search=" + encodeURIComponent(search),
                { signal: controller.signal });
            if (controller.signal.aborted) return;
            customers.forEach(customer => {
                const name = arabic ? (customer.nameAr || customer.name) : customer.name;
                const row = element("button", "list-group-item list-group-item-action",
                    name + " · " + customer.mobile);
                row.type = "button";
                row.addEventListener("click", () => {
                    if (saving || completed) return;
                    customerId = customer.customerId;
                    customerName = name;
                    customerSearch.value = name + " · " + customer.mobile;
                    customerLabel.textContent = text("Selected: ", "العميل: ") + customerName;
                    persistCart();
                    customerResults.replaceChildren();
                    notice("");
                });
                customerResults.append(row);
            });
        } catch (error) {
            if (error.name !== "AbortError") notice(error.message, true);
        }
    });

    document.querySelectorAll(".pos-product").forEach(button => button.addEventListener("click", async () => {
        if (saving || completed) return;
        if (!customerId) {
            notice(text("Select a customer before adding an item.", "اختر العميل أولاً."), true);
            customerSearch.focus();
            return;
        }
        productAbort?.abort();
        const controller = new AbortController();
        productAbort = controller;
        try {
            const product = await getJson(shell.dataset.productUrl + "?id=" + button.dataset.productId,
                { signal: controller.signal });
            if (controller.signal.aborted) return;
            selectedProduct = product;
            document.getElementById("dialogTitle").textContent = arabic ? product.nameAr || product.name : product.name;
            dialogFields.replaceChildren();

            const quantityLabel = element("label", "form-label", text("Quantity", "الكمية"));
            const quantity = element("input", "form-control mb-3");
            quantity.type = "number"; quantity.min = "1"; quantity.max = "100";
            quantity.value = "1"; quantity.id = "posQuantity"; quantity.required = true;
            quantityLabel.htmlFor = quantity.id;
            dialogFields.append(quantityLabel, quantity);

            if (product.sizes.length) {
                const label = element("label", "form-label", text("Size", "المقاس"));
                label.htmlFor = "posSize";
                const select = element("select", "form-select mb-3");
                select.id = "posSize";
                select.required = true;
                select.append(new Option(text("Select a size", "اختر المقاس"), ""));
                product.sizes.forEach(size => select.append(new Option(size.name, String(size.sizeId))));
                dialogFields.append(label, select);
            }

            product.measurements.forEach(measurement => {
                const label = element("label", "form-label",
                    (arabic ? measurement.nameAr || measurement.name : measurement.name) + " (" + measurement.unit + ")");
                const input = element("input", "form-control mb-2");
                input.type = "number"; input.min = "0"; input.max = "9999999.99";
                input.step = ".01"; input.required = measurement.isRequired;
                input.dataset.measurementId = String(measurement.measurementId);
                input.dataset.label = measurement.name;
                input.id = "measurement_" + measurement.measurementId;
                label.htmlFor = input.id;
                dialogFields.append(label, input);
            });

            const notesLabel = element("label", "form-label mt-2", text("Instructions", "ملاحظات"));
            const notes = element("textarea", "form-control");
            notes.id = "posNotes"; notes.maxLength = 1000; notes.rows = 2;
            notesLabel.htmlFor = notes.id;
            dialogFields.append(notesLabel, notes);
            dialog.showModal();
        } catch (error) {
            if (error.name !== "AbortError") notice(error.message, true);
        }
    }));

    document.getElementById("cancelProductButton").addEventListener("click", () => dialog.close());
    document.getElementById("addProductButton").addEventListener("click", () => {
        if (!selectedProduct || !customerId || saving || completed) return;
        const quantity = dialogFields.querySelector("#posQuantity");
        const size = dialogFields.querySelector("#posSize");
        const fields = [...dialogFields.querySelectorAll("[data-measurement-id]")];
        if (![quantity, size, ...fields].filter(Boolean).every(field => field.checkValidity())) {
            [quantity, size, ...fields].filter(Boolean).find(field => !field.checkValidity())?.reportValidity();
            return;
        }
        const measurements = fields.filter(field => field.value !== "").map(field => ({
            measurementId: Number(field.dataset.measurementId), value: Number(field.value)
        }));
        items.push({
            itemType: "Product", itemId: selectedProduct.productId,
            name: selectedProduct.name, nameAr: selectedProduct.nameAr,
            price: Number(selectedProduct.price), quantity: Number(quantity.value),
            sizeId: size ? Number(size.value) : null,
            sizeName: size ? size.selectedOptions[0].textContent : "",
            notes: dialogFields.querySelector("#posNotes").value,
            measurements,
            measurementLabels: fields.filter(field => field.value !== "").map(field =>
                field.dataset.label + ": " + field.value)
        });
        dialog.close();
        renderCart();
        notice("");
    });

    document.querySelectorAll(".pos-service").forEach(button => button.addEventListener("click", () => {
        if (saving || completed) return;
        if (!customerId) {
            notice(text("Select a customer before adding an item.", "اختر العميل أولاً."), true);
            customerSearch.focus();
            return;
        }
        items.push({
            itemType: "Service", itemId: Number(button.dataset.serviceId),
            name: button.dataset.name, nameAr: button.dataset.nameAr,
            price: Number(button.dataset.price), quantity: 1,
            sizeId: null, sizeName: "", notes: "", measurements: [], measurementLabels: []
        });
        renderCart();
        notice("");
    }));

    async function reserveRequest() {
        if (requestId > 0) return requestId;
        const data = await getJson(shell.dataset.reserveUrl, {
            method: "POST",
            headers: { RequestVerificationToken: token }
        });
        requestId = data.requestId;
        try { sessionStorage.setItem(pendingKey, String(requestId)); } catch { /* In-memory fallback. */ }
        return requestId;
    }

    form.addEventListener("submit", async event => {
        event.preventDefault();
        if (saving || completed) return;
        const date = document.getElementById("deliveryDate").value;
        if (!customerId || items.length < 1 || !date) {
            notice(text("Select a customer, delivery date and at least one item.", "اختر العميل وتاريخ التسليم وأضف عنصراً."), true);
            return;
        }
        saving = true;
        saveButton.disabled = true;
        notice(text("Saving order...", "جار حفظ الطلب..."));
        try {
            const id = await reserveRequest();
            const data = await getJson(shell.dataset.saveUrl, {
                method: "POST",
                headers: { "Content-Type": "application/json", RequestVerificationToken: token },
                body: JSON.stringify({
                    requestId: id, customerId, deliveryDate: date,
                    items: items.map(line => ({
                        itemType: line.itemType, itemId: line.itemId,
                        sizeId: line.sizeId, quantity: line.quantity,
                        notes: line.notes, measurements: line.measurements
                    }))
                })
            });
            completed = true;
            try {
                sessionStorage.removeItem(pendingKey);
                sessionStorage.removeItem(cartKey);
            } catch { /* Ignore storage restrictions. */ }
            notice(text("Saved unpaid order: ", "تم حفظ طلب غير مدفوع: ") +
                data.orderNumber + " · " + money(data.total));
            newButton.hidden = false;
        } catch (error) {
            notice(error.message, true);
            saveButton.disabled = false;
        } finally {
            saving = false;
        }
    });

    newButton.addEventListener("click", () => window.location.reload());
    if (!completed) renderCart();
})();
