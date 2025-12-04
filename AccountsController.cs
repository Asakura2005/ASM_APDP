csharp SIMS\Controllers\AccountsController.cs
// (showing only the updated constructor and Create POST)
using SIMS.DatabaseContext;
using SIMS.DatabaseContext.Entities;
using Microsoft.EntityFrameworkCore;
// ... other usings ...

namespace SIMS.Controllers
{
    public class AccountsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<long>> _roleManager;
        private readonly SimDbContext _dbContext;

        public AccountsController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<long>> roleManager,
            SimDbContext dbContext)    // inject DbContext
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dbContext = dbContext;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAccountViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // pre-checks
            if (await _userManager.FindByNameAsync(model.UserName) != null)
            {
                ModelState.AddModelError(nameof(model.UserName), "Tên đăng nhập đã tồn tại.");
                return View(model);
            }
            if (!string.IsNullOrWhiteSpace(model.Email) && await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng.");
                return View(model);
            }

            // create user + domain row in transaction
            using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                };

                var createResult = await _userManager.CreateAsync(user, model.Password);
                if (!createResult.Succeeded)
                {
                    foreach (var e in createResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
                    return View(model);
                }

                // ensure role exists and assign
                if (!string.IsNullOrWhiteSpace(model.Role))
                {
                    if (!await _roleManager.RoleExistsAsync(model.Role))
                        await _role_manager.CreateAsync(new IdentityRole<long>(model.Role));

                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                // create domain record depending on role
                if (string.Equals(model.Role, "User", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(model.Role, "Student", StringComparison.OrdinalIgnoreCase))
                {
                    var student = new Student
                    {
                        UserId = user.Id,
                        StudentNumber = "", // fill from model if you add fields
                        FullName = user.UserName ?? string.Empty,
                        PhoneNumber = "",
                        Address = ""
                    };
                    _dbContext.Students.Add(student);
                }
                else if (string.Equals(model.Role, "Teacher", StringComparison.OrdinalIgnoreCase))
                {
                    var teacher = new Teacher
                    {
                        UserId = user.Id,
                        FullName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty
                    };
                    _dbContext.Teachers.Add(teacher);
                }

                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["SuccessMessage"] = $"Tài khoản {model.UserName} đã được tạo.";
                return RedirectToAction(nameof(ManageUsers));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();

                // try to clean up identity user if created (avoid orphan)
                var createdUser = await _userManager.FindByNameAsync(model.UserName);
                if (createdUser != null)
                    await _userManager.DeleteAsync(createdUser);

                ModelState.AddModelError(string.Empty, "Lỗi khi lưu dữ liệu. Vui lòng thử lại.");
                // log ex if you have logger
                return View(model);
            }
        }

        // ... other actions ...
    }
}