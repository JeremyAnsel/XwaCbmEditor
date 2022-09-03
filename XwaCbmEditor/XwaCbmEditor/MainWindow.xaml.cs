using System;
using System.Linq;
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

                    if (cbm.Images.Count != 0)
                    {
                        var image = cbm.Images[0];
                        var palette = image.GetPalette32();
                        uint color = palette?[0] ?? 0;
                        byte b = (byte)((color >> 16) & 0xffU);
                        byte g = (byte)((color >> 8) & 0xffU);
                        byte r = (byte)(color & 0xffU);

                        disp(() =>
                        {
                            this.ImageBackgroundColor.SelectedColor = Color.FromRgb(r, g, b);
                        });
                    }
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

            Color color = this.ImageBackgroundColor.SelectedColor ?? Colors.Black;

            this.RunBusyAction(disp =>
            {
                try
                {
                    SetCbmBackgroundColor(cbm, color);
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

            Color color = this.ImageBackgroundColor.SelectedColor ?? Colors.Black;

            this.RunBusyAction(disp =>
            {
                try
                {
                    SetCbmBackgroundColor(cbm, color);
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

        private void SetCbmBackgroundColor(CbmFile cbm, Color color)
        {
            uint backgroundColor = (uint)((color.B << 16) | (color.G << 8) | color.R);

            foreach (var image in cbm.Images)
            {
                var palette = image.GetPalette32();

                if (palette == null)
                {
                    continue;
                }

                palette[0] = backgroundColor;
                image.SetPalette(palette);
            }
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
            dialog.Multiselect = true;

            if (dialog.ShowDialog(this) == false)
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                try
                {
                    for (int i = 0; i < dialog.FileNames.Length; i++)
                    {
                        var image = CbmImage.FromFile(dialog.FileNames[i]);

                        cbm.Images.Add(image);
                    }

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

            var images = this.ImagesList.SelectedItems;

            if (images.Count == 0)
            {
                return;
            }

            CbmImage image = images[0] as CbmImage;
            var dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".png";
            dialog.Filter = "Images (*.png, *.bmp)|*.png;*.bmp|PNG files (*.png)|*.png|BMP files (*.bmp)|*.bmp";
            dialog.FileName = System.IO.Path.GetFileNameWithoutExtension(cbm.FileName) + "-" + cbm.Images.IndexOf(image);

            string fileName, directory, extension;

            if (dialog.ShowDialog(this) == true)
            {
                fileName = dialog.FileName;
                directory = fileName.Substring(0, fileName.LastIndexOf('\\'));
                extension = fileName.Substring(fileName.LastIndexOf('.'));
            }
            else
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(cbm.FileName);

                foreach (CbmImage img in images)
                {
                    img.Save(directory + '\\' + name + "-" + cbm.Images.IndexOf(img) + extension);
                }
            });
        }

        private void SetImageColorKey_Click(object sender, RoutedEventArgs e)
        {
            var image = this.ImagesList.SelectedItem as CbmImage;

            if (image == null)
            {
                return;
            }

            if (this.DatImageColorKey.SelectedColor is null)
            {
                return;
            }

            Color colorKey = this.DatImageColorKey.SelectedColor.Value;

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

            if (this.DatImageColorKey0.SelectedColor is null || this.DatImageColorKey1.SelectedColor is null)
            {
                return;
            }

            Color colorKey0 = this.DatImageColorKey0.SelectedColor.Value;
            Color colorKey1 = this.DatImageColorKey1.SelectedColor.Value;

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
