namespace API.Models
{
    public class InfoColumn
    {
        public string Column { get; set; }
        public string Description { get; set; }
        public string data_type { get; set; }
        public double? character_maximum_length { get; set; }
        public bool is_nullable { get; set; }
    }
}