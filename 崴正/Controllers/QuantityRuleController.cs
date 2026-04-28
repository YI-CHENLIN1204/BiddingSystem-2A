using Microsoft.AspNetCore.Mvc;
using 崴正.Data;
using 崴正.Models;
using System.Linq;

namespace 崴正.Controllers
{
    public class QuantityRuleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuantityRuleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. 顯示所有規則的列表 (首頁)
        public IActionResult Index()
        {
            var rules = _context.QuantityRuleBases.ToList();
            return View(rules);
        }

        // 2. 點擊「編輯」進入的畫面 (GET)
        public IActionResult Edit(int id)
        {
            // 找出點擊的那筆規則
            var rule = _context.QuantityRuleBases.FirstOrDefault(r => r.RuleID == id);
            if (rule == null) return NotFound();
            
            return View(rule);
        }

        // 3. 按下「儲存」後執行的動作 (POST)
        [HttpPost]
        public IActionResult Edit(QuantityRuleBase updatedRule)
        {
            // 將修改後的資料存進 Supabase
            _context.QuantityRuleBases.Update(updatedRule);
            _context.SaveChanges();
            
            // 存檔成功後，跳轉回列表頁
            return RedirectToAction("Index");
        }
    }
}