using Tutorial9.Model;

namespace Tutorial9.Services;

public interface IWarehouseService
{
    Task<int> AddProductAsync(WarehouseDTO warehouseDto);
}