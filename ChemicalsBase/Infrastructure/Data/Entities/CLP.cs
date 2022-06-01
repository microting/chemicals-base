using System.ComponentModel.DataAnnotations.Schema;

namespace ChemicalsBase.Infrastructure.Data.Entities;

public class CLP : PnBase
{
    public ICollection<int>? HazardPictograms { get; set; }
    public ICollection<HazardStatement> HazardStatements { get; set; }
    public int? SignalWord { get; set; }
    public int? BeeSymbol { get; set; }
    public string? UFICode { get; set; }
}