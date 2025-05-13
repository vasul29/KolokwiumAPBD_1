using KolokwiumAPBD1.Exceptions;
using KolokwiumAPBD1.Models;
using KolokwiumAPBD1.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace KolokwiumAPBD1.Services;

public interface IDbService
{
    public Task<IEnumerable<DeliveryGetDetailsDTO>> GetDeliveryDetailsByIdAsync(int id);
    public Task<Delivery> AddDeliveryAsync(DeliveryCreateDTO delivery);
}
public class DbService(IConfiguration configuration) : IDbService
{
    private readonly string? _connectionString = configuration.GetConnectionString("Default");

    public async Task<IEnumerable<DeliveryGetDetailsDTO>> GetDeliveryDetailsByIdAsync(int id)
    {
        var result = new List<DeliveryGetDetailsDTO>();
        await using var connection = new SqlConnection(_connectionString);
        const string testId = "select * from delivery where delivery_id = @id";
        await using var commandTest = new SqlCommand(testId, connection);
        commandTest.Parameters.AddWithValue("@id", id);
        
        await connection.OpenAsync();
        var exists = await commandTest.ExecuteScalarAsync();
        if (exists == null)
        {
            throw new NotFoundException($"Delivery with id {id} not exist");
        }

        const string sql2 = "select date, C.first_name, C.last_name, C.date_of_birth, D.first_name, D.last_name, D.licence_number, P.name, P.price, PD.amount from Delivery join Customer C on C.customer_id = Delivery.customer_id join Driver D on D.driver_id = Delivery.driver_id join Product_Delivery PD on Delivery.delivery_id = PD.delivery_id join Product P on PD.product_id = P.product_id where Delivery.delivery_id = @id";
        
        await using var command = new SqlCommand(sql2, connection);
        command.Parameters.AddWithValue("@id", id);

        var products = new List<ProductInDeliveryDTO>();
        DateTime deliveryDate = default;
        CustomerDTO customer = null;
        DriverDTO driver = null;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (customer == null)
            {
                deliveryDate = reader.GetDateTime(0);

                customer = new CustomerDTO
                {
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    DateOfBirth = reader.GetDateTime(3)
                };

                driver = new DriverDTO
                {
                    FirstName = reader.GetString(4),
                    LastName = reader.GetString(5),
                    LicenceNumber = reader.GetString(6)
                };
            }

            var product = new ProductInDeliveryDTO
            {
                Name = reader.GetString(7),
                Price = reader.GetDecimal(8),
                Amount = reader.GetInt32(9)
            };
            products.Add(product);
        }

        if (customer != null && driver != null && products.Count > 0)
        {
            result.Add(new DeliveryGetDetailsDTO
            {
                Date = deliveryDate,
                Customer = customer,
                Driver = driver,
                Products = products
            });
        }
        return result;
    }

    public async Task<Delivery> AddDeliveryAsync(DeliveryCreateDTO delivery)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var productIds = new Dictionary<string, int>();
            
            
            var checkDeliveryCmd = new SqlCommand("SELECT * FROM Delivery WHERE delivery_id = @id", connection, (SqlTransaction)transaction);
            checkDeliveryCmd.Parameters.AddWithValue("@id", delivery.DeliveryId);
            var exists = await checkDeliveryCmd.ExecuteScalarAsync();
            if (exists != null)
                throw new Exception($"There is already {delivery.DeliveryId}.");
            
            var checkCustomerCmd = new SqlCommand("SELECT * FROM Customer WHERE customer_id = @id", connection, (SqlTransaction)transaction);
            checkCustomerCmd.Parameters.AddWithValue("@id", delivery.CustomerId);
            exists = await checkCustomerCmd.ExecuteScalarAsync();
            if (exists == null)
                throw new Exception($"There is no client with {delivery.CustomerId}.");
            
            
            var insertDeliveryCmd = new SqlCommand(
                "INSERT INTO Delivery (delivery_id, customer_id, driver_id, date) VALUES (@deliveryId, @customerId, @driverId, @date)",
                connection, (SqlTransaction)transaction);
            insertDeliveryCmd.Parameters.AddWithValue("@deliveryId", delivery.DeliveryId);
            insertDeliveryCmd.Parameters.AddWithValue("@customerId", delivery.CustomerId);
            insertDeliveryCmd.Parameters.AddWithValue("@driverId", delivery.LicenseNumer);
            insertDeliveryCmd.Parameters.AddWithValue("@date", DateTime.Now);
            await insertDeliveryCmd.ExecuteNonQueryAsync();
            
            foreach (var product in delivery.Products)
            {
                var insertPDcmd = new SqlCommand(
                    "INSERT INTO Product_Delivery (product_id, delivery_id, amount) VALUES (@productId, @deliveryId, @amount)",
                    connection, (SqlTransaction)transaction);
                insertPDcmd.Parameters.AddWithValue("@productId", productIds[product.Name]);
                insertPDcmd.Parameters.AddWithValue("@deliveryId", delivery.DeliveryId);
                insertPDcmd.Parameters.AddWithValue("@amount", product.Amount);
                await insertPDcmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            
            return new Delivery
            {
                DeliveryId = delivery.DeliveryId,
                CustomerId = delivery.CustomerId,
                DriverId = delivery.LicenseNumer,
                Date = DateTime.Now
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        
    }
}