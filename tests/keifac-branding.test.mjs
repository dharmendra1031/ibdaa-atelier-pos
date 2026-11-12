import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");

test("shared footer displays KEIFAC vendor branding across all MVC pages", () => {
    const layout = read("src/IbdaaAtelier.Web/Views/Shared/_Layout.cshtml");
    const viewStart = read("src/IbdaaAtelier.Web/Views/_ViewStart.cshtml");
    const css = read("src/IbdaaAtelier.Web/wwwroot/css/site.css");
    const image = readFileSync(new URL("../src/IbdaaAtelier.Web/wwwroot/images/keifac-logo.png", import.meta.url));

    assert.match(viewStart, /Layout = "_Layout"/);
    assert.match(layout, /<footer class="site-footer">[\s\S]*class="keifac-credit"/);
    assert.match(layout, /Powered by/);
    assert.match(layout, /aria-label="Powered by KEIFAC"/);
    assert.match(layout, /https:\/\/www\.keifac\.com\//);
    assert.match(layout, /src="~\/images\/keifac-logo\.png" asp-append-version="true"/);
    assert.match(layout, /© @DateTime\.UtcNow\.Year IBDAA Atelier/);
    assert.match(css, /\.site-footer \.keifac-credit/);
    assert.deepEqual([...image.subarray(0, 8)], [137, 80, 78, 71, 13, 10, 26, 10]);
    assert.ok(image.length > 1000);
});
