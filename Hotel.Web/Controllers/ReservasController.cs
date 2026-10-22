
using Hotel.Web.Models;
using Hotel.Web.Repositorios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hotel.Web.Controllers;

public class ReservasController : Controller
{
    private readonly ReservaRepositorio _repositorio;

    public ReservasController(ReservaRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    // GET: /Reservas
    public async Task<IActionResult> Index()
    {
        var reservas = await _repositorio.ListarAsync();
        return View(reservas);
    }

    // GET: /Reservas/Disponibles
    public async Task<IActionResult> Disponibles(
        DateOnly? fechaIngreso,
        DateOnly? fechaSalida)
    {
        ViewBag.FechaIngreso = fechaIngreso?.ToString("yyyy-MM-dd");
        ViewBag.FechaSalida = fechaSalida?.ToString("yyyy-MM-dd");

        if (!fechaIngreso.HasValue || !fechaSalida.HasValue)
        {
            ViewBag.Noches = 0;
            return View(new List<Habitacion>());
        }

        if (fechaIngreso.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(
                "FechaIngreso",
                "La fecha de ingreso no puede ser anterior a hoy.");
        }

        if (fechaSalida.Value <= fechaIngreso.Value)
        {
            ModelState.AddModelError(
                "FechaSalida",
                "La fecha de salida debe ser posterior al ingreso.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Noches = 0;
            return View(new List<Habitacion>());
        }

        int noches = fechaSalida.Value.DayNumber
                   - fechaIngreso.Value.DayNumber;

        ViewBag.Noches = noches;

        var habitaciones = await _repositorio.BuscarDisponiblesAsync(
            fechaIngreso.Value,
            fechaSalida.Value);

        return View(habitaciones);
    }

    // GET: /Reservas/Create
    public async Task<IActionResult> Create()
    {
        await CargarListasAsync();

        return View(new Reserva
        {
            FechaIngreso = DateOnly.FromDateTime(DateTime.Today),
            FechaSalida = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Estado = "Pendiente"
        });
    }

    // POST: /Reservas/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Reserva reserva)
    {
            ModelState.Remove(nameof(Reserva.IdHuespedNavigation));
            ModelState.Remove(nameof(Reserva.IdHabitacionNavigation));
            ModelState.Remove(nameof(Reserva.Estado));


        var hoy = DateOnly.FromDateTime(DateTime.Today);

        if (reserva.FechaIngreso < hoy)
        {
            ModelState.AddModelError(
                nameof(reserva.FechaIngreso),
                "La fecha de ingreso no puede ser anterior a hoy.");
        }

        if (reserva.FechaSalida <= reserva.FechaIngreso)
        {
            ModelState.AddModelError(
                nameof(reserva.FechaSalida),
                "La fecha de salida debe ser posterior al ingreso.");
        }

        // Verificar que el huésped siga activo.
        var huespedes = await _repositorio.ListarHuespedesAsync();

        if (!huespedes.Any(h => h.IdHuesped == reserva.IdHuesped))
        {
            ModelState.AddModelError(
                nameof(reserva.IdHuesped),
                "Selecciona un huésped activo.");
        }

        // Verificar que la habitación siga activa.
        var habitaciones = await _repositorio.ListarHabitacionesAsync();

        if (!habitaciones.Any(h => h.IdHabitacion == reserva.IdHabitacion))
        {
            ModelState.AddModelError(
                nameof(reserva.IdHabitacion),
                "Selecciona una habitación activa.");
        }

        // Comprobar cruces de fechas solo si el rango es válido.
        if (reserva.FechaIngreso >= hoy &&
            reserva.FechaSalida > reserva.FechaIngreso &&
            habitaciones.Any(h => h.IdHabitacion == reserva.IdHabitacion))
        {
            bool ocupada = await _repositorio.EstaReservadaAsync(
                reserva.IdHabitacion,
                reserva.FechaIngreso,
                reserva.FechaSalida);

            if (ocupada)
            {
                ModelState.AddModelError(
                    nameof(reserva.IdHabitacion),
                    "La habitación ya está reservada en esas fechas.");
            }
        }

        if (!ModelState.IsValid)
        {
            await CargarListasAsync(
                reserva.IdHuesped,
                reserva.IdHabitacion);

            return View(reserva);
        }

        reserva.Estado = "Pendiente";
        reserva.FechaRegistro = DateTime.Now;

        await _repositorio.CrearAsync(reserva);

        TempData["Mensaje"] = "La reserva se registró correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // POST: /Reservas/Cancelar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        bool cancelada = await _repositorio.CancelarAsync(id);

        TempData["Mensaje"] = cancelada
            ? "La reserva se canceló correctamente."
            : "No se encontró la reserva o su estado no permite cancelarla.";

        return RedirectToAction(nameof(Index));
    }

    private async Task CargarListasAsync(
        int? huespedSeleccionado = null,
        int? habitacionSeleccionada = null)
    {
        var huespedes = await _repositorio.ListarHuespedesAsync();
        var habitaciones = await _repositorio.ListarHabitacionesAsync();

        ViewBag.IdHuesped = new SelectList(
            huespedes,
            nameof(Huesped.IdHuesped),
            nameof(Huesped.Apellidos),
            huespedSeleccionado);

        ViewBag.IdHabitacion = new SelectList(
            habitaciones.Select(h => new
            {
                h.IdHabitacion,
                Descripcion = h.Numero + " - " +
                    h.IdTipoHabitacionNavigation.Nombre
            }),
            nameof(Habitacion.IdHabitacion),
            "Descripcion",
            habitacionSeleccionada);
    }
}
