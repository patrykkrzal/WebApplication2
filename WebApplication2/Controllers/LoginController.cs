using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Rent.Models;
using Rent.DTO; // LoginUserDTO
using System.Threading.Tasks;

namespace Rent.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<User> _signInManager;

        public AuthController(SignInManager<User> signInManager)
        {
            _signInManager = signInManager;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _signInManager.PasswordSignInAsync(dto.UserName, dto.Password, false, false);

            if (!result.Succeeded)
                return Unauthorized(new { Message = "Invalid username or password" });

            return Ok(new { Message = "Login successful" });
        }
    }
}