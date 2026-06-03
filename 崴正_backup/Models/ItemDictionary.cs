using System.ComponentModel.DataAnnotations;

namespace 崴正.Models
{
    public class ItemDictionary
    {
        [Key]
        public int DictionaryId { get; set; }
        public string Keyword { get; set; } = "";
        public int Material_Id { get; set; }
    }
}