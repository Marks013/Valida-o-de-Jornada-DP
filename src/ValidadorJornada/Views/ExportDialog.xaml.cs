using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ValidadorJornada.Core.Services;
using ValidadorJornada.Core.Models;
using ValidadorJornada.ViewModels;

namespace ValidadorJornada.Views
{
    public partial class ExportDialog : Window, INotifyPropertyChanged
    {
        private readonly ExportViewModel _viewModel;
        private readonly ExportService _exportService;
        private bool _modoMultiplosColaboradores = false;
        private ObservableCollection<JornadaComMultiplosColaboradores> _jornadas = new();
        private bool _usarDataGlobal = true;
        private DateTime _dataGlobal = DateTime.Today;

        public ExportResult? Resultado { get; private set; }

        public bool ModoMultiplosColaboradores
        {
            get => _modoMultiplosColaboradores;
            set
            {
                _modoMultiplosColaboradores = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ModoMultiplosHorarios));
            }
        }

        public bool ModoMultiplosHorarios => !_modoMultiplosColaboradores;

        public ObservableCollection<JornadaComMultiplosColaboradores> Jornadas
        {
            get => _jornadas;
            set
            {
                _jornadas = value;
                OnPropertyChanged();
            }
        }

        public bool UsarDataGlobal
        {
            get => _usarDataGlobal;
            set
            {
                _usarDataGlobal = value;
                OnPropertyChanged();
                
                if (value)
                {
                    // Aplica data global a todos
                    foreach (var jornada in Jornadas)
                    {
                        jornada.DataUnicaLocal = DataGlobal;
                        foreach (var colab in jornada.Colaboradores)
                        {
                            colab.DataAlteracao = DataGlobal;
                        }
                    }
                }
            }
        }

        public DateTime DataGlobal
        {
            get => _dataGlobal;
            set
            {
                _dataGlobal = value;
                OnPropertyChanged();
                
                if (UsarDataGlobal)
                {
                    foreach (var jornada in Jornadas)
                    {
                        jornada.DataUnicaLocal = value;
                        foreach (var colab in jornada.Colaboradores)
                        {
                            colab.DataAlteracao = value;
                        }
                    }
                }
            }
        }

        public ICommand AdicionarColaboradorCommand { get; }
        public ICommand RemoverColaboradorCommand { get; }

        public ExportDialog(List<string> jornadasSelecionadas, ExportService exportService)
        {
            InitializeComponent();
            
            if (jornadasSelecionadas == null || jornadasSelecionadas.Count == 0)
                throw new ArgumentException("Nenhuma jornada selecionada");
            
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            
            _viewModel = new ExportViewModel(_exportService);
            DataContext = this;
            
            AdicionarColaboradorCommand = new RelayCommand<object>(AdicionarColaborador);
            RemoverColaboradorCommand = new RelayCommand<object>(RemoverColaborador);
            
            ProcessarJornadas(jornadasSelecionadas);
            AtualizarInstrucoes();
        }

        private void ProcessarJornadas(List<string> historicoSelecionado)
        {
            // SEMPRE ativa modo multi-colaborador
            ModoMultiplosColaboradores = true;
            
            Jornadas.Clear();

            var jornadasProcessadas = new HashSet<string>();

            foreach (var item in historicoSelecionado)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(item))
                        continue;

                    var (horarios, codigo) = ExtrairHorariosECodigo(item);
                    
                    if (string.IsNullOrWhiteSpace(horarios) || jornadasProcessadas.Contains(horarios))
                        continue;

                    var novaJornada = new JornadaComMultiplosColaboradores
                    {
                        Jornada = horarios,
                        Codigo = codigo,
                        Colaboradores = new ObservableCollection<ColaboradorInfo>
                        {
                            new ColaboradorInfo 
                            { 
                                Matricula = string.Empty,
                                Nome = string.Empty,
                                Cargo = string.Empty,
                                DataAlteracao = DateTime.Today 
                            }
                        },
                        UsarDataUnicaLocal = true,
                        DataUnicaLocal = DateTime.Today
                    };

                    Jornadas.Add(novaJornada);
                    jornadasProcessadas.Add(horarios);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao processar jornada: {ex.Message}");
                    continue;
                }
            }
        }

        private (string horarios, string? codigo) ExtrairHorariosECodigo(string linha)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(linha))
                    return (string.Empty, null);

                var partes = linha.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (partes.Length == 0) 
                    return (string.Empty, null);

                var primeiraLinha = partes[0];
                var inicioHorarios = primeiraLinha.IndexOf(']');
                
                if (inicioHorarios < 0 || inicioHorarios >= primeiraLinha.Length - 1) 
                    return (string.Empty, null);
                
                var horarios = primeiraLinha.Substring(inicioHorarios + 1).Trim();
                
                if (horarios.Contains("Sábado:"))
                {
                    horarios = horarios.Replace(" + Sábado:", " Sábado:");
                }
                
                if (string.IsNullOrWhiteSpace(horarios))
                    return (string.Empty, null);
                
                string? codigo = null;
                var regexPatterns = new[]
                {
                    @"(?:Código|Código|Codigo):\s*([^\)]+)\)",
                    @"\((?:Código|Código|Codigo):\s*([^\)]+)\)"
                };
                
                foreach (var parte in partes)
                {
                    foreach (var pattern in regexPatterns)
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(parte, pattern);
                        if (match.Success && match.Groups.Count > 1)
                        {
                            var cod = match.Groups[1].Value?.Trim();
                            if (!string.IsNullOrWhiteSpace(cod))
                            {
                                codigo = cod;
                                break;
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(codigo))
                        break;
                }
                
                return (horarios, codigo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao extrair horários/código: {ex.Message}");
                return (string.Empty, null);
            }
        }

        private void AdicionarColaborador(object? parameter)
        {
            if (parameter is JornadaComMultiplosColaboradores jornada)
            {
                var dataInicial = UsarDataGlobal 
                    ? DataGlobal 
                    : (jornada.UsarDataUnicaLocal 
                        ? jornada.DataUnicaLocal 
                        : DateTime.Today);
                
                var novoColab = new ColaboradorInfo
                {
                    Matricula = string.Empty,
                    Nome = string.Empty,
                    Cargo = string.Empty,
                    DataAlteracao = dataInicial
                };
                
                jornada.Colaboradores.Add(novoColab);
                OnPropertyChanged(nameof(Jornadas));
            }
        }

        private void RemoverColaborador(object? parameter)
        {
            // Parameter pode vir como array: [jornada, colaborador]
            if (parameter is object[] array && array.Length == 2)
            {
                if (array[0] is JornadaComMultiplosColaboradores jornada && 
                    array[1] is ColaboradorInfo colaborador)
                {
                    if (jornada.Colaboradores.Count > 1)
                    {
                        jornada.Colaboradores.Remove(colaborador);
                        OnPropertyChanged(nameof(Jornadas));
                    }
                    else
                    {
                        MessageBox.Show(
                            "Cada jornada deve ter ao menos 1 colaborador",
                            "Aviso",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    }
                }
            }
        }

        private void BtnRemoverColaborador_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ColaboradorInfo colaborador)
            {
                // Encontra a jornada que contém este colaborador
                var jornada = Jornadas.FirstOrDefault(j => j.Colaboradores.Contains(colaborador));
                
                if (jornada != null)
                {
                    if (jornada.Colaboradores.Count > 1)
                    {
                        jornada.Colaboradores.Remove(colaborador);
                        OnPropertyChanged(nameof(Jornadas));
                    }
                    else
                    {
                        MessageBox.Show(
                            "Cada jornada deve ter ao menos 1 colaborador",
                            "Aviso",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    }
                }
            }
        }

        private void AtualizarInstrucoes()
        {
            if (txtInstrucoes == null) return;
            
            if (Jornadas.Count == 1)
            {
                txtInstrucoes.Text = 
                    "• Um horário com múltiplos colaboradores\n" +
                    "• Adicione quantos colaboradores precisar\n" +
                    "• O PDF será salvo na área de Trabalho\n" +
                    "• Campos vazios terão espaço para preenchimento manual";
            }
            else
            {
                txtInstrucoes.Text = 
                    "• Múltiplos horários, cada um com seus colaboradores\n" +
                    "• Adicione colaboradores a cada horário individualmente\n" +
                    "• O PDF será salvo na área de Trabalho\n" +
                    "• Campos vazios terão espaço para preenchimento manual";
            }
        }

        private void BtnGerar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GerarModoMultiplosColaboradores();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao gerar PDF:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                if (btnGerar != null)
                    btnGerar.IsEnabled = true;
                if (btnCancelar != null)  
                    btnCancelar.IsEnabled = true;
            }
        }

        private void GerarModoMultiplosColaboradores()
        {
            if (Jornadas == null || Jornadas.Count == 0)
            {
                MessageBox.Show("Nenhuma jornada para exportar", "Aviso", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidarDatasMultiplosColaboradores())
                return;

            btnGerar.IsEnabled = false;
            btnCancelar.IsEnabled = false;

            var jornadasParaExportar = new List<JornadaEditavel>();

            foreach (var jornada in Jornadas)
            {
                foreach (var colab in jornada.Colaboradores)
                {
                    var dataFinal = UsarDataGlobal 
                        ? DataGlobal 
                        : (jornada.UsarDataUnicaLocal 
                            ? jornada.DataUnicaLocal 
                            : colab.DataAlteracao);
                    
                    jornadasParaExportar.Add(new JornadaEditavel
                    {
                        Jornada = jornada.Jornada,
                        Codigo = jornada.Codigo ?? string.Empty,
                        Matricula = colab.Matricula,
                        Nome = colab.Nome,
                        Cargo = colab.Cargo,
                        DataAlteracao = dataFinal
                    });
                }
            }

            Resultado = _exportService.ExportarJornadasIndividuais(
                jornadasParaExportar,
                DataGlobal
            );

            if (Resultado != null && Resultado.Sucesso)
            {
                ExibirSucessoEFechar(Resultado);
            }
            else
            {
                MessageBox.Show(
                    Resultado?.Mensagem ?? "Erro ao gerar PDF",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void ExibirSucessoEFechar(ExportResult resultado)
        {
            var msgResult = MessageBox.Show(
                $"{resultado.Mensagem}\n\n" +
                $"Total de jornadas: {resultado.TotalJornadas}\n" +
                $"Arquivo: {System.IO.Path.GetFileName(resultado.CaminhoArquivo)}\n\n" +
                $"Deseja abrir o arquivo agora?",
                "PDF Gerado",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information
            );

            if (msgResult == MessageBoxResult.Yes && !string.IsNullOrEmpty(resultado.CaminhoArquivo))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = resultado.CaminhoArquivo,
                    UseShellExecute = true
                });
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidarDatasMultiplosColaboradores()
        {
            var hoje = DateTime.Today;
            var datasInvalidas = new List<string>();

            DateTime primeiroDiaValido;
            DateTime ultimoDiaValido;

            if (hoje.Day >= 25)
            {
                primeiroDiaValido = new DateTime(hoje.Year, hoje.Month, 1);
                var proximoMes = hoje.AddMonths(1);
                ultimoDiaValido = new DateTime(proximoMes.Year, proximoMes.Month, 5);
            }
            else
            {
                primeiroDiaValido = new DateTime(hoje.Year, hoje.Month, 1);
                ultimoDiaValido = new DateTime(hoje.Year, hoje.Month, DateTime.DaysInMonth(hoje.Year, hoje.Month));
            }

            if (UsarDataGlobal)
            {
                if (DataGlobal < primeiroDiaValido || DataGlobal > ultimoDiaValido)
                {
                    MessageBox.Show(
                        $"⚠️ Data global inválida!\n\n" +
                        $"Período permitido: {primeiroDiaValido:dd/MM/yyyy} até {ultimoDiaValido:dd/MM/yyyy}\n\n" +
                        GetRegraExplicacao(hoje),
                        "Data Inválida",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return false;
                }
            }
            else
            {
                foreach (var jornada in Jornadas)
                {
                    if (jornada.UsarDataUnicaLocal)
                    {
                        if (jornada.DataUnicaLocal < primeiroDiaValido || jornada.DataUnicaLocal > ultimoDiaValido)
                        {
                            datasInvalidas.Add($"• Jornada {jornada.Jornada}: {jornada.DataUnicaLocal:dd/MM/yyyy}");
                        }
                    }
                    else
                    {
                        foreach (var colab in jornada.Colaboradores)
                        {
                            if (colab.DataAlteracao < primeiroDiaValido || colab.DataAlteracao > ultimoDiaValido)
                            {
                                datasInvalidas.Add($"• {colab.Nome} ({jornada.Jornada}): {colab.DataAlteracao:dd/MM/yyyy}");
                            }
                        }
                    }
                }

                if (datasInvalidas.Any())
                {
                    MessageBox.Show(
                        $"⚠️ Datas inválidas encontradas:\n\n" +
                        string.Join("\n", datasInvalidas) + "\n\n" +
                        $"Período permitido: {primeiroDiaValido:dd/MM/yyyy} até {ultimoDiaValido:dd/MM/yyyy}\n\n" +
                        GetRegraExplicacao(hoje),
                        "Datas Inválidas",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return false;
                }
            }

            return true;
        }

        private string GetRegraExplicacao(DateTime hoje)
        {
            if (hoje.Day >= 25)
                return "📌 Regra: A partir do dia 25, alterações podem ser feitas até o dia 5 do mês seguinte.";
            else
                return "📌 Regra: Alterações permitidas apenas no mês atual.";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
