using Microsoft.EntityFrameworkCore;

namespace TodoWebApi.Models.DbContexts
{
    public class MasterDbContext : DbContext
    {
        public MasterDbContext()
        {
        }

        public MasterDbContext(DbContextOptions<MasterDbContext> options)
            : base(options)
        {
        }


        public virtual DbSet<User> Users { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity.ToTable("user");

                entity.Property(e => e.Id).HasColumnType("int(11)");
                entity.Property(e => e.PasswordHash).HasMaxLength(200);
                entity.Property(e => e.PasswordSlat).HasMaxLength(200);
                entity.Property(e => e.Username).HasMaxLength(200);
            });

            
        }

        
    }
}
