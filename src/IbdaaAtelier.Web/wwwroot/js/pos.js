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
    const toast = document.getElementById("posToast");
    const toastText = document.getElementById("posToastText");
    const saveButton = document.getElementById("saveOrderButton");
    const newButton = document.getElementById("newOrderButton");
    const savedOrderLink = document.getElementById("savedOrderLink");
    const viewSavedOrderLink = document.getElementById("viewSavedOrderLink");
    const deliveryDate = document.getElementById("deliveryDate");
    const quickCustomerDialog = document.getElementById("quickCustomerDialog");
    const quickCustomerForm = document.getElementById("quickCustomerForm");
    const quickCustomerError = document.getElementById("quickCustomerError");
    const quickCustomerSave = document.getElementById("saveQuickCustomer");
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
    let editingIndex = -1;
    let saving = false;
    let completed = false;
    let requestId = 0;
    let customerAbort = null;
    let productAbort = null;
    const text = (en, ar) => arabic ? ar : en;

    try {
        requestId = Number(sessionStorage.getItem(pendingKey)) || 0;
        const saved = JSON.parse(sessionStorage.getItem(cartKey) || "null");
        if (saved && Array.isArray(saved.items)) {
            items = saved.items;
            customerId = saved.customerId || 0;
            customerName = saved.customerName || "";
            if (saved.deliveryDate && saved.deliveryDate >= deliveryDate.min)
                deliveryDate.value = saved.deliveryDate;
            if (customerId) {
                customerLabel.textContent = text("Selected: ", "العميل: ") + customerName;
                customerSearch.value = customerName;
            }
        }
    } catch { /* In-memory fallback when browser storage is unavailable. */ }

    const money = amount => Number(amount).toFixed(3) + " KWD";
    const notice = (value, error = false, popup = error) => {
        message.textContent = value;
        message.className = error ? "small text-danger mb-3" : "small text-secondary mb-3";
        if (!value) toast.hidden = true;
        else if (popup) {
            toastText.textContent = value;
            toast.classList.toggle("pos-toast-error", error);
            toast.setAttribute("role", error ? "alert" : "status");
            toast.hidden = false;
        }
    };
    document.getElementById("closePosToast").addEventListener("click", () => {
        toast.hidden = true;
    });

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

    function setBusy(button, busy, label) {
        if (busy) {
            button.dataset.idleLabel = button.textContent;
            const spinner = element("span", "spinner-border spinner-border-sm me-2");
            spinner.setAttribute("aria-hidden", "true");
            button.replaceChildren(spinner, document.createTextNode(label));
        } else if (button.dataset.idleLabel) {
            button.textContent = button.dataset.idleLabel;
        }
        button.disabled = busy;
        button.setAttribute("aria-busy", String(busy));
    }

    function persistCart() {
        try { sessionStorage.setItem(cartKey, JSON.stringify({
            items, customerId, customerName, deliveryDate: deliveryDate.value
        })); }
        catch { /* Keep the current in-memory order. */ }
    }

    const productPanel = document.getElementById("catalogueProducts");
    const servicePanel = document.getElementById("catalogueServices");
    const productsTab = document.getElementById("showProducts");
    const servicesTab = document.getElementById("showServices");

    function switchCatalogue(showServices) {
        productPanel.hidden = showServices;
        servicePanel.hidden = !showServices;
        productsTab.className = showServices ? "btn btn-outline-primary flex-fill" : "btn btn-primary flex-fill";
        servicesTab.className = showServices ? "btn btn-primary flex-fill" : "btn btn-outline-primary flex-fill";
        productsTab.setAttribute("aria-pressed", String(!showServices));
        servicesTab.setAttribute("aria-pressed", String(showServices));
    }
    productsTab.addEventListener("click", () => switchCatalogue(false));
    servicesTab.addEventListener("click", () => switchCatalogue(true));

    function renderCart() {
        cartView.replaceChildren();
        cartCount.textContent = String(items.length);
        if (items.length === 0) {
            cartView.append(element("p", "pos-empty-cart", text(
                "Choose a customer, then tap a product to add it.",
                "اختر العميل ثم اضغط على المنتج لإضافته.")));
        }
        items.forEach((line, index) => {
            const row = element("div", "pos-cart-item border-bottom py-2");
            const title = element("div", "d-flex justify-content-between gap-2");
            title.append(element("strong", "", (arabic ? line.nameAr || line.name : line.name) + " × " + line.quantity));
            const actions = element("div", "d-inline-flex gap-1");
            const edit = element("button", "btn btn-sm btn-outline-primary", text("Edit", "تعديل"));
            edit.type = "button";
            edit.addEventListener("click", () => {
                if (saving || completed) return;
                if (line.itemType === "Product") openProduct(line.itemId, index);
                else editService(index);
            });
            actions.append(edit);
            const remove = element("button", "btn btn-sm btn-outline-secondary", text("Remove", "حذف"));
            remove.type = "button";
            remove.addEventListener("click", () => {
                if (saving || completed) return;
                items.splice(index, 1);
                renderCart();
            });
            actions.append(remove);
            title.append(actions);
            row.append(title);
            const details = [line.sizeName, ...line.measurementLabels].filter(Boolean).join(" · ");
            if (details) row.append(element("small", "d-block text-secondary", details));
            row.append(element("small", "d-block product-pricing", money(line.price * line.quantity)));
            cartView.append(row);
        });
        totalView.textContent = money(items.reduce((sum, line) => sum + line.price * line.quantity, 0));
        saveButton.disabled = saving || completed || !customerId || items.length === 0;
        persistCart();
    }

    deliveryDate.addEventListener("change", persistCart);

    document.getElementById("openQuickCustomer").addEventListener("click", () => {
        if (saving || completed) return;
        if (customerId && items.length > 0 && !window.confirm(text(
            "Changing customer clears the cart. Continue?",
            "تغيير العميل يمسح عناصر الطلب. متابعة?"))) return;
        quickCustomerError.textContent = "";
        quickCustomerForm.reset();
        const typed = customerSearch.value.trim();
        if (/^[+\d\s()-]{8,20}$/.test(typed))
            document.getElementById("quickCustomerMobile").value = typed;
        quickCustomerDialog.showModal();
        document.getElementById("quickCustomerName").focus();
    });

    quickCustomerForm.addEventListener("submit", async event => {
        event.preventDefault();
        if (saving || completed || !quickCustomerForm.reportValidity()) return;
        if (quickCustomerSave.disabled) return;
        setBusy(quickCustomerSave, true, text("Saving customer...", "جار حفظ العميل..."));
        quickCustomerError.textContent = "";
        try {
            const customer = await getJson(shell.dataset.customerCreateUrl, {
                method: "POST",
                headers: { RequestVerificationToken: token },
                body: new FormData(quickCustomerForm)
            });
            items = [];
            customerId = customer.customerId;
            customerName = arabic ? customer.nameAr || customer.name : customer.name;
            customerSearch.value = customerName + " · " + customer.mobile;
            customerLabel.textContent = text("Selected: ", "العميل: ") + customerName;
            customerResults.replaceChildren();
            quickCustomerDialog.close();
            renderCart();
            notice(text("Customer saved and selected.", "تم حفظ العميل واختياره."), false, true);
        } catch (error) {
            const detail = error instanceof TypeError
                ? text("Unable to confirm. Search by mobile before trying again.",
                    "تعذر التأكد من الحفظ. ابحث برقم الهاتف قبل المحاولة مجدداً.")
                : error.message;
            quickCustomerError.textContent = detail;
            notice(detail, true);
        } finally {
            setBusy(quickCustomerSave, false);
        }
    });

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
        saveButton.disabled = true;
        customerResults.replaceChildren();
        customerAbort?.abort();
        const search = customerSearch.value.trim();
        if (search.length < 2) return;
        customerResults.append(element("div", "list-group-item text-secondary",
            text("Searching customers...", "جار البحث عن العملاء...")));
        const controller = new AbortController();
        customerAbort = controller;
        try {
            const customers = await getJson(shell.dataset.customerUrl + "?search=" + encodeURIComponent(search),
                { signal: controller.signal });
            if (controller.signal.aborted) return;
            customerResults.replaceChildren();
            if (customers.length === 0)
                customerResults.append(element("div", "list-group-item text-secondary",
                    text("No customer found. Use New customer.", "لم يتم العثور على عميل. أضف عميلاً جديداً.")));
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
                    customerAbort?.abort();
                    customerResults.replaceChildren();
                    renderCart();
                    notice("");
                });
                customerResults.append(row);
            });
        } catch (error) {
            if (error.name !== "AbortError") {
                customerResults.replaceChildren();
                notice(error.message, true);
            }
        }
    });

    async function openProduct(productId, index = -1) {
        if (saving || completed) return;
        if (!customerId) {
            notice(text("Select a customer before adding an item.", "اختر العميل أولاً."), true);
            customerSearch.focus();
            return;
        }
        productAbort?.abort();
        const controller = new AbortController();
        productAbort = controller;
        const productTile = [...document.querySelectorAll(".pos-product")]
            .find(button => button.dataset.productId === String(productId));
        productTile?.setAttribute("aria-busy", "true");
        productTile?.classList.add("is-loading");
        notice(text("Loading product...", "جار تحميل المنتج..."));
        try {
            const product = await getJson(shell.dataset.productUrl + "?id=" + encodeURIComponent(productId),
                { signal: controller.signal });
            if (controller.signal.aborted) return;
            selectedProduct = product;
            editingIndex = index;
            const current = index >= 0 ? items[index] : null;
            document.getElementById("addProductButton").textContent =
                current ? text("Update item", "تحديث العنصر") : text("Add to order", "إضافة للطلب");
            document.getElementById("dialogTitle").textContent = arabic ? product.nameAr || product.name : product.name;
            dialogFields.replaceChildren();

            if (Array.isArray(product.images) && product.images.length) {
                const gallery = element("div", "pos-dialog-gallery mb-3");
                const preview = element("img", "pos-dialog-image");
                preview.src = product.images[0];
                preview.alt = arabic ? product.nameAr || product.name : product.name;
                preview.width = 400;
                preview.height = 480;
                gallery.append(preview);
                if (product.images.length > 1) {
                    const thumbnails = element("div", "pos-dialog-thumbnails");
                    product.images.forEach((url, index) => {
                        const button = element("button", "pos-dialog-thumbnail");
                        button.type = "button";
                        button.setAttribute("aria-label", text("View image ", "عرض الصورة ") + (index + 1));
                        if (index === 0) button.classList.add("active");
                        const thumb = element("img", "");
                        thumb.src = url;
                        thumb.alt = "";
                        thumb.loading = "lazy";
                        button.append(thumb);
                        button.addEventListener("click", () => {
                            preview.src = url;
                            thumbnails.querySelector(".active")?.classList.remove("active");
                            button.classList.add("active");
                        });
                        thumbnails.append(button);
                    });
                    gallery.append(thumbnails);
                }
                dialogFields.append(gallery);
            }

            const quantityLabel = element("label", "form-label", text("Quantity", "الكمية"));
            const quantity = element("input", "form-control mb-3");
            quantity.type = "number"; quantity.min = "1"; quantity.max = "100";
            quantity.value = String(current?.quantity ?? 1); quantity.id = "posQuantity"; quantity.required = true;
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
                if (current?.sizeId) select.value = String(current.sizeId);
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
                const previous = current?.measurements?.find(value => value.measurementId === measurement.measurementId);
                if (previous) input.value = String(previous.value);
                label.htmlFor = input.id;
                dialogFields.append(label, input);
            });

            const notesLabel = element("label", "form-label mt-2", text("Instructions", "ملاحظات"));
            const notes = element("textarea", "form-control");
            notes.id = "posNotes"; notes.maxLength = 1000; notes.rows = 2;
            notes.value = current?.notes ?? "";
            notesLabel.htmlFor = notes.id;
            dialogFields.append(notesLabel, notes);
            notice("");
            dialog.showModal();
        } catch (error) {
            if (error.name !== "AbortError") notice(error.message, true);
        } finally {
            productTile?.removeAttribute("aria-busy");
            productTile?.classList.remove("is-loading");
        }
    }

    document.querySelectorAll(".pos-product").forEach(button =>
        button.addEventListener("click", () => openProduct(button.dataset.productId)));

    function editService(index) {
        const line = items[index];
        if (!line || saving || completed) return;
        productAbort?.abort();
        selectedProduct = null;
        editingIndex = index;
        document.getElementById("dialogTitle").textContent = arabic ? line.nameAr || line.name : line.name;
        document.getElementById("addProductButton").textContent = text("Update item", "تحديث العنصر");
        dialogFields.replaceChildren();
        const quantityLabel = element("label", "form-label", text("Quantity", "الكمية"));
        const quantity = element("input", "form-control mb-3");
        quantity.type = "number"; quantity.min = "1"; quantity.max = "100";
        quantity.value = String(line.quantity); quantity.id = "posQuantity"; quantity.required = true;
        quantityLabel.htmlFor = quantity.id;
        const notesLabel = element("label", "form-label", text("Instructions", "ملاحظات"));
        const notes = element("textarea", "form-control");
        notes.id = "posNotes"; notes.maxLength = 1000; notes.rows = 2;
        notes.value = line.notes ?? "";
        notesLabel.htmlFor = notes.id;
        dialogFields.append(quantityLabel, quantity, notesLabel, notes);
        dialog.showModal();
    }

    document.getElementById("cancelProductButton").addEventListener("click", () => dialog.close());
    document.getElementById("addProductButton").addEventListener("click", () => {
        if (!customerId || saving || completed) return;
        if (editingIndex >= 0 && items[editingIndex]?.itemType === "Service") {
            const quantity = dialogFields.querySelector("#posQuantity");
            if (!quantity.checkValidity()) { quantity.reportValidity(); return; }
            items[editingIndex] = {
                ...items[editingIndex], quantity: Number(quantity.value),
                notes: dialogFields.querySelector("#posNotes").value
            };
            editingIndex = -1;
            dialog.close();
            renderCart();
            return;
        }
        if (!selectedProduct) return;
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
        const nextItem = {
            itemType: "Product", itemId: selectedProduct.productId,
            name: selectedProduct.name, nameAr: selectedProduct.nameAr,
            price: Number(selectedProduct.price), quantity: Number(quantity.value),
            sizeId: size ? Number(size.value) : null,
            sizeName: size ? size.selectedOptions[0].textContent : "",
            notes: dialogFields.querySelector("#posNotes").value,
            measurements,
            measurementLabels: fields.filter(field => field.value !== "").map(field =>
                field.dataset.label + ": " + field.value)
        };
        if (editingIndex >= 0) items[editingIndex] = nextItem;
        else items.push(nextItem);
        editingIndex = -1;
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
        const date = deliveryDate.value;
        if (!customerId || items.length < 1 || !date) {
            notice(text("Select a customer, delivery date and at least one item.", "اختر العميل وتاريخ التسليم وأضف عنصراً."), true);
            return;
        }
        saving = true;
        setBusy(saveButton, true, text("Processing order...", "جار معالجة الطلب..."));
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
                data.orderNumber + " · " + money(data.total), false, true);
            const savedId = "?id=" + encodeURIComponent(data.orderId);
            savedOrderLink.href = shell.dataset.paymentUrl + savedId;
            viewSavedOrderLink.href = shell.dataset.orderUrl + savedId;
            savedOrderLink.hidden = false;
            viewSavedOrderLink.hidden = false;
            newButton.hidden = false;
        } catch (error) {
            notice(error instanceof TypeError
                ? text("Save status could not be confirmed. Retry Save safely; do not start a new order.",
                    "تعذر التأكد من الحفظ. أعد المحاولة بأمان ولا تبدأ طلباً جديداً.")
                : error.message, true);
        } finally {
            saving = false;
            setBusy(saveButton, false);
            if (completed) {
                saveButton.textContent = text("Order saved", "تم حفظ الطلب");
                saveButton.disabled = true;
            } else {
                saveButton.disabled = !customerId || items.length === 0;
            }
        }
    });

    newButton.addEventListener("click", () => window.location.reload());
    if (!completed) renderCart();
})();
