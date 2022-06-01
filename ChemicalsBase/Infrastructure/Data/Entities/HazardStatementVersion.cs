using System.ComponentModel.DataAnnotations.Schema;

namespace ChemicalsBase.Infrastructure.Data.Entities;

public class HazardStatementVersion : PnBase
{
    [ForeignKey("HazardStatement")]
    public int HazardStatementId { get; set; }
    public int? Class { get; set; }
    public int? Category { get; set; }
    public int? Statement { get; set; }
}