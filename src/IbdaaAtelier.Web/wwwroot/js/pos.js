(() => {
    "use strict";

    const shell = document.getElementById("posShell");
    if (!shell) return;

    const form = document.getElementById("posCheckoutForm");
    const cartView = document.getElementById("orderCart");
    const cartCount = document.getElementById("cartCount");
    const totalView = document.getElementById("cartTotal");
    const subtotalView = document.getElementById("cartSubtotal");
    const discountInput = document.getElementById("posDiscountAmount");
    const invoiceForm = document.getElementById("findInvoiceForm");
    const invoiceInput = document.getElementById("findInvoiceNumber");
    const invoiceButton = document.getElementById("findInvoiceButton");
    const invoiceMessage = document.getElementById("findInvoiceMessage");
    const newOrderCart = document.getElementById("newOrderCart");
    const boundInvoiceView = document.getElementById("boundInvoice");
    const invoiceNumberView = document.getElementById("nextOrderNumber");
    const customerSearch = document.getElementById("customerSearch");
    const customerResults = document.getElementById("customerResults");
    const customerLabel = document.getElementById("selectedCustomer");
    const customerMobileSearch = document.getElementById("customerMobileSearch");
    const message = document.getElementById("posMessage");
    const toast = document.getElementById("posToast");
    const toastText = document.getElementById("posToastText");
    const saveButton = document.getElementById("saveOrderButton");
    const clearCartButton = document.getElementById("clearCartButton");
    const unpaidButton = document.getElementById("posChooseUnpaid");
    const paidButton = document.getElementById("posChoosePaid");
    const paymentDialog = document.getElementById("posPaymentDialog");
    const paymentConfirm = document.getElementById("confirmPosPayment");
    const paymentError = document.getElementById("posPaymentError");
    const paymentAmount = document.getElementById("posPaymentAmount");
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
    const checkoutKey = "ibdaa.pending-checkout";
    const lastSaleKey = "ibdaa.last-sale-message";
    let customerId = 0;
    let customerName = "";
    let customerMobile = "";
    let items = [];
    let selectedProduct = null;
    let editingIndex = -1;
    let saving = false;
    let completed = false;
    let requestId = 0;
    let customerAbort = null;
    let productAbort = null;
    let boundInvoiceId = 0;
    let boundInvoiceBalance = 0;
    let paidMode = false;
    let paymentMethod = "Cash";
    let savedOrder = null;
    let paymentRequestId = 0;
    let paymentPayload = null;
    let paymentBusy = false;
    let recoveryInvoiceNumber = "";
    let newInvoiceNumber = invoiceNumberView.textContent;
    const text = (en, ar) => arabic ? ar : en;

    try {
        requestId = Number(sessionStorage.getItem(pendingKey)) || 0;
        const pending = JSON.parse(sessionStorage.getItem(checkoutKey) || "null");
        if (pending && Number.isSafeInteger(pending.order?.orderId) && pending.order.orderId > 0) {
            savedOrder = pending.order;
            paymentRequestId = pending.requestId || 0;
            paymentPayload = pending.payload || null;
            completed = true;
            paidMode = true;
        } else if (pending?.previousNumber && pending.payload) {
            recoveryInvoiceNumber = pending.previousNumber;
            paymentRequestId = pending.requestId || 0;
            paymentPayload = pending.payload;
        }
        if (paymentPayload) {
            paymentMethod = paymentPayload.lines.length > 1 ? "Split" :
                paymentPayload.lines[0]?.method || "Cash";
        }
        const saved = JSON.parse(sessionStorage.getItem(cartKey) || "null");
        if (saved && Array.isArray(saved.items)) {
            items = saved.items;
            customerId = saved.customerId || 0;
            customerName = saved.customerName || "";
            customerMobile = saved.customerMobile || "";
            if (discountInput && saved.discountAmount !== undefined)
                discountInput.value = String(saved.discountAmount);
            if (saved.deliveryDate && saved.deliveryDate >= deliveryDate.min)
                deliveryDate.value = saved.deliveryDate;
            if (customerId) {
                customerLabel.textContent = text("Selected: ", "العميل: ") + customerName;
                customerSearch.value = customerName;
                customerMobileSearch.value = customerMobile;
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
            items, customerId, customerName, customerMobile, deliveryDate: deliveryDate.value,
            discountAmount: discountInput?.value ?? "0"
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
    if (shell.dataset.showServices === "true") switchCatalogue(true);

    function showInvoice(invoice) {
        boundInvoiceId = invoice.orderId;
        boundInvoiceBalance = invoice.balance;
        invoiceNumberView.textContent = invoice.orderNumber;
        document.getElementById("boundInvoiceCustomer").textContent =
            arabic ? invoice.customerNameAr || invoice.customerName : invoice.customerName;
        document.getElementById("boundInvoiceMobile").textContent = invoice.customerMobile;
        document.getElementById("boundInvoiceDelivery").textContent =
            text("Delivery: ", "التسليم: ") + String(invoice.deliveryDate).split("-").reverse().join("-");
        document.getElementById("boundOrderStatus").textContent =
            text("Order: ", "الطلب: ") + invoice.status;
        document.getElementById("boundInvoiceStatus").textContent =
            invoice.status === "Cancelled" ? text("CANCELLED", "ملغى") :
            invoice.total <= 0 ? text("NO PAYMENT DUE", "لا يوجد مبلغ مستحق") :
            invoice.balance <= 0 ? text("PAID", "مدفوع") :
            invoice.paid > 0 ? text("PARTIALLY PAID", "مدفوع جزئياً") : text("UNPAID", "غير مدفوع");

        const lines = document.getElementById("boundInvoiceItems");
        lines.replaceChildren();
        invoice.items.forEach(line => {
            const row = element("div", "pos-cart-item");
            const name = element("div", "pos-cart-item-name");
            name.append(element("strong", "", arabic ? line.nameAr || line.name : line.name));
            if (line.sizeName) name.append(element("small", "d-block text-secondary", line.sizeName));
            row.append(name);
            row.append(element("span", "pos-cart-quantity", String(line.quantity)));
            row.append(element("span", "pos-cart-unit-price", money(line.unitPrice)));
            row.append(element("strong", "pos-cart-line-total", money(line.lineTotal)));
            row.append(element("span", "pos-cart-actions"));
            lines.append(row);
        });
        cartCount.textContent = String(invoice.items.length);
        document.getElementById("boundInvoiceSubtotal").textContent = money(invoice.subtotal);
        document.getElementById("boundInvoiceDiscountRow").hidden = invoice.discountAmount <= 0;
        document.getElementById("boundInvoiceDiscount").textContent = "-" + money(invoice.discountAmount);
        document.getElementById("boundInvoiceTotal").textContent = money(invoice.total);
        document.getElementById("boundInvoicePaid").textContent = money(invoice.paid);
        document.getElementById("boundInvoiceBalance").textContent = money(invoice.balance);
        const receive = document.getElementById("boundInvoicePayment");
        receive.hidden = (invoice.balance <= 0 && !paymentPayload) || invoice.status === "Cancelled";
        newOrderCart.hidden = true;
        boundInvoiceView.hidden = false;
        notice("");
    }

    invoiceForm.addEventListener("submit", async event => {
        event.preventDefault();
        if (saving || paymentBusy || savedOrder || paymentPayload ||
            invoiceButton.disabled || !invoiceInput.reportValidity()) return;
        const number = invoiceInput.value.trim();
        if (!number) return;
        if (!boundInvoiceId && items.length && !window.confirm(text(
            "Open saved invoice? Your unsaved cart will be kept until you return.",
            "فتح فاتورة محفوظة؟ ستبقى السلة الحالية حتى تعود."))) return;

        invoiceMessage.textContent = "";
        invoiceMessage.className = "small text-secondary mb-2";
        setBusy(invoiceButton, true, text("Finding...", "جار البحث..."));
        try {
            const invoice = await getJson(shell.dataset.invoiceUrl +
                "?number=" + encodeURIComponent(number));
            showInvoice(invoice);
            invoiceMessage.textContent = text("Saved invoice loaded (read-only).", "تم عرض الفاتورة المحفوظة للقراءة فقط.");
        } catch (error) {
            invoiceMessage.textContent = error.message;
            invoiceMessage.className = "small text-danger mb-2";
        } finally {
            setBusy(invoiceButton, false);
        }
    });

    document.getElementById("closeInvoiceButton").addEventListener("click", () => {
        if (paymentBusy || paymentPayload) {
            notice(text("Finish the pending payment before leaving this invoice.",
                "أكمل الدفع المعلق قبل مغادرة الفاتورة."), true);
            return;
        }
        boundInvoiceId = 0;
        boundInvoiceBalance = 0;
        boundInvoiceView.hidden = true;
        newOrderCart.hidden = false;
        invoiceNumberView.textContent = newInvoiceNumber;
        invoiceInput.value = "";
        invoiceMessage.textContent = "";
        invoiceMessage.className = "small text-secondary mb-2";
        renderCart();
    });

    function renderCart() {
        cartView.replaceChildren();
        cartCount.textContent = String(items.length);
        if (items.length === 0) {
            cartView.append(element("p", "pos-empty-cart", customerId
                ? text("Tap a product or alteration to add it.", "اضغط على منتج أو خدمة تعديل لإضافتها.")
                : text("Choose a customer, then tap a product to add it.", "اختر العميل ثم اضغط على المنتج لإضافته.")));
        }
        items.forEach((line, index) => {
            const row = element("div", "pos-cart-item");
            const item = element("div", "pos-cart-item-name");
            item.append(element("strong", "", arabic ? line.nameAr || line.name : line.name));
            const details = [line.sizeName, ...line.measurementLabels].filter(Boolean).join(" · ");
            if (details) item.append(element("small", "d-block text-secondary", details));
            row.append(item);

            const quantity = element("div", "pos-cart-quantity");
            const decrease = element("button", "", "−");
            decrease.type = "button";
            decrease.setAttribute("aria-label", text("Decrease quantity", "تقليل الكمية"));
            decrease.disabled = saving || completed || line.quantity <= 1;
            decrease.addEventListener("click", () => {
                if (saving || completed || line.quantity <= 1) return;
                line.quantity--;
                renderCart();
            });
            const count = element("span", "", String(line.quantity));
            const increase = element("button", "", "+");
            increase.type = "button";
            increase.setAttribute("aria-label", text("Increase quantity", "زيادة الكمية"));
            increase.disabled = saving || completed || line.quantity >= 100;
            increase.addEventListener("click", () => {
                if (saving || completed || line.quantity >= 100) return;
                line.quantity++;
                renderCart();
            });
            quantity.append(decrease, count, increase);
            row.append(quantity);
            row.append(element("span", "pos-cart-unit-price", money(line.price)));
            row.append(element("strong", "pos-cart-line-total", money(line.price * line.quantity)));

            const actions = element("div", "pos-cart-actions");
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
            row.append(actions);
            cartView.append(row);
        });
        const subtotal = items.reduce((sum, line) => sum + line.price * line.quantity, 0);
        const discount = Number(discountInput?.value || 0);
        subtotalView.textContent = money(subtotal);
        totalView.textContent = money(Math.max(0, subtotal -
            (Number.isFinite(discount) && discount >= 0 ? discount : 0)));
        clearCartButton.disabled = saving || completed || items.length === 0;
        saveButton.disabled = saving || completed || !customerId || items.length === 0;
        persistCart();
    }

    clearCartButton.addEventListener("click", () => {
        if (saving || completed || items.length === 0) return;
        items = [];
        renderCart();
    });

    deliveryDate.addEventListener("change", persistCart);
    discountInput?.addEventListener("input", renderCart);

    document.getElementById("openQuickCustomer").addEventListener("click", () => {
        if (saving || completed) return;
        if (customerId && items.length > 0 && !window.confirm(text(
            "Changing customer clears the cart. Continue?",
            "تغيير العميل يمسح عناصر الطلب. متابعة?"))) return;
        quickCustomerError.textContent = "";
        quickCustomerForm.reset();
        const typed = customerMobileSearch.value.trim() || customerSearch.value.trim();
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
            customerMobile = customer.mobile;
            customerSearch.value = customerName;
            customerMobileSearch.value = customer.mobile;
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

    async function searchCustomers(input) {
        if (saving || completed) return;
        if (customerId && items.length > 0) {
            if (!window.confirm(text("Changing customer clears the cart. Continue?",
                "تغيير العميل يمسح عناصر الطلب. متابعة؟"))) {
                customerSearch.value = customerName;
                customerMobileSearch.value = customerMobile;
                return;
            }
            items = [];
            renderCart();
        }
        customerId = 0;
        customerName = "";
        customerMobile = "";
        if (input === customerSearch) customerMobileSearch.value = "";
        else customerSearch.value = "";
        persistCart();
        customerLabel.textContent = text("No customer selected", "لم يتم اختيار عميل");
        saveButton.disabled = true;
        customerResults.replaceChildren();
        customerAbort?.abort();
        const search = input.value.trim();
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
                    customerMobile = customer.mobile;
                    customerSearch.value = name;
                    customerMobileSearch.value = customer.mobile;
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
    }

    customerSearch.addEventListener("input", () => searchCustomers(customerSearch));
    customerMobileSearch.addEventListener("input", () => searchCustomers(customerMobileSearch));

    async function openProduct(productId, index = -1) {
        if (saving || completed) return;
        if (boundInvoiceId) {
            notice(text("Close the saved invoice before adding items.", "أغلق الفاتورة المحفوظة قبل إضافة عناصر."), true);
            return;
        }
        if (index < 0 && items.length >= 30) {
            notice(text("One order supports up to 30 items. Save this order first.",
                "الحد الأقصى للطلب ٣٠ عنصراً. احفظ الطلب أولاً."), true);
            return;
        }
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

            const formGrid = element("div", "pos-dialog-form-grid");
            const quantityField = element("div", "pos-dialog-field");
            const quantityLabel = element("label", "form-label", text("Quantity", "الكمية"));
            const quantity = element("input", "form-control");
            quantity.type = "number"; quantity.min = "1"; quantity.max = "100";
            quantity.value = String(current?.quantity ?? 1); quantity.id = "posQuantity"; quantity.required = true;
            quantityLabel.htmlFor = quantity.id;
            quantityField.append(quantityLabel, quantity);
            formGrid.append(quantityField);

            if (product.sizes.length) {
                const sizeField = element("div", "pos-dialog-field");
                const label = element("label", "form-label", text("Size", "المقاس"));
                label.htmlFor = "posSize";
                const select = element("select", "form-select");
                select.id = "posSize";
                select.required = true;
                select.append(new Option(text("Select a size", "اختر المقاس"), ""));
                product.sizes.forEach(size => select.append(new Option(size.name, String(size.sizeId))));
                if (current?.sizeId) select.value = String(current.sizeId);
                sizeField.append(label, select);
                formGrid.append(sizeField);
            }

            product.measurements.forEach(measurement => {
                const field = element("div", "pos-dialog-field");
                const label = element("label", "form-label",
                    (arabic ? measurement.nameAr || measurement.name : measurement.name) + " (" + measurement.unit + ")");
                const input = element("input", "form-control");
                input.type = "number"; input.min = "0"; input.max = "9999999.99";
                input.step = ".01"; input.required = measurement.isRequired;
                input.dataset.measurementId = String(measurement.measurementId);
                input.dataset.label = measurement.name;
                input.id = "measurement_" + measurement.measurementId;
                const previous = current?.measurements?.find(value => value.measurementId === measurement.measurementId);
                if (previous) input.value = String(previous.value);
                label.htmlFor = input.id;
                field.append(label, input);
                formGrid.append(field);
            });
            dialogFields.append(formGrid);

            const notesLabel = element("label", "form-label mt-2", text("Instructions", "ملاحظات"));
            const notes = element("textarea", "form-control");
            notes.id = "posNotes"; notes.maxLength = 1000; notes.rows = 2;
            notes.value = current?.notes ?? "";
            notesLabel.htmlFor = notes.id;
            dialogFields.append(notesLabel, notes);
            document.getElementById("productDialogBody").scrollTop = 0;
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
        document.getElementById("productDialogBody").scrollTop = 0;
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
        if (boundInvoiceId) {
            notice(text("Close the saved invoice before adding items.", "أغلق الفاتورة المحفوظة قبل إضافة عناصر."), true);
            return;
        }
        if (items.length >= 30) {
            notice(text("One order supports up to 30 items. Save this order first.",
                "الحد الأقصى للطلب ٣٠ عنصراً. احفظ الطلب أولاً."), true);
            return;
        }
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

    function orderTotal() {
        const subtotal = items.reduce((sum, line) => sum + line.price * line.quantity, 0);
        return Math.round((subtotal - Number(discountInput?.value || 0)) * 1000);
    }

    function validateOrder() {
        const date = deliveryDate.value;
        if (!customerId || items.length < 1 || !date) {
            throw new Error(text("Select a customer, delivery date and at least one item.",
                "اختر العميل وتاريخ التسليم وأضف عنصراً."));
        }
        const subtotal = items.reduce((sum, line) => sum + line.price * line.quantity, 0);
        const discountAmount = Number(discountInput?.value || 0);
        if (discountInput && (!discountInput.checkValidity() || !Number.isFinite(discountAmount) ||
            (discountInput.value.trim() !== "" &&
                !/^(\d+(\.\d{1,3})?|\.\d{1,3})$/.test(discountInput.value.trim())) ||
            discountAmount > subtotal)) {
            throw new Error(text("Discount must be between zero and the subtotal with up to three decimals.",
                "يجب أن يكون الخصم بين الصفر والمجموع الفرعي وبحد أقصى ثلاث منازل عشرية."));
        }
        return { date, discountAmount };
    }

    function rememberCheckout() {
        try {
            if (savedOrder || paymentPayload) {
                sessionStorage.setItem(checkoutKey, JSON.stringify({
                    order: savedOrder, previousNumber: boundInvoiceId ? invoiceNumberView.textContent : null,
                    requestId: paymentRequestId, payload: paymentPayload
                }));
            }
        } catch { /* The current request remains available in memory. */ }
    }

    async function saveOrder() {
        if (savedOrder) return savedOrder;
        if (saving || completed || boundInvoiceId) throw new Error("An order is already being saved.");
        const { date, discountAmount } = validateOrder();
        saving = true;
        clearCartButton.disabled = true;
        setBusy(saveButton, true, text("Processing order...", "جار معالجة الطلب..."));
        notice(text("Saving order...", "جار حفظ الطلب..."));
        try {
            const id = await reserveRequest();
            const data = await getJson(shell.dataset.saveUrl, {
                method: "POST",
                headers: { "Content-Type": "application/json", RequestVerificationToken: token },
                body: JSON.stringify({
                    requestId: id, customerId, deliveryDate: date, discountAmount,
                    items: items.map(line => ({
                        itemType: line.itemType, itemId: line.itemId,
                        sizeId: line.sizeId, quantity: line.quantity,
                        notes: line.notes, measurements: line.measurements
                    }))
                })
            });
            savedOrder = data;
            completed = true;
            invoiceNumberView.textContent = data.orderNumber;
            if (discountInput) discountInput.disabled = true;
            try { sessionStorage.removeItem(pendingKey); } catch { /* In-memory fallback. */ }
            rememberCheckout();
            return data;
        } finally {
            saving = false;
            setBusy(saveButton, false);
            saveButton.disabled = completed || !customerId || items.length === 0;
            clearCartButton.disabled = completed || items.length === 0;
        }
    }

    function openPrintTab() {
        const tab = window.open("", "_blank");
        if (tab) {
            tab.document.title = "Preparing invoice";
            tab.document.body.textContent = text("Preparing RDLC invoice...", "جار تجهيز الفاتورة...");
        }
        return tab;
    }

    function printInvoice(orderId, tab) {
        if (!tab || tab.closed) return false;
        tab.location.replace(shell.dataset.printUrl + "?id=" + encodeURIComponent(orderId) + "&paper=80mm");
        return true;
    }

    function finishSale(orderId, tab, savedMessage, isPrevious = false) {
        const printOpened = printInvoice(orderId, tab);
        try {
            sessionStorage.removeItem(checkoutKey);
            if (!isPrevious) {
                sessionStorage.removeItem(pendingKey);
                sessionStorage.removeItem(cartKey);
            }
            sessionStorage.setItem(lastSaleKey, savedMessage +
                (printOpened ? "" : text(" · Print from Orders.", " · اطبع من الطلبات.")));
        } catch { /* No financial change depends on browser storage. */ }
        window.location.reload();
    }

    function setPaidMode(paid) {
        paidMode = paid;
        unpaidButton.className = paid ? "btn btn-outline-primary" : "btn btn-primary";
        paidButton.className = paid ? "btn btn-primary" : "btn btn-outline-primary";
        unpaidButton.setAttribute("aria-pressed", String(!paid));
        paidButton.setAttribute("aria-pressed", String(paid));
        saveButton.hidden = paid;
        unpaidButton.disabled = completed || saving || paymentBusy;
    }

    const paymentInputs = ["posPaymentAmount", "posCashReceived", "posSplitCash",
        "posSplitKnet", "posSplitWamd", "posSplitCashReceived"]
        .map(id => document.getElementById(id));

    function parseMills(value, required = false) {
        const raw = String(value || "").trim();
        if (!raw && !required) return 0;
        if (!/^\d+(\.\d{1,3})?$/.test(raw)) throw new Error(text(
            "Enter a valid KWD amount (up to three decimals).",
            "أدخل مبلغاً صحيحاً بالدينار الكويتي حتى ثلاث منازل عشرية."));
        const [whole, fraction = ""] = raw.split(".");
        const mills = Number(whole) * 1000 + Number(fraction.padEnd(3, "0"));
        if (!Number.isSafeInteger(mills)) throw new Error("Payment amount is too large.");
        return mills;
    }

    function paymentDue() {
        return Math.round(Number(boundInvoiceId ? boundInvoiceBalance :
            savedOrder ? savedOrder.total : orderTotal() / 1000) * 1000);
    }

    function paymentLines() {
        const due = paymentDue();
        const lines = [];
        const add = (method, amount, received = "") => {
            const mills = parseMills(amount);
            const tendered = method === "Cash" && received.trim()
                ? parseMills(received) : mills;
            if (tendered < mills) throw new Error(text(
                "Cash received must be at least the cash payment amount.",
                "يجب أن يكون النقد المستلم مساوياً أو أكبر من مبلغ الدفع النقدي."));
            if (mills > 0) lines.push({
                method, amount: mills / 1000,
                ...(method === "Cash" ? { tendered: tendered / 1000 } : {})
            });
            else if (method === "Cash" && tendered > 0)
                throw new Error("Enter the cash payment amount.");
        };
        if (paymentMethod === "Split") {
            add("Cash", document.getElementById("posSplitCash").value,
                document.getElementById("posSplitCashReceived").value);
            add("KNET", document.getElementById("posSplitKnet").value);
            add("Card", document.getElementById("posSplitWamd").value);
            if (lines.length < 2) throw new Error(text(
                "Enter amounts in at least two payment methods.",
                "أدخل مبالغ في طريقتي دفع على الأقل."));
        } else {
            add(paymentMethod, paymentAmount.value,
                paymentMethod === "Cash" ? document.getElementById("posCashReceived").value : "");
        }
        const total = lines.reduce((sum, line) => sum + Math.round(line.amount * 1000), 0);
        if (total <= 0 || total > due) throw new Error(text(
            "Payment must be greater than zero and cannot exceed the balance.",
            "يجب أن يكون الدفع أكبر من صفر ولا يتجاوز الرصيد."));
        const cash = lines.find(line => line.method === "Cash");
        const change = cash ? Math.round((cash.tendered - cash.amount) * 1000) : 0;
        return { lines, total, change, due };
    }

    function updatePaymentPreview() {
        const due = paymentDue();
        document.getElementById("posPaymentDue").textContent = money(due / 1000);
        let total = 0;
        let change = 0;
        let error = "";
        try {
            const result = paymentPayload
                ? { total: paymentPayload.lines.reduce((sum, line) => sum + Math.round(line.amount * 1000), 0),
                    change: Math.round(paymentPayload.lines.reduce((sum, line) =>
                        sum + (line.method === "Cash" ? (line.tendered - line.amount) : 0), 0) * 1000) }
                : paymentLines();
            total = result.total;
            change = result.change;
        } catch (exception) {
            if (paymentInputs.some(input => input.value.trim())) error = exception.message;
        }
        document.getElementById("posPaymentEntered").textContent = money(total / 1000);
        document.getElementById("posPaymentRemaining").textContent = money(Math.max(0, due - total) / 1000);
        document.getElementById("posPaymentChange").textContent = money(change / 1000);
        paymentError.textContent = paymentPayload ? text(
            "Retrying the original payment request to prevent duplicates.",
            "تتم إعادة محاولة طلب الدفع نفسه لمنع التكرار.") : error;
        paymentConfirm.disabled = paymentBusy || (total <= 0 && !paymentPayload) || !!error;
    }

    function setPaymentMethod(method) {
        if (paymentBusy || (paymentPayload && method !== paymentMethod)) return;
        paymentMethod = method;
        document.querySelectorAll("[data-pos-method]").forEach(button => {
            const active = button.dataset.posMethod === method;
            button.className = active ? "btn btn-primary" : "btn btn-outline-primary";
            button.setAttribute("aria-pressed", String(active));
        });
        document.getElementById("posSinglePayment").hidden = method === "Split";
        document.getElementById("posSplitPayment").hidden = method !== "Split";
        document.getElementById("posCashReceivedRow").hidden = method !== "Cash";
        updatePaymentPreview();
    }

    function openPaymentDialog() {
        if (saving || paymentBusy || (!boundInvoiceId && completed && !savedOrder)) return;
        if (!boundInvoiceId && !savedOrder) {
            try { validateOrder(); } catch (error) { notice(error.message, true); return; }
        }
        if (boundInvoiceId && boundInvoiceBalance <= 0 && !paymentPayload) return;
        if (!boundInvoiceId && paymentDue() <= 0) {
            notice(text("No payment is due. Use UNPAID to save this order.",
                "لا يوجد مبلغ مستحق. احفظ الفاتورة دون دفع."), true);
            return;
        }
        if (!paymentPayload) {
            paymentMethod = "Cash";
            paymentInputs.forEach(input => { input.value = ""; input.disabled = false; });
            paymentAmount.value = (paymentDue() / 1000).toFixed(3);
        } else paymentInputs.forEach(input => { input.disabled = true; });
        setPaymentMethod(paymentMethod);
        document.getElementById("posPaymentTitle").textContent = boundInvoiceId
            ? text("Receive Balance", "تحصيل المتبقي") : text("Collect Payment", "تحصيل الدفع");
        updatePaymentPreview();
        paymentDialog.showModal();
    }

    unpaidButton.addEventListener("click", () => {
        if (completed || paymentBusy) return;
        setPaidMode(false);
    });
    paidButton.addEventListener("click", () => {
        setPaidMode(true);
        openPaymentDialog();
    });
    document.getElementById("boundInvoicePayment").addEventListener("click", openPaymentDialog);
    document.getElementById("closePaymentDialog").addEventListener("click", () => {
        if (!paymentBusy) paymentDialog.close();
    });
    paymentDialog.addEventListener("cancel", event => {
        if (paymentBusy) event.preventDefault();
    });
    paymentDialog.addEventListener("close", () => {
        if (!savedOrder && !paymentPayload && !boundInvoiceId) setPaidMode(false);
    });
    document.querySelectorAll("[data-pos-method]").forEach(button =>
        button.addEventListener("click", () => setPaymentMethod(button.dataset.posMethod)));
    paymentInputs.forEach(input => input.addEventListener("input", updatePaymentPreview));

    paymentConfirm.addEventListener("click", async () => {
        if (paymentBusy || paymentConfirm.disabled) return;
        let lines;
        try { lines = paymentPayload?.lines ?? paymentLines().lines; }
        catch (error) { paymentError.textContent = error.message; return; }

        const tab = openPrintTab();
        paymentBusy = true;
        setBusy(paymentConfirm, true, text("Processing payment...", "جار حفظ الدفعة..."));
        paymentError.textContent = "";
        try {
            const order = boundInvoiceId ? { orderId: boundInvoiceId,
                orderNumber: invoiceNumberView.textContent } : await saveOrder();
            if (!paymentPayload) {
                if (!paymentRequestId) {
                    const prepared = await getJson(shell.dataset.paymentPrepareUrl, {
                        method: "POST",
                        headers: { "Content-Type": "application/json", RequestVerificationToken: token },
                        body: JSON.stringify(order.orderId)
                    });
                    paymentRequestId = prepared.requestId;
                    rememberCheckout();
                }
                paymentPayload = { requestId: paymentRequestId, orderId: order.orderId, lines };
                rememberCheckout();
            }
            const result = await getJson(shell.dataset.paymentRecordUrl, {
                method: "POST",
                headers: { "Content-Type": "application/json", RequestVerificationToken: token },
                body: JSON.stringify(paymentPayload)
            });
            paymentDialog.close();
            finishSale(order.orderId, tab, order.orderNumber + " · " +
                text(result.balance > 0 ? "Partially paid" : "Paid",
                    result.balance > 0 ? "مدفوع جزئياً" : "مدفوع"), !!boundInvoiceId);
        } catch (error) {
            if (tab && !tab.closed) tab.close();
            paymentError.textContent = error instanceof TypeError
                ? text("Unable to confirm. Retry Confirm Payment without changing amounts.",
                    "تعذر التأكد من الدفع. أعد المحاولة بنفس المبالغ.")
                : error.message;
            notice(paymentError.textContent, true);
        } finally {
            paymentBusy = false;
            setBusy(paymentConfirm, false);
            updatePaymentPreview();
        }
    });

    form.addEventListener("submit", async event => {
        event.preventDefault();
        if (paidMode || saving || completed || boundInvoiceId) return;
        try { validateOrder(); } catch (error) { notice(error.message, true); return; }
        const tab = openPrintTab();
        try {
            const order = await saveOrder();
            finishSale(order.orderId, tab, text("Saved unpaid order: ", "تم حفظ طلب غير مدفوع: ") +
                order.orderNumber);
        } catch (error) {
            if (tab && !tab.closed) tab.close();
            notice(error instanceof TypeError
                ? text("Save status could not be confirmed. Retry Save safely; do not start a new order.",
                    "تعذر التأكد من الحفظ. أعد المحاولة بأمان ولا تبدأ طلباً جديداً.")
                : error.message, true);
        }
    });

    setPaidMode(paidMode);
    if (savedOrder) {
        invoiceNumberView.textContent = savedOrder.orderNumber;
        notice(text("Payment pending for ", "الدفع معلق للفاتورة ") +
            savedOrder.orderNumber + text(". Click PAID to retry.", ". اضغط PAID لإعادة المحاولة."), true);
    } else if (recoveryInvoiceNumber) {
        invoiceInput.value = recoveryInvoiceNumber;
        getJson(shell.dataset.invoiceUrl + "?number=" + encodeURIComponent(recoveryInvoiceNumber))
            .then(invoice => {
                showInvoice(invoice);
                notice(text("Previous payment needs confirmation. Use Receive Balance to retry safely.",
                    "يحتاج الدفع السابق إلى تأكيد. أعد المحاولة من تحصيل المتبقي."), true);
            })
            .catch(error => notice(error.message, true));
    } else {
        try {
            const status = sessionStorage.getItem(lastSaleKey);
            if (status) {
                sessionStorage.removeItem(lastSaleKey);
                notice(status, false, true);
            }
        } catch { /* Storage unavailable. */ }
    }
    renderCart();
})();
