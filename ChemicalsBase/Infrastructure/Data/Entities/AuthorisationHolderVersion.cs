using System.ComponentModel.DataAnnotations.Schema;

namespace ChemicalsBase.Infrastructure.Data.Entities;

public class AuthorisationHolderVersion : BaseEntity
{
    public string RemoteId { get; set; }
    public string? Name { get; set; }

    public int AddressId { get; set; }
}