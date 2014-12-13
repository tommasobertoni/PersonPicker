using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace PersonPicker.Model
{
    public class DisplayablePerson
    {
        public int Id { get; set; }

        public string Label { get; set; }

        public BitmapImage Thumbnail { get; set; }
    }
}
