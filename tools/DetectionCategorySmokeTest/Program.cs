using 崴正.Models;
using 崴正.Services;

var service =
    new DetectionCategoryService();

var input =
    new List<OneDSavedResultItem>
    {
        new()
        {
            Chapter = "結構體工程",
            Name = "SD420W高拉力鋼筋#4以上",
            Unit = "KG",
            Quantity = 100000m
        },
        new()
        {
            Chapter = "結構體工程",
            Name = "SD420W高拉力鋼筋#3以下",
            Unit = "KG",
            Quantity = 40000m
        },
        new()
        {
            Chapter = "結構體工程",
            Name = "預拌混凝土 3500psi",
            Unit = "M3",
            Quantity = 3200m
        },
        new()
        {
            Chapter = "結構體工程",
            Name = "結構體模板",
            Unit = "M2",
            Quantity = 26000m
        },
        new()
        {
            Chapter = "鋼骨鋼構工程",
            Name = "H型鋼鋼柱",
            Unit = "T",
            Quantity = 850m
        },
        new()
        {
            Chapter = "結構體工程",
            Name = "SD420W鋼筋續接器",
            Unit = "個",
            Quantity = 780m
        }
    };

var preview =
    service.BuildPreview(
        input
    );

Assert(
    preview.TotalItems == 6,
    "總工項數應為 6"
);

Assert(
    preview.ClassifiedItems == 5,
    "應有 5 筆成功分類"
);

Assert(
    preview.PendingItems == 1,
    "鋼筋續接器應維持待確認"
);

var rebar =
    preview.Groups.SingleOrDefault(x =>
        x.MajorCategory == "結構體工程" &&
        x.DetectionCategory == "鋼筋"
    );

Assert(
    rebar != null,
    "應建立「結構體工程 → 鋼筋」群組"
);

Assert(
    rebar!.ItemCount == 2,
    "鋼筋群組應包含 2 筆工項"
);

Assert(
    rebar.AggregationStatus == "Ready",
    "同為 KG 的鋼筋應可安全彙整"
);

Assert(
    rebar.TotalQuantity == 140000m,
    "鋼筋彙整量應為 140000 KG"
);

var accessory =
    preview.Items.Single(x =>
        x.OriginalItemName.Contains(
            "續接器",
            StringComparison.Ordinal
        )
    );

Assert(
    accessory.ClassificationStatus ==
        "Pending",
    "續接器不可直接併入鋼筋重量"
);

var mixedUnitPreview =
    service.BuildPreview(
        new[]
        {
            new OneDSavedResultItem
            {
                Chapter = "結構體工程",
                Name = "SD420W高拉力鋼筋#4以上",
                Unit = "KG",
                Quantity = 100000m
            },
            new OneDSavedResultItem
            {
                Chapter = "結構體工程",
                Name = "SD420W高拉力鋼筋#5",
                Unit = "T",
                Quantity = 50m
            }
        }
    );

var mixedRebar =
    mixedUnitPreview.Groups.Single();

Assert(
    mixedRebar.AggregationStatus ==
        "NeedsUnitNormalization",
    "KG 與 T 不可直接相加，應要求單位標準化"
);

Assert(
    mixedRebar.TotalQuantity == null,
    "單位未標準化前不得產生錯誤合計"
);

Console.WriteLine(
    "2A Detection Category Smoke Test: PASS"
);

Console.WriteLine(
    $"分類結果：{preview.ClassifiedItems}/{preview.TotalItems}，待確認 {preview.PendingItems}"
);

Console.WriteLine(
    $"鋼筋彙整：{rebar.TotalQuantity} {rebar.AggregateUnit}"
);

Console.WriteLine(
    "混合單位防呆：PASS"
);

return;

static void Assert(
    bool condition,
    string message)
{
    if (!condition)
    {
        Console.Error.WriteLine(
            $"FAIL: {message}"
        );

        Environment.Exit(1);
    }
}
