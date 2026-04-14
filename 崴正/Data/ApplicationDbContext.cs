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
        public DbSet<QuantityRuleBase> QuantityRuleBases { get; set; }
public DbSet<HistoricalTransaction> HistoricalTransactions { get; set; }
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
            modelBuilder.Entity<QuantityRuleBase>(entity =>
            {
                entity.ToTable("quantity_rule_base");
                entity.HasKey(e => e.RuleID);
                
                // 加上這幾行翻譯蒟蒻，告訴 EF Core 對應到資料庫裡真實的小寫欄位名稱
                entity.Property(e => e.RuleID).HasColumnName("rule_id");
                entity.Property(e => e.Material_ID).HasColumnName("material_id");
                entity.Property(e => e.Base_Unit_Ratio).HasColumnName("base_unit_ratio");
                entity.Property(e => e.Z_Score_Threshold).HasColumnName("z_score_threshold");
            });

            modelBuilder.Entity<HistoricalTransaction>(entity =>
            {
                entity.ToTable("historical_transaction");
                entity.HasKey(e => e.Transaction_ID);

                // 歷史紀錄表也一樣要加翻譯
                entity.Property(e => e.Transaction_ID).HasColumnName("transaction_id");
                entity.Property(e => e.Project_ID).HasColumnName("project_id");
                entity.Property(e => e.Material_ID).HasColumnName("material_id");
                entity.Property(e => e.Actual_Quantity).HasColumnName("actual_quantity");
                entity.Property(e => e.Unit_Price).HasColumnName("unit_price");
                entity.Property(e => e.Transaction_Date).HasColumnName("transaction_date");
            });
            
        }
    }
}
