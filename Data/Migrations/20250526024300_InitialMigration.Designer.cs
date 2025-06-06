﻿// <auto-generated />
using System;
using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Data.Migrations
{
    [DbContext(typeof(EvnContext))]
    [Migration("20250526024300_InitialMigration")]
    partial class InitialMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Data.Models.AdditionalVehicleInfo", b =>
                {
                    b.Property<int>("VariableId")
                        .HasColumnType("int");

                    b.Property<string>("VehicleId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Value")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("VariableId", "VehicleId", "Value");

                    b.HasIndex("VehicleId");

                    b.ToTable("AdditionalCarInfo");
                });

            modelBuilder.Entity("Data.Models.Vehicle", b =>
                {
                    b.Property<string>("Vin")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("DealerId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("ModifiedDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Vin");

                    b.ToTable("Vehicle");
                });

            modelBuilder.Entity("Data.Models.VehicleVariable", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("VehicleVariable");

                    b.HasData(
                        new
                        {
                            Id = 5,
                            Name = "Body Class"
                        },
                        new
                        {
                            Id = 15,
                            Name = "Drive Type"
                        },
                        new
                        {
                            Id = 24,
                            Name = "Fuel Type - Primary"
                        },
                        new
                        {
                            Id = 26,
                            Name = "Make"
                        },
                        new
                        {
                            Id = 27,
                            Name = "Manufacturer Name"
                        },
                        new
                        {
                            Id = 28,
                            Name = "Model"
                        },
                        new
                        {
                            Id = 29,
                            Name = "Model Year"
                        },
                        new
                        {
                            Id = 34,
                            Name = "Series"
                        },
                        new
                        {
                            Id = 37,
                            Name = "Transmission Style"
                        },
                        new
                        {
                            Id = 38,
                            Name = "Trim"
                        },
                        new
                        {
                            Id = 39,
                            Name = "Vehicle Type"
                        },
                        new
                        {
                            Id = 64,
                            Name = "Engine Configuration"
                        },
                        new
                        {
                            Id = 66,
                            Name = "Fuel Type - Secondary"
                        },
                        new
                        {
                            Id = 139,
                            Name = "Top Speed"
                        });
                });

            modelBuilder.Entity("Data.Models.AdditionalVehicleInfo", b =>
                {
                    b.HasOne("Data.Models.VehicleVariable", "Variable")
                        .WithMany()
                        .HasForeignKey("VariableId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Data.Models.Vehicle", "Vehicle")
                        .WithMany("AdditionalVehicleInfo")
                        .HasForeignKey("VehicleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Variable");

                    b.Navigation("Vehicle");
                });

            modelBuilder.Entity("Data.Models.Vehicle", b =>
                {
                    b.Navigation("AdditionalVehicleInfo");
                });
#pragma warning restore 612, 618
        }
    }
}
