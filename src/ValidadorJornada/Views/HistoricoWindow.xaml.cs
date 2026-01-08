using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using ValidadorJornada.Core.Services;

namespace ValidadorJornada.Views
{
    public partial class HistoricoWindow : Window
    {
        private readonly HistoricoService _historico;
        private readonly ExportService _exportService;
        private ObservableCollection<HistoricoItemSelectable> _items;

        public HistoricoWindow(HistoricoService historico)
        {
            InitializeComponent();
            _historico = historico;
            _exportService = new ExportService();
            _items = new ObservableCollection<HistoricoItemSelectable>();
            CarregarHistorico();
        }

        private void CarregarHistorico()
        {
            var historico = _historico.ObterTodos();
            _items.Clear();
            
            foreach (var item in historico)
            {
                var selectable = new HistoricoItemSelectable 
                { 
                    Texto = item,
                    IsSelected = false
                };
                selectable.PropertyChanged += Item_PropertyChanged;
                _items.Add(selectable);
            }

            lstHistorico.ItemsSource = _items;
            AtualizarStatus();
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HistoricoItemSelectable.IsSelected))
            {
                AtualizarStatus();
            }
        }

        private void ChkSelecionarTodos_Changed(object sender, RoutedEventArgs e)
        {
            var isChecked = chkSelecionarTodos.IsChecked == true;
            
            foreach (var item in _items)
            {
                item.IsSelected = isChecked;
            }
            
            AtualizarStatus();
        }

        private void ChkItem_Changed(object sender, RoutedEventArgs e)
        {
            AtualizarStatus();
            
            var totalSelecionados = _items.Count(i => i.IsSelected);
            chkSelecionarTodos.IsChecked = totalSelecionados == _items.Count && _items.Count > 0
                ? true
                : totalSelecionados > 0
                    ? (bool?)null
                    : false;
        }

        private void AtualizarStatus()
        {
            var selecionados = _items.Count(i => i.IsSelected);
            
            if (selecionados > 0)
            {
                txtStatus.Text = $"✓ {selecionados} jornada(s) selecionada(s)";
                btnExportar.IsEnabled = true;
            }
            else
            {
                txtStatus.Text = string.Empty;
                btnExportar.IsEnabled = false;
            }
        }

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            var jornadasSelecionadas = _items
                .Where(i => i.IsSelected)
                .Select(i => i.Texto)
                .ToList();

            if (jornadasSelecionadas.Count == 0)
            {
                MessageBox.Show(
                    "Selecione ao menos uma jornada para exportar.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            var dialog = new ExportDialog(jornadasSelecionadas, _exportService)
            {
                Owner = this
            };

            dialog.ShowDialog();
        }

        private void BtnLimpar_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show(
                "Tem certeza que deseja excluir todo o histórico?\n\nEsta ação não pode ser desfeita.",
                "Confirmar exclusão",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (resultado == MessageBoxResult.Yes)
            {
                _historico.LimparTudo();
                CarregarHistorico();
                MessageBox.Show(
                    "Histórico limpo com sucesso!",
                    "Concluído",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }
    }

    public class HistoricoItemSelectable : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Texto { get; set; } = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
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
