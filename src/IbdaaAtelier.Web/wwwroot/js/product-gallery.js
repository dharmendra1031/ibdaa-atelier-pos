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
