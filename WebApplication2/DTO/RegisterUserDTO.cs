using Rent.Models;
using System.ComponentModel.DataAnnotations;

namespace Rent.DTO
{
    public class CreateUserDTO
    {

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
        public string Password{ get; set; }

        [Required]
        [MaxLength(50)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MaxLength(9)]
        public string Phone_number { get; set; }

        [MaxLength(30)]
        public string Role { get; set; } = "user";
    }
}
