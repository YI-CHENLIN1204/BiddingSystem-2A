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
        public DbSet<HistoryOperationReceipt> HistoryOperationReceipts { get; set; }
        public DbSet<MaterialMaster> MaterialMasters { get; set; }
        public DbSet<MaterialCategory> MaterialCategories { get; set; }
        public DbSet<HistoricalTransaction> HistoricalTransactions { get; set; }
        public DbSet<ItemDictionary> ItemDictionaries { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<FinalBidItem> FinalBidItems { get; set; }
        public DbSet<EstimationSetting> EstimationSettings { get; set; }
        public DbSet<UnitCategory> UnitCategories { get; set; }
        public DbSet<UnitMaster> UnitMasters { get; set; }
        public DbSet<DetectionCategory> DetectionCategories { get; set; }
        public DbSet<DetectionCategoryMaterialMap> DetectionCategoryMaterialMaps { get; set; }
        public DbSet<AnalysisCase> AnalysisCases { get; set; }
        public DbSet<AnalysisBidVersion> AnalysisBidVersions { get; set; }
        public DbSet<AnalysisBidItem> AnalysisBidItems { get; set; }
        public DbSet<AnalysisRun> AnalysisRuns { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<HistoryOperationReceipt>(entity =>
            {
                entity.ToTable("history_operation_receipt");
                entity.HasKey(x => x.OperationId);
                entity.Property(x => x.OperationId).HasColumnName("operation_id");
                entity.Property(x => x.ProjectId).HasColumnName("project_id");
                entity.Property(x => x.ActionType).HasColumnName("action_type");
                entity.Property(x => x.ResultJson).HasColumnName("result_json").HasColumnType("jsonb");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<MaterialCategory>(
            entity =>
            {
                entity.ToTable("material_category");

                entity.HasKey(e => e.CategoryId);

                entity.Property(e => e.CategoryId).HasColumnName("category_id");

                entity.Property(e => e.CategoryName).HasColumnName("category_name");

                entity.Property(e => e.IsActive).HasColumnName("is_active");

                entity.Property(e => e.SortOrder).HasColumnName("sort_order");

                entity.HasIndex(e => e.CategoryName).IsUnique();
            }
        );

            modelBuilder.Entity<ProjectMaster>(entity =>
            {
                entity.ToTable("project_master");

                entity.HasKey(e => e.ProjectId);

                entity.Property(e => e.ProjectId).HasColumnName("project_id");

                entity.Property(e => e.ProjectName).HasColumnName("project_name");

                entity.Property(e => e.TotalFloorArea).HasColumnName("total_floor_area");

                entity.Property(e => e.SiteArea).HasColumnName("site_area");

                entity.Property(e => e.FloorCount).HasColumnName("floor_count");

                entity.Property(e => e.BasementFloorCount).HasColumnName("basement_floor_count");

                entity.Property(e => e.StartDate).HasColumnName("start_date").HasColumnType("date");

                entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");

                entity.Property(e => e.ProjectType).HasColumnName("project_type");

                entity.Property(e => e.StructureType).HasColumnName("structure_type");

                entity.Property(e => e.City).HasColumnName("city");

                entity.Property(e => e.SourceFileName).HasColumnName("source_file_name");

                entity.Property(e => e.SourceFileHash).HasColumnName("source_file_hash");

                entity.Property(e => e.ImportedAt).HasColumnName("imported_at").HasColumnType("timestamp with time zone");

                entity.Property(e => e.HistoryStatus).HasColumnName("history_status");

                entity.Property(e => e.HistoryApprovedAt).HasColumnName("history_approved_at").HasColumnType("timestamp with time zone");

                entity.Property(e => e.HistoryApprovedBy).HasColumnName("history_approved_by");
            });

            modelBuilder.Entity<UnitCategory>(
                entity =>
                {
                    entity.ToTable("unit_category");

                    entity.HasKey(e =>
                        e.UnitCategoryId);

                    entity.Property(e =>
                            e.UnitCategoryId)
                        .HasColumnName(
                            "unit_category_id");

                    entity.Property(e =>
                            e.CategoryName)
                        .HasColumnName(
                            "category_name");

                    entity.Property(e =>
                            e.AllowConversion)
                        .HasColumnName(
                            "allow_conversion");

                    entity.Property(e =>
                            e.IsActive)
                        .HasColumnName(
                            "is_active");

                    entity.Property(e =>
                            e.SortOrder)
                        .HasColumnName(
                            "sort_order");
                }
            );

            modelBuilder.Entity<UnitMaster>(
                entity =>
                {
                    entity.ToTable("unit_master");

                    entity.HasKey(e =>
                        e.UnitId);

                    entity.Property(e =>
                            e.UnitId)
                        .HasColumnName(
                            "unit_id");

                    entity.Property(e =>
                            e.UnitName)
                        .HasColumnName(
                            "unit_name");

                    entity.Property(e =>
                            e.UnitCategoryId)
                        .HasColumnName(
                            "unit_category_id");

                    entity.Property(e =>
                            e.ToBaseFactor)
                        .HasColumnName(
                            "to_base_factor")
                        .HasPrecision(20, 10);

                    entity.Property(e =>
                            e.IsBaseUnit)
                        .HasColumnName(
                            "is_base_unit");

                    entity.Property(e =>
                            e.IsActive)
                        .HasColumnName(
                            "is_active");

                    entity.Property(e =>
                            e.SortOrder)
                        .HasColumnName(
                            "sort_order");

                    entity.HasOne(e =>
                            e.UnitCategory)
                        .WithMany(e =>
                            e.Units)
                        .HasForeignKey(e =>
                            e.UnitCategoryId)
                        .OnDelete(
                            DeleteBehavior.Restrict)
                        .HasConstraintName(
                            "fk_unit_master_category");

                    entity.HasIndex(e =>
                            e.UnitCategoryId)
                        .HasDatabaseName(
                            "ix_unit_master_category");
                }
            );

            modelBuilder.Entity<DetectionCategory>(
                entity =>
                {
                    entity.ToTable(
                        "detection_category"
                    );

                    entity.HasKey(e =>
                        e.DetectionCategoryId);

                    entity.Property(e =>
                            e.DetectionCategoryId)
                        .HasColumnName(
                            "detection_category_id"
                        );

                    entity.Property(e =>
                            e.MajorCategoryName)
                        .HasColumnName(
                            "major_category_name"
                        );

                    entity.Property(e =>
                            e.DetectionCategoryName)
                        .HasColumnName(
                            "detection_category_name"
                        );

                    entity.Property(e =>
                            e.UnitId)
                        .HasColumnName(
                            "unit_id"
                        );

                    entity.Property(e =>
                            e.IsActive)
                        .HasColumnName(
                            "is_active"
                        );

                    entity.Property(e =>
                            e.SortOrder)
                        .HasColumnName(
                            "sort_order"
                        );

                    entity.HasOne(e =>
                            e.UnitMaster)
                        .WithMany()
                        .HasForeignKey(e =>
                            e.UnitId)
                        .OnDelete(
                            DeleteBehavior.Restrict
                        )
                        .HasConstraintName(
                            "fk_detection_category_unit"
                        );

                    entity.HasIndex(e => new
                        {
                            e.MajorCategoryName,
                            e.DetectionCategoryName
                        })
                        .IsUnique()
                        .HasDatabaseName(
                            "ux_detection_category_name"
                        );
                }
            );

            modelBuilder.Entity<DetectionCategoryMaterialMap>(
                entity =>
                {
                    entity.ToTable(
                        "detection_category_material_map"
                    );

                    entity.HasKey(e =>
                        e.MaterialId);

                    entity.Property(e =>
                            e.DetectionCategoryId)
                        .HasColumnName(
                            "detection_category_id"
                        );

                    entity.Property(e =>
                            e.MaterialId)
                        .HasColumnName(
                            "material_id"
                        );

                    entity.Property(e =>
                            e.IsActive)
                        .HasColumnName(
                            "is_active"
                        );

                    entity.Property(e =>
                            e.CreatedAt)
                        .HasColumnName(
                            "created_at"
                        )
                        .HasColumnType(
                            "timestamp with time zone"
                        );

                    entity.Property(e =>
                            e.UpdatedAt)
                        .HasColumnName(
                            "updated_at"
                        )
                        .HasColumnType(
                            "timestamp with time zone"
                        );

                    entity.HasOne(e =>
                            e.DetectionCategory)
                        .WithMany(e =>
                            e.MaterialMappings)
                        .HasForeignKey(e =>
                            e.DetectionCategoryId)
                        .OnDelete(
                            DeleteBehavior.Cascade
                        )
                        .HasConstraintName(
                            "fk_detection_map_category"
                        );

                    entity.HasOne(e =>
                            e.Material)
                        .WithMany()
                        .HasForeignKey(e =>
                            e.MaterialId)
                        .OnDelete(
                            DeleteBehavior.Restrict
                        )
                        .HasConstraintName(
                            "fk_detection_map_material"
                        );

                    entity.HasIndex(e =>
                            e.DetectionCategoryId)
                        .HasDatabaseName(
                            "ix_detection_map_category"
                        );
                }
            );

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("audit_log");

                entity.HasKey(e => e.LogId);

                entity.Property(e => e.LogId).HasColumnName("log_id");

                entity.Property(e => e.Module).HasColumnName("module");

                entity.Property(e => e.ActionType).HasColumnName("action_type");

                entity.Property(e => e.EntityType).HasColumnName("entity_type");

                entity.Property(e => e.EntityId).HasColumnName("entity_id");

                entity.Property(e => e.ProjectId).HasColumnName("project_id");

                entity.Property(e => e.Description).HasColumnName("description");

                entity.Property(e => e.OldValue).HasColumnName("old_value");

                entity.Property(e => e.NewValue).HasColumnName("new_value");

                entity.Property(e => e.ActionUser).HasColumnName("action_user");

                entity.Property(e =>
                    e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType(
                        "timestamp with time zone"
                    );
            });

            modelBuilder.Entity<MaterialMaster>(entity =>
            {
                entity.ToTable("material_master");
                entity.HasKey(e => e.MaterialId);

                entity.Property(e => e.MaterialId).HasColumnName("material_id");
                entity.Property(e => e.MaterialName).HasColumnName("material_name");
                entity.Property(e => e.CategoryId).HasColumnName("category_id");
                entity.HasOne(e => e.MaterialCategory).WithMany(e => e.Materials).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_material_master_category");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.UnitId).HasColumnName("unit_id");
                entity.HasOne(e =>e.UnitMaster).WithMany().HasForeignKey(e =>e.UnitId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_material_master_unit");
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
                    .HasConstraintName("fk_historical_transaction_project_master");
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
                entity.Property(e => e.MajorCategoryOverride).HasColumnName("major_category_override");
                entity.Property(e => e.ItemName).HasColumnName("item_name");
                entity.Property(e => e.Spec).HasColumnName("spec");
                entity.Property(e => e.Unit).HasColumnName("unit");
                entity.Property(e => e.Qty).HasColumnName("qty");
                entity.Property(e => e.UnitPrice).HasColumnName("unit_price");
                entity.Property(e => e.Amount).HasColumnName("amount");
                entity.Property(e => e.Note).HasColumnName("note");
                entity.Property(e => e.MaterialId).HasColumnName("material_id");
                entity.Property(e => e.MappingStatus).HasColumnName("mapping_status");
                entity.Property(e => e.MappingIssueCode).HasColumnName("mapping_issue_code");
                entity.Property(e => e.MappingIssueMessage).HasColumnName("mapping_issue_message");
                entity.Property(e => e.DataIssueCode).HasColumnName("data_issue_code");
                entity.Property(e => e.DataIssueMessage).HasColumnName("data_issue_message");
                entity.Property(e => e.MatchedKeyword).HasColumnName("matched_keyword");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
                entity.HasOne(e => e.ProjectMaster).WithMany().HasForeignKey(e => e.ProjectMasterId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Material).WithMany().HasForeignKey(e => e.MaterialId).OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.IsIgnored).HasColumnName("is_ignored");
            });

            modelBuilder.Entity<EstimationSetting>(entity =>
            {
                entity.ToTable("estimation_settings");

                entity.HasKey(e => e.SettingsId);

                entity.Property(e => e.SettingsId)
                    .HasColumnName("settings_id");

                entity.Property(e => e.TopK)
                    .HasColumnName("top_k");

                entity.Property(e => e.MinimumSampleCount)
                    .HasColumnName("minimum_sample_count");

                entity.Property(e => e.LowerQuantile)
                    .HasColumnName("lower_quantile");

                entity.Property(e => e.MedianQuantile)
                    .HasColumnName("median_quantile");

                entity.Property(e => e.UpperQuantile)
                    .HasColumnName("upper_quantile");

                entity.Property(e => e.SimilarityExponent)
                    .HasColumnName("similarity_exponent");

                entity.Property(e => e.TimeHalfLifeYears)
                    .HasColumnName("time_half_life_years");

                entity.Property(e => e.MinimumSimilarityScore)
                    .HasColumnName("minimum_similarity_score");

                entity.Property(e => e.StructureWeight)
                    .HasColumnName("structure_weight");

                entity.Property(e => e.AreaWeight)
                    .HasColumnName("area_weight");

                entity.Property(e => e.SiteAreaWeight)
                    .HasColumnName("site_area_weight");

                entity.Property(e => e.FloorWeight)
                    .HasColumnName("floor_weight");

                entity.Property(e => e.YearWeight)
                    .HasColumnName("year_weight");

                entity.Property(e => e.CityWeight)
                    .HasColumnName("city_weight");

                entity.Property(e => e.AreaDifferenceCutoffRatio)
                    .HasColumnName("area_difference_cutoff_ratio");

                entity.Property(e => e.SiteAreaDifferenceCutoffRatio)
                    .HasColumnName("site_area_difference_cutoff_ratio");

                entity.Property(e => e.GroundFloorPenaltyRatio)
                    .HasColumnName("ground_floor_penalty_ratio");

                entity.Property(e => e.BasementFloorPenaltyRatio)
                    .HasColumnName("basement_floor_penalty_ratio");

                entity.Property(e => e.YearDifferenceCutoffYears)
                    .HasColumnName("year_difference_cutoff_years");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp with time zone");

                entity.Property(e => e.UpdatedBy)
                    .HasColumnName("updated_by");
            });
        }
    }
}
