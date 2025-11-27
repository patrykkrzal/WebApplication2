using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rent.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string First_name { get; set; }

        [Required]
        [MaxLength(50)]
        public string Last_name { get; set; }

        [Required]
        [MaxLength(50)]
        public string Login { get; set; }

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        [MaxLength(50)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MaxLength(9)]
        public string Phone_number { get; set; }

        [Required]
        [MaxLength(255)]
        public string Address { get; set; }

        [MaxLength(30)]
        public string Role { get; set; } = "user";

        public RentalInfo? RentalInfo { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
