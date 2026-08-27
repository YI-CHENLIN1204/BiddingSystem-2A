using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using 崴正.Data;
using 崴正.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization;


namespace 崴正.Controllers
{
    public class QuantityRuleViewModel
    {
        public int RuleID { get; set; }
        public int Material_ID { get; set; }
        public string? MaterialName { get; set; }
        public string? BaseUnit { get; set; }
        public decimal Z_Score_Threshold { get; set; }
        public decimal Base_Unit_Ratio { get; set; }
        public decimal Unit_Consumption { get; set; } 
        public string? Project_Type { get; set; }
        public string? Structure_Type { get; set; }
        public string? Floor_Zone { get; set; }
        public decimal Waste_Rate_Percent { get; set; }
    }
    public class QuantityRuleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuantityRuleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. 列表頁 (Read)
        // ==========================================
        public IActionResult Index()
        {
            var rules = (from r in _context.QuantityRuleBases
                         join m in _context.MaterialMasters on r.Material_ID equals m.MaterialId
                         select new QuantityRuleViewModel
                         {
                             RuleID = r.RuleID,
                             Material_ID = r.Material_ID,
                             MaterialName = m.MaterialName,
                             BaseUnit = m.BaseUnit,
                            Z_Score_Threshold = r.Z_Score_Threshold,
                            Unit_Consumption = r.Unit_Consumption, 
                            Project_Type = r.Project_Type,         
                            Structure_Type = r.Structure_Type,     
                            Floor_Zone = r.Floor_Zone,            
                            Waste_Rate_Percent = r.Waste_Rate_Percent 
                         }).ToList();

            return View(rules);
        }

        // ==========================================
        // 2. 編輯功能 (Update)
        // ==========================================
        // (GET) 打開編輯畫面
        public IActionResult Edit(int id)
        {
            var rule = _context.QuantityRuleBases.FirstOrDefault(r => r.RuleID == id);
            if (rule == null) return NotFound();

            var material = _context.MaterialMasters.FirstOrDefault(m => m.MaterialId == rule.Material_ID);
            ViewBag.MaterialName = material?.MaterialName ?? "未知建材";
            ViewBag.BaseUnit = material?.BaseUnit ?? "";

            return View(rule);
        }

        // (POST) 儲存編輯資料
        [HttpPost]
        public IActionResult Edit(QuantityRuleBase updatedRule)
        {
            _context.QuantityRuleBases.Update(updatedRule);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "參數規則修改成功！";
            return RedirectToAction("Index");
        }

        // ==========================================
        // 3. 新增功能 (Create)
        // ==========================================
        public IActionResult Create()
        {
            var materials = _context.MaterialMasters.ToList();
            ViewBag.Materials = materials;
            return View();
        }

        // (POST) 接收表單，執行防呆與存檔
// (POST) 接收表單，執行防呆與存檔
        [HttpPost]
        public IActionResult Create(QuantityRuleBase newRule)
        {
            // 🛡️ 多維度防呆：檢查同一個建材，在「完全相同的情境條件」下是否重複？
            // (允許同一個建材，在不同樓層有不同的規則)
            var existingRule = _context.QuantityRuleBases
                .FirstOrDefault(r => 
                    r.Material_ID == newRule.Material_ID &&
                    r.Project_Type == newRule.Project_Type &&
                    r.Structure_Type == newRule.Structure_Type &&
                    r.Floor_Zone == newRule.Floor_Zone);

            if (existingRule != null)
            {
                TempData["ErrorMessage"] = "建立失敗！該建材在「相同的情境條件下」已經有設定過規則了。";
                return RedirectToAction("Index");
            }

            _context.QuantityRuleBases.Add(newRule);
            _context.SaveChanges();
            
            TempData["SuccessMessage"] = "參數規則新增成功！";
            return RedirectToAction("Index");
        }

        // ==========================================
        // 4. 刪除功能 (Delete)
        // ==========================================
        // (POST) 執行刪除
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var rule = _context.QuantityRuleBases.FirstOrDefault(r => r.RuleID == id);
            if (rule != null)
            {
                _context.QuantityRuleBases.Remove(rule);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "規則已成功刪除！";
            }
            return RedirectToAction("Index");
        }
    }
}