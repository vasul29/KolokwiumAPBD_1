namespace KolokwiumAPBD1.Models.DTOs;

public class DeliveryCreateDTO
{
    public int DeliveryId { get; set; }
    public int CustomerId { get; set; }
    public string LicenseNumer { get; set; }
    public List<ProductInDeliveryDTO> Products { get; set; }
}