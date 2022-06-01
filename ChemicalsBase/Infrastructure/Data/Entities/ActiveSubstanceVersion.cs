using System.ComponentModel.DataAnnotations.Schema;

namespace ChemicalsBase.Infrastructure.Data.Entities;

public class ActiveSubstanceVersion : PnBase
{
    public string RemoteId { get; set; }
    public int ChemicalId { get; set; }
    [ForeignKey("ActiveSubstance")]
    public int ActiveSubstanceId { get; set; }
    public string Name { get; set; }
    public string? CASNo { get; set; }
    public double? Concentration { get; set; }
    public int? Unit { get; set; }
    public int? ActionMechanism { get; set; }
    public int? CotyledonousWeedType { get; set; }
}