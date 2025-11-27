using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Rent.Models;

namespace Rent.DTO
{
    public class CreateEquipmentDTO
    {
    
        public string? Size { get; set; }
        [Required] 
        public string Name { get; set; }
        public decimal Price { get; set; }

        public ICollection<OrderedItem> OrderedItems { get; set; } = new List<OrderedItem>();
    }
}

