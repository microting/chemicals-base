using System.ComponentModel.DataAnnotations.Schema;

namespace ChemicalsBase.Infrastructure.Data.Entities;

public class DPD : PnBase
{
    public ICollection<int> RiskPhrases { get; set; }
    public int? IndicationDangerFlammable { get; set; }
    public int? IndicationDangerToxicity { get; set; }
    public int? IndicationDangerEnvironment { get; set; }
}