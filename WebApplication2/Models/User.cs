using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rent.Models
{
    // IdentityUser already defines: Id (string), UserName, Email, PhoneNumber, PasswordHash, etc.
    // Keep only additional domain properties to avoid conflicts.
    public class User : IdentityUser
    {
     
        [MaxLength(50)]
        public string First_name { get; set; }

      
        [MaxLength(50)]
        public string Last_name { get; set; }

        [MaxLength(50)]
        public string? Login { get; set; }

    
        public RentalInfo? RentalInfo { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
