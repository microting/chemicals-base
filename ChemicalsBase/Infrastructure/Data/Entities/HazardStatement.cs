using System.ComponentModel.DataAnnotations.Schema;

namespace ChemicalsBase.Infrastructure.Data.Entities;

public class HazardStatement : PnBase
{
    [ForeignKey("CLP")]
    public int CLPId { get; set; }
    public int? Class { get; set; }
    public int? Category { get; set; }
    public int? Statement { get; set; }
}