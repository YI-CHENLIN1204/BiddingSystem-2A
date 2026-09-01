using System.ComponentModel.DataAnnotations;

namespace 崴正.Models
{
    public class QuantityRuleBase
    {
        [Key]
        public int RuleID { get; set; }

        public int Material_ID { get; set; }

        public string? Project_Type { get; set; }

        public string? Structure_Type { get; set; }

        public bool Is_Core_Estimation { get; set; }
    }
}