csharp SIMS\DatabaseContext\SimDbContext.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext.Entities;

namespace SIMS.DatabaseContext
{
    public class SimDbContext : IdentityDbContext<ApplicationUser, IdentityRole<long>, long>
    {
        public SimDbContext(DbContextOptions<SimDbContext> options) : base(options)
        {
        }

        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<Teacher> Teachers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>().ToTable("aspnetusers");
            modelBuilder.Entity<IdentityRole<long>>().ToTable("aspnetroles");
            modelBuilder.Entity<IdentityUserRole<long>>().ToTable("aspnetuserroles");
            modelBuilder.Entity<IdentityUserClaim<long>>().ToTable("aspnetuserclaims");
            modelBuilder.Entity<IdentityUserLogin<long>>().ToTable("aspnetuserlogins");
            modelBuilder.Entity<IdentityRoleClaim<long>>().ToTable("aspnetroleclaims");
            modelBuilder.Entity<IdentityUserToken<long>>().ToTable("aspnetusertokens");

            // Map custom tables if you already have them named differently:
            modelBuilder.Entity<Student>().ToTable("students");
            modelBuilder.Entity<Teacher>().ToTable("teachers");
        }
    }
}