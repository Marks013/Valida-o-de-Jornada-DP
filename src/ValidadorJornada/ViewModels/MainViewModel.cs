using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using ValidadorJornada.Core.Services;
using ValidadorJornada.Core.Models;
using ValidadorJornada.Core.Helpers;
using ValidadorJornada.Views;

namespace ValidadorJornada.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly JornadaValidator _validator;
        private readonly HistoricoService _historico;
        private readonly CodigoService _codigoService;
        private readonly SettingsService _settingsService;
        private bool _disposed = false;

        private string _horarios = string.Empty;
        private string _horariosDomingoFeriado = string.Empty;
        private string _mensagemResultado = "Aguardando validação...";
        private string _detalhesResultado = string.Empty;
        private string _mensagemInterjornada = string.Empty;
        private SolidColorBrush _corResultado = new SolidColorBrush(Color.FromRgb(245, 245, 245));
        private SolidColorBrush _borderResultado = new SolidColorBrush(Color.FromRgb(189, 189, 189));
        private SolidColorBrush _corInterjornada = new SolidColorBrush(Color.FromRgb(26, 26, 26));
        private bool _validarInterjornadaAtivo = false;
        private bool _autoFormatarHorarios = false;
        private bool _modoJornadaSabado = false;
        private bool _isLoading = false;
        
        private string _ultimaValidacao = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel(JornadaValidator validator, HistoricoService historico, CodigoService codigoService)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _historico = historico ?? throw new ArgumentNullException(nameof(historico));
            _codigoService = codigoService ?? throw new ArgumentNullException(nameof(codigoService));
            _settingsService = new SettingsService();

            ValidarCommand = new AsyncCommand(ValidarAsync);
            LimparCommand = new RelayCommand(Limpar);
            VerHistoricoCommand = new RelayCommand(AbrirHistorico);
            ConfigurarCodigosCommand = new RelayCommand(AbrirConfigCodigos);
            AbrirValidacaoLoteCommand = new RelayCommand(AbrirValidacaoLote);

            CarregarConfiguracoes();
            AtualizarHistorico();
        }

        #region Propriedades

        public string AppVersion => VersionInfo.FullVersionWithDate;
		
        public string Horarios
        {
            get => _horarios;
            set
            {
                _horarios = value;
                OnPropertyChanged();
                VerificarModoJornadaSabado();
            }
        }

        public string HorariosDomingoFeriado
        {
            get => _horariosDomingoFeriado;
            set
            {
                _horariosDomingoFeriado = value;
                OnPropertyChanged();
            }
        }

        public string MensagemResultado
        {
            get => _mensagemResultado;
            set
            {
                _mensagemResultado = value;
                OnPropertyChanged();
            }
        }

        public string DetalhesResultado
        {
            get => _detalhesResultado;
            set
            {
                _detalhesResultado = value;
                OnPropertyChanged();
            }
        }

        public string MensagemInterjornada
        {
            get => _mensagemInterjornada;
            set
            {
                _mensagemInterjornada = value;
                OnPropertyChanged();
            }
        }

        public SolidColorBrush CorResultado
        {
            get => _corResultado;
            set
            {
                _corResultado = value;
                OnPropertyChanged();
            }
        }

        public SolidColorBrush BorderResultado
        {
            get => _borderResultado;
            set
            {
                _borderResultado = value;
                OnPropertyChanged();
            }
        }

        public SolidColorBrush CorInterjornada
        {
            get => _corInterjornada;
            set
            {
                _corInterjornada = value;
                OnPropertyChanged();
            }
        }

        public bool ValidarInterjornadaAtivo
        {
            get => _validarInterjornadaAtivo;
            set
            {
                _validarInterjornadaAtivo = value;
                OnPropertyChanged();
                
                if (value && ModoJornadaSabado)
                {
                    ModoJornadaSabado = false;
                }

                if (!value && !ModoJornadaSabado)
                {
                    HorariosDomingoFeriado = string.Empty;
                    MensagemInterjornada = string.Empty;
                }
            }
        }

        public bool AutoFormatarHorarios
        {
            get => _autoFormatarHorarios;
            set
            {
                _autoFormatarHorarios = value;
                OnPropertyChanged();
                SalvarConfiguracoes();
            }
        }

        public bool ModoJornadaSabado
        {
            get => _modoJornadaSabado;
            set
            {
                _modoJornadaSabado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LabelCampoSecundario));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public string LabelCampoSecundario => ModoJornadaSabado 
            ? "Jornada Sábado (4 horas)" 
            : "Jornada Domingo/Feriado";

        public ObservableCollection<string> HistoricoRecente { get; } = new();

        public ICommand ValidarCommand { get; }
        public ICommand LimparCommand { get; }
        public ICommand VerHistoricoCommand { get; }
        public ICommand ConfigurarCodigosCommand { get; }
        public ICommand AbrirValidacaoLoteCommand { get; }

        #endregion

        #region Validação

        private async Task ValidarAsync()
        {
            if (IsLoading || _disposed) return;
            
            try
            {
                IsLoading = true;
                
                if (string.IsNullOrWhiteSpace(Horarios))
                {
                    ExibirAviso("Digite os horários");
                    return;
                }

                await Task.Delay(100);

                if (AutoFormatarHorarios)
                {
                    await Task.Run(() =>
                    {
                        if (_disposed) return;
                        
                        if (HorarioFormatter.PrecisaFormatar(Horarios))
                        {
                            var formatado = HorarioFormatter.FormatarHorarios(Horarios);
                            App.Current.Dispatcher.Invoke(() => Horarios = formatado);
                        }
                        
                        if (!string.IsNullOrWhiteSpace(HorariosDomingoFeriado) && 
                            HorarioFormatter.PrecisaFormatar(HorariosDomingoFeriado))
                        {
                            var formatado = HorarioFormatter.FormatarHorarios(HorariosDomingoFeriado);
                            App.Current.Dispatcher.Invoke(() => HorariosDomingoFeriado = formatado);
                        }
                    });
                }

                await Task.Run(() =>
                {
                    if (_disposed) return;
                    
                    if ((ModoJornadaSabado || ValidarInterjornadaAtivo) && 
                        !string.IsNullOrWhiteSpace(HorariosDomingoFeriado))
                    {
                        ValidarComInterjornada();
                    }
                    else
                    {
                        ValidarSimples();
                    }
                });

                AtualizarHistorico();
            }
            catch (Exception ex)
            {
                ExibirErro($"Erro: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ValidarSimples()
        {
            if (_disposed) return;
            
            var resultado = _validator.Validar(Horarios);
            App.Current.Dispatcher.Invoke(() => ExibirResultado(resultado));

            if (resultado.Valido && resultado.DuracaoCalculada != "08:00")
            {
                var chave = HorarioNormalizer.Normalizar(Horarios);
                if (_ultimaValidacao != chave)
                {
                    _historico.Salvar(resultado, Horarios, false);
                    _ultimaValidacao = chave;
                }
            }
        }

        private void ValidarComInterjornada()
        {
            if (_disposed) return;
            
            var (j1, j2, msgInter) = _validator.ValidarComInterjornada(
                Horarios, 
                HorariosDomingoFeriado, 
                ModoJornadaSabado
            );

            App.Current.Dispatcher.Invoke(() =>
            {
                if (_disposed) return;
                
                if (!j1.Valido || !j2.Valido)
                {
                    var mensagemErros = string.Empty;
                    
                    if (!j1.Valido)
                        mensagemErros += $"Jornada Principal:\n{j1.Mensagem}\n\n";
                    
                    if (!j2.Valido)
                        mensagemErros += $"Jornada {(ModoJornadaSabado ? "Sábado" : "Secundária")}:\n{j2.Mensagem}";
                    
                    ExibirErro(mensagemErros.Trim());
                    
                    MensagemInterjornada = !string.IsNullOrEmpty(msgInter) ? msgInter : string.Empty;
                    
                    if (!string.IsNullOrEmpty(msgInter))
                    {
                        CorInterjornada = msgInter.StartsWith("❌") 
                            ? new SolidColorBrush(Color.FromRgb(231, 76, 60))
                            : new SolidColorBrush(Color.FromRgb(26, 26, 26));
                    }
                    return;
                }
                
                ExibirResultado(j2);
                MensagemInterjornada = msgInter;
                
                CorInterjornada = msgInter.StartsWith("❌") 
                    ? new SolidColorBrush(Color.FromRgb(231, 76, 60))
                    : new SolidColorBrush(Color.FromRgb(26, 26, 26));
            });

            var interjornadaValida = !msgInter.StartsWith("❌");
            var chaveCompleta = $"{HorarioNormalizer.Normalizar(Horarios)}|{HorarioNormalizer.Normalizar(HorariosDomingoFeriado)}";
            
            if (j1.Valido && j2.Valido && interjornadaValida && _ultimaValidacao != chaveCompleta)
            {
                if (ModoJornadaSabado)
                {
                    var codigoPrincipal = _codigoService.BuscarCodigo(Horarios);
                    var codigoSabado = _codigoService.BuscarCodigo(HorariosDomingoFeriado);
                    
                    string? codigoCompleto = null;
                    if (!string.IsNullOrWhiteSpace(codigoPrincipal) && !string.IsNullOrWhiteSpace(codigoSabado))
                        codigoCompleto = $"{codigoPrincipal} + {codigoSabado}";
                    else if (!string.IsNullOrWhiteSpace(codigoPrincipal))
                        codigoCompleto = codigoPrincipal;
                    else if (!string.IsNullOrWhiteSpace(codigoSabado))
                        codigoCompleto = codigoSabado;
                    
                    var horarioCompleto = $"{Horarios} + Sábado: {HorariosDomingoFeriado}";
                    
                    var resultadoCompleto = new ValidationResult
                    {
                        Valido = true,
                        Mensagem = "✅ Jornada Sábado - 4h (Complemento 8h diária)",
                        DuracaoCalculada = "04:00",
                        TipoDia = "Sábado",
                        Codigo = codigoCompleto,
                        HorasSemanais = j2.HorasSemanais,
                        HorasMensais = j2.HorasMensais,
                        Intervalo = j2.Intervalo
                    };
                    
                    _historico.Salvar(resultadoCompleto, horarioCompleto, true);
                }
                
                _ultimaValidacao = chaveCompleta;
            }
        }

        private void VerificarModoJornadaSabado()
        {
            if (_disposed) return;
            
            if (string.IsNullOrWhiteSpace(Horarios))
            {
                if (ModoJornadaSabado)
                {
                    ModoJornadaSabado = false;
                    HorariosDomingoFeriado = string.Empty;
                    MensagemInterjornada = string.Empty;
                }
                return;
            }

            try
            {
                var resultado = _validator.Validar(Horarios);
                
                if (resultado.Valido && resultado.DuracaoCalculada == "08:00")
                {
                    if (!ValidarInterjornadaAtivo)
                    {
                        ModoJornadaSabado = true;
                    }
                    return;
                }
            }
            catch
            {
            }
            
            if (ModoJornadaSabado)
            {
                ModoJornadaSabado = false;
                HorariosDomingoFeriado = string.Empty;
                MensagemInterjornada = string.Empty;
            }
        }

        #endregion

        #region Exibição

        private void ExibirResultado(ValidationResult resultado)
        {
            if (_disposed) return;
            
            MensagemResultado = resultado.Mensagem;

            if (resultado.Valido)
            {
                DetalhesResultado = $"📊 {resultado.HorasSemanais}h semanais • {resultado.HorasMensais}h mensais\n" +
                                   $"📅 {resultado.TipoDia}\n" +
                                   $"⏱ {(resultado.Intervalo != null ? $"Intervalo: {resultado.Intervalo} • " : "")}Duração: {resultado.DuracaoCalculada}";

                CorResultado = new SolidColorBrush(Color.FromRgb(236, 253, 245));
                BorderResultado = new SolidColorBrush(Color.FromRgb(39, 174, 96));
            }
            else
            {
                DetalhesResultado = string.Empty;
                CorResultado = new SolidColorBrush(Color.FromRgb(254, 242, 242));
                BorderResultado = new SolidColorBrush(Color.FromRgb(231, 76, 60));
            }
        }

        private void ExibirAviso(string mensagem)
        {
            MensagemResultado = $"⚠️ {mensagem}";
            DetalhesResultado = string.Empty;
            CorResultado = new SolidColorBrush(Color.FromRgb(254, 242, 242));
            BorderResultado = new SolidColorBrush(Color.FromRgb(231, 76, 60));
        }

        private void ExibirErro(string mensagem)
        {
            MensagemResultado = $"❌ {mensagem}";
            DetalhesResultado = string.Empty;
            CorResultado = new SolidColorBrush(Color.FromRgb(254, 242, 242));
            BorderResultado = new SolidColorBrush(Color.FromRgb(231, 76, 60));
        }

        #endregion

        #region Outros

        private void Limpar()
        {
            if (_disposed) return;
            
            Horarios = string.Empty;
            HorariosDomingoFeriado = string.Empty;
            MensagemResultado = "Aguardando validação...";
            DetalhesResultado = string.Empty;
            MensagemInterjornada = string.Empty;
            CorResultado = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            BorderResultado = new SolidColorBrush(Color.FromRgb(189, 189, 189));
            CorInterjornada = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            ModoJornadaSabado = false;
            _ultimaValidacao = string.Empty;
        }

        private void AtualizarHistorico()
        {
            if (_disposed) return;
            
            App.Current.Dispatcher.Invoke(() =>
            {
                HistoricoRecente.Clear();
                foreach (var item in _historico.ObterRecentes(5))
                {
                    HistoricoRecente.Add(item);
                }
            });
        }

        private void AbrirHistorico()
        {
            if (_disposed) return;
            
            var historicoWindow = new HistoricoWindow(_historico);
            historicoWindow.ShowDialog();
            AtualizarHistorico();
        }

        private void AbrirConfigCodigos()
        {
            if (_disposed) return;
            
            var configWindow = new ConfigCodigoWindow(_codigoService);
            configWindow.ShowDialog();
        }

        private void AbrirValidacaoLote()
        {
            if (_disposed) return;
            
            var window = new ValidacaoLoteWindow
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        private void CarregarConfiguracoes()
        {
            try
            {
                var settings = _settingsService.LoadSettings();
                _autoFormatarHorarios = settings.AutoFormatarHorarios;
                
                OnPropertyChanged(nameof(AutoFormatarHorarios));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar: {ex.Message}");
            }
        }

        private void SalvarConfiguracoes()
        {
            if (_disposed) return;
            
            try
            {
                var settings = new UserSettings
                {
                    AutoFormatarHorarios = AutoFormatarHorarios,
                    ValidarInterjornadaAtivo = false
                };
                _settingsService.SaveSettings(settings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao salvar: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                try
                {
                    (_historico as IDisposable)?.Dispose();
                    (_codigoService as IDisposable)?.Dispose();
                    
                    (ValidarCommand as IDisposable)?.Dispose();
                    (LimparCommand as IDisposable)?.Dispose();
                    (VerHistoricoCommand as IDisposable)?.Dispose();
                    (ConfigurarCodigosCommand as IDisposable)?.Dispose();
                    (AbrirValidacaoLoteCommand as IDisposable)?.Dispose();
                    
                    HistoricoRecente?.Clear();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro no dispose: {ex.Message}");
                }
            }

            _disposed = true;
        }

        ~MainViewModel()
        {
            Dispose(false);
        }

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
