﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VolunteerApi.DTOs;
using VolunteerApi.Models;

namespace VolunteerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<EventosController> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly GoogleAIOptions _googleAIOptions;
        private readonly string _modeloGemini;

        public EventosController(AppDbContext context, IConfiguration config,
            ILogger<EventosController> logger, IHttpClientFactory httpClientFactory, IOptions<GoogleAIOptions> googleAIOptions)
        {
            _googleAIOptions = googleAIOptions.Value;

            _context = context;
            _config = config;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();

            _apiKey = _googleAIOptions.ApiKey;
            _modeloGemini = _googleAIOptions.Model ?? "gemini-1.5-pro";
        }

        [HttpGet]
        public async Task<IActionResult> GetEventos()
        {
            var eventos = await _context.Eventos
                .Include(e => e.Organizador)
                .Include(e => e.Voluntarios)
                .Select(e => new EventoDTO
                {
                    EventoId = e.EventoId,
                    NombreEvento = e.NombreEvento,
                    Fecha = e.Fecha,
                    Ubicacion = e.Ubicacion,
                    Descripcion = e.Descripcion,
                    Organizador = e.Organizador == null ? null : new UsuarioCreateDTO
                    {
                        UsuarioId = e.Organizador.UsuarioId,
                        Nombre = e.Organizador.Nombre,
                        Apellido = e.Organizador.Apellido,
                        Email = e.Organizador.Email,
                        RolId = e.Organizador.RolId
                    },
                    Voluntarios = e.Voluntarios.Select(v => new VoluntarioDTO
                    {
                        VoluntarioId = v.VoluntarioId,
                        Sexo = v.Sexo,
                        Domicilio = v.Domicilio,
                        NumeroCelular = v.NumeroCelular,
                        Especialidad = v.Especialidad != null ? v.Especialidad.NombreEspecialidad : null
                    }).ToList()
                }).ToListAsync();

            return Ok(eventos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvento(int id)
        {
            var evento = await _context.Eventos
                .Include(e => e.Voluntarios)
                .FirstOrDefaultAsync(e => e.EventoId == id);

            return evento == null ? NotFound() : Ok(evento);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutEvento(int id, Evento evento)
        {
            if (id != evento.EventoId) return BadRequest();

            _context.Entry(evento).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventoExists(id))
                    return NotFound();
                else
                    throw;
            }

        }

        [HttpPost]
        public async Task<IActionResult> CrearEvento([FromBody] EventoCreateDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var nuevoEvento = new Evento
            {
                NombreEvento = dto.NombreEvento,
                Fecha = dto.Fecha,
                Ubicacion = dto.Ubicacion,
                Descripcion = dto.Descripcion,
                OrganizadorId = dto.OrganizadorId
            };

            _context.Eventos.Add(nuevoEvento);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Evento creado exitosamente", eventoId = nuevoEvento.EventoId });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvento(int id)
        {
            var evento = await _context.Eventos.FindAsync(id);
            if (evento == null) return NotFound();

            _context.Eventos.Remove(evento);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EventoExists(int id)
        {
            return _context.Eventos.Any(e => e.EventoId == id);
        }
    }
}
