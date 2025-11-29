using System.ComponentModel.DataAnnotations;

namespace Rent.DTO
{
    public class LoginUserDTO
    {

        [Required]
        [MaxLength(50)]
        public string Login { get; set; }

        [Required]
        [MaxLength(255)]
        public string Password { get; set; }
    }
}
