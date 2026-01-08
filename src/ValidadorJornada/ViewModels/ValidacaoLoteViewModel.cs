using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ValidadorJornada.Core.Models;
using ValidadorJornada.Core.Services;

namespace ValidadorJornada.ViewModels
{
    public class ValidacaoLoteViewModel : INotifyPropertyChanged
    {
        private readonly ValidacaoLoteService _validacaoService;
        private string _caminhoArquivo = string.Empty;
        private bool _validarPeriodos = true;
        private bool _validarJornada = true;
        private bool _validarIntervalos = true;
        private bool _usarHorariosAgrupados = false;
        private int _progressoAtual;
        private int _progressoTotal;
        private string _mensagemProgresso = string.Empty;
        private bool _processando;
        private RelatorioValidacaoLote? _relatorioAtual;
        
        public ValidacaoLoteViewModel()
        {
            var configService = new ConfigService();
            var jornadaConfig = configService.LoadConfig();
            var codigoService = new CodigoService(jornadaConfig.SkipHeadersOnImport);
            var validator = new ValidacaoLoteJornadaValidator(jornadaConfig, codigoService);
            var excelValidator = new ExcelValidatorService(validator);
            _validacaoService = new ValidacaoLoteService(excelValidator);
            
            SelecionarArquivoCommand = new RelayCommand(SelecionarArquivo);
            ValidarCommand = new AsyncCommand(ExecutarValidacao, () => PodeValidar);
            GerarRelatorioCommand = new RelayCommand(GerarRelatorio, () => _relatorioAtual != null);
            AbrirArquivoCommand = new RelayCommand(AbrirArquivo, () => !string.IsNullOrEmpty(_caminhoArquivo));
        }
        
        public ICommand SelecionarArquivoCommand { get; }
        public ICommand ValidarCommand { get; }
        public ICommand GerarRelatorioCommand { get; }
        public ICommand AbrirArquivoCommand { get; }
        
        public string CaminhoArquivo
        {
            get => _caminhoArquivo;
            set
            {
                _caminhoArquivo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NomeArquivo));
                OnPropertyChanged(nameof(PodeValidar));
            }
        }
        
        public string NomeArquivo => string.IsNullOrEmpty(_caminhoArquivo) 
            ? "Nenhum arquivo selecionado" 
            : Path.GetFileName(_caminhoArquivo);
        
        public bool ValidarPeriodos
        {
            get => _validarPeriodos;
            set { _validarPeriodos = value; OnPropertyChanged(); }
        }
        
        public bool ValidarJornada
        {
            get => _validarJornada;
            set { _validarJornada = value; OnPropertyChanged(); }
        }
        
        public bool ValidarIntervalos
        {
            get => _validarIntervalos;
            set { _validarIntervalos = value; OnPropertyChanged(); }
        }
        
        public bool UsarHorariosAgrupados
        {
            get => _usarHorariosAgrupados;
            set { _usarHorariosAgrupados = value; OnPropertyChanged(); }
        }
        
        public int ProgressoAtual
        {
            get => _progressoAtual;
            set { _progressoAtual = value; OnPropertyChanged(); }
        }
        
        public int ProgressoTotal
        {
            get => _progressoTotal;
            set { _progressoTotal = value; OnPropertyChanged(); }
        }
        
        public string MensagemProgresso
        {
            get => _mensagemProgresso;
            set { _mensagemProgresso = value; OnPropertyChanged(); }
        }
        
        public bool Processando
        {
            get => _processando;
            set
            {
                _processando = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PodeValidar));
            }
        }
        
        public bool PodeValidar => !string.IsNullOrEmpty(_caminhoArquivo) && !_processando;
        
        public string ResumoValidacao => _relatorioAtual?.ResumoTexto ?? "Aguardando validação...";
        
        public int TotalValidos => _relatorioAtual?.Validos ?? 0;
        public int TotalErros => _relatorioAtual?.Erros ?? 0;
        public int TotalAvisos => _relatorioAtual?.Avisos ?? 0;
        
        private void SelecionarArquivo()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Arquivos Excel|*.xlsx;*.xls",
                Title = "Selecionar Planilha de Horários"
            };
            
            if (dialog.ShowDialog() == true)
            {
                CaminhoArquivo = dialog.FileName;
                _relatorioAtual = null;
                AtualizarResumo();
            }
        }
        
        private async System.Threading.Tasks.Task ExecutarValidacao()
        {
            if (string.IsNullOrEmpty(_caminhoArquivo)) return;
            
            try
            {
                Processando = true;
                ProgressoAtual = 0;
                ProgressoTotal = 100;
                MensagemProgresso = "Iniciando validação...";
                
                var config = new ValidacaoLoteConfig
                {
                    ValidarPeriodos = _validarPeriodos,
                    ValidarJornada = _validarJornada,
                    ValidarIntervalos = _validarIntervalos,
                    UsarHorariosAgrupados = _usarHorariosAgrupados
                };
                
                var progresso = new Progress<ProgressoValidacao>(p =>
                {
                    ProgressoAtual = p.LinhaAtual;
                    ProgressoTotal = p.TotalLinhas;
                    MensagemProgresso = p.Mensagem;
                });
                
                _relatorioAtual = await _validacaoService.ExecutarValidacao(
                    _caminhoArquivo, 
                    config, 
                    progresso);
                
                MensagemProgresso = "Validação concluída!";
                AtualizarResumo();
                
                MessageBox.Show(
                    $"Validação concluída!\n\n{_relatorioAtual.ResumoTexto}\n\nArquivo atualizado com cores e aba 'Erros_Validacao'.",
                    "Sucesso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao processar arquivo:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                MensagemProgresso = "Erro na validação";
            }
            finally
            {
                Processando = false;
            }
        }
        
        private void GerarRelatorio()
        {
            if (_relatorioAtual == null) return;
            
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "PDF|*.pdf",
                    FileName = $"Relatorio_Validacao_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };
                
                if (dialog.ShowDialog() == true)
                {
                    _validacaoService.GerarRelatorioPDF(_relatorioAtual, dialog.FileName);
                    
                    var result = MessageBox.Show(
                        "Relatório PDF gerado com sucesso!\n\nDeseja abrir o arquivo?",
                        "Sucesso",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = dialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao gerar relatório:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        private void AbrirArquivo()
        {
            if (string.IsNullOrEmpty(_caminhoArquivo) || !File.Exists(_caminhoArquivo)) return;
            
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _caminhoArquivo,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao abrir arquivo:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        private void AtualizarResumo()
        {
            OnPropertyChanged(nameof(ResumoValidacao));
            OnPropertyChanged(nameof(TotalValidos));
            OnPropertyChanged(nameof(TotalErros));
            OnPropertyChanged(nameof(TotalAvisos));
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}