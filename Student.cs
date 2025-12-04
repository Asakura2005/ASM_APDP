using System;

namespace SIMS.DatabaseContext.Entities
{
    public class Student
    {
        public long Id { get; set; }
        public long UserId { get; set; }           // FK to aspnetusers (ApplicationUser.Id)
        public string StudentNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}