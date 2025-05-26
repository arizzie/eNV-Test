using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
    // VehicleVariable class represents a variable that can be associated with a vehicle.
    // This will be used to filter what data we want from the API
    // This is similar to what is done in the API

    [Table("VehicleVariable")]
    public class VehicleVariable
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
