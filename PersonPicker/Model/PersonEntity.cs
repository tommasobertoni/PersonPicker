using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using Windows.UI.Xaml.Media.Imaging;
using System.IO;

namespace PersonPicker.Model
{
    public class PersonEntity
    {
        public const string DEFAULT_USER_IMAGE_URI = "ms-appx:/Images/default-user.png";

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public string Label { get; set; }

        public string ThumbnailBase64 { get; set; }

        public async Task<BitmapImage> GetBitmapImageAsync()
        {
            if (ThumbnailBase64 == null) return null;

            byte[] thumbnailBytes = Convert.FromBase64String(ThumbnailBase64);
            BitmapImage btmpImage = new BitmapImage();
            await btmpImage.SetSourceAsync(new MemoryStream(thumbnailBytes).AsRandomAccessStream());
            return btmpImage;
        }

        public static BitmapImage GetDefaultBitmapImage()
        {
            return new BitmapImage(new Uri(PersonEntity.DEFAULT_USER_IMAGE_URI, UriKind.RelativeOrAbsolute));
        }
    }
}
