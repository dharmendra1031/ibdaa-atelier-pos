import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const controller = read("src/IbdaaAtelier.Web/Controllers/UsersController.cs");
const forms = read("src/IbdaaAtelier.Web/Models/Security/UserForms.cs");
const login = read("src/IbdaaAtelier.Web/Controllers/AuthController.cs");
const program = read("src/IbdaaAtelier.Web/Program.cs");
const migration = read("database/019_user_session_version.sql");

test("user management is admin-only and protected by antiforgery", () => {
    assert.match(controller, /Authorize\(Roles = "Admin"\)/);
    assert.equal((controller.match(/ValidateAntiForgeryToken/g) ?? []).length, 3);
    assert.match(controller, /PasswordHasher|IPasswordHasher<ShopUser>/);
    assert.match(controller, /hasher\.HashPassword/);
    assert.match(controller, /User\.Create/);
    assert.match(controller, /User\.ResetPassword/);
});

test("avoid disabling or demoting the final administrator", () => {
    assert.match(controller, /currentUserId == form\.UserId/);
    assert.match(controller, /CountAsync\(u => u\.Role == "Admin" && u\.IsActive/);
    assert.match(controller, /IsolationLevel\.Serializable/);
    assert.match(controller, /UPDLOCK, HOLDLOCK/);
});

test("password reset changes a numeric authentication version", () => {
    assert.match(migration, /AuthVersion INT NOT NULL/);
    assert.match(migration, /DB_NAME\(\) <> @ExpectedDatabase/);
    assert.match(controller, /user\.AuthVersion\+\+/);
    assert.match(login, /new Claim\("auth_version"/);
    assert.match(program, /FindFirstValue\("auth_version"\)/);
    assert.doesNotMatch(migration, /UNIQUEIDENTIFIER|NEWID\(|UUID/i);
});

test("new users need a strong confirmed password and a controlled role", () => {
    assert.match(forms, /StringLength\(128, MinimumLength = 12\)/);
    assert.match(forms, /Compare\(nameof\(Password\)\)/);
    assert.match(controller, /role is "Admin" or "Cashier"/);
    assert.match(controller, /u\.UserName == form\.UserName/);
});
