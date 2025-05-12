using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Tutorial9.Exceptions;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class WarehouseService : IWarehouseService
{
    private readonly string _connectionString;
    public WarehouseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
    }
    
    public async Task<int> AddProductAsync(WarehouseDTO warehouseDto)
    {
        await using SqlConnection con = new SqlConnection(_connectionString);
        await using SqlCommand com = new SqlCommand();

        com.Connection = con;
        await con.OpenAsync();
        DbTransaction transaction = await con.BeginTransactionAsync();
        com.Transaction = transaction as SqlTransaction;

        try
        {
            com.CommandText = "SELECT COUNT(*) FROM Product WHERE IdProduct = @IdProduct";
            com.Parameters.AddWithValue("@IdProduct", warehouseDto.IdProduct);
            var productExists = (int)await com.ExecuteScalarAsync() > 0;
            if (!productExists) throw new NotFoundException("Product not found");

            com.Parameters.Clear();

            com.CommandText = "SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            com.Parameters.AddWithValue("@IdWarehouse", warehouseDto.IdWarehouse);
            var warehouseExists = (int)await com.ExecuteScalarAsync() > 0;
            if (!warehouseExists) throw new NotFoundException("Warehouse not found");

            com.Parameters.Clear();

            com.CommandText = @"
                SELECT IdOrder FROM [Order]
                WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt";
            com.Parameters.AddWithValue("@IdProduct", warehouseDto.IdProduct);
            com.Parameters.AddWithValue("@Amount", warehouseDto.Amount);
            com.Parameters.AddWithValue("@CreatedAt", warehouseDto.CreatedAt);

            int orderId = 0;
            await using (var reader = await com.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                    orderId = reader.GetInt32(0);
            }

            if (orderId == 0) throw new NotFoundException("Order not found");
            com.Parameters.Clear();

            com.CommandText = "SELECT COUNT(*) FROM Product_Warehouse WHERE IdOrder = @IdOrder";
            com.Parameters.AddWithValue("@IdOrder", orderId);
            var alreadyFulfilled = (int)await com.ExecuteScalarAsync() > 0;
            if (alreadyFulfilled) throw new ConflictException("Order already fulfilled");

            com.Parameters.Clear();

            com.CommandText = "UPDATE [Order] SET FulfilledAt = @FulfilledAt WHERE IdOrder = @IdOrder";
            com.Parameters.AddWithValue("@IdOrder", orderId);
            com.Parameters.AddWithValue("@FulfilledAt", DateTime.Now);
            await com.ExecuteNonQueryAsync();

            com.Parameters.Clear();

            com.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
            com.Parameters.AddWithValue("@IdProduct", warehouseDto.IdProduct);

            decimal unitPrice = 0;
            await using (var readerSecond = await com.ExecuteReaderAsync())
            {
                if (await readerSecond.ReadAsync())
                    unitPrice = readerSecond.GetDecimal(readerSecond.GetOrdinal("Price"));
            }

            com.Parameters.Clear();

            com.CommandText = @"
                INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);";

            var createdAt = DateTime.Now;

            com.Parameters.AddWithValue("@IdWarehouse", warehouseDto.IdWarehouse);
            com.Parameters.AddWithValue("@IdProduct", warehouseDto.IdProduct);
            com.Parameters.AddWithValue("@IdOrder", orderId);
            com.Parameters.AddWithValue("@Amount", warehouseDto.Amount);
            com.Parameters.AddWithValue("@Price", warehouseDto.Amount * unitPrice);
            com.Parameters.AddWithValue("@CreatedAt", createdAt);

            await com.ExecuteNonQueryAsync();
            com.Parameters.Clear();


            com.CommandText = @"
                SELECT IdProductWarehouse
                FROM Product_Warehouse
                WHERE IdWarehouse = @IdWarehouse
                AND IdProduct = @IdProduct
                AND IdOrder = @IdOrder
                AND Amount = @Amount
                AND Price = @Price
                AND CreatedAt = @CreatedAt;";

            com.Parameters.AddWithValue("@IdWarehouse", warehouseDto.IdWarehouse);
            com.Parameters.AddWithValue("@IdProduct", warehouseDto.IdProduct);
            com.Parameters.AddWithValue("@IdOrder", orderId);
            com.Parameters.AddWithValue("@Amount", warehouseDto.Amount);
            com.Parameters.AddWithValue("@Price", warehouseDto.Amount * unitPrice);
            com.Parameters.AddWithValue("@CreatedAt", createdAt);

            var insertedId = await com.ExecuteScalarAsync();
            await transaction.CommitAsync();

            return Convert.ToInt32(insertedId);

        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}