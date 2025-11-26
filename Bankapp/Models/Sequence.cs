using System.ComponentModel.DataAnnotations;

namespace Bankapp.Models
{
    public class Sequence
    {
        [Key]
        public string Key { get; set; }
        public int Value { get; set; }
    }
}
