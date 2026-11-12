import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const seed = read("database/029_demo_seed.sql");
const details = read("database/030_demo_media_and_details.sql");
const upgrade = read("database/037_replace_ladies_test_catalog.sql");

test("official site-based seed contains exactly sixteen menswear products", () => {
    const productSection = seed.split("FROM (VALUES")[1].split(") item(CategoryName")[0];
    const rows = productSection.split("\n").filter(row => row.trim().startsWith("(N'"));
    assert.equal(rows.length, 16);
    const expected = [
        ['قندورة "سلطان"', "23.000"],
        ["قفطان الزمرد", "26.000"],
        ['قندورة "أصيل"', "25.000"],
        ["قندورة نسيج الغسق", "24.000"],
        ['جابادور "أمير الأناقة"', "23.000"],
        ["جابادور الأزرق الكشخة", "25.000"],
        ['قفطان "نسمات الأفق"', "24.000"],
        ['قندورة "هيبة صيف"', "25.000"],
        ['جلابة "نسيم كازا"', "30.000"],
        ['جابادور "إبداع" المغربي', "24.000"],
        ["قندورة الهبة", "23.000"],
        ["جلابية الحضور", "30.000"],
        ['جابادور "الأفق الأزرق"', "26.000"],
        ["عرض الصيف (٢قندورة+قفطان)", "75.000, 66.000"],
        ['"أناقة الأب والابن" بلمسة مغربية', "51.000"],
        ['"نسمات الصيف" Summer Breeze', "19.000"]
    ];
    for (const [name, price] of expected) {
        const row = rows.find(line => line.includes("N'" + name + "'"));
        assert.ok(row, "Missing product " + name);
        assert.ok(row.includes(price), "Incorrect listed price for " + name);
    }
});

test("no female example products, artificial customer profiles or guessed service prices", () => {
    assert.doesNotMatch(seed, /Classic Black Abaya|Evening Dress|Matching Scarf|Dress hem|Abaya hem/);
    assert.doesNotMatch(seed, /Demo Customer|Demo address|Synthetic test record/);
    assert.doesNotMatch(seed, /INSERT dbo\.Customers|INSERT dbo\.AlterationServices/);
    assert.match(seed, /N'Gandouras'/);
    assert.match(seed, /N'Jabadors'/);
    assert.match(seed, /https:\/\/www\.ibdaaatelier\.com\/products/);
});

test("gents details only use measurement definitions from SRS, not guessed chart values or images", () => {
    for (const field of ["Chest", "Shoulder", "Sleeve Length", "Total Length"])
        assert.ok(details.includes("N'" + field + "'"), "Missing " + field);
    assert.match(details, /N'cm', 0, field\.SortOrder/);
    assert.doesNotMatch(details, /INSERT dbo\.ProductImages|INSERT dbo\.SizeChartValues/);
    assert.doesNotMatch(details, /\/uploads\/products\/.*demo\.png/);
});

test("existing TEST catalogue migration does not change invoices or reseed numeric IDs", () => {
    assert.match(upgrade, /DB_NAME\(\) <> N'IBDAA_TEST'/);
    assert.match(upgrade, /UPDATE dbo\.Products SET IsActive = 0/);
    assert.match(upgrade, /WHERE NOT EXISTS \(SELECT 1 FROM dbo\.Products existing WHERE existing\.NameAr = item\.NameAr\)/);
    assert.match(upgrade, /UPDATE definition SET IsActive = 0/);
    assert.match(upgrade, /INSERT dbo\.MeasurementDefinitions/);
    assert.match(upgrade, /NOT EXISTS \(\s*SELECT 1 FROM dbo\.Orders history WHERE history\.CustomerId = customer\.CustomerId/);
    assert.doesNotMatch(upgrade, /DELETE FROM dbo\.Orders|UPDATE dbo\.Orders|TRUNCATE TABLE|DBCC CHECKIDENT/);
});
