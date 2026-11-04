using System.ComponentModel.DataAnnotations;

namespace SakilaWebMVC.Models
{
    public class Country
    {
        public int CountryId { get; set; }

        [Required(ErrorMessage = "El nombre del país es obligatorio.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El país debe tener entre 2 y 50 caracteres.")]
        public string CountryName { get; set; } = string.Empty;

        public DateTime LastUpdate { get; set; } = DateTime.Now;
    }
}