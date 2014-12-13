using PersonPicker.Common;
using PersonPicker.DataAccess;
using PersonPicker.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace PersonPicker
{
    public sealed partial class AddPersonPage : Page
    {
        private PersonEntity _personEntity;

        public AddPersonPage()
        {
            this.InitializeComponent();
            Loaded += AddPersonPage_Loaded;
        }

        void AddPersonPage_Loaded(object sender, RoutedEventArgs e)
        {
            (Application.Current as App).OnFilesOpenPicked += OnFilesOpenPicked;

            _personEntity = new PersonEntity();
            RefreshThumbnail();
        }

        private FileOpenPicker GetFileOpenPicker()
        {
             FileOpenPicker fopicker = new FileOpenPicker();
            fopicker.ViewMode = PickerViewMode.Thumbnail;
            fopicker.FileTypeFilter.Add(".png");
            fopicker.FileTypeFilter.Add(".jpg");
            fopicker.FileTypeFilter.Add(".jpeg");

            return fopicker;
        }

        private void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            GetFileOpenPicker().PickSingleFileAndContinue();
        }

        private async void showDialog()
        {
            MessageDialog dialog = new MessageDialog("Are you sure you wish to exit?", "Exit");
            await dialog.ShowAsync();
        }

        private async void OnFilesOpenPicked(IReadOnlyList<StorageFile> files)
        {
            if (files.Count > 0)
            {
                var imageFile = files.FirstOrDefault();
                if (imageFile != null)
                {
                    _personEntity.ThumbnailBase64 = await GetBase64CodingFromImageStreamAsync(imageFile);
                    RefreshThumbnail();
                }
            }
        }

        private async Task<string> GetBase64CodingFromImageStreamAsync(StorageFile imageFile)
        {
            byte[] fileBytes = null;
            BitmapImage btmpImage = new BitmapImage();
            btmpImage.SetSource(await imageFile.OpenAsync(FileAccessMode.Read));
            using (IRandomAccessStream imageStream = await imageFile.OpenReadAsync())
            {
                uint thumbnail_width = 64;
                uint thumbnail_height = (uint) (btmpImage.PixelHeight * thumbnail_width / btmpImage.PixelWidth);

                IRandomAccessStream compressedStream = await CompressImageStreamAsync(imageStream,thumbnail_width, thumbnail_height, 0.3);
                fileBytes = new byte[compressedStream.Size];
                using (DataReader reader = new DataReader(compressedStream))
                {
                    await reader.LoadAsync((uint)compressedStream.Size);
                    reader.ReadBytes(fileBytes);
                }
            }

            return Convert.ToBase64String(fileBytes);
        }

        private async Task<InMemoryRandomAccessStream> CompressImageStreamAsync(
            IRandomAccessStream imageStream, uint destWidth, uint destHeight, double destQuality)
        {
            // create bitmap decoder from source stream
            BitmapDecoder bmpDecoder = await BitmapDecoder.CreateAsync(imageStream);

            // bitmap transform if you need any
            BitmapTransform bmpTransform = new BitmapTransform
            {
                ScaledHeight = destWidth,
                ScaledWidth = destHeight,
                InterpolationMode = BitmapInterpolationMode.Cubic
            };

            PixelDataProvider pixelData = await bmpDecoder.GetPixelDataAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight, bmpTransform, ExifOrientationMode.RespectExifOrientation, ColorManagementMode.DoNotColorManage);
            InMemoryRandomAccessStream destStream = new InMemoryRandomAccessStream(); // destination stream

            // define new quality for the image
            var propertySet = new BitmapPropertySet();
            var quality = new BitmapTypedValue(destQuality, PropertyType.Single);
            propertySet.Add("ImageQuality", quality);

            // create encoder with desired quality
            BitmapEncoder bmpEncoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, destStream, propertySet);
            bmpEncoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight, destWidth, destHeight, 300, 300, pixelData.DetachPixelData());
            await bmpEncoder.FlushAsync();
            return destStream;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string label = tbLabel.Text;
            if (string.IsNullOrWhiteSpace(label))
            {
                DisplayMessage("Please write a label for the person");
                return;
            }
            _personEntity.Label = label;

            SQLitePickDb _pickdb = new SQLitePickDb();
            int affectedRows = _pickdb.InsertNewPerson(_personEntity);

            Frame.GoBack();
        }

        private async void DisplayMessage(string message)
        {
            await new MessageDialog(message).ShowAsync();
        }

        private async void RefreshThumbnail()
        {
            BitmapImage btmpImage = await _personEntity.GetBitmapImageAsync();
            if (btmpImage == null)
            {
                btmpImage = PersonEntity.GetDefaultBitmapImage();
            }

            imgThumbnail.Source = btmpImage;
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tbLabel.Text = "";
            _personEntity = new PersonEntity();
            RefreshThumbnail();
        }
    }
}
