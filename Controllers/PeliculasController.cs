using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SakilaWebMVC.Models;
using SakilaWebMVC.Filters;

namespace SakilaWebMVC.Controllers
{
    public class PeliculasController : Controller
    {
        private readonly string _cadenaSQL;

        public PeliculasController(IConfiguration configuration)
        {
            _cadenaSQL = configuration.GetConnectionString("DefaultConnection");
        }

        // 1. Catálogo de Películas con Buscador y filtro de eliminación lógica
        [HttpGet]
        public IActionResult Index(string buscar)
        {
            if (HttpContext.Session.GetString("Usuario") == null)
            {
                return RedirectToAction("Login", "Acceso");
            }

            List<Film> lista = new List<Film>();

            using (SqlConnection conexion = new SqlConnection(_cadenaSQL))
            {
                string query = "SELECT film_id, title, description, release_year, rental_duration, rental_rate, length, rating FROM film WHERE activo = 1";

                if (!string.IsNullOrEmpty(buscar))
                {
                    query += " AND title LIKE @Buscar";
                }

                SqlCommand cmd = new SqlCommand(query, conexion);
                cmd.CommandTimeout = 15;

                if (!string.IsNullOrEmpty(buscar))
                {
                    cmd.Parameters.AddWithValue("@Buscar", "%" + buscar + "%");
                }

                conexion.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Film
                        {
                            FilmId = Convert.ToInt32(reader["film_id"]),
                            Title = reader["title"].ToString() ?? string.Empty,
                            Description = reader["description"] != DBNull.Value ? reader["description"].ToString() ?? string.Empty : string.Empty,
                            ReleaseYear = reader["release_year"] != DBNull.Value ? Convert.ToInt32(reader["release_year"]) : (int?)null,
                            RentalDuration = Convert.ToByte(reader["rental_duration"]),
                            RentalRate = Convert.ToDecimal(reader["rental_rate"]),
                            Length = reader["length"] != DBNull.Value ? Convert.ToInt32(reader["length"]) : 0,
                            Rating = reader["rating"] != DBNull.Value ? reader["rating"].ToString() ?? string.Empty : string.Empty
                        });
                    }
                }
            }

            return View(lista);
        }

        // 2. Detalle de una Película
        [HttpGet]
        public IActionResult Detalle(int id)
        {
            if (HttpContext.Session.GetString("Usuario") == null)
            {
                return RedirectToAction("Login", "Acceso");
            }

            Film pelicula = new Film();

            using (SqlConnection conexion = new SqlConnection(_cadenaSQL))
            {
                string query = "SELECT film_id, title, description, release_year, rental_duration, rental_rate, length, rating FROM film WHERE film_id = @Id";
                SqlCommand cmd = new SqlCommand(query, conexion);
                cmd.CommandTimeout = 15;
                cmd.Parameters.AddWithValue("@Id", id);
                conexion.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        pelicula.FilmId = Convert.ToInt32(reader["film_id"]);
                        pelicula.Title = reader["title"].ToString() ?? string.Empty;
                        pelicula.Description = reader["description"] != DBNull.Value ? reader["description"].ToString() ?? string.Empty : string.Empty;
                        pelicula.ReleaseYear = reader["release_year"] != DBNull.Value ? Convert.ToInt32(reader["release_year"]) : (int?)null;
                        pelicula.RentalDuration = Convert.ToByte(reader["rental_duration"]);
                        pelicula.RentalRate = Convert.ToDecimal(reader["rental_rate"]);
                        pelicula.Length = reader["length"] != DBNull.Value ? Convert.ToInt32(reader["length"]) : 0;
                        pelicula.Rating = reader["rating"] != DBNull.Value ? reader["rating"].ToString() ?? string.Empty : string.Empty;
                    }
                }
            }

            return View(pelicula);
        }

        // 3. Alquilar película verificando disponibilidad en inventario y rental
        [HttpPost]
        public IActionResult Alquilar(int filmId)
        {
            int? clienteId = HttpContext.Session.GetInt32("ClienteId");
            if (clienteId == null)
            {
                TempData["Error"] = "No se pudo identificar al cliente en la sesión.";
                return RedirectToAction("Login", "Acceso");
            }

            using (SqlConnection conexion = new SqlConnection(_cadenaSQL))
            {
                conexion.Open();

                string queryCopia = @"
                    SELECT TOP 1 i.inventory_id 
                    FROM inventory i
                    LEFT JOIN rental r ON i.inventory_id = r.inventory_id AND r.return_date IS NULL
                    WHERE i.film_id = @FilmId AND r.rental_id IS NULL";

                SqlCommand cmdCopia = new SqlCommand(queryCopia, conexion);
                cmdCopia.CommandTimeout = 15;
                cmdCopia.Parameters.AddWithValue("@FilmId", filmId);

                object resultadoCopia = cmdCopia.ExecuteScalar();

                if (resultadoCopia == null || resultadoCopia == DBNull.Value)
                {
                    TempData["Error"] = "Lo sentimos, no hay copias disponibles para alquilar en este momento.";
                    return RedirectToAction("Index");
                }

                int inventoryId = Convert.ToInt32(resultadoCopia);

                string queryInsertarRental = @"
                    INSERT INTO rental (rental_date, inventory_id, customer_id, staff_id, last_update)
                    VALUES (GETDATE(), @InventoryId, @CustomerId, 1, GETDATE())";

                SqlCommand cmdRental = new SqlCommand(queryInsertarRental, conexion);
                cmdRental.CommandTimeout = 15;
                cmdRental.Parameters.AddWithValue("@InventoryId", inventoryId);
                cmdRental.Parameters.AddWithValue("@CustomerId", clienteId.Value);

                cmdRental.ExecuteNonQuery();
            }

            TempData["Mensaje"] = "¡Película alquilada con éxito!";
            return RedirectToAction("Index");
        }

        // 4. Reporte: Películas más alquiladas (Top 10)
        public IActionResult PeliculasMasAlquiladas()
        {
            if (HttpContext.Session.GetString("Usuario") == null)
            {
                return RedirectToAction("Login", "Acceso");
            }

            List<ReportePeliculaViewModel> reporte = new List<ReportePeliculaViewModel>();

            using (SqlConnection conexion = new SqlConnection(_cadenaSQL))
            {
                string query = @"
                    SELECT TOP 10 f.title, COUNT(r.rental_id) AS TotalAlquileres
                    FROM film f
                    INNER JOIN inventory i ON f.film_id = i.film_id
                    INNER JOIN rental r ON i.inventory_id = r.inventory_id
                    GROUP BY f.title
                    ORDER BY TotalAlquileres DESC";

                SqlCommand cmd = new SqlCommand(query, conexion);
                cmd.CommandTimeout = 15;
                conexion.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reporte.Add(new ReportePeliculaViewModel
                        {
                            Titulo = reader["title"].ToString() ?? string.Empty,
                            CantidadAlquileres = Convert.ToInt32(reader["TotalAlquileres"])
                        });
                    }
                }
            }

            return View(reporte);
        }

        // 5. Mis Alquileres (Filtrado por el cliente autenticado en Session)
        public IActionResult MisAlquileres()
        {
            int? clienteId = HttpContext.Session.GetInt32("ClienteId");
            if (clienteId == null)
            {
                return RedirectToAction("Login", "Acceso");
            }

            List<RentalViewModel> listaAlquileres = new List<RentalViewModel>();

            using (SqlConnection conexion = new SqlConnection(_cadenaSQL))
            {
                string query = @"
                    SELECT r.rental_id, f.title, r.rental_date, r.return_date, i.inventory_id
                    FROM rental r
                    INNER JOIN inventory i ON r.inventory_id = i.inventory_id
                    INNER JOIN film f ON i.film_id = f.film_id
                    WHERE r.customer_id = @CustomerId
                    ORDER BY r.rental_date DESC";

                SqlCommand cmd = new SqlCommand(query, conexion);
                cmd.CommandTimeout = 15;
                cmd.Parameters.AddWithValue("@CustomerId", clienteId.Value);
                conexion.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        listaAlquileres.Add(new RentalViewModel
                        {
                            RentalId = Convert.ToInt32(reader["rental_id"]),
                            FilmTitle = reader["title"].ToString() ?? string.Empty,
                            RentalDate = Convert.ToDateTime(reader["rental_date"]),
                            ReturnDate = reader["return_date"] != DBNull.Value ? Convert.ToDateTime(reader["return_date"]) : (DateTime?)null
                        });
                    }
                }
            }

            return View(listaAlquileres);
        }

        // 6. Devolución de película (Verificando propiedad y estado)
        [HttpPost]
        public IActionResult Devolver(int rentalId)
        {
            int? clienteId = HttpContext.Session.GetInt32("ClienteId");

            using (SqlConnection conexion = new SqlConnection(_cadenaSQL))
            {
                conexion.Open();

                string queryCheck = "SELECT customer_id, return_date FROM rental WHERE rental_id = @RentalId";
                SqlCommand cmdCheck = new SqlCommand(queryCheck, conexion);
                cmdCheck.CommandTimeout = 15;
                cmdCheck.Parameters.AddWithValue("@RentalId", rentalId);

                using (SqlDataReader reader = cmdCheck.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int propietarioId = Convert.ToInt32(reader["customer_id"]);
                        bool yaDevuelto = reader["return_date"] != DBNull.Value;

                        if (propietarioId != clienteId && HttpContext.Session.GetString("Rol") != "Administrador")
                        {
                            TempData["Error"] = "No puedes devolver un alquiler que no te pertenece.";
                            return RedirectToAction("MisAlquileres");
                        }

                        if (yaDevuelto)
                        {
                            TempData["Error"] = "Este alquiler ya fue devuelto anteriormente.";
                            return RedirectToAction("MisAlquileres");
                        }
                    }
                }

                string queryUpdate = "UPDATE rental SET return_date = GETDATE(), last_update = GETDATE() WHERE rental_id = @RentalId";
                SqlCommand cmdUpdate = new SqlCommand(queryUpdate, conexion);
                cmdUpdate.CommandTimeout = 15;
                cmdUpdate.Parameters.AddWithValue("@RentalId", rentalId);
                cmdUpdate.ExecuteNonQuery();
            }

            TempData["Mensaje"] = "¡Película devuelta con éxito!";
            return RedirectToAction("MisAlquileres");
        }

        // 7. Eliminación Lógica (Solo Administrador)
        [HttpPost]
        [VerificarRol("Administrador")]
        public IActionResult EliminacionLogica(int filmId)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaSQL))
            {
                string query = "UPDATE film SET activo = 0 WHERE film_id = @FilmId";
                SqlCommand cmd = new SqlCommand(query, conexion);
                cmd.CommandTimeout = 15;
                cmd.Parameters.AddWithValue("@FilmId", filmId);

                conexion.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["Mensaje"] = "Película eliminada lógicamente con éxito.";
            return RedirectToAction("Index");
        }

        // 8. Crear Película - GET (Solo Administrador)
        [HttpGet]
        [VerificarRol("Administrador")]
        public IActionResult Crear()
        {
            return View();
        }

        // 9. Crear Película - POST (Solo Administrador)
        [HttpPost]
        [VerificarRol("Administrador")]
        public IActionResult Crear(string title, string description, int release_year, byte rental_duration, decimal rental_rate, string rating)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaSQL))
            {
                conexion.Open();
                string query = @"
                    INSERT INTO film (title, description, release_year, language_id, rental_duration, rental_rate, rating, activo, last_update)
                    VALUES (@Title, @Description, @ReleaseYear, 1, @RentalDuration, @RentalRate, @Rating, 1, GETDATE())";

                SqlCommand cmd = new SqlCommand(query, conexion);
                cmd.CommandTimeout = 15;
                cmd.Parameters.AddWithValue("@Title", title ?? string.Empty);
                cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ReleaseYear", release_year);
                cmd.Parameters.AddWithValue("@RentalDuration", rental_duration);
                cmd.Parameters.AddWithValue("@RentalRate", rental_rate);
                cmd.Parameters.AddWithValue("@Rating", (object?)rating ?? "G");

                cmd.ExecuteNonQuery();
            }

            TempData["Mensaje"] = "¡Película creada con éxito!";
            return RedirectToAction("Index");
        }
    }
}