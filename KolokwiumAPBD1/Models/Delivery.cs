namespace KolokwiumAPBD1.Models;

public class Delivery
{
    public int DeliveryId { get; set; }
    public int CustomerId { get; set; }
    public string DriverId { get; set; }
    public DateTime Date { get; set; }
}