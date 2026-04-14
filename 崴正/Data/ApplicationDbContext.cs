using Microsoft.EntityFrameworkCore;
using 崴正.Models;

namespace 崴正.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProjectMaster> ProjectMasters { get; set; }
        public DbSet<MaterialMaster> MaterialMasters { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProjectMaster>(entity =>
            {
                entity.ToTable("project_master");
                entity.HasKey(e => e.ProjectId);

                entity.Property(e => e.ProjectId).HasColumnName("project_id");
                entity.Property(e => e.ProjectName).HasColumnName("project_name");
                entity.Property(e => e.TotalFloorArea).HasColumnName("total_floor_area");
                entity.Property(e => e.StartDate).HasColumnName("start_date");
                entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            });

            modelBuilder.Entity<MaterialMaster>(entity =>
            {
                entity.ToTable("material_master");
                entity.HasKey(e => e.MaterialId);

                entity.Property(e => e.MaterialId).HasColumnName("material_id");
                entity.Property(e => e.MaterialName).HasColumnName("material_name");
                entity.Property(e => e.Category).HasColumnName("category");
                entity.Property(e => e.BaseUnit).HasColumnName("base_unit");
            });
        }
    }
}
