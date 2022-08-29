using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChemicalsBase.Infrastructure.Data.Entities;

public class Chemical : PnBase
{
    public string Name { get; set; }
    public string RegistrationNo { get; set; }
    public int? PestControlType { get; set; }
    public int? Status { get; set; }
    public int? PesticideAuthorisationType { get; set; }
    public int? BiocideAuthorisationType { get; set; }
    public int? AuthorisationHolderId { get; set; }
    public virtual AuthorisationHolder AuthorisationHolder { get; set; }
    public DateTime? AuthorisationDate { get; set; }
    public DateTime? AuthorisationExpirationDate { get; set; }
    public DateTime? AuthorisationTerminationDate { get; set; }
    public DateTime? SalesDeadline { get; set; }
    public DateTime? UseAndPossesionDeadline { get; set; }
    public DateTime? PossessionDeadline { get; set; }
    public int? FormulationType { get; set; }
    public int? FormulationSubType { get; set; }
    public int? PesticideUser { get; set; }
    public ICollection<int> BiocideUser { get; set; } // virtual
    public ICollection<int> PesticideProductGroup { get; set; } // virtual
    public int BiocideProductGroup { get; set; }
    public ICollection<int> BiocideProductType { get; set; } // virtual
    public int? ActiveSubstanceType { get; set; }
    public ICollection<ActiveSubstance> ActiveSubstances { get; set; }
    public string? Use { get; set; }
    public ICollection<int> PesticidePossibleUse { get; set; }
    public ICollection<int> BiocidePossibleUse { get; set; }
    public ICollection<int> BiocideSpecialUse { get; set; }

    [ForeignKey("ClassificationAndLabelingId")]
    public int ClassificationAndLabelingId { get; set; }
    public virtual ClassificationAndLabeling ClassificationAndLabeling { get; set; }
    public ICollection<string> BarcodeValue { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    public virtual List<LoadTax> LoadTaxe { get; set; }
    public ICollection<Product> Products { get; set; }

    public string RemoteId { get; set; }
    public bool Verified { get; set; }


}