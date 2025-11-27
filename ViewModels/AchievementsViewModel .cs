using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenCoinMovil.DTO;

namespace GreenCoinMovil.ViewModels
{
    public partial class AchievementsViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private int _logrosDesbloqueados;

        [ObservableProperty]
        private int _totalLogros;

        [ObservableProperty]
        private double _progresoLogros;

        [ObservableProperty]
        private string _progresoTexto;

        [ObservableProperty]
        private ObservableCollection<Logro> _logros;

        [ObservableProperty]
        private ObservableCollection<Logro> _logrosCompletados;

        [ObservableProperty]
        private ObservableCollection<Logro> _logrosEnProgreso;

        [ObservableProperty]
        private int _puntosTotales;

        public AchievementsViewModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
            Logros = new ObservableCollection<Logro>();
            LogrosCompletados = new ObservableCollection<Logro>();
            LogrosEnProgreso = new ObservableCollection<Logro>();
            LoadAchievementsDataCommand.Execute(null);
        }

        [RelayCommand]
        private async Task LoadAchievementsData()
        {
            IsBusy = true;
            try
            {
                // Cargar logros desde la API
                await CargarLogrosDesdeAPI();
                ActualizarProgreso();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Error al cargar los logros: {ex.Message}", "OK");
                // Fallback a datos por defecto
                CargarLogrosPorDefecto();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CargarLogrosDesdeAPI()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/logros");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    // Aquí necesitarías parsear la respuesta HTML o cambiar el endpoint
                    // Por ahora usamos datos simulados
                    await CargarLogrosSimulados();
                }
                else
                {
                    await CargarLogrosSimulados();
                }
            }
            catch (Exception ex)
            {
                await CargarLogrosSimulados();
            }
        }

        private async Task CargarLogrosSimulados()
        {
            // Simular carga desde base de datos
            await Task.Delay(1000);

            var logrosSimulados = new List<Logro>
            {
                new Logro
                {
                    Id = 1,
                    Nombre = "Primeros Pasos",
                    Descripcion = "Completa tu primer reciclaje",
                    Icono = "🌱",
                    IconoEstado = "✓",
                    ColorFondo = "#E8F5E8",
                    ColorEstado = "#00b894",
                    Desbloqueado = true,
                    ProgresoActual = 1,
                    ProgresoTotal = 1,
                    PuntosRequeridos = 0
                },
                new Logro
                {
                    Id = 2,
                    Nombre = "Reciclador Activo",
                    Descripcion = "Realiza 10 reciclajes en total",
                    Icono = "♻️",
                    IconoEstado = "3/10",
                    ColorFondo = "#E8F5E8",
                    ColorEstado = "#636e72",
                    Desbloqueado = false,
                    ProgresoActual = 3,
                    ProgresoTotal = 10,
                    PuntosRequeridos = 200
                },
                new Logro
                {
                    Id = 3,
                    Nombre = "Amigo del Planeta",
                    Descripcion = "Recicla 5 tipos diferentes de materiales",
                    Icono = "🌍",
                    IconoEstado = "2/5",
                    ColorFondo = "#E8F5E8",
                    ColorEstado = "#636e72",
                    Desbloqueado = false,
                    ProgresoActual = 2,
                    ProgresoTotal = 5,
                    PuntosRequeridos = 150
                },
                new Logro
                {
                    Id = 4,
                    Nombre = "Coleccionista Verde",
                    Descripcion = "Alcanza 500 puntos totales",
                    Icono = "⭐",
                    IconoEstado = "350/500",
                    ColorFondo = "#E8F5E8",
                    ColorEstado = "#636e72",
                    Desbloqueado = false,
                    ProgresoActual = 350,
                    ProgresoTotal = 500,
                    PuntosRequeridos = 500
                }
            };

            Logros.Clear();
            foreach (var logro in logrosSimulados)
            {
                Logros.Add(logro);
            }

            // Actualizar estadísticas
            LogrosDesbloqueados = logrosSimulados.Count(l => l.Desbloqueado);
            TotalLogros = logrosSimulados.Count;
            PuntosTotales = 350; // Esto debería venir del usuario
        }

        private void CargarLogrosPorDefecto()
        {
            Logros.Clear();

            var logros = new List<Logro>
            {
                new Logro
                {
                    Id = 1,
                    Nombre = "Primeros Pasos",
                    Descripcion = "Completa tu primer reciclaje",
                    Icono = "🌱",
                    IconoEstado = "✓",
                    ColorFondo = "#E8F5E8",
                    ColorEstado = "#00b894",
                    Desbloqueado = true,
                    ProgresoActual = 1,
                    ProgresoTotal = 1,
                    PuntosRequeridos = 0
                }
            };

            foreach (var logro in logros)
            {
                Logros.Add(logro);
            }

            LogrosDesbloqueados = 1;
            TotalLogros = 15;
        }

        private void ActualizarProgreso()
        {
            ProgresoLogros = TotalLogros > 0 ? (double)LogrosDesbloqueados / TotalLogros : 0;
            ProgresoTexto = $"{LogrosDesbloqueados} de {TotalLogros} logros desbloqueados";
        }

        [RelayCommand]
        private async Task ActualizarLogros()
        {
            await LoadAchievementsData();
        }
    }

}
