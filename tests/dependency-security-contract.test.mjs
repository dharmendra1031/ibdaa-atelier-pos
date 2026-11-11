import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const project = readFileSync(
    new URL("../src/IbdaaAtelier.Web/IbdaaAtelier.Web.csproj", import.meta.url), "utf8");

test("patched PKCS package replaces vulnerable transitive 9.0.11 dependency", () => {
    assert.match(project, /System.Security.Cryptography.Pkcs" Version="10.0.13"/);
    assert.doesNotMatch(project, /System.Security.Cryptography.Pkcs" Version="9.0.11"/);
});
