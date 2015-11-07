using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JeremyAnsel.Xwa.Cbm;

namespace XwaCbmEditor
{
    public class ImageConverter : BaseConverter, IValueConverter
    {
        public ImageConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var image = value as CbmImage;

            if (image == null)
            {
                return null;
            }

            var data = image.GetImageData();

            if (data == null)
            {
                return null;
            }

            return BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Bgra32, null, data, image.Width * 4);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
