
using Hotel.Web.Models;
using Hotel.Web.Repositorios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hotel.Web.Controllers;

public class HabitacionesController : Controller
{
    private readonly HabitacionRepositorio _repositorio;

    public HabitacionesController(HabitacionRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    // GET: /Habitaciones
    public async Task<IActionResult> Index(int? idTipoHabitacion)
    {
        ViewBag.TipoSeleccionado = idTipoHabitacion;

        await CargarTiposAsync(idTipoHabitacion);

        var habitaciones = await _repositorio.ListarAsync(idTipoHabitacion);

        return View(habitaciones);
    }

    // GET: /Habitaciones/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
            return NotFound();

        var habitacion = await _repositorio.ObtenerPorIdAsync(id.Value);

        if (habitacion is null)
            return NotFound();

        return View(habitacion);
    }

    // GET: /Habitaciones/Create
    public async Task<IActionResult> Create()
    {
        await CargarTiposAsync();

        return View();
    }

    // POST: /Habitaciones/Create
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Habitacion habitacion)
{
    ModelState.Remove(nameof(Habitacion.IdTipoHabitacionNavigation));

    if (await _repositorio.ExisteNumeroAsync(habitacion.Numero))
    {
        ModelState.AddModelError(
            nameof(habitacion.Numero),
            "Ya existe una habitación con ese número.");
    }

    if (!ModelState.IsValid)
    {
        await CargarTiposAsync(habitacion.IdTipoHabitacion);
        return View(habitacion);
    }

    habitacion.Activo = true;

    await _repositorio.CrearAsync(habitacion);

    TempData["Mensaje"] = "La habitación se registró correctamente.";

    return RedirectToAction(nameof(Index));
}
    // GET: /Habitaciones/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
            return NotFound();

        var habitacion = await _repositorio.ObtenerPorIdAsync(id.Value);

        if (habitacion is null)
            return NotFound();

        await CargarTiposAsync(habitacion.IdTipoHabitacion);

        return View(habitacion);
    }

    // POST: /Habitaciones/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Habitacion habitacion)
    {
        ModelState.Remove(nameof(Habitacion.IdTipoHabitacionNavigation));
        
        if (id != habitacion.IdHabitacion)
            return NotFound();

        if (await _repositorio.ExisteNumeroAsync(
            habitacion.Numero, habitacion.IdHabitacion))
        {
            ModelState.AddModelError(
                nameof(habitacion.Numero),
                "Ya existe otra habitación con ese número.");
        }

        if (!ModelState.IsValid)
        {
            await CargarTiposAsync(habitacion.IdTipoHabitacion);
            return View(habitacion);
        }

        // Mantener la desactivación lógica si la habitación estaba inactiva.
        var existente = await _repositorio.ObtenerPorIdAsync(id);

        if (existente is null)
            return NotFound();

        habitacion.Activo = existente.Activo;

        await _repositorio.ActualizarAsync(habitacion);

        TempData["Mensaje"] = "La habitación se actualizó correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // GET: /Habitaciones/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
            return NotFound();

        var habitacion = await _repositorio.ObtenerPorIdAsync(id.Value);

        if (habitacion is null)
            return NotFound();

        return View(habitacion);
    }

    // POST: /Habitaciones/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var habitacion = await _repositorio.ObtenerPorIdAsync(id);

        if (habitacion is null)
            return NotFound();

        await _repositorio.DesactivarAsync(id);

        TempData["Mensaje"] = "La habitación se desactivó correctamente.";

        return RedirectToAction(nameof(Index));
    }

    private async Task CargarTiposAsync(int? seleccionado = null)
    {
        var tipos = await _repositorio.ListarTiposAsync();

        ViewBag.IdTipoHabitacion = new SelectList(
            tipos,
            nameof(TipoHabitacion.IdTipoHabitacion),
            nameof(TipoHabitacion.Nombre),
            seleccionado);
    }
}
