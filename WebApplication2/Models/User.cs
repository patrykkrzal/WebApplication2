using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rent.Models
{
    // Domain user extending IdentityUser; do NOT redeclare Id/PasswordHash/Email/PhoneNumber.
    public class User : IdentityUser
    {
        [Required, MaxLength(50)]
        public string First_name { get; set; }

        [Required, MaxLength(50)]
        public string Last_name { get; set; }

        // Optional separate login alias (Identity uses UserName)
        [MaxLength(50)]
        public string? Login { get; set; }

        [MaxLength(30)]
        public string Role { get; set; } = "user";

        public RentalInfo? RentalInfo { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
