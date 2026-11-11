(() => {
    const form = document.getElementById("receivePaymentForm");
    if (!form) return;

    const amounts = [...form.querySelectorAll(".payment-amount")];
    const cash = document.getElementById("amount_0");
    const tendered = document.getElementById("cashTendered");
    const total = document.getElementById("paymentTotal");
    const change = document.getElementById("cashChange");
    const save = document.getElementById("savePayment");
    const fill = document.getElementById("fillPaymentBalance");
    const method = document.getElementById("quickPaymentMethod");
    const value = element => Number(element?.value) || 0;

    function update() {
        total.textContent = amounts.reduce((sum, input) => sum + value(input), 0).toFixed(3) + " KWD";
        if (cash && tendered)
            change.textContent = Math.max(0, value(tendered) - value(cash)).toFixed(3);
    }

    [...amounts, tendered].filter(Boolean).forEach(input =>
        input.addEventListener("input", update));

    fill?.addEventListener("click", () => {
        const balance = Number(form.dataset.balance);
        if (!Number.isFinite(balance) || balance <= 0) return;
        amounts.forEach(input => {
            input.value = input.dataset.method === method.value ? balance.toFixed(3) : "0.000";
        });
        if (tendered) tendered.value = "";
        update();
    });

    form.addEventListener("submit", () => {
        if (!form.checkValidity()) return;
        save.disabled = true;
    });
    update();
})();
