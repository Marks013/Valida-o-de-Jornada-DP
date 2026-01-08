using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using ValidadorJornada.Core.Services;
using ValidadorJornada.ViewModels;

namespace ValidadorJornada.Views
{
    public partial class ExportDialog : Window
    {
        private readonly ExportViewModel _viewModel;
        private readonly ExportService _exportService;
        private readonly ObservableCollection<JornadaEditavel> _jornadas;
        private bool _dataUnicaMode = true;

        public ExportResult? Resultado { get; private set; }

        public ExportDialog(List<string> jornadasSelecionadas, ExportService exportService)
        {
            InitializeComponent();
            
            if (jornadasSelecionadas == null || jornadasSelecionadas.Count == 0)
                throw new ArgumentException("Nenhuma jornada selecionada");
            
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            
            _viewModel = new ExportViewModel(_exportService);
            DataContext = _viewModel;
            
            _jornadas = new ObservableCollection<JornadaEditavel>();
            ProcessarJornadas(jornadasSelecionadas);
            lstJornadas.ItemsSource = _jornadas;
            
            _viewModel.DataReferencia = DateTime.Today;
            foreach (var j in _jornadas)
                j.DataAlteracao = DateTime.Today;
            
            AtualizarInstrucoes();
            AtualizarVisibilidadeDataIndividual();
        }

        private void ProcessarJornadas(List<string> historicoSelecionado)
        {
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

                    _jornadas.Add(new JornadaEditavel
                    {
                        Jornada = horarios,
                        Codigo = codigo ?? string.Empty,
                        Matricula = string.Empty,
                        Nome = string.Empty,
                        Cargo = string.Empty,
                        DataAlteracao = DateTime.Today
                    });

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

        private void ChkDataUnica_Changed(object sender, RoutedEventArgs e)
        {
            _dataUnicaMode = chkDataUnica.IsChecked == true;
            AtualizarVisibilidadeDataIndividual();
            
            if (_dataUnicaMode && _viewModel != null)
            {
                foreach (var j in _jornadas)
                    j.DataAlteracao = _viewModel.DataReferencia;
            }
            
            AtualizarInstrucoes();
        }

        private void AtualizarVisibilidadeDataIndividual()
        {
            if (pnlDataUnica != null)
                pnlDataUnica.Visibility = _dataUnicaMode ? Visibility.Visible : Visibility.Collapsed;
            
            if (lstJornadas == null) return;
            
			// Aguardar o carregamento dos containers
			lstJornadas.Dispatcher.BeginInvoke(() =>
            {
				foreach (var item in lstJornadas.Items)
				{
					var container = lstJornadas.ItemContainerGenerator.ContainerFromItem(item);
					if (container != null)
					{	
				       var panel = FindVisualChild<System.Windows.Controls.StackPanel>(container as DependencyObject, "pnlDataIndividual");
                       if (panel != null)
                           panel.Visibility = _dataUnicaMode ? Visibility.Collapsed : Visibility.Visible;
					}
				}	
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private T? FindVisualChild<T>(DependencyObject? parent, string name) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
                if (child is T typedChild && (child as FrameworkElement)?.Name == name)
                    return typedChild;

                var result = FindVisualChild<T>(child, name);
                if (result != null) return result;
            }

            return null;
        }

        private void AtualizarInstrucoes()
        {
            if (txtInstrucoes == null) return;
            
            txtInstrucoes.Text = _dataUnicaMode
                ? "• Será usada a mesma data para todas as jornadas\n" +
                  "• O PDF será salvo na área de Trabalho\n" +
                  "• Campos vazios terão espaço para preenchimento manual"
                : "• Cada jornada terá data individual\n" +
                  "• O PDF será salvo na área de Trabalho\n" +
                  "• Campos vazios terão espaço para preenchimento manual";
        }

        private void BtnGerar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_jornadas == null || _jornadas.Count == 0)
                {
                    MessageBox.Show("Nenhuma jornada para exportar", "Aviso", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validar datas antes de gerar
                if (!ValidarDatas())
                    return;

                btnGerar.IsEnabled = false;
                btnCancelar.IsEnabled = false;

                if (_dataUnicaMode && _viewModel != null)
                {
                    foreach (var j in _jornadas)
                        j.DataAlteracao = _viewModel.DataReferencia;
                }

                Resultado = _exportService.ExportarJornadasIndividuais(
                    _jornadas.ToList(),
                    _viewModel?.DataReferencia ?? DateTime.Today
                );

                if (Resultado != null && Resultado.Sucesso)
                {
                    var msgResult = MessageBox.Show(
                        $"{Resultado.Mensagem}\n\n" +
                        $"Total de jornadas: {Resultado.TotalJornadas}\n" +
                        $"Arquivo: {System.IO.Path.GetFileName(Resultado.CaminhoArquivo)}\n\n" +
                        $"Deseja abrir o arquivo agora?",
                        "PDF Gerado",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information
                    );

                    if (msgResult == MessageBoxResult.Yes && !string.IsNullOrEmpty(Resultado.CaminhoArquivo))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = Resultado.CaminhoArquivo,
                            UseShellExecute = true
                        });
                    }

                    DialogResult = true;
                    Close();
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

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidarDatas()
        {
            var hoje = DateTime.Today;
            var datasInvalidas = new List<string>();

            // Determinar período válido
            DateTime primeiroDiaValido;
            DateTime ultimoDiaValido;

            if (hoje.Day >= 25)
            {
                // Do dia 25 em diante: libera mês atual até dia 5 do mês seguinte
                primeiroDiaValido = new DateTime(hoje.Year, hoje.Month, 1);
                
                var proximoMes = hoje.AddMonths(1);
                ultimoDiaValido = new DateTime(proximoMes.Year, proximoMes.Month, 5);
            }
            else
            {
                // Antes do dia 25: apenas mês atual
                primeiroDiaValido = new DateTime(hoje.Year, hoje.Month, 1);
                ultimoDiaValido = new DateTime(hoje.Year, hoje.Month, DateTime.DaysInMonth(hoje.Year, hoje.Month));
            }

            // Validar data única ou datas individuais
            if (_dataUnicaMode)
            {
                var data = _viewModel?.DataReferencia ?? DateTime.Today;
                if (data < primeiroDiaValido || data > ultimoDiaValido)
                {
                    MessageBox.Show(
                        $"⚠️ Data inválida!\n\n" +
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
                foreach (var jornada in _jornadas)
                {
                    if (jornada.DataAlteracao < primeiroDiaValido || jornada.DataAlteracao > ultimoDiaValido)
                    {
                        datasInvalidas.Add($"• {jornada.Jornada}: {jornada.DataAlteracao:dd/MM/yyyy}");
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
    }

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
}