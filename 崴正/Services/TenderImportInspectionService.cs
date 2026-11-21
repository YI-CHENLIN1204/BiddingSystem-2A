using 崴正.Models;

namespace 崴正.Services;

// 上傳前的唯讀檢查，不使用歷史資料、不寫入資料庫，也不宣稱模型已判定異常。
public static class TenderImportInspectionService
{
    public sealed record InspectionRow(
        string SheetName,
        int RowNumber,
        string ItemCode,
        string ItemName,
        string Spec,
        string Unit,
        decimal? Qty,
        decimal? UnitPrice,
        decimal? Amount,
        string Category1,
        string Category2,
        string Category3,
        string? EffectiveMajorCategory,
        string CandidateType,
        decimal? StandardQuantity,
        string? StandardUnit,
        string[] Warnings
    );

    public sealed record QuantitySummary(
        string CandidateType,
        string Label,
        string Unit,
        int CandidateRowCount,
        int ConvertedRowCount,
        int ReviewRowCount,
        decimal CandidateQuantity
    );

    public sealed record InspectionReport(
        int TotalRows,
        int WarningRows,
        QuantitySummary[] QuantitySummaries,
        InspectionRow[] Rows,
        string Notice
    );

    // 與正式 AnalysisCase 數量偵錯共用，避免把施工服務或配件當成材料重量/體積。
    public static string ClassifyQuantityCandidate(string? itemName, string? spec)
    {
        string sourceName = ((itemName ?? "") + " " + (spec ?? ""))
            .Replace(" ", "").ToUpperInvariant();
        bool hasRebar = sourceName.Contains("鋼筋", StringComparison.Ordinal)
            || sourceName.Contains("SD420W", StringComparison.Ordinal)
            || sourceName.Contains("SD420", StringComparison.Ordinal)
            || sourceName.Contains("SD280", StringComparison.Ordinal);
        bool hasConcrete = sourceName.Contains("混凝土", StringComparison.Ordinal)
            || sourceName.Contains("預拌", StringComparison.Ordinal);
        bool rebarAccessory = hasRebar && new[]
        {
            "續接", "接頭", "套筒", "植筋", "綁紮", "加工", "組立",
            "彎紮", "切割", "運費", "檢驗", "試驗", "工資", "人工"
        }.Any(sourceName.Contains);
        bool concreteService = hasConcrete && new[]
        {
            "打除", "澆置", "搗築", "鑽心", "檢驗", "試驗", "運費", "工資", "人工"
        }.Any(sourceName.Contains);

        if (rebarAccessory || concreteService) return "Excluded";
        if (hasRebar && !hasConcrete) return "Rebar";
        if (hasConcrete && !hasRebar) return "Concrete";
        return "Other";
    }

    public static InspectionReport Build(IReadOnlyCollection<TenderImportRowDto> source)
    {
        var duplicateCounts = source
            .GroupBy(x => string.Join("\u001f", x.SheetName, x.ItemName, x.Spec, x.Unit,
                x.Qty?.ToString() ?? "", x.UnitPrice?.ToString() ?? ""))
            .ToDictionary(x => x.Key, x => x.Count());

        var rows = source.Select(item =>
        {
            string category = ClassifyQuantityCandidate(item.ItemName, item.Spec);

            string? majorCategory = TwoAScopePolicy.ResolveMajorCategory(
                item.Category1, item.Category2, item.Category3);
            var warnings = new List<string>();
            decimal? standardQuantity = null;
            string? standardUnit = null;

            if (category == "Rebar" || category == "Concrete")
            {
                standardUnit = category == "Rebar" ? "kg" : "m³";
                if (!item.Qty.HasValue || item.Qty.Value <= 0)
                {
                    warnings.Add("數量缺漏或不大於零");
                }
                else
                {
                    string unit = (item.Unit ?? "").Trim()
                        .Replace(" ", "").Replace("　", "").ToUpperInvariant();
                    decimal? factor = category == "Rebar"
                        ? unit switch
                        {
                            "KG" or "公斤" or "千克" => 1m,
                            "T" or "TON" or "MT" or "噸" or "公噸" => 1000m,
                            _ => null
                        }
                        : unit switch
                        {
                            "M3" or "M³" or "M^3" or "立方公尺" or "立方米" or "㎥" or "CBM" => 1m,
                            _ => null
                        };

                    if (factor.HasValue)
                        standardQuantity = item.Qty.Value * factor.Value;
                    else
                        warnings.Add("單位無法安全換算，未納入候選合計");
                }

                if (majorCategory != "結構體工程")
                    warnings.Add("工程大項尚未確認為結構體工程，正式模型可能不納入");

                string key = string.Join("\u001f", item.SheetName, item.ItemName, item.Spec,
                    item.Unit, item.Qty?.ToString() ?? "", item.UnitPrice?.ToString() ?? "");
                if (duplicateCounts[key] > 1)
                    warnings.Add("同工作表出現相同工項、規格與數量，請檢查是否重複");
            }
            else if (category == "Excluded")
            {
                warnings.Add("疑似輔助工項，未納入鋼筋／混凝土候選用量");
            }

            return new InspectionRow(
                item.SheetName, item.RowNumber, item.ItemCode, item.ItemName,
                item.Spec, item.Unit ?? string.Empty, item.Qty, item.UnitPrice, item.Amount,
                item.Category1, item.Category2, item.Category3, majorCategory,
                category, standardQuantity, standardUnit, warnings.ToArray());
        }).ToArray();

        QuantitySummary CreateSummary(string type, string label, string unit)
        {
            var candidates = rows.Where(x => x.CandidateType == type).ToArray();
            return new QuantitySummary(
                type, label, unit, candidates.Length,
                candidates.Count(x => x.StandardQuantity.HasValue),
                candidates.Count(x => x.Warnings.Length > 0),
                candidates.Sum(x => x.StandardQuantity ?? 0m)
            );
        }

        return new InspectionReport(
            rows.Length,
            rows.Count(x => x.Warnings.Length > 0),
            new[]
            {
                CreateSummary("Rebar", "鋼筋", "kg"),
                CreateSummary("Concrete", "混凝土", "m³")
            },
            rows,
            "此處僅依名稱及單位顯示候選數量，尚未驗證決標真實性、工程大項及可能重複計價，不是異常判定，也不會產生歷史基準。"
        );
    }
}
