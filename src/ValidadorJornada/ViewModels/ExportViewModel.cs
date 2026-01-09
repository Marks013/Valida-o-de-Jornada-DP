using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ValidadorJornada.Core.Services;

namespace ValidadorJornada.ViewModels
{
    /// <summary>
    /// ViewModel para ExportDialog - Gerencia apenas DataReferencia global
    /// 
    /// NOTA: Este ViewModel é usado APENAS no modo "Múltiplos Horários".
    /// No modo "Multi-Colaborador", os dados são gerenciados diretamente 
    /// pela classe JornadaComMultiplosColaboradores no code-behind.
    /// 
    /// Responsabilidades:
    /// - DataReferencia: Data global para o modo "Múltiplos Horários"
    /// - ValidarData: Validação da data conforme regras de negócio
    /// - MensagemStatus: Feedback visual de validação
    /// </summary>
    public class ExportViewModel : INotifyPropertyChanged
    {
        private readonly ExportService _exportService;
        private DateTime _dataReferencia;
        private string _mensagemStatus = string.Empty;
        private bool _isProcessing = false;

        public ExportViewModel(ExportService exportService)
        {
            _exportService = exportService;
            _dataReferencia = DateTime.Today;
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
            }
        }

        public ExportResult? Resultado { get; private set; }

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
