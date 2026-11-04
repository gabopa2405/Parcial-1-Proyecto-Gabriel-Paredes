using System.ComponentModel.DataAnnotations;

namespace SakilaWebMVC.Models
{
    public class Film
    {
        public int FilmId { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public int? ReleaseYear { get; set; }
        public byte RentalDuration { get; set; }
        public decimal RentalRate { get; set; }
        public int Length { get; set; }
        public string Rating { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }
}