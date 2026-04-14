using System;

namespace 崴正.Models
{
    public class HistoricalTransaction
    {
        public int Transaction_ID { get; set; }
        public int Project_ID { get; set; }
        public int Material_ID { get; set; }
        public decimal Actual_Quantity { get; set; }
        public decimal Unit_Price { get; set; }
        public DateTime Transaction_Date { get; set; }
    }
}