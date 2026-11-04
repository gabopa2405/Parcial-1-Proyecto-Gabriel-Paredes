using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SakilaWebMVC.Models;

namespace SakilaWebMVC.Controllers
{
    public class CountriesController : Controller
    {
        private readonly string _connectionString;

        public CountriesController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no fue encontrada.");
        }

        // 1. GET: Countries/Index (Con Filtro de Búsqueda y Paginación Optimizada)
        public IActionResult Index(string searchString, int page = 1)
        {
            int pageSize = 10;
            int totalRecords = 0;
            List<Country> countries = new List<Country>();

            // Sanitizamos el término de búsqueda
            searchString = searchString?.Trim() ?? string.Empty;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // 1. Contar total de registros aplicando el filtro WHERE si existe búsqueda
                string countQuery = "SELECT COUNT(*) FROM country WHERE (@Search = '' OR country LIKE '%' + @Search + '%')";
                using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                {
                    countCommand.Parameters.AddWithValue("@Search", searchString);
                    totalRecords = (int)countCommand.ExecuteScalar();
                }

                // 2. Consulta paginada filtrada por búsqueda
                string query = @"
                    SELECT country_id, country, last_update 
                    FROM country 
                    WHERE (@Search = '' OR country LIKE '%' + @Search + '%')
                    ORDER BY country ASC 
                    OFFSET @Offset ROWS 
                    FETCH NEXT @PageSize ROWS ONLY";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Search", searchString);
                    command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                    command.Parameters.AddWithValue("@PageSize", pageSize);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            countries.Add(new Country
                            {
                                CountryId = Convert.ToInt32(reader["country_id"]),
                                CountryName = reader["country"].ToString() ?? string.Empty,
                                LastUpdate = Convert.ToDateTime(reader["last_update"])
                            });
                        }
                    }
                }
            }

            // Datos para la vista
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(decimal.Divide(totalRecords, pageSize));
            ViewBag.CurrentFilter = searchString;

            return View(countries);
        }

        // 2. GET: Countries/Create (Mostrar formulario para nuevo país)
        public IActionResult Create()
        {
            return View();
        }

        // 3. POST: Countries/Create (Guardar el nuevo país en SQL Server)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Country country)
        {
            if (!ModelState.IsValid)
            {
                return View(country);
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "INSERT INTO country (country, last_update) VALUES (@CountryName, GETDATE())";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CountryName", country.CountryName);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // 4. GET: Countries/Edit/5 (Mostrar formulario de edición con el país cargado)
        public IActionResult Edit(int id)
        {
            Country country = new Country();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT country_id, country FROM country WHERE country_id = @CountryId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CountryId", id);
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            country.CountryId = Convert.ToInt32(reader["country_id"]);
                            country.CountryName = reader["country"].ToString() ?? string.Empty;
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }
            }

            return View(country);
        }

        // 5. POST: Countries/Edit/5 (Actualizar los datos en SQL Server)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Country country)
        {
            if (!ModelState.IsValid)
            {
                return View(country);
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "UPDATE country SET country = @CountryName, last_update = GETDATE() WHERE country_id = @CountryId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CountryName", country.CountryName);
                    command.Parameters.AddWithValue("@CountryId", country.CountryId);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // 6. POST: Countries/Delete/5 (Eliminar un país de la base de datos)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = "DELETE FROM country WHERE country_id = @CountryId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CountryId", id);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException)
            {
                // Aquí capturamos si SQL Server rechaza el borrado por la clave foránea (Integridad Referencial)
                TempData["Error"] = "No se puede eliminar este país porque tiene ciudades asociadas (Restricción de Clave Foránea).";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}