(() => {
    const form = document.getElementById("receivePaymentForm");
    if (!form) return;

    document.querySelectorAll(".order-feedback .btn-close").forEach(button =>
        button.addEventListener("click", () => { button.parentElement.hidden = true; }));

    const amounts = [...form.querySelectorAll(".payment-amount")];
    const cash = document.getElementById("amount_0");
    const tendered = document.getElementById("cashTendered");
    const total = document.getElementById("paymentTotal");
    const change = document.getElementById("cashChange");
    const save = document.getElementById("savePayment");
    const error = document.getElementById("paymentError");
    const fill = document.getElementById("fillPaymentBalance");
    const method = document.getElementById("quickPaymentMethod");
    const value = element => Number(element?.value) || 0;

    function update() {
        total.textContent = amounts.reduce((sum, input) => sum + value(input), 0).toFixed(3) + " KWD";
        if (cash && tendered)
            change.textContent = Math.max(0, value(tendered) - value(cash)).toFixed(3);
    }

    [...amounts, tendered].filter(Boolean).forEach(input =>
        input.addEventListener("input", () => {
            error.hidden = true;
            update();
        }));

    fill?.addEventListener("click", () => {
        const balance = Number(form.dataset.balance);
        if (!Number.isFinite(balance) || balance <= 0) return;
        amounts.forEach(input => {
            input.value = input.dataset.method === method.value ? balance.toFixed(3) : "0.000";
        });
        if (tendered) tendered.value = "";
        update();
    });

    const saveLabel = save.textContent.trim();
    form.addEventListener("submit", event => {
        if (save.disabled) {
            event.preventDefault();
            return;
        }
        if (!form.reportValidity()) {
            event.preventDefault();
            return;
        }
        const amount = amounts.reduce((sum, input) => sum + value(input), 0);
        const balance = Number(form.dataset.balance);
        if (!Number.isFinite(balance) || amount <= 0 || amount > balance + 0.000001) {
            event.preventDefault();
            error.textContent = document.documentElement.lang === "ar"
                ? "يجب أن يكون المبلغ أكبر من صفر ولا يتجاوز الرصيد المتبقي."
                : "Payment must be greater than zero and cannot exceed the balance.";
            error.hidden = false;
            amounts.find(input => value(input) > 0)?.focus();
            return;
        }
        error.hidden = true;
        save.disabled = true;
        save.setAttribute("aria-busy", "true");
        const spinner = document.createElement("span");
        spinner.className = "spinner-border spinner-border-sm me-2";
        spinner.setAttribute("aria-hidden", "true");
        save.replaceChildren(spinner, document.createTextNode(
            document.documentElement.lang === "ar" ? "جار حفظ الدفعة..." : "Processing payment..."));
    });
    window.addEventListener("pageshow", () => {
        save.disabled = false;
        save.setAttribute("aria-busy", "false");
        save.textContent = saveLabel;
    });
    update();
})();
