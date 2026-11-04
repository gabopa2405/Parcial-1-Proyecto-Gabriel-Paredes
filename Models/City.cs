using System.ComponentModel.DataAnnotations;

namespace SakilaWebMVC.Models
{
    public class City
    {
        public int CityId { get; set; }

        [Required(ErrorMessage = "El nombre de la ciudad es obligatorio.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "La ciudad debe tener entre 2 y 50 caracteres.")]
        public string CityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un país.")]
        public int CountryId { get; set; }

        // Propiedad clave para el JOIN
        public string? CountryName { get; set; }

        public DateTime LastUpdate { get; set; } = DateTime.Now;
    }
}