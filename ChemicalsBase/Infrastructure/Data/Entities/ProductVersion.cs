using System.ComponentModel.DataAnnotations.Schema;

namespace ChemicalsBase.Infrastructure.Data.Entities;

public class ProductVersion : PnBase
{
    public int ChemicalId { get; set; }
    [ForeignKey("Product")]
    public int ProductId { get; set; }
    public string? Barcode { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
    public bool IsValid { get; set; }
    public string FileName { get; set; }
    public string Checksum { get; set; }
}