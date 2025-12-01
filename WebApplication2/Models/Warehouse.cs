using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rent.Models
{
    public class Warehouse
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string Equipment_name { get; set; }
        public int Quantity { get; set; }
        [MaxLength(255)]
        public string? Sizes { get; set; }

        public ICollection<Worker> Workers { get; set; } = new List<Worker>();
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
    }
}
