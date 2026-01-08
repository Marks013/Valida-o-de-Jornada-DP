using System;
using System.Windows;

namespace ValidadorJornada
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Erro crítico inesperado:\n\n{e.ExceptionObject}",
                "Erro Fatal",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Erro na interface:\n\n{e.Exception.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            e.Handled = true;
        }
    }
} 
