"""One-time, repeat-safe IBDAA_TEST import of the public IBDAA catalogue."""
import concurrent.futures
import json
import re
import subprocess
import tempfile
import urllib.request
from datetime import datetime, timezone
from decimal import Decimal
from pathlib import Path
from urllib.parse import urlparse

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "src/IbdaaAtelier.Web/wwwroot"
SITE = "https://ibdaaatelier.com"
API = "https://api.ibdaaatelier.com"
DB = "IBDAA_TEST"


def get(url, headers=None):
    with urllib.request.urlopen(urllib.request.Request(url, headers=headers or {}), timeout=45) as response:
        return response.read(), response.headers.get_content_type()


def literal(value):
    if value is None:
        return "NULL"
    return "N'" + str(value).replace("'", "''") + "'"


def fetch_image(task):
    product, index, url = task
    parsed = urlparse(url)
    if parsed.hostname != "api.ibdaaatelier.com" or not parsed.path.startswith("/storage/v1/object/public/product-images/"):
        raise ValueError(f"Unexpected image URL: {url}")
    extension = Path(parsed.path).suffix.lower()
    if extension not in {".jpg", ".jpeg", ".png", ".webp"}:
        raise ValueError(f"Unsupported image type: {url}")
    relative = f"/uploads/products/site-{product['id']}/{index:02d}{extension}"
    target = WEB / relative.lstrip("/")
    if not target.exists() or target.stat().st_size == 0:
        try:
            data, mime = get(url)
        except Exception as error:
            raise RuntimeError(f"Image download failed: {url}") from error
        if not mime.startswith("image/") or not data:
            raise ValueError(f"Invalid image response: {url}")
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)
    return product["id"], index, relative


