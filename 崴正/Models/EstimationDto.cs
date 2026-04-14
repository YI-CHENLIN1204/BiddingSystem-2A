using System.Collections.Generic;

namespace 崴正.Models
{

    public class BidRequestDto
    {
        public int Project_Id { get; set; }
        public List<BidItemDto> Items { get; set; }
    }

    public class BidItemDto
    {
        public int Material_Id { get; set; }
        public string Floor_Zone { get; set; }      
        public decimal Input_Quantity { get; set; } 
    }

    // 系統回傳的報告
    public class EstimationResponseDto
    {
        public int Project_Id { get; set; }
        public List<EstimationResultDto> Results { get; set; } = new List<EstimationResultDto>();
    }

    public class EstimationResultDto
    {
        public int Material_Id { get; set; }
        public string Floor_Zone { get; set; }
        public decimal Calculated_Z_Score { get; set; }
        public bool Is_Abnormal { get; set; }
        public decimal Predicted_Unit_Price { get; set; } 
        public decimal Reference_Total_Price { get; set; }
    }
}