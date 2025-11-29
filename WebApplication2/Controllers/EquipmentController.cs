using Microsoft.AspNetCore.Mvc;
using Rent.Data;
using Rent.DTO;
using Rent.Models;
using System.Linq;

namespace RentControllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            
          var allEquipment =  dbContext.Equipment.ToList();
            return Ok(allEquipment);
        }


        [HttpPost] 
        public IActionResult AddEquipment(CreateEquipmentDTO addEquipment)
        {
            var equipmentEntity = new Equipment()
            {
                Name = addEquipment.Name,
                Price = addEquipment.Price,
                Size = addEquipment.Size
            };
            dbContext.Equipment.Add(equipmentEntity);
            dbContext.SaveChanges();
            return Ok(equipmentEntity);
        }

     
    }
}
