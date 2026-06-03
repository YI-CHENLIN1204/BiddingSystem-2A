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
        public string? Project_Type { get; set; }
        public string? Structure_Type { get; set; }
        public string? Floor_Zone { get; set; }
        public decimal Waste_Rate_Percent { get; set; }
    }
}