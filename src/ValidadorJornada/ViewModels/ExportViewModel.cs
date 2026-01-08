using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ValidadorJornada.Core.Services;
using ValidadorJornada.ViewModels;

namespace ValidadorJornada.ViewModels
{
    public class ExportViewModel : INotifyPropertyChanged
    {
        private readonly ExportService _exportService;
        private DateTime _dataReferencia;
        private string _mensagemStatus = string.Empty;
        private bool _isProcessing = false;
        private string _matricula = string.Empty;
        private string _nome = string.Empty;
        private string _cargo = string.Empty;

        public ExportViewModel(ExportService exportService)
        {
            _exportService = exportService;
            _dataReferencia = DateTime.Today;
            
            ConfirmarCommand = new RelayCommand(Confirmar, CanConfirmar);
        }

        public DateTime DataReferencia
        {
            get => _dataReferencia;
            set
            {
                _dataReferencia = value;
                OnPropertyChanged();
                ValidarData();
            }
        }

        public string Matricula
        {
            get => _matricula;
            set
            {
                _matricula = value;
                OnPropertyChanged();
            }
        }

        public string Nome
        {
            get => _nome;
            set
            {
                _nome = value;
                OnPropertyChanged();
            }
        }

        public string Cargo
        {
            get => _cargo;
            set
            {
                _cargo = value;
                OnPropertyChanged();
            }
        }

        public string MensagemStatus
        {
            get => _mensagemStatus;
            set
            {
                _mensagemStatus = value;
                OnPropertyChanged();
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand ConfirmarCommand { get; }
        public ExportResult? Resultado { get; private set; }

        private void Confirmar()
        {
            if (!ValidarData())
                return;

            IsProcessing = true;
            MensagemStatus = "Gerando PDF...";

            Resultado = new ExportResult { Sucesso = true };
            
            IsProcessing = false;
        }

        private bool CanConfirmar()
        {
            return !IsProcessing && ValidarData();
        }

        private bool ValidarData()
        {
            var hoje = DateTime.Now;
            var primeiroDia = new DateTime(hoje.Year, hoje.Month, 1);
            var ultimoDia = new DateTime(hoje.Year, hoje.Month, DateTime.DaysInMonth(hoje.Year, hoje.Month));

            if (DataReferencia < primeiroDia || DataReferencia > ultimoDia)
            {
                MensagemStatus = $"⚠️ Data deve estar entre {primeiroDia:dd/MM/yyyy} e {ultimoDia:dd/MM/yyyy}";
                return false;
            }

            MensagemStatus = string.Empty;
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
