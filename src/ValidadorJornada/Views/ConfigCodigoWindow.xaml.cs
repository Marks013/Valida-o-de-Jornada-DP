using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using ValidadorJornada.Core.Services;
using ValidadorJornada.Core.Helpers;

namespace ValidadorJornada.Views
{
    public partial class ConfigCodigoWindow : Window
    {
        private readonly CodigoService _codigoService;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private CancellationTokenSource? _cancellationToken;
        private bool _isProcessing = false;

        public ConfigCodigoWindow(CodigoService codigoService)
        {
            InitializeComponent();
            _codigoService = codigoService ?? throw new ArgumentNullException(nameof(codigoService));
            CarregarInformacoes();
        }

        private void CarregarInformacoes()
        {
            try
            {
                var dataAtualizacao = _codigoService.ObterDataAtualizacao();
                var totalCodigos = _codigoService.ObterTotalCodigos();
                var isAtivo = _codigoService.IsAtivo();

                if (isAtivo)
                {
                    rbAtivado.IsChecked = true;
                }
                else
                {
                    rbDesativado.IsChecked = true;
                }

                if (dataAtualizacao.HasValue)
                {
                    txtDataAtualizacao.Text = $"Última atualização: {dataAtualizacao.Value:dd/MM/yyyy HH:mm}";
                }
                else
                {
                    txtDataAtualizacao.Text = "Última atualização: Nunca";
                }

                txtTotalCodigos.Text = $"Total de códigos: {totalCodigos}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao carregar informações:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void BtnSelecionar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Arquivos Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Arquivos CSV (*.csv)|*.csv|Todos os arquivos (*.*)|*.*",
                    Title = "Importar Códigos",
                    FilterIndex = 1
                };

                if (dialog.ShowDialog() == true)
                {
                    txtArquivo.Text = dialog.FileName;
                    btnDiagnostico.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao selecionar arquivo:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void BtnDiagnostico_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtArquivo.Text))
            {
                MessageBox.Show("Selecione um arquivo primeiro.", "Aviso", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var resultado = DiagnosticarArquivo(txtArquivo.Text);
                
                MessageBox.Show(resultado, "Diagnóstico de Importação", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao diagnosticar arquivo:\n{ex.Message}", "Erro", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string DiagnosticarArquivo(string caminhoArquivo)
        {
            var sb = new StringBuilder();
            var codigosUnicos = new HashSet<string>();
            var horariosMap = new Dictionary<string, List<string>>();
            int linhasVazias = 0;
            
            var dados = ExcelHelper.LerArquivo(caminhoArquivo, true);
            
            foreach (var (codigo, horarios) in dados)
            {
                if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(horarios))
                {
                    linhasVazias++;
                    continue;
                }
                
                codigosUnicos.Add(codigo.Trim());
                var horarioNormalizado = HorarioNormalizer.Normalizar(horarios);
                
                if (!horariosMap.ContainsKey(horarioNormalizado))
                    horariosMap[horarioNormalizado] = new List<string>();
                
                horariosMap[horarioNormalizado].Add(codigo.Trim());
            }
            
            var duplicados = horariosMap.Where(x => x.Value.Count > 1).ToList();
            int codigosPerdidos = codigosUnicos.Count - horariosMap.Count;
            
            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine("   DIAGNÓSTICO DE IMPORTAÇÃO");
            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine($"Total de linhas: {dados.Count}");
            sb.AppendLine($"Linhas vazias/inválidas: {linhasVazias}");
            sb.AppendLine($"Linhas processadas: {dados.Count - linhasVazias}");
            sb.AppendLine($"Códigos únicos: {codigosUnicos.Count}");
            sb.AppendLine($"Horários únicos: {horariosMap.Count}");
            sb.AppendLine($"Códigos que serão importados: {horariosMap.Count}");
            
            if (codigosPerdidos > 0)
            {
                sb.AppendLine($"\n⚠️  CÓDIGOS PERDIDOS: {codigosPerdidos}");
                sb.AppendLine("───────────────────────────────────────────");
                sb.AppendLine("Motivo: Horários duplicados (último sobrescreve)\n");
            }
            
            if (duplicados.Any())
            {
                sb.AppendLine("HORÁRIOS DUPLICADOS DETECTADOS:\n");
                foreach (var dup in duplicados)
                {
                    sb.AppendLine($"Horário: {dup.Key}");
                    sb.AppendLine($"  Códigos conflitantes ({dup.Value.Count}):");
                    foreach (var cod in dup.Value)
                        sb.AppendLine($"    • {cod}");
                    sb.AppendLine();
                }
                sb.AppendLine($"⚠️  Apenas o último código de cada grupo será importado!");
            }
            else
            {
                sb.AppendLine("\n✅ Nenhum problema detectado!");
            }
            
            sb.AppendLine("═══════════════════════════════════════════");
            
            return sb.ToString();
        }

        private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;

            bool shouldClose = false;

            await _semaphore.WaitAsync();
            
            try
            {
                _isProcessing = true;
                btnSalvar.IsEnabled = false;
                btnSelecionar.IsEnabled = false;
                _cancellationToken = new CancellationTokenSource();

                if (rbDesativado.IsChecked == true)
                {
                    var resultado = MessageBox.Show(
                        "Tem certeza que deseja desativar os códigos?\n\nTodos os códigos cadastrados serão removidos.",
                        "Confirmar",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );

                    if (resultado == MessageBoxResult.Yes)
                    {
                        await Task.Run(() => _codigoService.SetAtivo(false));
                        
                        MessageBox.Show(
                            "Códigos desativados com sucesso!",
                            "Sucesso",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );

                        shouldClose = true;
                    }
                    return;
                }

                if (rbAtivado.IsChecked == true)
                {
                    if (string.IsNullOrWhiteSpace(txtArquivo.Text))
                    {
                        MessageBox.Show(
                            "Selecione um arquivo Excel (.xlsx, .xls) ou CSV para importar.",
                            "Aviso",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    shouldClose = await ImportarArquivoAsync(txtArquivo.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao salvar configurações:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                _isProcessing = false;
                btnSalvar.IsEnabled = true;
                btnSelecionar.IsEnabled = true;
                
                if (_cancellationToken != null)
                {
                    _cancellationToken.Dispose();
                    _cancellationToken = null;
                }
                
                _semaphore.Release();
            }
            
            if (shouldClose)
            {
                Close();
            }
        }

        private async Task<bool> ImportarArquivoAsync(string caminhoArquivo)
        {
            pnlProgresso.Visibility = Visibility.Visible;
            progressBar.Value = 0;
            txtProgresso.Text = "Importando arquivo...";
            txtDetalhesProgresso.Text = "Iniciando...";

            try
            {
                var progress = new Progress<(int current, int total)>(p =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (p.total > 0)
                        {
                            progressBar.Maximum = p.total;
                            progressBar.Value = p.current;
                            txtDetalhesProgresso.Text = $"{p.current} de {p.total} linhas processadas";
                            
                            var percentual = (p.current * 100) / p.total;
                            txtProgresso.Text = $"Importando... {percentual}%";
                        }
                    });
                });

                var result = await Task.Run(() =>
                {
                    var importResult = _codigoService.ImportarArquivo(caminhoArquivo);
                    
                    if (importResult.TotalLinhas > 0)
                    {
                        for (int i = 0; i <= importResult.TotalLinhas; i += Math.Max(1, importResult.TotalLinhas / 20))
                        {
                            if (_cancellationToken?.IsCancellationRequested == true)
                                break;
                                
                            ((IProgress<(int, int)>)progress).Report((i, importResult.TotalLinhas));
                            Thread.Sleep(50);
                        }
                    }
                    
                    return importResult;
                }, _cancellationToken?.Token ?? CancellationToken.None);

                progressBar.Value = progressBar.Maximum;
                txtProgresso.Text = "Importação concluída!";
                txtDetalhesProgresso.Text = $"{result.TotalImportado} códigos importados com sucesso";

                await Task.Delay(1500);

                MessageBox.Show(
                    $"Códigos importados com sucesso!\n\n" +
                    $"Total de linhas: {result.TotalLinhas}\n" +
                    $"Linhas processadas: {result.LinhasProcessadas}\n" +
                    $"Códigos importados: {result.TotalImportado}",
                    "Sucesso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                return true;
            }
            catch (OperationCanceledException)
            {
                txtProgresso.Text = "Importação cancelada";
                txtDetalhesProgresso.Text = "";
                return false;
            }
            catch (Exception ex)
            {
                txtProgresso.Text = "Erro na importação";
                txtDetalhesProgresso.Text = ex.Message;
                
                MessageBox.Show(
                    $"Erro ao importar arquivo:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                
                return false;
            }
            finally
            {
                await Task.Delay(1000);
                pnlProgresso.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing)
            {
                _cancellationToken?.Cancel();
            }
            
            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_isProcessing)
            {
                var result = MessageBox.Show(
                    "Importação em andamento. Deseja cancelar?",
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                
                _cancellationToken?.Cancel();
            }
            
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            _cancellationToken?.Dispose();
            _semaphore?.Dispose();
        }
    }
}