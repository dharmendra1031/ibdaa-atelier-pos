import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");

test("KEIFAC credit stays in Admin navigation without duplicating the POS footer", () => {
    const layout = read("src/IbdaaAtelier.Web/Views/Shared/_Layout.cshtml");
    const credit = read("src/IbdaaAtelier.Web/Views/Shared/_KeifacCredit.cshtml");
    const viewStart = read("src/IbdaaAtelier.Web/Views/_ViewStart.cshtml");
    const css = read("src/IbdaaAtelier.Web/wwwroot/css/site.css");
    const image = readFileSync(new URL("../src/IbdaaAtelier.Web/wwwroot/images/keifac-logo.png", import.meta.url));

    assert.match(viewStart, /Layout = "_Layout"/);
    assert.match(layout, /@if \(User\.IsInRole\("Admin"\)\)/);
    assert.match(layout, /<div class="sidebar-keifac-credit"><partial name="_KeifacCredit" \/><\/div>/);
    assert.match(layout, /@if \(controller != "Pos"\)\s*\{\s*<footer class="site-footer"/);
    assert.match(credit, /Powered by/);
    assert.match(credit, /https:\/\/www\.keifac\.com\//);
    assert.match(credit, /src="~\/images\/keifac-logo\.png" asp-append-version="true"/);
    assert.match(layout, /© @DateTime\.UtcNow\.Year IBDAA Atelier/);
    assert.match(css, /\.sidebar-keifac-credit/);
    assert.deepEqual([...image.subarray(0, 8)], [137, 80, 78, 71, 13, 10, 26, 10]);
    assert.ok(image.length > 1000);
});
