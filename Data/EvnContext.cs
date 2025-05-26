//create DBcontext class
using Microsoft.EntityFrameworkCore;
using Data.Models;

namespace Data
{
    public class EvnContext : DbContext
    {
        public EvnContext(DbContextOptions<EvnContext> options) : base(options)
        {
        }
        public virtual DbSet<Vehicle> Vehicles { get; set; }
        public virtual DbSet<AdditionalVehicleInfo> AdditionalVehicleInfo { get; set; }
        public virtual DbSet<VehicleVariable> VehicleVariables { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            // Configure the relationship between Vehicle and AdditionalCarInfo
            // Primary key for AdditionalCarInfo is a composite key of VariableId, VehicleId, and Value.
            // Value is added to the composite key for future proofing. If we ever need to add more car info, there are duplicate VariableId's in the API (e.g. 129 "Other Engine Info")
            modelBuilder.Entity<AdditionalVehicleInfo>()
                .HasKey(aci => new { aci.VariableId, aci.VehicleId, aci.Value });

            modelBuilder.Entity<AdditionalVehicleInfo>()
                .HasOne(aci => aci.Vehicle)
                .WithMany(c => c.AdditionalVehicleInfo)
                .HasForeignKey(aci => aci.VehicleId);

            modelBuilder.Entity<AdditionalVehicleInfo>()
                .HasOne(aci => aci.Variable)
                .WithMany()
                .HasForeignKey(aci => aci.VariableId);


            // Seed Variables with important ones from the API
            modelBuilder.Entity<VehicleVariable>().HasData(
              new VehicleVariable { Id = 5, Name = "Body Class" },
              new VehicleVariable { Id = 15, Name = "Drive Type" },
              new VehicleVariable { Id = 24, Name = "Fuel Type - Primary" },
              new VehicleVariable { Id = 26, Name = "Make" },
              new VehicleVariable { Id = 27, Name = "Manufacturer Name" },
              new VehicleVariable { Id = 28, Name = "Model" },
              new VehicleVariable { Id = 29, Name = "Model Year" },
              new VehicleVariable { Id = 34, Name = "Series" },
              new VehicleVariable { Id = 37, Name = "Transmission Style" },
              new VehicleVariable { Id = 38, Name = "Trim" },
              new VehicleVariable { Id = 39, Name = "Vehicle Type" },
              new VehicleVariable { Id = 64, Name = "Engine Configuration" },
              new VehicleVariable { Id = 66, Name = "Fuel Type - Secondary" },
              new VehicleVariable { Id = 139, Name = "Top Speed" }
          );

            base.OnModelCreating(modelBuilder);
        }
    }
}