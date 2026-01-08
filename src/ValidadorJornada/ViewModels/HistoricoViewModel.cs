using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ValidadorJornada.Core.Services;

namespace ValidadorJornada.ViewModels
{
    public class HistoricoViewModel : INotifyPropertyChanged
    {
        private readonly HistoricoService _historico;
        private ObservableCollection<string> _itens = new();

        public HistoricoViewModel(HistoricoService historico)
        {
            _historico = historico;
            CarregarHistorico();
        }

        public ObservableCollection<string> Itens
        {
            get => _itens;
            set
            {
                _itens = value;
                OnPropertyChanged();
            }
        }

        private void CarregarHistorico()
        {
            var lista = _historico.ObterTodos();
            Itens.Clear();
            foreach (var item in lista)
                Itens.Add(item);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}