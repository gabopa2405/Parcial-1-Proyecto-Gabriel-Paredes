using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace SakilaWebMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("La cadena de conexión no fue encontrada.");
        }

        public IActionResult Index()
        {
            int totalCountries = 0;
            int totalCities = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // 1. Contar total de países usando ExecuteScalar()
                using (SqlCommand cmdCountry = new SqlCommand("SELECT COUNT(*) FROM country", connection))
                {
                    totalCountries = (int)cmdCountry.ExecuteScalar();
                }

                // 2. Contar total de ciudades usando ExecuteScalar()
                using (SqlCommand cmdCity = new SqlCommand("SELECT COUNT(*) FROM city", connection))
                {
                    totalCities = (int)cmdCity.ExecuteScalar();
                }
            }

            // Enviamos los contadores a la vista mediante ViewBag
            ViewBag.TotalCountries = totalCountries;
            ViewBag.TotalCities = totalCities;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}