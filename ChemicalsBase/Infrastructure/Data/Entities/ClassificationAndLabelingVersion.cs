using System.ComponentModel.DataAnnotations.Schema;

namespace ChemicalsBase.Infrastructure.Data.Entities;

public class ClassificationAndLabelingVersion : PnBase
{
    [ForeignKey("ClassificationAndLabeling")]
    public int ClassificationAndLabelingId { get; set; }
    public int CLPId { get; set; }
    public int DPDId { get; set; }
}