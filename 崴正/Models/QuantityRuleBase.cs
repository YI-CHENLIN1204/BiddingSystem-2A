using System.ComponentModel.DataAnnotations;

namespace 崴正.Models
{
    public class QuantityRuleBase
    {
        [Key]
        public int RuleID { get; set; }
        public int Material_ID { get; set; }
        public decimal Z_Score_Threshold { get; set; }
        
        // 正式更名：單位基準用量
        public decimal Unit_Consumption { get; set; }

        // 新增的估算多維度
        public string? Project_Type { get; set; }   // 建案類型 (如: 住宅, 廠房)
        public string? Structure_Type { get; set; } // 結構類型 (如: RC, SC)
        public string? Floor_Zone { get; set; }     // 樓層區塊 (如: 地下室, 15F以上)
        public decimal Waste_Rate_Percent { get; set; }// 耗損率 (%)
    }
}