def main():
    html, _ = get(SITE)
    bundle_path = re.search(rb'src="(/assets/index-[^\"]+\.js)"', html).group(1).decode()
    bundle, _ = get(SITE + bundle_path)
    # The site's public client key is used only in memory, never written to disk.
    key = re.search(rb'eyJhbGciOiJIUzI1Ni[^"\']+', bundle).group().decode()
    headers = {"apikey": key, "Authorization": "Bearer " + key}
    products = json.loads(get(API + "/rest/v1/products?select=id,name_en,name_ar,description_en,description_ar,price,compare_at_price,images,category_id,sort_order,is_active&is_active=eq.true&order=sort_order.asc", headers)[0])
    categories = json.loads(get(API + "/rest/v1/categories?select=id,name_ar,slug,is_active", headers)[0])
    if len(products) < 30 or any(not p.get("images") or p.get("price") is None for p in products):
        raise ValueError("Catalogue is unexpectedly incomplete; database was not changed")
    category_by_id = {c["id"]: c for c in categories}
    if any(p["category_id"] not in category_by_id for p in products):
        raise ValueError("Missing category metadata")
    tasks = [(p, i, url) for p in products for i, url in enumerate(p["images"])]
    images = []
    failed = []
    with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
        futures = {pool.submit(fetch_image, task): task for task in tasks}
        for future in concurrent.futures.as_completed(futures):
            try:
                images.append(future.result())
            except Exception as error:
                failed.append(str(error))
    if failed:
        print(f"Unavailable source images ({len(failed)}):")
        for error in failed:
            print(error)
    if any(not any(image[0] == p["id"] for image in images) for p in products):
        raise ValueError("A product has no downloadable image; database was not changed")
    paths = {(pid, index): path for pid, index, path in images}

    lines = [
        "SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON;",
        "IF DB_NAME() <> N'IBDAA_TEST' THROW 51360, 'TEST database only.', 1;",
        "BEGIN TRANSACTION;",
        "IF OBJECT_ID(N'dbo.WebsiteProductImportMap', N'U') IS NULL CREATE TABLE dbo.WebsiteProductImportMap (SourceProductId uniqueidentifier NOT NULL PRIMARY KEY, ProductId int NOT NULL UNIQUE REFERENCES dbo.Products(ProductId), ImportedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME());",
    ]
    category_names = {
        "المغربي التقليدي": "Traditional Moroccan",
        "المغربي الكاجوال": "Casual Moroccan",
        "كاجوال ترند": "Casual Trend",
        "خدمة تفصيل الدشداشة": "Dishdasha Tailoring",
    }
    for cat in categories:
        arabic = cat["name_ar"].strip()
        english = category_names.get(arabic, arabic)
        lines.append(f"IF NOT EXISTS (SELECT 1 FROM dbo.ProductCategories WHERE NameAr={literal(arabic)}) INSERT dbo.ProductCategories(Name,NameAr,DisplayOrder) VALUES ({literal(english)},{literal(arabic)},0);")
    lines.append("DECLARE @ProductId int;")
    for order, p in enumerate(products):
        source_id = p["id"]
        cat = category_by_id[p["category_id"]]["name_ar"].strip()
        price = Decimal(str(p["price"]))
        compare = Decimal(str(p["compare_at_price"])) if p.get("compare_at_price") is not None else Decimal(0)
        regular = max(price, compare)
        discount = price if compare > price else None
        name = (p.get("name_en") or p["name_ar"]).strip()
        name_ar = p["name_ar"].strip()
        desc = (p.get("description_en") or "")[:2000]
        desc_ar = (p.get("description_ar") or "")[:2000]
        lines += [
            f"SET @ProductId=(SELECT ProductId FROM dbo.WebsiteProductImportMap WHERE SourceProductId='{source_id}');",
            "IF @ProductId IS NULL BEGIN",
            f"INSERT dbo.Products(CategoryId,Name,NameAr,Description,DescriptionAr,ProductType,RegularPrice,DiscountPrice,DisplayOrder,IsActive) VALUES ((SELECT TOP 1 CategoryId FROM dbo.ProductCategories WHERE NameAr={literal(cat)}),{literal(name)},{literal(name_ar)},{literal(desc)},{literal(desc_ar)},N'Ready-made',{regular:.3f},{'NULL' if discount is None else f'{discount:.3f}'},{order + 1},1);",
            f"SET @ProductId=CONVERT(int,SCOPE_IDENTITY()); INSERT dbo.WebsiteProductImportMap(SourceProductId,ProductId) VALUES ('{source_id}',@ProductId); END ELSE BEGIN",
            f"UPDATE dbo.Products SET CategoryId=(SELECT TOP 1 CategoryId FROM dbo.ProductCategories WHERE NameAr={literal(cat)}),Name={literal(name)},NameAr={literal(name_ar)},Description={literal(desc)},DescriptionAr={literal(desc_ar)},RegularPrice={regular:.3f},DiscountPrice={'NULL' if discount is None else f'{discount:.3f}'},DisplayOrder={order + 1},IsActive=1 WHERE ProductId=@ProductId;",
            "DELETE dbo.ProductImages WHERE ProductId=@ProductId; END;",
        ]
        available = sorted(index for pid, index, _ in images if pid == source_id)
        for display_order, index in enumerate(available):
            lines.append(f"INSERT dbo.ProductImages(ProductId,ImagePath,IsPrimary,DisplayOrder) VALUES (@ProductId,{literal(paths[source_id,index])},{1 if display_order == 0 else 0},{display_order});")
    # Only the known 2026-11-12 catalogue is retired. Historical rows stay intact.
    old_names = ["Casa Breeze Jalabiya", "Presence Jalabiya", "Sultan Gandoura", "Aseel Gandoura", "Dusk Weave Gandoura", "Summer Prestige Gandoura", "Al Hiba Gandoura", "Emerald Kaftan", "Horizon Breeze Kaftan", "Prince of Elegance Jabador", "Blue Kashkha Jabador", "Ibdaa Moroccan Jabador", "Blue Horizon Jabador", "Summer Offer (2 Gandouras + Kaftan)", "Father and Son Moroccan Set", "Summer Breeze"]
    lines.append("UPDATE dbo.Products SET IsActive=0 WHERE Name IN (" + ",".join(map(literal, old_names)) + ") AND ProductId NOT IN (SELECT ProductId FROM dbo.WebsiteProductImportMap);")
    current_ids = ",".join("'" + p["id"] + "'" for p in products)
    lines.append("UPDATE dbo.Products SET IsActive=0 WHERE ProductId IN (SELECT ProductId FROM dbo.WebsiteProductImportMap WHERE SourceProductId NOT IN (" + current_ids + "));")
    lines += ["COMMIT TRANSACTION;", "SELECT COUNT(*) AS ActiveProducts FROM dbo.Products WHERE IsActive=1;", "SELECT COUNT(*) AS ActiveProductImages FROM dbo.ProductImages i JOIN dbo.Products p ON p.ProductId=i.ProductId WHERE p.IsActive=1;"]
    with tempfile.NamedTemporaryFile("w", encoding="utf-8-sig", suffix=".sql", delete=False) as temp:
        temp.write("\n".join(lines))
        sql_file = Path(temp.name)
    try:
        subprocess.run(["sqlcmd", "-S", "(localdb)\\MSSQLLocalDB", "-d", DB, "-E", "-b", "-I", "-f", "i:65001", "-i", str(sql_file)], check=True)
    finally:
        sql_file.unlink(missing_ok=True)
    print(f"Imported {len(products)} products and {len(images)} images from {SITE}; captured {datetime.now(timezone.utc).isoformat()}")


if __name__ == "__main__":
    main()
