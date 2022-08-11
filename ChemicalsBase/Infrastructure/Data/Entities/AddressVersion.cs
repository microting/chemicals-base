namespace ChemicalsBase.Infrastructure.Data.Entities;

public class AddressVersion : BaseEntity
{
    public string? StreetName { get; set; }
    public string? StreetBuildingNo { get; set; }
    public string? Floor { get; set; }
    public string? Room { get; set; }
    public string? PostalCode { get; set; }
    public string? CityName { get; set; }
    public string? DistricName { get; set; }
    public int Country { get; set; }
    public string? PostOfficeBox { get; set; }
}