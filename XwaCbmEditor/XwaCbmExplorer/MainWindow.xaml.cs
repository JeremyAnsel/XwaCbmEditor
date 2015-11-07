using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using JeremyAnsel.Xwa.Cbm;
using Microsoft.Win32;

namespace XwaCbmExplorer
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        private void RunBusyAction(Action action)
        {
            this.RunBusyAction(dispatcher => action());
        }

        private void RunBusyAction(Action<Action<Action>> action)
        {
            this.BusyIndicator.IsBusy = true;

            Action<Action> dispatcherAction = a =>
            {
                this.Dispatcher.Invoke(a);
            };

            Task.Factory.StartNew(state =>
            {
                var disp = (Action<Action>)state;
                disp(() => { this.BusyIndicator.IsBusy = true; });
                action(disp);
                disp(() => { this.BusyIndicator.IsBusy = false; });
            }, dispatcherAction);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.ExecuteOpen(null, null);
        }

        private void ExecuteOpen(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Select a folder";
            dialog.DefaultExt = ".cbm";
            dialog.CheckFileExists = true;
            dialog.Filter = "CBM files (*.cbm)|*.cbm";

            string directory;

            if (dialog.ShowDialog(this) == true)
            {
                directory = System.IO.Path.GetDirectoryName(dialog.FileName);
                directory = System.IO.Path.GetDirectoryName(directory);
            }
            else
            {
                return;
            }

            this.DataContext = null;

            this.RunBusyAction(disp =>
            {
                try
                {
                    var cbmFiles = System.IO.Directory.EnumerateFiles(directory, "*.CBM", System.IO.SearchOption.AllDirectories)
                        .Select(file => CbmFile.FromFile(file))
                        .ToDictionary(
                            t => string.Concat(System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(t.FileName)), "-", System.IO.Path.GetFileNameWithoutExtension(t.FileName)).ToUpperInvariant(),
                            t => t);

                    disp(() => this.DataContext = cbmFiles);
                }
                catch (Exception ex)
                {
                    disp(() => MessageBox.Show(this, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error));
                }
            });
        }
    }
}
