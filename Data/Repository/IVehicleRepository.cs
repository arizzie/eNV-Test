using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository
{
    public interface IVehicleRepository
    {
        Task<PaginationResponse> GetVehiclesAsync(GetVinsQuery query);
        Task<Vehicle?> GetVehicleByVinAsync(string vin);
        Task<int> SaveVehiclesBatchAsync(IEnumerable<Vehicle> vehiclesToProcess);
        Task<IEnumerable<VehicleVariable>> GetVariableFilter();
        Task<Dictionary<string,Vehicle>> GetVehiclesByVinAsync(IEnumerable<string> Vins);
    }
}
