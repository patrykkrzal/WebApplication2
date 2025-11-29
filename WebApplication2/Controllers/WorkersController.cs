using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DataContext _db;

        public WorkersController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, DataContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] CreateWorkerDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1) Utwórz konto użytkownika (Identity)
            var user = new User
            {
                UserName = dto.Email,        // login = email
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                First_name = dto.FirstName,
                Last_name = dto.LastName,
                Login = dto.Email,
                     // domenowe pole w Twoim User (opcjonalne)
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
                return BadRequest(new { Errors = createResult.Errors.Select(e => e.Description) });

            // 2) Zapewnij istnienie roli i przypisz
            const string roleName = "Worker";
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var roleCreate = await _roleManager.CreateAsync(new IdentityRole(roleName));
                if (!roleCreate.Succeeded)
                    return BadRequest(new { Errors = roleCreate.Errors.Select(e => e.Description), Message = "Nie udało się utworzyć roli 'Worker'." });
            }

            var addToRole = await _userManager.AddToRoleAsync(user, roleName);
            if (!addToRole.Succeeded)
                return BadRequest(new { Errors = addToRole.Errors.Select(e => e.Description), Message = "Nie udało się przypisać roli 'Worker'." });

            // 3) Utwórz encję domenową Worker (pola pracownika)
            var worker = new Worker
            {
                First_name = dto.FirstName,
                Last_name = dto.LastName,
                Email = dto.Email,
                Phone_number = dto.PhoneNumber,
                Address = dto.Address,
                Role = "worker",
                WorkStart = dto.WorkStart,
                WorkEnd = dto.WorkEnd,
                Working_Days = dto.Working_Days,
                Job_Title = dto.Job_Title
                // Jeśli chcesz powiązać z RentalInfo, dodaj dto.RentalInfoId i ustaw:
                // RentalInfo = await _db.RentalInfo.FindAsync(dto.RentalInfoId)
            };

            _db.Workers.Add(worker);
            await _db.SaveChangesAsync();

            return Ok(new { Message = "Worker created successfully", UserId = user.Id, WorkerId = worker.Id });
        }
    }
}
