using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JeremyAnsel.Xwa.Cbm;
using Microsoft.Win32;

namespace XwaCbmEditor
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;
            this.ExecuteNew(null, null);
        }

        private static readonly DependencyProperty CbmFileProperty = DependencyProperty.Register("CbmFile", typeof(CbmFile), typeof(MainWindow));

        public CbmFile CbmFile
        {
            get { return (CbmFile)this.GetValue(CbmFileProperty); }
            set
            {
                this.SetValue(CbmFileProperty, null);
                this.SetValue(CbmFileProperty, value);
            }
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

                try
                {
                    action(disp);
                }
                catch (Exception ex)
                {
                    disp(() => MessageBox.Show(this, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error));
                }

                disp(() => { this.BusyIndicator.IsBusy = false; });
            }, dispatcherAction);
        }

        private void ExecuteNew(object sender, ExecutedRoutedEventArgs e)
        {
            this.RunBusyAction(disp =>
            {
                var cbm = new CbmFile();

                disp(() => this.CbmFile = cbm);
            });
        }

        private void ExecuteOpen(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.DefaultExt = ".cbm";
            dialog.CheckFileExists = true;
            dialog.Filter = "CBM files (*.cbm)|*.cbm";

            string fileName;

            if (dialog.ShowDialog(this) == true)
            {
                fileName = dialog.FileName;
            }
            else
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                try
                {
                    var cbm = CbmFile.FromFile(fileName);
                    disp(() => this.CbmFile = cbm);
                }
                catch (Exception ex)
                {
                    disp(() => MessageBox.Show(this, fileName + "\n" + ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error));
                }
            });
        }

        private void ExecuteSave(object sender, ExecutedRoutedEventArgs e)
        {
            var cbm = this.CbmFile;

            if (cbm == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(cbm.FileName))
            {
                this.ExecuteSaveAs(null, null);
                return;
            }

            this.RunBusyAction(disp =>
            {
                try
                {
                    cbm.Decompress();
                    cbm.Save(cbm.FileName);

                    disp(() => this.CbmFile = cbm);
                }
                catch (Exception ex)
                {
                    disp(() => MessageBox.Show(this, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error));
                }
            });
        }

        private void ExecuteSaveAs(object sender, ExecutedRoutedEventArgs e)
        {
            var cbm = this.CbmFile;

            if (cbm == null)
            {
                return;
            }

            var dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".cbm";
            dialog.Filter = "CBM files (*.cbm)|*.cbm";
            dialog.FileName = System.IO.Path.GetFileName(cbm.FileName);

            string fileName;

            if (dialog.ShowDialog(this) == true)
            {
                fileName = dialog.FileName;
            }
            else
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                try
                {
                    cbm.Decompress();
                    cbm.Save(fileName);
                    disp(() => this.CbmFile = cbm);
                }
                catch (Exception ex)
                {
                    disp(() => MessageBox.Show(this, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error));
                }
            });
        }

        private void NewImage_Click(object sender, RoutedEventArgs e)
        {
            var cbm = this.CbmFile;

            if (cbm == null)
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                var image = new CbmImage();
                cbm.Images.Add(image);

                disp(() => this.CbmFile = this.CbmFile);
                disp(() => this.ImagesList.SelectedIndex = this.ImagesList.Items.Count - 1);
            });
        }

        private void AddImage_Click(object sender, RoutedEventArgs e)
        {
            var cbm = this.CbmFile;

            if (cbm == null)
            {
                return;
            }

            var dialog = new OpenFileDialog();
            dialog.CheckFileExists = true;
            dialog.DefaultExt = ".png";
            dialog.Filter = "Images (*.png, *.bmp, *.jpg)|*.png;*.bmp;*.jpg|PNG files (*.png)|*.png|BMP files (*.bmp)|*.bmp|JPG files (*.jpg)|*.jpg";

            string fileName;

            if (dialog.ShowDialog(this) == true)
            {
                fileName = dialog.FileName;
            }
            else
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                try
                {
                    var image = CbmImage.FromFile(fileName);

                    cbm.Images.Add(image);

                    disp(() => this.CbmFile = this.CbmFile);
                    disp(() => this.ImagesList.SelectedIndex = this.ImagesList.Items.Count - 1);
                }
                catch (Exception ex)
                {
                    disp(() => MessageBox.Show(this, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error));
                }
            });
        }

        private void ReplaceImage_Click(object sender, RoutedEventArgs e)
        {
            var image = this.ImagesList.SelectedItem as CbmImage;

            if (image == null)
            {
                return;
            }

            var dialog = new OpenFileDialog();
            dialog.CheckFileExists = true;
            dialog.DefaultExt = ".png";
            dialog.Filter = "Images (*.png, *.bmp, *.jpg)|*.png;*.bmp;*.jpg|PNG files (*.png)|*.png|BMP files (*.bmp)|*.bmp|JPG files (*.jpg)|*.jpg";

            string fileName;

            if (dialog.ShowDialog(this) == true)
            {
                fileName = dialog.FileName;
            }
            else
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                try
                {
                    image.ReplaceWithFile(fileName);

                    disp(() => this.CbmFile = this.CbmFile);
                }
                catch (Exception ex)
                {
                    disp(() => MessageBox.Show(this, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error));
                }
            });
        }

        private void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            var cbm = this.CbmFile;

            if (cbm == null)
            {
                return;
            }

            int index = this.ImagesList.SelectedIndex;

            if (index == -1)
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                cbm.Images.RemoveAt(index);

                disp(() => this.ImagesList.SelectedIndex = -1);
                disp(() => this.CbmFile = this.CbmFile);
            });
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            var cbm = this.CbmFile;

            if (cbm == null)
            {
                return;
            }

            var image = this.ImagesList.SelectedItem as CbmImage;

            if (image == null)
            {
                return;
            }

            var dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".png";
            dialog.Filter = "Images (*.png, *.bmp)|*.png;*.bmp|PNG files (*.png)|*.png|BMP files (*.bmp)|*.bmp";
            dialog.FileName = System.IO.Path.GetFileNameWithoutExtension(cbm.FileName) + "-" + cbm.Images.IndexOf(image);

            string fileName;

            if (dialog.ShowDialog(this) == true)
            {
                fileName = dialog.FileName;
            }
            else
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                image.Save(fileName);
            });
        }

        private void SetImageColorKey_Click(object sender, RoutedEventArgs e)
        {
            var image = this.ImagesList.SelectedItem as CbmImage;

            if (image == null)
            {
                return;
            }

            Color colorKey = this.DatImageColorKey.SelectedColor;

            this.RunBusyAction(disp =>
            {
                image.MakeColorTransparent(colorKey.R, colorKey.G, colorKey.B);
                disp(() => this.CbmFile = this.CbmFile);
            });
        }

        private void SetImageColorKeyRange_Click(object sender, RoutedEventArgs e)
        {
            var image = this.ImagesList.SelectedItem as CbmImage;

            if (image == null)
            {
                return;
            }

            Color colorKey0 = this.DatImageColorKey0.SelectedColor;
            Color colorKey1 = this.DatImageColorKey1.SelectedColor;

            this.RunBusyAction(disp =>
            {
                image.MakeColorTransparent(colorKey0.R, colorKey0.G, colorKey0.B, colorKey1.R, colorKey1.G, colorKey1.B);
                disp(() => this.CbmFile = this.CbmFile);
            });
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var image = (Image)sender;

            if (image == null)
            {
                return;
            }

            var source = image.Source as BitmapSource;

            if (source == null)
            {
                return;
            }

            var position = e.GetPosition(image);

            int x = Math.Max(Math.Min((int)(position.X * source.PixelWidth / image.ActualWidth), source.PixelWidth - 1), 0);
            int y = Math.Max(Math.Min((int)(position.Y * source.PixelHeight / image.ActualHeight), source.PixelHeight - 1), 0);

            byte[] pixel = new byte[4];

            source.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, source.PixelWidth * 4, 0);

            var color = Color.FromRgb(pixel[2], pixel[1], pixel[0]);

            this.DatImageColorKey.SelectedColor = color;
            this.DatImageColorKey0.SelectedColor = color;
            this.DatImageColorKey1.SelectedColor = color;
        }
    }
}
