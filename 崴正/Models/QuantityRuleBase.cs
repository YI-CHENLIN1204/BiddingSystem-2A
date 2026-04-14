namespace 崴正.Models
{
    public class QuantityRuleBase
    {
        public int RuleID { get; set; }
        public int Material_ID { get; set; }
        public decimal Base_Unit_Ratio { get; set; }
        public decimal Z_Score_Threshold { get; set; }
    }
}