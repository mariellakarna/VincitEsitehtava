using HuonevarausAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HuonevarausAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
    }
}
