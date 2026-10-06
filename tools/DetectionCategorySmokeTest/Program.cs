using 崴正.Models;
using 崴正.Services;

var service =
    new DetectionCategoryService();

var input =
    new List<OneDSavedResultItem>
    {
        new()
        {
            Chapter = "假設工程",
            Name = "臨時水電及施工設施",
            Unit = "式",
            Quantity = 1m
        },
        new()
        {
            Chapter = "土方及基礎工程",
            Name = "地下室土方開挖",
            Unit = "M3",
            Quantity = 8200m
        },
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
            Chapter = "結構體工程",
            Name = "SD420W鋼筋續接器",
            Unit = "個",
            Quantity = 780m
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
            Chapter = "防水工程",
            Name = "屋頂防水層",
            Unit = "M2",
            Quantity = 1800m
        },
        new()
        {
            Chapter = "門窗工程",
            Name = "鋁門窗",
            Unit = "樘",
            Quantity = 126m
        },
        new()
        {
            Chapter = "景觀工程",
            Name = "景觀植栽",
            Unit = "式",
            Quantity = 1m
        }
    };

var preview =
    service.BuildPreview(
        input
    );

Assert(
    preview.TotalItems == 11,
    "完整建案示範應有 11 筆工項"
);

Assert(
    preview.MajorCategoryCount == 7,
    "應保留 1D 提供的 7 個工程大項"
);

Assert(
    preview.MajorCategories.Any(x =>
        x.MajorCategory == "防水工程"
    ),
    "防水工程必須保留，不可因 2A 尚未建立細項規則而遺失"
);

Assert(
    preview.MajorCategories.Any(x =>
        x.MajorCategory == "門窗工程"
    ),
    "門窗工程必須保留，不可因 2A 尚未建立細項規則而遺失"
);

Assert(
    preview.ClassifiedItems == 5,
    "目前 POC 應有 5 筆可歸入 2A 偵錯細項"
);

Assert(
    preview.PendingItems == 6,
    "其餘工程大項與鋼筋配件應維持待建立或待確認"
);

var structural =
    preview.MajorCategories.Single(x =>
        x.MajorCategory == "結構體工程"
    );

Assert(
    structural.HasDetectionRules,
    "結構體工程應已有 2A 細項規則"
);

var expectedStructuralCategories =
    new HashSet<string>(
        new[]
        {
            "鋼筋",
            "混凝土",
            "模板"
        },
        StringComparer.OrdinalIgnoreCase
    );

Assert(
    structural.DetectionCategories.Count ==
        expectedStructuralCategories.Count &&
    structural.DetectionCategories.All(
        expectedStructuralCategories.Contains
    ),
    "結構體工程應可往下形成鋼筋、混凝土、模板等偵錯細項"
);

var waterproof =
    preview.MajorCategories.Single(x =>
        x.MajorCategory == "防水工程"
    );

Assert(
    !waterproof.HasDetectionRules,
    "防水工程目前應保留 1D 大項，但不硬建立未確認的 2A 細項"
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
    "鋼筋群組應包含 2 筆原始工項"
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
    "2A Project-wide Detection Preview Smoke Test: PASS"
);

Console.WriteLine(
    $"工程大項：{preview.MajorCategoryCount}，建案工項：{preview.TotalItems}"
);

Console.WriteLine(
    $"2A 已歸類細項：{preview.ClassifiedItems}，待建立/確認：{preview.PendingItems}"
);

Console.WriteLine(
    $"鋼筋彙整：{rebar.TotalQuantity} {rebar.AggregateUnit}"
);

Console.WriteLine(
    "未建立細項的大項保留：PASS"
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
