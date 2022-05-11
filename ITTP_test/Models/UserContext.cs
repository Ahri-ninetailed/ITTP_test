using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
namespace ITTP_test.Models
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>().HasData(
                new User()
                {
                    Guid = Guid.NewGuid(),
                    Login = "Admin",
                    Password = "Admin",
                    Name = "Admin",
                    Genger = 2,
                    Birthday = null,
                    Admin = true,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "Admin",
                    ModifiedOn = DateTime.Now,
                    ModifiedBy = "Admin",

                });
        }
        public DbSet<User> Users { get; set; }
    }
}
