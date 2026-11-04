using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SakilaWebMVC.Filters
{
    public class VerificarRolAttribute : ActionFilterAttribute
    {
        private readonly string _rolRequerido;

        public VerificarRolAttribute(string rolRequerido)
        {
            _rolRequerido = rolRequerido;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var usuario = context.HttpContext.Session.GetString("Usuario");
            var rolUsuario = context.HttpContext.Session.GetString("Rol");

            if (string.IsNullOrEmpty(usuario))
            {
                context.Result = new RedirectToActionResult("Login", "Acceso", null);
                return;
            }

            if (rolUsuario != _rolRequerido)
            {
                context.Result = new RedirectToActionResult("Index", "Peliculas", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}