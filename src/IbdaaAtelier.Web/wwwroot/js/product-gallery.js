document.querySelectorAll(".image-upload-input").forEach(input => {
    const preview = input.parentElement.querySelector(".image-preview");
    if (!preview) return;

    let previewUrl;
    input.addEventListener("change", () => {
        if (previewUrl) URL.revokeObjectURL(previewUrl);
        const file = input.files?.[0];
        preview.hidden = !file;
        if (!file) {
            preview.removeAttribute("src");
            return;
        }

        previewUrl = URL.createObjectURL(file);
        preview.src = previewUrl;
    });
    window.addEventListener("pagehide", () => {
        if (previewUrl) URL.revokeObjectURL(previewUrl);
    }, { once: true });
});

// Confirm irreversible photo deletion before posting the existing form.
document.querySelectorAll(".image-delete-form").forEach(form => {
    form.addEventListener("submit", event => {
        const message = document.documentElement.lang === "ar"
            ? "هل تريد حذف هذه الصورة؟ لا يمكن التراجع عن ذلك."
            : "Remove this photograph? This cannot be undone.";
        if (!window.confirm(message)) event.preventDefault();
    });
});
