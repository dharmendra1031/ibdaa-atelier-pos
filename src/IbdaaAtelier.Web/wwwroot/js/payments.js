(() => {
    const form = document.getElementById("receivePaymentForm");
    if (!form) return;

    const amounts = [...form.querySelectorAll(".payment-amount")];
    const cash = document.getElementById("amount_0");
    const tendered = document.getElementById("cashTendered");
    const total = document.getElementById("paymentTotal");
    const change = document.getElementById("cashChange");
    const save = document.getElementById("savePayment");
    const value = element => Number(element?.value) || 0;

    function update() {
        total.textContent = amounts.reduce((sum, input) => sum + value(input), 0).toFixed(3) + " KWD";
        if (cash && tendered)
            change.textContent = Math.max(0, value(tendered) - value(cash)).toFixed(3);
    }

    [...amounts, tendered].filter(Boolean).forEach(input =>
        input.addEventListener("input", update));

    form.addEventListener("submit", () => { save.disabled = true; });
    update();
})();
