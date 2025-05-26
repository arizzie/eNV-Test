using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Data.Models
{

    // AdditionalVehicleInfo class represents additional information about a vehicle.
    // It is done in a way that you dynamically add new variables to the database without changing the code
    [Table("AdditionalCarInfo")]
    public class AdditionalVehicleInfo
    {
        public string Value { get; set; }

        [ForeignKey(nameof(Variable))]
        public int VariableId { get; set; }
        public VehicleVariable Variable { get; set; }

        [ForeignKey(nameof(Vehicle))]
        public string VehicleId { get; set; }
        [JsonIgnore]
        public Vehicle Vehicle { get; set; }
    }
}