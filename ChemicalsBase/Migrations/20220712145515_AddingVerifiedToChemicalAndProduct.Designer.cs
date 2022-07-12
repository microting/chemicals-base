﻿// <auto-generated />
using System;
using ChemicalsBase.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ChemicalsBase.Migrations
{
    [DbContext(typeof(ChemicalsDbContext))]
    [Migration("20220712145515_AddingVerifiedToChemicalAndProduct")]
    partial class AddingVerifiedToChemicalAndProduct
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.ActiveSubstance", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int?>("ActionMechanism")
                        .HasColumnType("int");

                    b.Property<string>("CASNo")
                        .HasColumnType("longtext");

                    b.Property<int>("ChemicalId")
                        .HasColumnType("int");

                    b.Property<int?>("ChemicalVersionId")
                        .HasColumnType("int");

                    b.Property<double?>("Concentration")
                        .HasColumnType("double");

                    b.Property<int?>("CotyledonousWeedType")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("RemoteId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("Unit")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UpdatedByUserId")
                        .HasColumnType("int");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.Property<string>("WorkflowState")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("ChemicalId");

                    b.HasIndex("ChemicalVersionId");

                    b.ToTable("ActiveSubstances");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.ActiveSubstanceVersion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int?>("ActionMechanism")
                        .HasColumnType("int");

                    b.Property<int>("ActiveSubstanceId")
                        .HasColumnType("int");

                    b.Property<string>("CASNo")
                        .HasColumnType("longtext");

                    b.Property<int>("ChemicalId")
                        .HasColumnType("int");

                    b.Property<double?>("Concentration")
                        .HasColumnType("double");

                    b.Property<int?>("CotyledonousWeedType")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("RemoteId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("Unit")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UpdatedByUserId")
                        .HasColumnType("int");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.Property<string>("WorkflowState")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.ToTable("ActiveSubstanceVersions");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.Chemical", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int?>("ActiveSubstanceType")
                        .HasColumnType("int");

                    b.Property<DateTime?>("AuthorisationDate")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("AuthorisationExpirationDate")
                        .HasColumnType("datetime(6)");

                    b.Property<int?>("AuthorisationHolderId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("AuthorisationTerminationDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("BarcodeValue")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("BiocideAuthorisationType")
                        .HasColumnType("int");

                    b.Property<string>("BiocidePossibleUse")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("BiocideSpecialUse")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("BiocideUser")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("ClassificationAndLabelingId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<int?>("FormulationSubType")
                        .HasColumnType("int");

                    b.Property<int?>("FormulationType")
                        .HasColumnType("int");

                    b.Property<DateTime?>("LastUpdatedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("PestControlType")
                        .HasColumnType("int");

                    b.Property<int?>("PesticideAuthorisationType")
                        .HasColumnType("int");

                    b.Property<string>("PesticidePossibleUse")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("PesticideProductGroup")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("PesticideUser")
                        .HasColumnType("int");

                    b.Property<DateTime?>("PossessionDeadline")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("RegistrationNo")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("RemoteId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("SalesDeadline")
                        .HasColumnType("datetime(6)");

                    b.Property<int?>("Status")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UpdatedByUserId")
                        .HasColumnType("int");

                    b.Property<string>("Use")
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("UseAndPossesionDeadline")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("Verified")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.Property<string>("WorkflowState")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("ClassificationAndLabelingId");

                    b.ToTable("Chemicals");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.ChemicalVersion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int?>("ActiveSubstanceType")
                        .HasColumnType("int");

                    b.Property<DateTime?>("AuthorisationDate")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("AuthorisationExpirationDate")
                        .HasColumnType("datetime(6)");

                    b.Property<int?>("AuthorisationHolderId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("AuthorisationTerminationDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("BarcodeValue")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("BiocideAuthorisationType")
                        .HasColumnType("int");

                    b.Property<string>("BiocidePossibleUse")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("BiocideSpecialUse")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("BiocideUser")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("ChemicalId")
                        .HasColumnType("int");

                    b.Property<int>("ClassificationAndLabelingId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<int?>("FormulationSubType")
                        .HasColumnType("int");

                    b.Property<int?>("FormulationType")
                        .HasColumnType("int");

                    b.Property<DateTime?>("LastUpdatedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("PestControlType")
                        .HasColumnType("int");

                    b.Property<int?>("PesticideAuthorisationType")
                        .HasColumnType("int");

                    b.Property<string>("PesticidePossibleUse")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("PesticideProductGroup")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("PesticideUser")
                        .HasColumnType("int");

                    b.Property<DateTime?>("PossessionDeadline")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("RegistrationNo")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("RemoteId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("SalesDeadline")
                        .HasColumnType("datetime(6)");

                    b.Property<int?>("Status")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UpdatedByUserId")
                        .HasColumnType("int");

                    b.Property<string>("Use")
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("UseAndPossesionDeadline")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("Verified")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.Property<string>("WorkflowState")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.ToTable("ChemicalVersions");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.ClassificationAndLabeling", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("CLPId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<int>("DPDId")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UpdatedByUserId")
                        .HasColumnType("int");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.Property<string>("WorkflowState")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("CLPId");

                    b.HasIndex("DPDId");

                    b.ToTable("ClassificationAndLabelings");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.ClassificationAndLabelingVersion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("CLPId")
                        .HasColumnType("int");

                    b.Property<int>("ClassificationAndLabelingId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<int>("DPDId")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UpdatedByUserId")
                        .HasColumnType("int");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.Property<string>("WorkflowState")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.ToTable("ClassificationAndLabelingVersions");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.CLP", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int?>("BeeSymbol")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<string>("HazardPictograms")
                        .HasColumnType("longtext");

                    b.Property<int?>("SignalWord")
                        .HasColumnType("int");

                    b.Property<string>("UFICode")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UpdatedByUserId")
                        .HasColumnType("int");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.Property<string>("WorkflowState")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.ToTable("Clps");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.CLPVersion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int?>("BeeSymbol")
                        .HasColumnType("int");

                    b.Property<int>("CLPId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<string>("HazardPictograms")
                        .HasColumnType("longtext");

                    b.Property<int?>("SignalWord")
                        .HasColumnType("int");

                    b.Property<string>("UFICode")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UpdatedByUserId")
                        .HasColumnType("int");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.Property<string>("WorkflowState")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.ToTable("ClpVersions");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.DPD", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<int?>("IndicationDangerEnvironment")
                        .HasColumnType("int");

                    b.Property<int?>("IndicationDangerFlammable")
                        .HasColumnType("int");

                    b.Property<int?>("IndicationDangerToxicity")
                        .HasColumnType("int");

                    b.Property<string>("RiskPhrases")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UpdatedByUserId")
                        .HasColumnType("int");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.Property<string>("WorkflowState")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.ToTable("Dpds");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.DPDVersion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<int>("DPDId")
                        .HasColumnType("int");

                    b.Property<int?>("IndicationDangerEnvironment")
                        .HasColumnType("int");

                    b.Property<int?>("IndicationDangerFlammable")
                        .HasColumnType("int");

                    b.Property<int?>("IndicationDangerToxicity")
                        .HasColumnType("int");

                    b.Property<string>("RiskPhrases")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UpdatedByUserId")
                        .HasColumnType("int");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.Property<string>("WorkflowState")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.ToTable("DpdVersions");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.HazardStatement", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("CLPId")
                        .HasColumnType("int");

                    b.Property<int?>("CLPVersionId")
                        .HasColumnType("int");

                    b.Property<int?>("Category")
                        .HasColumnType("int");

                    b.Property<int?>("Class")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<int?>("Statement")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UpdatedByUserId")
                        .HasColumnType("int");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.Property<string>("WorkflowState")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("CLPId");

                    b.HasIndex("CLPVersionId");

                    b.ToTable("HazardStatements");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.HazardStatementVersion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int?>("Category")
                        .HasColumnType("int");

                    b.Property<int?>("Class")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<int>("HazardStatementId")
                        .HasColumnType("int");

                    b.Property<int?>("Statement")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UpdatedByUserId")
                        .HasColumnType("int");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.Property<string>("WorkflowState")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.ToTable("HazardStatementVersions");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.LoadTax", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int?>("ChemicalId")
                        .HasColumnType("int");

                    b.Property<int?>("ChemicalVersionId")
                        .HasColumnType("int");

                    b.Property<double?>("Concentration")
                        .HasColumnType("double");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("Date")
                        .HasColumnType("datetime(6)");

                    b.Property<double?>("EnvironmentalBehaviour")
                        .HasColumnType("double");

                    b.Property<double?>("EnvironmentalEffect")
                        .HasColumnType("double");

                    b.Property<double?>("Fee")
                        .HasColumnType("double");

                    b.Property<double?>("Health")
                        .HasColumnType("double");

                    b.Property<int>("ProductId")
                        .HasColumnType("int");

                    b.Property<int?>("Unit")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UpdatedByUserId")
                        .HasColumnType("int");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.Property<string>("WorkflowState")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("ChemicalId");

                    b.HasIndex("ChemicalVersionId");

                    b.ToTable("LoadTaxes");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.LoadTaxVersion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<double?>("Concentration")
                        .HasColumnType("double");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("Date")
                        .HasColumnType("datetime(6)");

                    b.Property<double?>("EnvironmentalBehaviour")
                        .HasColumnType("double");

                    b.Property<double?>("EnvironmentalEffect")
                        .HasColumnType("double");

                    b.Property<double?>("Fee")
                        .HasColumnType("double");

                    b.Property<double?>("Health")
                        .HasColumnType("double");

                    b.Property<int>("LoadTaxId")
                        .HasColumnType("int");

                    b.Property<int>("ProductId")
                        .HasColumnType("int");

                    b.Property<int?>("Unit")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UpdatedByUserId")
                        .HasColumnType("int");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.Property<string>("WorkflowState")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.ToTable("LoadTaxVersions");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.Product", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Barcode")
                        .HasColumnType("longtext");

                    b.Property<string>("Checksum")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("ChemicalId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("IsActive")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("IsValid")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Name")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UpdatedByUserId")
                        .HasColumnType("int");

                    b.Property<bool>("Verified")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.Property<string>("WorkflowState")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("ChemicalId");

                    b.ToTable("Products");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.ProductVersion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Barcode")
                        .HasColumnType("longtext");

                    b.Property<string>("Checksum")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("ChemicalId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("IsActive")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("IsValid")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Name")
                        .HasColumnType("longtext");

                    b.Property<int>("ProductId")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UpdatedByUserId")
                        .HasColumnType("int");

                    b.Property<bool>("Verified")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.Property<string>("WorkflowState")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.ToTable("ProductVersions");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.ActiveSubstance", b =>
                {
                    b.HasOne("ChemicalsBase.Infrastructure.Data.Entities.Chemical", null)
                        .WithMany("ActiveSubstances")
                        .HasForeignKey("ChemicalId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ChemicalsBase.Infrastructure.Data.Entities.ChemicalVersion", null)
                        .WithMany("ActiveSubstances")
                        .HasForeignKey("ChemicalVersionId");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.Chemical", b =>
                {
                    b.HasOne("ChemicalsBase.Infrastructure.Data.Entities.ClassificationAndLabeling", "ClassificationAndLabeling")
                        .WithMany()
                        .HasForeignKey("ClassificationAndLabelingId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ClassificationAndLabeling");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.ClassificationAndLabeling", b =>
                {
                    b.HasOne("ChemicalsBase.Infrastructure.Data.Entities.CLP", "CLP")
                        .WithMany()
                        .HasForeignKey("CLPId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ChemicalsBase.Infrastructure.Data.Entities.DPD", "DPD")
                        .WithMany()
                        .HasForeignKey("DPDId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CLP");

                    b.Navigation("DPD");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.HazardStatement", b =>
                {
                    b.HasOne("ChemicalsBase.Infrastructure.Data.Entities.CLP", null)
                        .WithMany("HazardStatements")
                        .HasForeignKey("CLPId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ChemicalsBase.Infrastructure.Data.Entities.CLPVersion", null)
                        .WithMany("HazardStatements")
                        .HasForeignKey("CLPVersionId");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.LoadTax", b =>
                {
                    b.HasOne("ChemicalsBase.Infrastructure.Data.Entities.Chemical", null)
                        .WithMany("LoadTaxe")
                        .HasForeignKey("ChemicalId");

                    b.HasOne("ChemicalsBase.Infrastructure.Data.Entities.ChemicalVersion", null)
                        .WithMany("LoadTaxe")
                        .HasForeignKey("ChemicalVersionId");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.Product", b =>
                {
                    b.HasOne("ChemicalsBase.Infrastructure.Data.Entities.Chemical", null)
                        .WithMany("Products")
                        .HasForeignKey("ChemicalId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.Chemical", b =>
                {
                    b.Navigation("ActiveSubstances");

                    b.Navigation("LoadTaxe");

                    b.Navigation("Products");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.ChemicalVersion", b =>
                {
                    b.Navigation("ActiveSubstances");

                    b.Navigation("LoadTaxe");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.CLP", b =>
                {
                    b.Navigation("HazardStatements");
                });

            modelBuilder.Entity("ChemicalsBase.Infrastructure.Data.Entities.CLPVersion", b =>
                {
                    b.Navigation("HazardStatements");
                });
#pragma warning restore 612, 618
        }
    }
}
