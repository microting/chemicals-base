using System.ComponentModel.DataAnnotations.Schema;

namespace ChemicalsBase.Infrastructure.Data.Entities;

public class DPDVersion : PnBase
{
    [ForeignKey("DPD")]
    public int DPDId { get; set; }
    public ICollection<int> RiskPhrases { get; set; }
    public int? IndicationDangerFlammable { get; set; }
    public int? IndicationDangerToxicity { get; set; }
    public int? IndicationDangerEnvironment { get; set; }
}