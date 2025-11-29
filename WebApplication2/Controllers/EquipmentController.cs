using Microsoft.AspNetCore.Mvc;
using Rent.Data;
using Rent.DTO;
using Rent.Models;
using System.Linq;

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

        [HttpPost("add")] // unique route to avoid Swagger conflicts
        public IActionResult AddEquipment([FromBody] CreateEquipmentDTO addEquipment)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var equipmentEntity = new Equipment()
            {
                Name = addEquipment.Name,
                Price = addEquipment.Price,
                Size = addEquipment.Size
            };
            dbContext.Equipment.Add(equipmentEntity);
            dbContext.SaveChanges();
            return Created($"/api/equipment/{equipmentEntity.Id}", equipmentEntity);
        }
    }
}
