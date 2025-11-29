using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rent.Data;
using Rent.DTO;
using Rent.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Rent.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly DataContext _db;

        public WorkersController(UserManager<User> userManager, DataContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] CreateWorkerDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new User
            {
                UserName = dto.Email,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                First_name = dto.FirstName,
                Last_name = dto.LastName,
                Login = dto.Email,
            };

            var createUser = await _userManager.CreateAsync(user, dto.Password);
            if (!createUser.Succeeded)
                return BadRequest(new { Errors = createUser.Errors.Select(e => e.Description) });

            await _userManager.AddToRoleAsync(user, "Worker");

            // Walidacja istnienia RentalInfo
            bool rentalExists = await _db.RentalInfo.AnyAsync(r => r.Id == dto.RentalInfoId);
            if (!rentalExists)
                return BadRequest(new { Message = $"RentalInfo o Id={dto.RentalInfoId} nie istnieje." });

            var worker = new Worker
            {
                First_name = dto.FirstName,
                Last_name = dto.LastName,
                Email = dto.Email,
                Phone_number = dto.PhoneNumber,
                Address = dto.Address,
                WorkStart = dto.WorkStart,
                WorkEnd = dto.WorkEnd,
                Working_Days = dto.Working_Days,
                Job_Title = dto.Job_Title,
                RentalInfoId = dto.RentalInfoId
            };

            _db.Workers.Add(worker);
            await _db.SaveChangesAsync();

            return Ok(new { Message = "Worker created successfully", UserId = user.Id, WorkerId = worker.Id });
        }
    }
}