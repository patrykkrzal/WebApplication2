using Microsoft.AspNetCore.Mvc;
using Rent.Data;
using Rent.DTO;
using Rent.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore; // for Database facade
using Microsoft.AspNetCore.Authorization;

namespace Rent.Controllers
{
    [ApiController]
    [Route("api/equipment")]
    public class EquipmentController : ControllerBase
    {
        private readonly DataContext dbContext;

        public EquipmentController(DataContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult GetAllEquipment()
        {
            var allEquipment = dbContext.Equipment.ToList();
            return Ok(allEquipment);
        }

        [Authorize(Roles="Admin,Worker")]
        [HttpPost("add")] // unique route to avoid Swagger conflicts
        public IActionResult AddEquipment([FromBody] CreateEquipmentDTO addEquipment)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // use stored procedure spAddEquipment
            dbContext.Database.ExecuteSqlRaw(
                "EXEC dbo.spAddEquipment @p0, @p1, @p2",
                (int)addEquipment.Type,
                (int)addEquipment.Size,
                addEquipment.Price
            );

            // Return refreshed list or simple acknowledgment
            return Ok(new { Message = "Equipment added via stored procedure" });
        }
    }
}
