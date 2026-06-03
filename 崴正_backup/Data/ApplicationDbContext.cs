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
        public DbSet<ItemDictionary> ItemDictionaries { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
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

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("audit_log");
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.LogId).HasColumnName("log_id");
                entity.Property(e => e.SourceFileName).HasColumnName("source_file_name");
                entity.Property(e => e.SheetName).HasColumnName("sheet_name");
                entity.Property(e => e.RowNumber).HasColumnName("row_number");
                entity.Property(e => e.ItemName).HasColumnName("item_name");
                entity.Property(e => e.OriginalStatusMessage).HasColumnName("original_status_message");
                entity.Property(e => e.OverrideReason).HasColumnName("override_reason");
                entity.Property(e => e.ActionUser).HasColumnName("action_user");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
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
                
               
                entity.Property(e => e.RuleID).HasColumnName("rule_id");
                entity.Property(e => e.Material_ID).HasColumnName("material_id");
                entity.Property(e => e.Unit_Consumption).HasColumnName("unit_consumption");
                entity.Property(e => e.Project_Type).HasColumnName("project_type");
                entity.Property(e => e.Structure_Type).HasColumnName("structure_type");
                entity.Property(e => e.Floor_Zone).HasColumnName("floor_zone");
                entity.Property(e => e.Waste_Rate_Percent).HasColumnName("waste_rate_percent");
                entity.Property(e => e.Z_Score_Threshold).HasColumnName("z_score_threshold");
            });

            modelBuilder.Entity<ItemDictionary>(entity =>
            {
                entity.ToTable("item_dictionary");
                entity.HasKey(e => e.DictionaryId);
                entity.Property(e => e.DictionaryId).HasColumnName("dictionary_id");
                entity.Property(e => e.Keyword).HasColumnName("keyword");
                entity.Property(e => e.Material_Id).HasColumnName("material_id");
            });

            modelBuilder.Entity<HistoricalTransaction>(entity =>
            {
                // 指定資料庫表名稱為小寫
                entity.ToTable("historical_transaction");
                entity.HasKey(e => e.Transaction_ID);

                // 關鍵設定：確保屬性對應到資料庫真實欄位名稱
                entity.Property(e => e.Transaction_ID).HasColumnName("transaction_id");
                entity.Property(e => e.Material_ID).HasColumnName("material_id");
                entity.Property(e => e.Actual_Quantity).HasColumnName("actual_quantity");
                entity.Property(e => e.Unit_Price).HasColumnName("unit_price");
                entity.Property(e => e.Transaction_Date).HasColumnName("transaction_date");
                entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
                entity.Property(e => e.Project_Id).HasColumnName("project_id"); // 這裡可能有重複定義
                entity.Property(e => e.Is_Outlier).HasColumnName("is_outlier");
            });
                        
        }
    }
}
