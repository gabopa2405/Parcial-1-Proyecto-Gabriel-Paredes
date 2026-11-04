using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace SakilaWebMVC.Controllers
{
    public class AccesoController : Controller
    {
        private readonly string _cadenaSQL;

        public AccesoController(IConfiguration configuration)
        {
            _cadenaSQL = configuration.GetConnectionString("DefaultConnection");
        }

        private string ConvertirSha256(string texto)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                texto = texto.Trim();
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(texto));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        [HttpGet]
        public IActionResult Registrar()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Registrar(string correo, string password, string rol)
        {
            string passwordHash = ConvertirSha256(password);

            using (SqlConnection conexion = new SqlConnection(_cadenaSQL))
            {
                conexion.Open();
                int? clienteIdAsignado = null;

                if (rol == "Cliente")
                {
                    string queryCustomer = @"
                        INSERT INTO customer (store_id, first_name, last_name, email, address_id, active, create_date) 
                        VALUES (1, 'User', @Correo, @Correo, 1, 1, GETDATE());
                        SELECT SCOPE_IDENTITY();";

                    SqlCommand cmdCust = new SqlCommand(queryCustomer, conexion);
                    cmdCust.CommandTimeout = 15;
                    cmdCust.Parameters.AddWithValue("@Correo", correo);

                    object custIdObj = cmdCust.ExecuteScalar();
                    if (custIdObj != null && custIdObj != DBNull.Value)
                    {
                        clienteIdAsignado = Convert.ToInt32(custIdObj);
                    }
                }

                string queryUsuario = "INSERT INTO Usuarios (Correo, PasswordHash, Rol, ClienteId) VALUES (@Correo, @PasswordHash, @Rol, @ClienteId)";
                SqlCommand cmdUser = new SqlCommand(queryUsuario, conexion);
                cmdUser.CommandTimeout = 15;
                cmdUser.Parameters.AddWithValue("@Correo", correo);
                cmdUser.Parameters.AddWithValue("@PasswordHash", passwordHash);
                cmdUser.Parameters.AddWithValue("@Rol", rol ?? "Cliente");
                cmdUser.Parameters.AddWithValue("@ClienteId", (object?)clienteIdAsignado ?? DBNull.Value);

                cmdUser.ExecuteNonQuery();
            }

            TempData["Mensaje"] = "Registro exitoso. Ahora puede iniciar sesión.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string correo, string password)
        {
            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(password))
            {
                ViewData["Error"] = "Por favor ingrese el correo y la contraseña.";
                return View();
            }

            correo = correo.Trim();
            string passwordHash = ConvertirSha256(password);

            using (SqlConnection conexion = new SqlConnection(_cadenaSQL))
            {
                conexion.Open();
                string query = "SELECT UsuarioId, Correo, Rol, ClienteId FROM Usuarios WHERE LTRIM(RTRIM(Correo)) = LTRIM(RTRIM(@Correo)) AND LTRIM(RTRIM(PasswordHash)) = LTRIM(RTRIM(@PasswordHash))";

                SqlCommand cmd = new SqlCommand(query, conexion);
                cmd.CommandTimeout = 15; // Evita el error de tiempo de espera
                cmd.Parameters.AddWithValue("@Correo", correo);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        HttpContext.Session.SetString("Usuario", reader["Correo"].ToString() ?? string.Empty);
                        HttpContext.Session.SetString("Rol", reader["Rol"].ToString() ?? string.Empty);

                        if (reader["ClienteId"] != DBNull.Value)
                        {
                            HttpContext.Session.SetInt32("ClienteId", Convert.ToInt32(reader["ClienteId"]));
                        }

                        return RedirectToAction("Index", "Peliculas");
                    }
                    else
                    {
                        ViewData["Error"] = "Correo o contraseña incorrectos.";
                        return View();
                    }
                }
            }
        }

        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}