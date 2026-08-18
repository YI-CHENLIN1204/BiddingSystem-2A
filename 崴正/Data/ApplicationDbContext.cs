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
        public DbSet<UnmappedItem> UnmappedItems { get; set; }
        public DbSet<FinalBidItem> FinalBidItems { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProjectMaster>(entity =>
            {
                entity.ToTable("project_master");

                entity.HasKey(e => e.ProjectId);

                entity.Property(e => e.ProjectId)
                    .HasColumnName("project_id");

                entity.Property(e => e.ProjectName).HasColumnName("project_name");

                entity.Property(e => e.TotalFloorArea).HasColumnName("total_floor_area");
                
                entity.Property(e => e.FloorRange).HasColumnName("floor_range");

                entity.Property(e => e.FloorCount).HasColumnName("floor_count");

                entity.Property(e => e.BasementFloorCount).HasColumnName("basement_floor_count");

                entity.Property(e => e.FloorCount).HasColumnName("floor_count");

                entity.Property(e => e.StartDate).HasColumnName("start_date").HasColumnType("date");

                entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");

                entity.Property(e => e.ProjectType).HasColumnName("project_type");

                entity.Property(e => e.StructureType).HasColumnName("structure_type");

                entity.Property(e => e.City).HasColumnName("city");
                
                entity.Property(e => e.SourceFileName).HasColumnName("source_file_name");

                entity.Property(e => e.SourceFileHash).HasColumnName("source_file_hash");

                entity.Property(e => e.ImportedAt).HasColumnName("imported_at") .HasColumnType("timestamp with time zone");
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
                entity.ToTable("historical_transaction");

                entity.HasKey(e => e.TransactionId);

                entity.Property(e => e.TransactionId).HasColumnName("transaction_id");

                entity.Property(e => e.MaterialId).HasColumnName("material_id");

                entity.Property(e => e.ActualQuantity).HasColumnName("actual_quantity");

                entity.Property(e => e.UnitPrice).HasColumnName("unit_price");

                entity.Property(e => e.TransactionDate).HasColumnName("transaction_date").HasColumnType("date");

                entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");

                entity.Property(e => e.ProjectMasterId).HasColumnName("project_master_id");

                entity.Property(e => e.IsOutlier).HasColumnName("is_outlier");

                entity.HasIndex(e => e.ProjectMasterId).HasDatabaseName("ix_historical_transaction_project_master_id");
                
                entity.Property(e => e.FinalBidItemId).HasColumnName("final_bid_item_id");
                
                entity.HasOne(e => e.ProjectMaster)
                    .WithMany(e => e.HistoricalTransactions)
                    .HasForeignKey(e => e.ProjectMasterId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName(
                        "fk_historical_transaction_project_master");
            });

            modelBuilder.Entity<FinalBidItem>(entity =>
            {
                entity.ToTable("final_bid_item");
                entity.HasKey(e => e.FinalBidItemId);
                entity.Property(e => e.FinalBidItemId).HasColumnName("final_bid_item_id");
                entity.Property(e => e.ProjectMasterId).HasColumnName("project_master_id");
                entity.Property(e => e.SourceFileName).HasColumnName("source_file_name");
                entity.Property(e => e.SheetName).HasColumnName("sheet_name");
                entity.Property(e => e.RowNumber).HasColumnName("row_number");
                entity.Property(e => e.ItemCode).HasColumnName("item_code");
                entity.Property(e => e.Category1).HasColumnName("category1");
                entity.Property(e => e.Category2).HasColumnName("category2");
                entity.Property(e => e.Category3).HasColumnName("category3");
                entity.Property(e => e.ItemName).HasColumnName("item_name");
                entity.Property(e => e.Spec).HasColumnName("spec");
                entity.Property(e => e.Unit).HasColumnName("unit");
                entity.Property(e => e.Qty) .HasColumnName("qty");
                entity.Property(e => e.UnitPrice) .HasColumnName("unit_price");
                entity.Property(e => e.Amount) .HasColumnName("amount");
                entity.Property(e => e.Note).HasColumnName("note");
                entity.Property(e => e.MaterialId).HasColumnName("material_id");
                entity.Property(e => e.MappingStatus).HasColumnName("mapping_status");
                entity.Property(e => e.MappingIssueCode).HasColumnName("mapping_issue_code");entity.Property(e => e.MappingIssueMessage).HasColumnName("mapping_issue_message");
                entity.Property(e => e.MatchedKeyword).HasColumnName("matched_keyword");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
                entity.HasOne(e => e.ProjectMaster)
                    .WithMany()
                    .HasForeignKey(e => e.ProjectMasterId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Material)
                    .WithMany()
                    .HasForeignKey(e => e.MaterialId)
                    .OnDelete(DeleteBehavior.Restrict);
            });            
        }
    }
}
