using KolokwiumAPBD1.Exceptions;
using KolokwiumAPBD1.Models.DTOs;
using KolokwiumAPBD1.Services;
using Microsoft.AspNetCore.Mvc;

namespace KolokwiumAPBD1.Controllers;

[ApiController]
[Route("api/deliveries")]
public class DeliveriesController(IDbService dbService) : ControllerBase
{
    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetDeliveryDetailsById([FromRoute] int id)
    {
        try
        {
            return Ok(await dbService.GetDeliveryDetailsByIdAsync(id));
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddDelivery([FromBody] DeliveryCreateDTO delivery)
    {
        var myDelivery = await dbService.AddDeliveryAsync(delivery);
        return Created();
    }
}