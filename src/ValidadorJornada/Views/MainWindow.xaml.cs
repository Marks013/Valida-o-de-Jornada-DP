using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ValidadorJornada.Core.Services;
using ValidadorJornada.Core.Helpers;
using ValidadorJornada.ViewModels;

namespace ValidadorJornada.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            try
            {
                var configService = new ConfigService();
                var jornadaConfig = configService.LoadConfig();
                
                // Inicializa serviços com configurações
                var historicoService = new HistoricoService(jornadaConfig.CacheHistoricoMinutos);
                var codigoService = new CodigoService(jornadaConfig.SkipHeadersOnImport);
                var validator = new JornadaValidator(jornadaConfig, codigoService);
                
                DataContext = new MainViewModel(validator, historicoService, codigoService);

                // Adiciona validação de entrada em tempo real
                txtHorarios.PreviewTextInput += TxtHorarios_PreviewTextInput;
                
                // Verifica se o campo existe antes de adicionar handler
                var txtDomingoFeriado = this.FindName("txtHorariosDomingoFeriado") as TextBox;
                if (txtDomingoFeriado != null)
                {
                    txtDomingoFeriado.PreviewTextInput += TxtHorarios_PreviewTextInput;
                    DataObject.AddPastingHandler(txtDomingoFeriado, OnPaste);
                }

                // Previne paste de caracteres inválidos
                DataObject.AddPastingHandler(txtHorarios, OnPaste);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao inicializar aplicação:\n\n{ex.Message}",
                    "Erro de Inicialização",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Previne digitação de caracteres inválidos
        /// </summary>
        private void TxtHorarios_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Valida cada caractere digitado
            foreach (char c in e.Text)
            {
                if (!InputValidator.IsCaractereValido(c))
                {
                    e.Handled = true;
                    
                    // Feedback visual sutil (opcional)
                    if (sender is TextBox textBox)
                    {
                        FlashInvalidInput(textBox);
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Previne colar texto com caracteres inválidos
        /// </summary>
        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var texto = (string)e.DataObject.GetData(typeof(string));
                
                if (!InputValidator.ContemApenasCaracteresValidos(texto))
                {
                    // Remove caracteres inválidos
                    var textoLimpo = InputValidator.RemoverCaracteresInvalidos(texto);
                    
                    if (string.IsNullOrEmpty(textoLimpo))
                    {
                        e.CancelCommand();
                        return;
                    }

                    // Cria novo DataObject com texto limpo
                    var dataObject = new DataObject();
                    dataObject.SetData(typeof(string), textoLimpo);
                    e.DataObject = dataObject;
                }
            }
        }

        /// <summary>
        /// Feedback visual quando caractere inválido é digitado
        /// </summary>
        private async void FlashInvalidInput(TextBox textBox)
        {
            var originalBorder = textBox.BorderBrush;
            textBox.BorderBrush = System.Windows.Media.Brushes.Red;
            
            await System.Threading.Tasks.Task.Delay(200);
            
            textBox.BorderBrush = originalBorder;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // Limpa recursos
            if (DataContext is MainViewModel viewModel)
            {
                // Dispõe serviços se implementarem IDisposable
                (viewModel as IDisposable)?.Dispose();
            }
        }
    }
}