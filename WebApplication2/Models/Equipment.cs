using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rent.Models
{
    public class Equipment
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(255)]
        public string? Name { get; set; }

        public string? size { get; set; }

        [Required]
        public bool Is_In_Werehouse { get; set; }

        [Required]
        public decimal Price { get; set; }

        public bool Is_Reserved { get; set; }

        public ICollection<OrderedItem> OrderedItems { get; set; } = new List<OrderedItem>();

        public RentalInfo? RentalInfo { get; set; }
    }
}
