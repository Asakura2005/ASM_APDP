// File: Controllers/AccountsController.cs

using Microsoft.AspNetCore.Mvc;
using SIMS.Models; // CreateAccountViewModel, UserListItemViewModel
using Microsoft.AspNetCore.Identity;
using SIMS.DatabaseContext.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using SIMS.DatabaseContext; // ADD THIS LINE

namespace SIMS.Controllers
{
    public class AccountsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<long>> _roleManager;
        private readonly SimDbContext _dbContext; // ADD THIS LINE

        public AccountsController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<long>> roleManager,
            SimDbContext dbContext) // ADD THIS LINE
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dbContext = dbContext; // ADD THIS LINE
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateAccountViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAccountViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

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

            using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email
                };

                var createResult = await _userManager.CreateAsync(user, model.Password);
                if (!createResult.Succeeded)
                {
                    foreach (var e in createResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
                    return View(model);
                }

                if (!string.IsNullOrWhiteSpace(model.Role))
                {
                    if (!await _roleManager.RoleExistsAsync(model.Role))
                        await _roleManager.CreateAsync(new IdentityRole<long>(model.Role));

                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                // domain record
                if (string.Equals(model.Role, "User", StringComparison.OrdinalIgnoreCase))
                {
                    _dbContext.Students.Add(new Student
                    {
                        UserId = user.Id,
                        FullName = model.UserName,
                        StudentNumber = "" // collect if needed
                    });
                }
                else if (string.Equals(model.Role, "Teacher", StringComparison.OrdinalIgnoreCase))
                {
                    _dbContext.Teachers.Add(new Teacher
                    {
                        UserId = user.Id,
                        FullName = model.UserName,
                        Email = model.Email
                    });
                }

                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["SuccessMessage"] = $"Tài khoản {model.UserName} đã được tạo.";
                return RedirectToAction(nameof(ManageUsers));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();

                // cleanup created identity user to avoid orphan
                var created = await _userManager.FindByNameAsync(model.UserName);
                if (created != null) await _userManager.DeleteAsync(created);

                // log ex if you have ILogger
                ModelState.AddModelError(string.Empty, "Lỗi khi lưu dữ liệu. Vui lòng thử lại.");
                return View(model);
            }
        }

        // Return list of users (with their primary role) to the view
        public async Task<IActionResult> ManageUsers()
        {
            var users = _userManager.Users.ToList();
            var model = new List<UserListItemViewModel>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                model.Add(new UserListItemViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? string.Empty
                });
            }

            return View(model);
        }

        public IActionResult ManageTeachers()
        {
            return View();
        }
    }
}