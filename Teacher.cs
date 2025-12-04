csharp SIMS\DatabaseContext\Entities\Teacher.cs
using System;

namespace SIMS.DatabaseContext.Entities
{
    public class Teacher
    {
        public long Id { get; set; }
        public long UserId { get; set; }           // FK to aspnetusers
        public string StaffNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }
}