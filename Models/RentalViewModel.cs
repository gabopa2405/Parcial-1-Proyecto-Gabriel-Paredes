namespace SakilaWebMVC.Models
{
    public class RentalViewModel
    {
        public int RentalId { get; set; }
        public string FilmTitle { get; set; }
        public DateTime RentalDate { get; set; }
        public DateTime? ReturnDate { get; set; }
    }
}