using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbdaaAtelier.Web.Models.Catalog;

[Table("SizeChartValues", Schema = "dbo")]
public sealed class SizeChartValue
{
    [Key]
    public int ChartValueId { get; set; }
    public int SizeId { get; set; }
    public int MeasurementId { get; set; }
    [Column(TypeName = "decimal(9,2)")]
    public decimal Value { get; set; }
}
