using System.Windows;
using ValidadorJornada.ViewModels;

namespace ValidadorJornada.Views
{
    public partial class ValidacaoLoteWindow : Window
    {
        public ValidacaoLoteWindow()
        {
            InitializeComponent();
            DataContext = new ValidacaoLoteViewModel();
        }
    }
}
