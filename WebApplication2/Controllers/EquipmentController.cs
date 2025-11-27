using Microsoft.AspNetCore.Mvc;
using Rent.Data;
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

    }
}
