//DB model for a Vehicle
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    // Vehicle class represents a vehicle with a VIN as primary key in the database.

    [Table("Vehicle")]
    public class Vehicle
    {
        [Key]
        public string Vin { get; set; }
        public string DealerId { get; set; }
        public DateTime ModifiedDate { get; set; }
        public ICollection<AdditionalVehicleInfo> AdditionalVehicleInfo { get; set; }
    }
}