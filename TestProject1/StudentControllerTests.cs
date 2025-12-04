using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SIMS.Controllers;
using SIMS.DatabaseContext;
using SIMS.DatabaseContext.Entities;
using SIMS.Models.Student;
using SIMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace TestProject1
{
    public class StudentControllerTests : IDisposable
    {
        private readonly SimDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly StudentController _controller;

        private readonly ApplicationUser _studentUser = new ApplicationUser
        {
            Id = 52,                    // user_id của Lo Chi Tham
            UserName = "lochitham@gmail.com"
        };

        private readonly Student _studentProfile = new Student
        {
            Id = 15,                    // student_id
            UserId = 52,
            FullName = "Lo Chi Tham",
            StudentNumber = "BH92929"
        };

        public StudentControllerTests()
        {
            var options = new DbContextOptionsBuilder<SimDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new SimDbContext(options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_studentUser);

            SeedDatabase().Wait();

            _controller = new StudentController(_context, _mockUserManager.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, _studentUser.UserName)
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        private async Task SeedDatabase()
        {
            await _context.Database.EnsureCreatedAsync();

            // USERS
            _context.Users.Add(_studentUser);

            // STUDENTS (11 students)
            _context.Students.AddRange(
                _studentProfile,
                new Student { Id = 16, UserId = 54, FullName = "Tong Giang", StudentNumber = "BH0001" },
                new Student { Id = 17, UserId = 55, FullName = "Luu Tuan Nghia", StudentNumber = "BH0002" },
                new Student { Id = 18, UserId = 56, FullName = "Ngo Dung", StudentNumber = "BH0003" },
                new Student { Id = 19, UserId = 57, FullName = "Lam Xung", StudentNumber = "BH0004" },
                new Student { Id = 20, UserId = 58, FullName = "Tan Minh", StudentNumber = "BH0005" },
                new Student { Id = 21, UserId = 59, FullName = "Hoa Vinh", StudentNumber = "BH0006" },
                new Student { Id = 22, UserId = 60, FullName = "Vo Tong", StudentNumber = "BH0008" },
                new Student { Id = 23, UserId = 61, FullName = "Dong Binh", StudentNumber = "BH0009" },
                new Student { Id = 25, UserId = 66, FullName = "Quan Thang Dao", StudentNumber = "BH0011" },
                new Student { Id = 27, UserId = 70, FullName = "Duong Hung", StudentNumber = "BH0013" }
            );

            // TEACHERS
            _context.Teachers.Add(new Teacher
            {
                Id = 9,
                UserId = 53,
                FullName = "Tieu Cai",
                TeacherNumber = "TC01"
            });

            // COURSES
            _context.Courses.Add(new Course { Id = 9, Name = "PE" });
            _context.Courses.Add(new Course { Id = 14, Name = "Vovinam" });

            // CLASSES
            _context.Classes.Add(new Class
            {
                Id = 14,
                Name = "Vovinam",
                Schedule = "Monday, 6am",
                CourseId = 14
            });

            // CLASS ASSIGNMENTS
            _context.ClassAssignments.AddRange(
                new ClassAssignment { Id = 69, StudentId = 15, TeacherId = 9, ClassId = 14, CourseId = 9 },
                new ClassAssignment { Id = 71, StudentId = 16, TeacherId = 9, ClassId = 14, CourseId = 14 },
                new ClassAssignment { Id = 72, StudentId = 19, TeacherId = 9, ClassId = 14, CourseId = 14 },
                new ClassAssignment { Id = 73, StudentId = 18, TeacherId = 9, ClassId = 14, CourseId = 14 },
                new ClassAssignment { Id = 74, StudentId = 21, TeacherId = 9, ClassId = 14, CourseId = 14 },
                new ClassAssignment { Id = 75, StudentId = 20, TeacherId = 9, ClassId = 14, CourseId = 14 }
            );

            // ATTENDANCE (cố ý để FAIL 1 test)
            _context.Attendances.AddRange(
                new Attendance { Id = 17, StudentId = 15, ClassId = 14, CourseId = 9, AttendanceDate = DateTime.Today, Status = "Present" },
                new Attendance { Id = 18, StudentId = 21, ClassId = 14, CourseId = 14, AttendanceDate = DateTime.Today, Status = "Present" },
                new Attendance { Id = 19, StudentId = 19, ClassId = 14, CourseId = 14, AttendanceDate = DateTime.Today, Status = "Late" },
                new Attendance { Id = 20, StudentId = 18, ClassId = 14, CourseId = 14, AttendanceDate = DateTime.Today, Status = "Absent" }
            );

            await _context.SaveChangesAsync();
        }

        public void Dispose() => _context.Dispose();

        // ====== TEST GIỮ NGUYÊN GỐC ======

        [Fact]
        public async Task Index_Admin_ReturnsFilteredStudents()
        {
            var result = await _controller.Index("BH92929");
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<StudentListViewModel>>(view.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Index_Admin_ReturnsAllStudents()
        {
            var result = await _controller.Index(null);
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<StudentListViewModel>>(view.Model);
            Assert.Equal(11, model.Count);
        }

        [Fact]
        public async Task Schedule_Student_ReturnsCorrectSchedule()
        {
            var result = await _controller.Schedule();
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<StudentScheduleViewModel>>(view.Model);

            Assert.Single(model);
            Assert.Equal("Vovinam", model[0].ClassName);
        }

        // ❗ TEST NÀY CỐ TÌNH CHO FAIL
        [Fact]
        public async Task AttendanceReport_ReturnsCorrectReport()
        {
            var result = await _controller.AttendanceReport();
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<StudentAttendanceReportViewModel>(view.Model);

            Assert.Equal(999, model.TotalRecords);       // Sai cố ý
        }
    }
}
