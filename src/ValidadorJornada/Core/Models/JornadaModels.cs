using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ValidadorJornada.Core.Models
{
    /// <summary>
    /// Modelo para uma jornada editável individual
    /// </summary>
    public class JornadaEditavel : INotifyPropertyChanged
    {
        private string _jornada = string.Empty;
        private string _codigo = string.Empty;
        private string _matricula = string.Empty;
        private string _nome = string.Empty;
        private string _cargo = string.Empty;
        private DateTime _dataAlteracao = DateTime.Today;

        public string Jornada
        {
            get => _jornada;
            set
            {
                _jornada = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Codigo
        {
            get => _codigo;
            set
            {
                _codigo = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Matricula
        {
            get => _matricula;
            set
            {
                _matricula = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Nome
        {
            get => _nome;
            set
            {
                _nome = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Cargo
        {
            get => _cargo;
            set
            {
                _cargo = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public DateTime DataAlteracao
        {
            get => _dataAlteracao;
            set
            {
                _dataAlteracao = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Modelo para jornada única com múltiplos colaboradores
    /// </summary>
    public class JornadaComMultiplosColaboradores : INotifyPropertyChanged
    {
        private string _jornada = string.Empty;
        private string? _codigo;
        private ObservableCollection<ColaboradorInfo> _colaboradores = new();
        private bool _usarDataUnicaLocal = true;
        private DateTime _dataUnicaLocal = DateTime.Today;

        public string Jornada
        {
            get => _jornada;
            set
            {
                _jornada = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string? Codigo
        {
            get => _codigo;
            set
            {
                _codigo = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ColaboradorInfo> Colaboradores
        {
            get => _colaboradores;
            set
            {
                _colaboradores = value ?? new ObservableCollection<ColaboradorInfo>();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Controle local de data para esta jornada específica
        /// (usado quando DataGlobal está desativada)
        /// </summary>
        public bool UsarDataUnicaLocal
        {
            get => _usarDataUnicaLocal;
            set
            {
                _usarDataUnicaLocal = value;
                OnPropertyChanged();
                
                if (value)
                {
                    foreach (var colab in Colaboradores)
                    {
                        colab.DataAlteracao = DataUnicaLocal;
                    }
                }
            }
        }

        public DateTime DataUnicaLocal
        {
            get => _dataUnicaLocal;
            set
            {
                _dataUnicaLocal = value;
                OnPropertyChanged();
                
                if (UsarDataUnicaLocal)
                {
                    foreach (var colab in Colaboradores)
                    {
                        colab.DataAlteracao = value;
                    }
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Modelo para informações de um colaborador
    /// </summary>
    public class ColaboradorInfo : INotifyPropertyChanged
    {
        private string _matricula = string.Empty;
        private string _nome = string.Empty;
        private string _cargo = string.Empty;
        private DateTime _dataAlteracao = DateTime.Today;

        public string Matricula
        {
            get => _matricula;
            set
            {
                _matricula = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Nome
        {
            get => _nome;
            set
            {
                _nome = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Cargo
        {
            get => _cargo;
            set
            {
                _cargo = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public DateTime DataAlteracao
        {
            get => _dataAlteracao;
            set
            {
                _dataAlteracao = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
