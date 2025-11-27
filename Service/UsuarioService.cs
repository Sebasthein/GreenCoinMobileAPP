using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GreenCoinMovil.Models;
using GreenCoinMovil.UsuarioDTO;

namespace GreenCoinMovil.Service
{
    public class UsuarioService
    {
        private readonly ApiService _apiService;

        public UsuarioService(ApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>
        /// Obtiene el perfil del usuario autenticado desde la API.
        /// </summary>
        /// <returns>El objeto Usuario con los datos del perfil.</returns>
        public async Task<Usuario> ObtenerMiPerfilAsync()
        {
            // Obtener el ID del usuario actual desde SecureStorage
            var userIdString = await SecureStorage.GetAsync("user_id");
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out long userId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado. Inicie sesión nuevamente.");
            }

            return await _apiService.ObtenerPerfilUsuarioAsync(userId);
        }

        /// <summary>
        /// Obtiene el perfil de un usuario por ID.
        /// </summary>
        /// <param name="id">ID del usuario</param>
        /// <returns>El objeto Usuario con los datos del perfil.</returns>
        public async Task<Usuario> ObtenerPerfilPorIdAsync(long id)
        {
            return await _apiService.ObtenerPerfilUsuarioAsync(id);
        }
    }
}
