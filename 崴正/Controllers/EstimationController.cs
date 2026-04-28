using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System;
using System.Collections.Generic;
using 崴正.Data;   
using 崴正.Models; 

namespace 崴正.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EstimationController : ControllerBase
    {
    
        private readonly ApplicationDbContext _context;

     
        public EstimationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("analyze")]
        public IActionResult AnalyzeBid([FromBody] BidRequestDto request)
        {
            var finalReport = new EstimationResponseDto { Project_Id = request.Project_Id };

    
            var groupedItems = request.Items
                .GroupBy(x => new { x.Material_Id, x.Floor_Zone })
                .Select(g => new {
                    Material_Id = g.Key.Material_Id,
                    Floor_Zone = g.Key.Floor_Zone,
                    Total_Quantity = g.Sum(x => x.Input_Quantity)
                }).ToList();

      
            foreach (var item in groupedItems)
            {
                var rule = _context.QuantityRuleBases.FirstOrDefault(r => r.Material_ID == item.Material_Id);
                decimal ruleThreshold = rule != null ? rule.Z_Score_Threshold : 2.0m;

                var historyData = _context.HistoricalTransactions
                    .Where(h => h.Material_ID == item.Material_Id && h.Is_Outlier == false)
                    .Select(h => new { h.Actual_Quantity, h.Unit_Price, h.Transaction_Date })
                    .ToList();

                decimal suggestedQty = 0;
                decimal calculatedZScore = 0;
                bool isAbnormal = false;
                string statusMsg = ""; 
                decimal predictedPrice = 0;


                if (!historyData.Any())
                {
                    if (rule != null)
                    {
                    
                        suggestedQty = item.Total_Quantity; 
                        statusMsg = "需人工首筆核定"; 
                    }
                    else
                    {
                        
                        statusMsg = "無基準資料";
                    }
                    isAbnormal = true; 
                }

                else
                {
                    double k = 0.005;
                    DateTime today = DateTime.Today;

                    double totalWeight = 0;
                    double weightedSum = 0;

  
                    var weightedData = historyData.Select(h => {
                        int daysOld = (today - h.Transaction_Date).Days;
                        double weight = Math.Exp(-k * daysOld);
                        
                        totalWeight += weight;
                        weightedSum += (double)h.Actual_Quantity * weight;
                        return new { h.Actual_Quantity, weight };
                    }).ToList();

                
                    double weightedMean = weightedSum / totalWeight;
                    suggestedQty = (decimal)weightedMean;

          
                    double varianceSum = weightedData.Sum(d => d.weight * Math.Pow((double)d.Actual_Quantity - weightedMean, 2));
                    decimal historySD = (decimal)Math.Sqrt(varianceSum / totalWeight);

             
                    if (historySD == 0)
                    {
                        calculatedZScore = 0;
                        isAbnormal = false;
                        statusMsg = "資料一致 (無波動)";
                    }
                    else
                    {

                        calculatedZScore = Math.Abs((item.Total_Quantity - (decimal)weightedMean) / historySD);
                        isAbnormal = calculatedZScore > ruleThreshold;
                        statusMsg = isAbnormal ? "異常高估" : "合理";
                    }

                    predictedPrice = historyData.Average(h => h.Unit_Price) * 1.02m;
                }

                decimal refTotalPrice = item.Total_Quantity * predictedPrice;

                finalReport.Results.Add(new EstimationResultDto
                {
                    Material_Id = item.Material_Id,
                    Floor_Zone = item.Floor_Zone,
                    Calculated_Z_Score = Math.Round(calculatedZScore, 2),
                    Is_Abnormal = isAbnormal,
                    Predicted_Unit_Price = Math.Round(predictedPrice, 0),
                    Reference_Total_Price = Math.Round(refTotalPrice, 0),
                    Suggested_Quantity = Math.Round(suggestedQty, 2),
                    Status_Message = statusMsg 
                });
            }

            return Ok(finalReport);
        }
    }
}