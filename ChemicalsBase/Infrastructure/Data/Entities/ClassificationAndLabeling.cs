using System.ComponentModel.DataAnnotations.Schema;

namespace ChemicalsBase.Infrastructure.Data.Entities;

public class ClassificationAndLabeling : PnBase
{
    [ForeignKey("CLP")]
    public int CLPId { get; set; }
    public virtual CLP CLP { get; set; }
    [ForeignKey("DPD")]
    public int DPDId { get; set; }
    public virtual DPD DPD { get; set; }
}