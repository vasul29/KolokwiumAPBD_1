namespace KolokwiumAPBD1.Models.DTOs;

public class DeliveryGetDetailsDTO
{
    public DateTime Date { get; set; }
    public CustomerDTO Customer { get; set; }
    public DriverDTO Driver { get; set; }
    public List<ProductInDeliveryDTO> Products { get; set; }
}