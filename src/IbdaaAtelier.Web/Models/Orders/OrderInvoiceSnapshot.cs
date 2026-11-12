using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Orders;

[Table("OrderInvoiceSnapshots", Schema = "dbo")]
public sealed class OrderInvoiceSnapshot
{
    [Key] public long InvoiceSnapshotId { get; set; }
    public long OrderId { get; set; }
    [MaxLength(150)] public string BusinessName { get; set; } = "";
    [MaxLength(150)] public string BusinessNameAr { get; set; } = "";
    [MaxLength(500)] public string? Address { get; set; }
    [MaxLength(500)] public string? AddressAr { get; set; }
    [MaxLength(25)] public string? Mobile { get; set; }
    [MaxLength(150)] public string? Email { get; set; }
    [MaxLength(260)] public string? LogoPath { get; set; }
    [MaxLength(500)] public string? ReceiptFooter { get; set; }
}
