using System.ComponentModel.DataAnnotations.Schema;

namespace ChemicalsBase.Infrastructure.Data.Entities;

public class LoadTax : PnBase
{
    [ForeignKey("Product")]
    public int ProductId { get; set; }
    public DateTime? Date { get; set; }
    public double? Fee { get; set; }
    public double? EnvironmentalEffect { get; set; }
    public double? EnvironmentalBehaviour { get; set; }
    public double? Health { get; set; }
    public double? Concentration { get; set; }
    public int? Unit { get; set; }
}