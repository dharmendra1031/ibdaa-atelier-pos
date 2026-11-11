import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const program = read("src/IbdaaAtelier.Web/Program.cs");
const auth = read("src/IbdaaAtelier.Web/Controllers/AuthController.cs");
const admin = read("src/IbdaaAtelier.Web/Controllers/AdminController.cs");
const layout = read("src/IbdaaAtelier.Web/Views/Shared/_Layout.cshtml");
const sql = read("database/003_user_authentication.sql");

test("numeric user and audit IDs with guarded, manual SQL setup", () => {
    assert.match(sql, /UserId INT IDENTITY\(1,1\)/);
    assert.match(sql, /AuditId BIGINT IDENTITY\(1,1\)/);
    assert.match(sql, /DB_NAME\(\) <> @ExpectedDatabase/);
    assert.match(sql, /UX_Users_UserName/);
    assert.doesNotMatch(sql, /UNIQUEIDENTIFIER|NEWID\s*\(/i);
    assert.doesNotMatch(sql, /INSERT INTO dbo\.Users/i);
});

test("cookies are secured and disabled accounts rejected on server", () => {
    assert.match(program, /CookieSecurePolicy\.Always/);
    assert.match(program, /Cookie\.HttpOnly = true/);
    assert.match(program, /OnValidatePrincipal/);
    assert.match(program, /!user\.IsActive/);
    assert.match(program, /UseAuthentication\(\)/);
    assert.match(program, /UseAuthorization\(\)/);
});

test("passwords use ASP.NET password hasher and one-time bootstrap", () => {
    assert.match(program, /IPasswordHasher<ShopUser>/);
    assert.match(program, /hasher\.HashPassword\(admin, password\)/);
    assert.match(program, /--bootstrap-admin/);
    assert.match(program, /db\.Users\.AnyAsync\(\)/);
    assert.match(auth, /VerifyHashedPassword/);
    assert.doesNotMatch(program, /PasswordHash\s*=\s*password/);
});

test("login and logout reject CSRF and safe redirect prevents open redirect", () => {
    assert.match(auth, /ValidateAntiForgeryToken/);
    assert.match(auth, /SignInAsync/);
    assert.match(auth, /SignOutAsync/);
    assert.match(auth, /Url\.IsLocalUrl\(form\.ReturnUrl\)/);
    assert.match(auth, /FailedLoginCount >= 5/);
    assert.match(admin, /Authorize\(Roles = "Admin"\)/);
    assert.match(layout, /asp-antiforgery="true"/);
});
