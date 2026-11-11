import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");

const project = read("src/IbdaaAtelier.Web/IbdaaAtelier.Web.csproj");
const program = read("src/IbdaaAtelier.Web/Program.cs");
const layout = read("src/IbdaaAtelier.Web/Views/Shared/_Layout.cshtml");
const language = read("src/IbdaaAtelier.Web/Controllers/LanguageController.cs");
const css = read("src/IbdaaAtelier.Web/wwwroot/css/site.css");
const bootstrap = read("src/IbdaaAtelier.Web/wwwroot/lib/bootstrap/css/bootstrap.min.css");
const bootstrapRtl = read("src/IbdaaAtelier.Web/wwwroot/lib/bootstrap/css/bootstrap.rtl.min.css");
const home = read("src/IbdaaAtelier.Web/Views/Home/Index.cshtml");
const catalogue = read("src/IbdaaAtelier.Web/Views/Catalog/Index.cshtml");

test("P0 uses .NET 10 and a compatible pinned SQL Server provider", () => {
    assert.match(project, /<TargetFramework>net10\.0<\/TargetFramework>/);
    assert.match(project, /Microsoft\.EntityFrameworkCore\.SqlServer" Version="10\.0\.14"/);
    assert.doesNotMatch(project, /Version="8\./);
});

test("Bootstrap 5.3.8 loads locally without a runtime CDN", () => {
    assert.match(bootstrap.slice(0, 300), /Bootstrap\s+v5\.3\.8/);
    assert.match(bootstrapRtl.slice(0, 300), /Bootstrap\s+v5\.3\.8/);
    assert.match(layout, /~\/lib\/bootstrap\/css\/bootstrap\.min\.css/);
    assert.match(layout, /~\/lib\/bootstrap\/css\/bootstrap\.rtl\.min\.css/);
    assert.match(layout, /~\/css\/site\.css/);
    assert.doesNotMatch(layout, /https?:\/\/|jquery|highcharts|bootstrap\.bundle/i);
});

test("English/Arabic switching uses a validated POST and local return URL", () => {
    assert.match(layout, /html lang=".*isArabic/);
    assert.match(layout, /dir=".*rtl.*ltr/);
    assert.match(layout, /asp-antiforgery="true"/);
    assert.match(layout, /asp-controller="Language"/);
    assert.match(program, /UseRequestLocalization\(localization\)/);
    assert.match(program, /CookieRequestCultureProvider/);
    assert.match(language, /\[ValidateAntiForgeryToken\]/);
    assert.match(language, /culture is not \("en-US" or "ar-KW"\)/);
    assert.match(language, /Url\.IsLocalUrl\(returnUrl\)/);
    assert.doesNotMatch(layout, /@Html\.AntiForgeryToken\(\)/);
});

test("The shell has visible, real navigation and no fake billing button", () => {
    assert.match(layout, /asp-controller="Home"/);
    assert.match(layout, /asp-controller="Catalog"/);
    assert.match(layout, /href="#mainContent"/);
    assert.match(home, /not enabled yet/);
    assert.match(catalogue, /not enabled yet/);
    assert.match(layout, /asp-controller="Pos"/);
    assert.doesNotMatch(layout, /asp-controller="Billing"/);
});

test("Responsive touch-friendly catalogue uses optimized layout primitives", () => {
    assert.match(layout, /container-xxl/);
    assert.match(catalogue, /row-cols-2 row-cols-md-3 row-cols-xl-4/);
    assert.match(catalogue, /loading="lazy"/);
    assert.match(catalogue, /width="360" height="450"/);
    assert.match(catalogue, /CultureInfo\.InvariantCulture/);
    assert.match(css, /min-height: 44px/);
    assert.match(css, /max-width: 767\.98px/);
    assert.match(css, /\[dir="rtl"\]/);
});
