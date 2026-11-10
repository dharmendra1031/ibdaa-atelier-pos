import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const sql = readFileSync(new URL('../database/001_initial_catalog.sql', import.meta.url), 'utf8');
const context = readFileSync(new URL('../src/IbdaaAtelier.Web/Data/AtelierDbContext.cs', import.meta.url), 'utf8');
const source = readFileSync(new URL('../src/IbdaaAtelier.Web/Controllers/CatalogController.cs', import.meta.url), 'utf8');
const program = readFileSync(new URL('../src/IbdaaAtelier.Web/Program.cs', import.meta.url), 'utf8');

const tables = {
  ProductCategories: 'CategoryId',
  Products: 'ProductId',
  ProductImages: 'ProductImageId',
  ProductSizes: 'SizeId',
  MeasurementDefinitions: 'MeasurementId',
  SizeChartValues: 'ChartValueId',
};

test('all catalogue primary keys are numeric SQL identities', () => {
  for (const [name, key] of Object.entries(tables)) {
    assert.match(sql, new RegExp(`CREATE TABLE dbo\\.${name}\\s*\\(\\s*${key} INT IDENTITY\\(1,1\\)`, 'i'));
    assert.match(context, new RegExp(`DbSet<[^>]+> ${name}\\b`));
  }
  assert.doesNotMatch(sql, /UNIQUEIDENTIFIER|NEWID\s*\(|NEWSEQUENTIALID/i);
});

test('product gallery has a single primary image and unlimited secondary images', () => {
  assert.match(sql, /CREATE UNIQUE INDEX UX_ProductImages_Primary/);
  assert.match(sql, /WHERE IsPrimary = 1/);
  assert.doesNotMatch(sql, /MaxImages|Image4|Image5/i);
});

test('measurements are scoped to a product or category', () => {
  assert.match(sql, /CK_Measurements_Scope/);
  assert.match(sql, /CategoryId IS NOT NULL AND ProductId IS NULL/);
  assert.match(sql, /CategoryId IS NULL AND ProductId IS NOT NULL/);
});

test('catalogue is active-only, read-only, and never self-migrates', () => {
  assert.match(source, /product\.IsActive/);
  assert.match(source, /category\.IsActive/);
  assert.doesNotMatch(source, /\[HttpPost|SaveChangesAsync|ExecuteSqlRaw/i);
  assert.doesNotMatch(program, /EnsureCreated|Migrate\(/);
});

test('guarded migration cannot run with its default database marker', () => {
  assert.match(sql, /__SET_TEST_DATABASE_NAME__/);
  assert.match(sql, /DB_NAME\(\) <> @ExpectedDatabase/);
  assert.match(sql, /BEGIN TRANSACTION/);
});
