using Xunit;
using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext;
using SIMS.Controllers;
using SIMS.DatabaseContext.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIMS.Models;
using System;
using Moq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;

namespace TestProject1
{
    public class ClassControllerTests : IDisposable
    {
        private readonly DbContextOptions<SimDbContext> _dbOptions;
        private readonly SimDbContext _context;
        private readonly ClassController _controller;

        public ClassControllerTests()
        {
            _dbOptions = new DbContextOptionsBuilder<SimDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new SimDbContext(_dbOptions);
            SeedDatabase().Wait();

            _controller = new ClassController(_context);

            var httpContext = new DefaultHttpContext();
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        private async Task SeedDatabase()
        {
            await _context.Database.EnsureCreatedAsync();

            // COURSES
            var math = new Course { Id = 1, Name = "Math" };
            var pe = new Course { Id = 9, Name = "PE" };
            var vovinamCourse = new Course { Id = 14, Name = "Vovinam" };

            _context.Courses.AddRange(math, pe, vovinamCourse);

            // CLASSES (từ database thật)
            var classA = new Class { Id = 1, Name = "Class A", Schedule = "Mon AM", CourseId = 1 };
            var football = new Class { Id = 7, Name = "Football Class", Schedule = "Tue PM" };
            var vovinam = new Class { Id = 14, Name = "Vovinam", Schedule = "Monday, 6am", CourseId = 9 };
            var se07202 = new Class { Id = 16, Name = "SE07202", Schedule = "Friday, 8pm" }; // unassigned

            _context.Classes.AddRange(classA, football, vovinam, se07202);

            // STUDENTS
            var stu1 = new Student { Id = 15, FullName = "Lo Chi Tham", StudentNumber = "BH92929" };
            var stu2 = new Student { Id = 16, FullName = "Tong Giang", StudentNumber = "BH0001" };

            _context.Students.AddRange(stu1, stu2);

            // ASSIGNMENTS (assign class 14)
            _context.ClassAssignments.Add(new ClassAssignment
            {
                ClassId = 14,
                CourseId = 9,
                StudentId = 15
            });

            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        // ===========================
        // GIỮ NGUYÊN TOÀN BỘ TEST BÊN DƯỚI
        // ===========================

        [Fact]
        public async Task Index_NoSearchString_ReturnsAllClasses()
        {
            var result = await _controller.Index(null);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Class>>(view.Model);

            Assert.Equal(4, model.Count);
        }

        [Theory]
        [InlineData("Class", 2)]
        [InlineData("Vovi", 1)]
        [InlineData("SE0", 1)]
        [InlineData("XYZ", 0)]
        public async Task Index_WithSearchString_ReturnsFilteredClasses(string search, int count)
        {
            var result = await _controller.Index(search);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Class>>(view.Model);

            Assert.Equal(count, model.Count);
        }

        [Fact]
        public async Task Details_WithValidId_ReturnsClassDetailsViewModelWithRoster()
        {
            var result = await _controller.Details(14); // Vovinam

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<ClassDetailsViewModel>(view.Model);

            Assert.Equal(14, model.ClassId);
            Assert.Equal("Vovinam", model.ClassName);

            Assert.True(model.CourseRoster.ContainsKey("PE"));
            Assert.Single(model.CourseRoster["PE"]);
            Assert.Contains(model.CourseRoster["PE"], s => s.FullName == "Lo Chi Tham");
        }

        [Fact]
        public async Task Create_Post_WithValidModel_AddsClassAndRedirects()
        {
            var newClass = new Class { Name = "New Class", Schedule = "Sat 9AM" };

            var result = await _controller.Create(newClass);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal(5, _context.Classes.Count());
            Assert.NotNull(_controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task Edit_Post_WithValidId_UpdatesClassAndRedirects()
        {
            var c = await _context.Classes.FindAsync(16L);
            c.Name = "Updated SE Class";

            var result = await _controller.Edit(16L, c);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var updated = await _context.Classes.FindAsync(16L);
            Assert.Equal("Updated SE Class", updated.Name);
        }

        [Fact]
        public async Task Delete_Get_ForAssignedClass_ShowsErrorMessage()
        {
            var result = await _controller.Delete(14L);

            var view = Assert.IsType<ViewResult>(result);
            Assert.NotNull(view.ViewData["ErrorMessage"]);
        }

        [Fact]
        public async Task DeleteConfirmed_DeletesUnassignedClass()
        {
            var result = await _controller.DeleteConfirmed(16L);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal(3, _context.Classes.Count());
        }

        [Fact]
        public async Task DeleteConfirmed_ForAssignedClass_FailsAndRedirects()
        {
            var result = await _controller.DeleteConfirmed(14L);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Delete", redirect.ActionName);
            Assert.Equal(4, _context.Classes.Count());
        }
    }
}
