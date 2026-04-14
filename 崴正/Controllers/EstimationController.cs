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
      
                var rule = _context.QuantityRuleBases
                                   .FirstOrDefault(r => r.Material_ID == item.Material_Id);
                                   
       
                var historyData = _context.HistoricalTransactions
                                          .Where(h => h.Material_ID == item.Material_Id)
                                          .Select(h => h.Actual_Quantity)
                                          .ToList();

      
                if (rule == null || !historyData.Any()) 
                {
                    continue; 
                }

                decimal historyMean = historyData.Average(); 
                

                double sumOfSquares = historyData.Select(val => (double)(val - historyMean))
                                                 .Select(val => val * val)
                                                 .Sum();
                decimal historySD = (decimal)Math.Sqrt(sumOfSquares / historyData.Count);
                
                decimal ruleThreshold = rule.Z_Score_Threshold; 

   
                decimal zScore = 0;
                bool isAbnormal = false;

                if (historySD > 0) 
                {
                    zScore = Math.Abs((item.Total_Quantity - historyMean) / historySD);
                    isAbnormal = zScore > ruleThreshold;
                }

  
                var avgUnitPrice = _context.HistoricalTransactions
                                           .Where(h => h.Material_ID == item.Material_Id)
                                           .Average(h => h.Unit_Price);
                
                decimal predictedPrice = avgUnitPrice * 1.02m; 
                decimal refTotalPrice = item.Total_Quantity * predictedPrice;


                finalReport.Results.Add(new EstimationResultDto
                {
                    Material_Id = item.Material_Id,
                    Floor_Zone = item.Floor_Zone,
                    Calculated_Z_Score = Math.Round(zScore, 2),
                    Is_Abnormal = isAbnormal,
                    Predicted_Unit_Price = Math.Round(predictedPrice, 0),
                    Reference_Total_Price = Math.Round(refTotalPrice, 0),
                    Suggested_Quantity = Math.Round(historyMean, 2)
                });
            }

            return Ok(finalReport);
        }
    }
}