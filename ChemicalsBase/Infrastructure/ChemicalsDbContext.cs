using System.Runtime.Intrinsics.Arm;
using System.Text.Json;
using ChemicalsBase.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ChemicalsBase.Infrastructure;

using Microsoft.EntityFrameworkCore;
public class ChemicalsDbContext: DbContext
{
    public ChemicalsDbContext() { }

    public ChemicalsDbContext(DbContextOptions<ChemicalsDbContext> options) : base(options)
    {
    }

    public DbSet<Chemical> Chemicals { get; set; }
    public DbSet<ChemicalVersion> ChemicalVersions { get; set; }
    public DbSet<ActiveSubstance> ActiveSubstances { get; set; }
    public DbSet<ActiveSubstanceVersion> ActiveSubstanceVersions { get; set; }
    public DbSet<ClassificationAndLabeling> ClassificationAndLabelings { get; set; }
    public DbSet<ClassificationAndLabelingVersion> ClassificationAndLabelingVersions { get; set; }
    public DbSet<CLP> Clps { get; set; }
    public DbSet<CLPVersion> ClpVersions { get; set; }
    public DbSet<DPD> Dpds { get; set; }
    public DbSet<DPDVersion> DpdVersions { get; set; }
    public DbSet<HazardStatement> HazardStatements { get; set; }
    public DbSet<HazardStatementVersion> HazardStatementVersions { get; set; }
    public DbSet<LoadTax> LoadTaxes { get; set; }
    public DbSet<LoadTaxVersion> LoadTaxVersions { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductVersion> ProductVersions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chemical>()
            .Property(e => e.BiocideUser)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null!)!,
                new ValueComparer<ICollection<int>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<int>)c.ToList()));
        modelBuilder.Entity<Chemical>()
            .Property(e => e.PesticideProductGroup)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null!)!,
                new ValueComparer<ICollection<int>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<int>)c.ToList()));
        modelBuilder.Entity<Chemical>()
            .Property(e => e.PesticidePossibleUse)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null!)!,
                new ValueComparer<ICollection<int>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<int>)c.ToList()));
        modelBuilder.Entity<Chemical>()
            .Property(e => e.BiocidePossibleUse)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null!)!,
                new ValueComparer<ICollection<int>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<int>)c.ToList()));
        modelBuilder.Entity<Chemical>()
            .Property(e => e.BiocideSpecialUse)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null!)!,
                new ValueComparer<ICollection<int>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<int>)c.ToList()));
        modelBuilder.Entity<Chemical>()
            .Property(e => e.BarcodeValue)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!)!,
                new ValueComparer<ICollection<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<string>)c.ToList()));
        modelBuilder.Entity<ChemicalVersion>()
            .Property(e => e.BiocideUser)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null!)!,
                new ValueComparer<ICollection<int>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<int>)c.ToList()));
        modelBuilder.Entity<ChemicalVersion>()
            .Property(e => e.PesticideProductGroup)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null!)!,
                new ValueComparer<ICollection<int>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<int>)c.ToList()));
        modelBuilder.Entity<ChemicalVersion>()
            .Property(e => e.PesticidePossibleUse)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null!)!,
                new ValueComparer<ICollection<int>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<int>)c.ToList()));
        modelBuilder.Entity<ChemicalVersion>()
            .Property(e => e.BiocidePossibleUse)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null!)!,
                new ValueComparer<ICollection<int>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<int>)c.ToList()));
        modelBuilder.Entity<ChemicalVersion>()
            .Property(e => e.BiocideSpecialUse)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null!)!,
                new ValueComparer<ICollection<int>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<int>)c.ToList()));
        modelBuilder.Entity<ChemicalVersion>()
            .Property(e => e.BarcodeValue)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!)!,
                new ValueComparer<ICollection<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<string>)c.ToList()));
        modelBuilder.Entity<CLP>()
            .Property(e => e.HazardPictograms)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null!)!,
                new ValueComparer<ICollection<int>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<int>)c.ToList()));
        modelBuilder.Entity<CLPVersion>()
            .Property(e => e.HazardPictograms)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null!)!,
                new ValueComparer<ICollection<int>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<int>)c.ToList()));
        modelBuilder.Entity<DPD>()
            .Property(e => e.RiskPhrases)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null!)!,
                new ValueComparer<ICollection<int>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<int>)c.ToList()));
        modelBuilder.Entity<DPDVersion>()
            .Property(e => e.RiskPhrases)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null!)!,
                new ValueComparer<ICollection<int>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<int>)c.ToList()));
    }
}