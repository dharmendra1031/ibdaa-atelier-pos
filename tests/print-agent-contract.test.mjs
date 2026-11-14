import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read = path => readFileSync(new URL("../" + path, import.meta.url), "utf8");
const invoice = read("src/IbdaaAtelier.Web/Controllers/InvoicesController.cs");
const api = read("src/IbdaaAtelier.Web/Controllers/PrintAgentController.cs");
const queue = read("src/IbdaaAtelier.Web/Services/PrintQueueService.cs");
const agent = read("src/IbdaaAtelier.PrintAgent/Program.cs");
const js = read("src/IbdaaAtelier.Web/wwwroot/js/pos.js");
const posView = read("src/IbdaaAtelier.Web/Views/Pos/Index.cshtml");
const shop = read("src/IbdaaAtelier.Web/Views/ShopSettings/Index.cshtml");
const migration = read("database/041_print_agent_queue.sql");

test("existing browser PDF flow remains independent of the local agent", () => {
    assert.match(js, /if \(shell\.dataset\.openInvoicePdf === "true"\)/);
    assert.match(js, /window\.open\("", "_blank"\)/);
    assert.match(js, /tab\.location\.replace\(pdfUrl\)/);
    assert.match(invoice, /return File\(pdf, "application\/pdf"\)/);
    assert.match(js, /data\.printJobId|job\.printJobId/);
    assert.doesNotMatch(invoice, /IPAddress\.IsLoopback|DirectInvoicePrinter/);
});

test("print jobs have SQL identity numbers and isolate terminals", () => {
    assert.match(migration, /TerminalId INT IDENTITY\(1,1\)/);
    assert.match(migration, /PrintJobId BIGINT IDENTITY\(1,1\)/);
    assert.match(migration, /UQ_PrintJobs_InvoiceTerminal UNIQUE \(TerminalId, OrderId\)/);
    assert.match(queue, /FixedTimeEquals/);
    assert.match(queue, /UPDLOCK, HOLDLOCK/);
    assert.match(queue, /job\.Status == "Failed"/);
    assert.match(queue, /job\.Status = "Processing"/);
    assert.match(queue, /job\.Status is "Completed" or "Failed"/);
    assert.match(queue, /"NeedsReview"/);
});

test("only a paired agent can claim and download its 80mm invoice", () => {
    assert.match(api, /\[AllowAnonymous\]/);
    assert.match(api, /AuthenticateAsync\(terminalId/);
    assert.match(api, /HttpPost\("next"\)/);
    assert.match(api, /GetClaimedAsync\(terminalId\.Value, jobId, ct\)/);
    assert.match(api, /RenderPdf\(invoice, "80mm"\)/);
    assert.match(agent, /X-Print-Terminal/);
    assert.match(agent, /X-Print-Token/);
    assert.match(agent, /Uri\.UriSchemeHttps/);
    assert.match(agent, /-print-to-default/);
    assert.match(agent, /printer command|Printer command/i);
});

test("cashier printer setup, status and manual retry reuse the POS", () => {
    assert.match(shop, /AddPrintTerminal/);
    assert.match(shop, /NewTerminalToken/);
    assert.match(shop, /\/Pos\?terminalId=/);
    assert.match(posView, /data-print-status-url=/);
    assert.match(js, /localStorage\.setItem\(printTerminalKey, selected\)/);
    assert.match(js, /rememberPrintJob\(/);
    assert.match(js, /watchPrintJob\(/);
    assert.match(js, /retryPrintButton\.addEventListener/);
    assert.match(invoice, /HttpPost, ValidateAntiForgeryToken, Authorize\(Roles = "Cashier"\)/);
});
