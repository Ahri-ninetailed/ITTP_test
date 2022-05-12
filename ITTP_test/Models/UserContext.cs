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
            //Database.EnsureDeleted();
            Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //изначальная запись админа в бд
            modelBuilder.Entity<User>().HasData(
                new User()
                {
                    Guid = Guid.NewGuid(),
                    Login = "Admin",
                    Password = "Admin",
                    Name = "Admin",
                    Genger = 2,
                    Admin = true,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "Admin",
                    ModifiedOn = DateTime.Now,
                    ModifiedBy = "Admin",

                });
            //ограничение уникальности к атрибуту Login
            modelBuilder.Entity<User>().HasIndex(u => u.Login).IsUnique(true);
            //поле Admin по умолчанию false
            modelBuilder.Entity<User>().Property(u => u.Admin).HasDefaultValue(false);
            //если пользователь не указал гендер, то он неизвестен
            modelBuilder.Entity<User>().Property(u => u.Genger).HasDefaultValue(2);
            modelBuilder.Entity<User>().Property(u => u.CreatedOn).HasDefaultValue(DateTime.Now);
            modelBuilder.Entity<User>().Property(u => u.ModifiedOn).HasDefaultValue(DateTime.Now);
        }
        public DbSet<User> Users { get; set; }
    }
}
