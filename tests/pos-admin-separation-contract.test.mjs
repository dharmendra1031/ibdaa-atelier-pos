import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");

test("admin signs into management and cashier signs into billing", () => {
    const auth = read("src/IbdaaAtelier.Web/Controllers/AuthController.cs");
    assert.match(auth, /user\.Role == "Admin" \? "Admin" : "Pos"/);
    assert.match(auth, /User\.IsInRole\("Admin"\) \? "Admin" : "Pos"/);
});

test("only cashiers can reach POS order and payment endpoints", () => {
    const pos = read("src/IbdaaAtelier.Web/Controllers/PosController.cs");
    const payment = read("src/IbdaaAtelier.Web/Controllers/PaymentsController.cs");
    assert.match(pos, /\[Authorize\(Roles = "Cashier"\)\]/);
    assert.match(payment, /\[Authorize\(Roles = "Cashier"\)\]/);
    assert.doesNotMatch(pos, /\[Authorize\(Roles = "Admin,Cashier"\)\]/);
    assert.doesNotMatch(payment, /\[Authorize\(Roles = "Admin,Cashier"\)\]/);
});

test("admin UI has no POS shortcut; cashier still sees POS navigation", () => {
    const nav = read("src/IbdaaAtelier.Web/Views/Shared/_Navigation.cshtml");
    const dashboard = read("src/IbdaaAtelier.Web/Views/Admin/Index.cshtml");
    const posView = read("src/IbdaaAtelier.Web/Views/Pos/Index.cshtml");
    const orderView = read("src/IbdaaAtelier.Web/Views/Orders/Details.cshtml");
    assert.match(nav, /@if \(!isAdmin\)[\s\S]*?asp-controller="Pos"/);
    assert.doesNotMatch(dashboard, /asp-controller="Pos"/);
    assert.doesNotMatch(posView, /asp-controller="Admin"/);
    assert.match(orderView, /User\.IsInRole\("Cashier"\) && Model\.Balance > 0/);
    assert.match(nav, /asp-controller="Orders"/);
    assert.match(nav, /asp-controller="Register"/);
});
