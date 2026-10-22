
using Hotel.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Web.Repositorios;

public class ReservaRepositorio
{
    private readonly HotelContext _context;

    public ReservaRepositorio(HotelContext context)
    {
        _context = context;
    }

    // Listar reservas con los datos del huésped y la habitación.
    public async Task<List<Reserva>> ListarAsync()
    {
        return await _context.Reservas
            .AsNoTracking()
            .Include(r => r.IdHuespedNavigation)
            .Include(r => r.IdHabitacionNavigation)
                .ThenInclude(h => h.IdTipoHabitacionNavigation)
            .OrderByDescending(r => r.FechaRegistro)
            .ToListAsync();
    }

    // Buscar una reserva por su identificador.
    public async Task<Reserva?> ObtenerPorIdAsync(int id)
    {
        return await _context.Reservas
            .AsNoTracking()
            .Include(r => r.IdHuespedNavigation)
            .Include(r => r.IdHabitacionNavigation)
            .FirstOrDefaultAsync(r => r.IdReserva == id);
    }

    // Obtener huéspedes activos para el formulario.
    public async Task<List<Huesped>> ListarHuespedesAsync()
    {
        return await _context.Huespeds
            .AsNoTracking()
            .Where(h => h.Activo)
            .OrderBy(h => h.Apellidos)
            .ThenBy(h => h.Nombres)
            .ToListAsync();
    }

    // Obtener habitaciones activas para el formulario.
    public async Task<List<Habitacion>> ListarHabitacionesAsync()
    {
        return await _context.Habitacions
            .AsNoTracking()
            .Include(h => h.IdTipoHabitacionNavigation)
            .Where(h => h.Activo)
            .OrderBy(h => h.Numero)
            .ToListAsync();
    }

    // Consultar habitaciones activas que no tengan reservas cruzadas.
    public async Task<List<Habitacion>> BuscarDisponiblesAsync(
        DateOnly fechaIngreso,
        DateOnly fechaSalida)
    {
        return await _context.Habitacions
            .AsNoTracking()
            .Include(h => h.IdTipoHabitacionNavigation)
            .Where(h => h.Activo)
            .Where(h => !_context.Reservas.Any(r =>
                r.IdHabitacion == h.IdHabitacion &&
                r.Estado != "Cancelada" &&
                r.FechaIngreso < fechaSalida &&
                r.FechaSalida > fechaIngreso))
            .OrderBy(h => h.Numero)
            .ToListAsync();
    }

    // Comprobar si una habitación tiene una reserva que se cruza con el rango.
    public async Task<bool> EstaReservadaAsync(
        int idHabitacion,
        DateOnly fechaIngreso,
        DateOnly fechaSalida,
        int? idReservaExcluir = null)
    {
        return await _context.Reservas.AnyAsync(r =>
            r.IdHabitacion == idHabitacion &&
            r.Estado != "Cancelada" &&
            (!idReservaExcluir.HasValue ||
             r.IdReserva != idReservaExcluir.Value) &&
            r.FechaIngreso < fechaSalida &&
            r.FechaSalida > fechaIngreso);
    }

    // Registrar una nueva reserva.
    public async Task CrearAsync(Reserva reserva)
    {
        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync();
    }

    // Cancelar una reserva pendiente o confirmada.
    public async Task<bool> CancelarAsync(int id)
    {
        var reserva = await _context.Reservas
            .FirstOrDefaultAsync(r => r.IdReserva == id);

        if (reserva is null ||
            (reserva.Estado != "Pendiente" &&
             reserva.Estado != "Confirmada"))
        {
            return false;
        }

        reserva.Estado = "Cancelada";
        await _context.SaveChangesAsync();

        return true;
    }
}
