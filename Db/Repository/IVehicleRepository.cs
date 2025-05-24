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
        Task<PaginationResponse<Vehicle>> GetVehiclesAsync(GetVinsQuery query);
        Task<Vehicle?> GetVehicleByVinAsync(string vin);
    }
}
