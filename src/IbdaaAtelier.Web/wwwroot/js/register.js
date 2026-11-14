(() => {
    const form = document.getElementById("cashExpenseForm");
    if (form) {
        const requestId = document.getElementById("expenseRequestId");
        const submit = document.getElementById("saveCashExpense");
        const error = document.getElementById("expenseError");
        let submitting = false;

        form.addEventListener("submit", async event => {
            if (submitting) {
                event.preventDefault();
                return;
            }
            if (!form.reportValidity()) {
                event.preventDefault();
                return;
            }
            if (requestId.value) {
                submitting = true;
                submit.disabled = true;
                return;
            }

            event.preventDefault();
            submit.disabled = true;
            error.hidden = true;
            try {
                const response = await fetch(form.dataset.reserveUrl, {
                    method: "POST",
                    body: new FormData(form)
                });
                const result = await response.json().catch(() => ({}));
                if (!response.ok || !Number.isSafeInteger(result.requestId) || result.requestId <= 0)
                    throw new Error(result.error || "Unable to prepare this expense. Please retry.");

                requestId.value = String(result.requestId);
                submit.disabled = false;
                form.requestSubmit(submit);
            } catch (exception) {
                error.textContent = exception.message;
                error.hidden = false;
                submit.disabled = false;
            }
        });

        window.addEventListener("pageshow", () => {
            submitting = false;
            submit.disabled = false;
        });
    }

    const closeForm = document.getElementById("closeRegisterForm");
    closeForm?.addEventListener("submit", event => {
        if (!closeForm.reportValidity()) return;
        const message = document.documentElement.lang === "ar"
            ? "هل تريد إغلاق الصندوق بالمبلغ المدخل؟"
            : "Close the register with the entered cash count?";
        if (!window.confirm(message)) event.preventDefault();
    });
})();
