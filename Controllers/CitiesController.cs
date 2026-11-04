using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using SakilaWebMVC.Models;

namespace SakilaWebMVC.Controllers
{
    public class CitiesController : Controller
    {
        private readonly string _connectionString;

        public CitiesController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("La cadena de conexión no fue encontrada.");
        }

        // 1. GET: Cities/Index (Con Filtro de Búsqueda y Paginación Optimizada)
        public IActionResult Index(string searchString, int page = 1)
        {
            int pageSize = 10;
            int totalRecords = 0;
            List<City> cities = new List<City>();

            searchString = searchString?.Trim() ?? string.Empty;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // 1. Contar total de registros filtrados (busca tanto por nombre de ciudad como por nombre de país)
                string countQuery = @"
                    SELECT COUNT(*) 
                    FROM city ci
                    INNER JOIN country co ON ci.country_id = co.country_id
                    WHERE (@Search = '' OR ci.city LIKE '%' + @Search + '%' OR co.country LIKE '%' + @Search + '%')";

                using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                {
                    countCommand.Parameters.AddWithValue("@Search", searchString);
                    totalRecords = (int)countCommand.ExecuteScalar();
                }

                // 2. Consulta paginada con INNER JOIN y filtro
                string query = @"
                    SELECT ci.city_id, ci.city, ci.country_id, co.country, ci.last_update 
                    FROM city ci
                    INNER JOIN country co ON ci.country_id = co.country_id
                    WHERE (@Search = '' OR ci.city LIKE '%' + @Search + '%' OR co.country LIKE '%' + @Search + '%')
                    ORDER BY ci.city ASC
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
                            cities.Add(new City
                            {
                                CityId = Convert.ToInt32(reader["city_id"]),
                                CityName = reader["city"].ToString() ?? string.Empty,
                                CountryId = Convert.ToInt32(reader["country_id"]),
                                CountryName = reader["country"].ToString() ?? string.Empty,
                                LastUpdate = Convert.ToDateTime(reader["last_update"])
                            });
                        }
                    }
                }
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(decimal.Divide(totalRecords, pageSize));
            ViewBag.CurrentFilter = searchString;

            return View(cities);
        }

        // 2. GET: Cities/Create (Cargar formulario y la lista desplegable de países)
        public IActionResult Create()
        {
            LoadCountriesDropdown();
            return View();
        }

        // 3. POST: Cities/Create (Guardar nueva ciudad en SQL Server)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(City city)
        {
            if (!ModelState.IsValid)
            {
                LoadCountriesDropdown(city.CountryId);
                return View(city);
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "INSERT INTO city (city, country_id, last_update) VALUES (@CityName, @CountryId, GETDATE())";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CityName", city.CityName);
                    command.Parameters.AddWithValue("@CountryId", city.CountryId);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // Método auxiliar para llenar el Dropdown de Países desde la BD
        private void LoadCountriesDropdown(object? selectedCountryId = null)
        {
            List<Country> countries = new List<Country>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "country_id, country FROM country ORDER BY country ASC";
                string fullQuery = "SELECT " + query; // Ordenado para evitar errores de sintaxis

                using (SqlCommand command = new SqlCommand(fullQuery, connection))
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            countries.Add(new Country
                            {
                                CountryId = Convert.ToInt32(reader["country_id"]),
                                CountryName = reader["country"].ToString() ?? string.Empty
                            });
                        }
                    }
                }
            }

            // ViewBag almacena la lista desplegable para que la vista Razor la pinte
            ViewBag.CountriesList = new SelectList(countries, "CountryId", "CountryName", selectedCountryId);
        }

        // 4. GET: Cities/Edit/5 (Cargar formulario de edición con la ciudad y su país seleccionado)
        public IActionResult Edit(int id)
        {
            City city = new City();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT city_id, city, country_id FROM city WHERE city_id = @CityId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CityId", id);
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            city.CityId = Convert.ToInt32(reader["city_id"]);
                            city.CityName = reader["city"].ToString() ?? string.Empty;
                            city.CountryId = Convert.ToInt32(reader["country_id"]);
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }
            }

            // Cargamos el dropdown pasando el ID actual para que aparezca seleccionado por defecto
            LoadCountriesDropdown(city.CountryId);
            return View(city);
        }

        // 5. POST: Cities/Edit/5 (Guardar cambios de la ciudad en SQL Server)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(City city)
        {
            if (!ModelState.IsValid)
            {
                LoadCountriesDropdown(city.CountryId);
                return View(city);
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "UPDATE city SET city = @CityName, country_id = @CountryId, last_update = GETDATE() WHERE city_id = @CityId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CityName", city.CityName);
                    command.Parameters.AddWithValue("@CountryId", city.CountryId);
                    command.Parameters.AddWithValue("@CityId", city.CityId);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // 6. POST: Cities/Delete/5 (Eliminar una ciudad de la base de datos)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = "DELETE FROM city WHERE city_id = @CityId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CityId", id);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException)
            {
                TempData["Error"] = "No se puede eliminar esta ciudad porque tiene registros asociados (como direcciones en la base Sakila).";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}