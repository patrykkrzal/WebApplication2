using Rent.Enums;
using Rent.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rent.DTO
{
    public class CreateEquipmentDTO
    {
    
        public Size Size { get; set; }
        [Required] 
        public EquipmentType Type { get; set; }
        public decimal Price { get; set; }

    }
}

