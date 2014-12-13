using PersonPicker.DataAccess;
using PersonPicker.Model;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace PersonPicker
{
    public sealed partial class MainPage : Page
    {
        private const string _QUESTION_MARK = "ms-appx:/Images/question-mark.png";

        private SQLitePickDb _pickDb;

        private ObservableCollection<PickEntity> _picks;
        private ObservableCollection<DisplayablePerson> _displayablePeople;
        private Windows.Storage.ApplicationDataContainer _storage;

        public MainPage()
        {
            this.InitializeComponent();

            _pickDb = new SQLitePickDb(); //OnNavigateTo is called before pageLoaded, so I need to initialize here the database
            Loaded += MainPage_Loaded;
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ReinitPicks();
            animPickedOpacity.Duration = new Duration(TimeSpan.FromMilliseconds(280));
            animQuestionMarkOpacity.Duration = new Duration(TimeSpan.FromMilliseconds(80));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ReinitPeople();
        }

        private void btnPick_Click(object sender, RoutedEventArgs e)
        {
            btnPick.IsEnabled = false;
            pickPersonAnimations();
        }

        private async void pickPersonAnimations()
        {
            if (pickedContainer.Opacity == 1)
            {
                initFadeOutPicked();
                await animPicked.BeginAsyncAnimation();

                initFadeInQuestionMark();
                await animQuestionMark.BeginAsyncAnimation();
            }

            PickEntity pick = pickPerson();

            await animQuestionMarkRotation.BeginAsyncAnimation();

            initFadeInPicked();
            await animPicked.BeginAsyncAnimation();

            _picks.Insert(0, pick);
            btnPick.IsEnabled = true;
        }

        private PickEntity pickPerson()
        {
            Random rand = new Random();
            DisplayablePerson pickedPerson = _displayablePeople[rand.Next(_displayablePeople.Count)];

            var pickEntity = new PickEntity
            {
                PersonId = pickedPerson.Id,
                PickedLabel = pickedPerson.Label,
                PickTime = DateTime.Now.ToUniversalTime()
            };
            _pickDb.InsertNewPick(pickEntity);

            imgPickedPerson.Source = pickedPerson.Thumbnail;
            tbPickedPerson.Text = pickedPerson.Label;

            return pickEntity;
        }

        private void initFadeInQuestionMark()
        {
            animQuestionMarkOpacity.From = 0;
            animQuestionMarkOpacity.To = 1;
        }

        private void no_used_initfadeOutQuestionMark()
        {
            animQuestionMarkOpacity.From = 1;
            animQuestionMarkOpacity.To = 0;
        }

        private void initFadeInPicked()
        {
            animPickedOpacity.From = 0;
            animPickedOpacity.To = 1;
        }

        private void initFadeOutPicked()
        {
            animPickedOpacity.From = 1;
            animPickedOpacity.To = 0;
        }

        public async void ReinitPeople()
        {
            _displayablePeople = new ObservableCollection<DisplayablePerson>();
            var people = _pickDb.GetAllPeople();
            foreach (var personEntity in people)
            {
                var displayablePerson = new DisplayablePerson
                {
                    Id = personEntity.Id,
                    Label = personEntity.Label,
                    Thumbnail = await personEntity.GetBitmapImageAsync() ?? PersonEntity.GetDefaultBitmapImage()
                };

                _displayablePeople.Add(displayablePerson);
            }

            listPeople.ItemsSource = _displayablePeople;
            CheckPickButtonEnableStatus();
        }

        public void ReinitPicks()
        {
            _picks = new ObservableCollection<PickEntity>(_pickDb.GetAllPicks());

            listLastPicked.ItemsSource = _picks;
        }

        private void btnAddPerson_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AddPersonPage));
        }

        private void pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (pivot.SelectedIndex)
            {
                case 0:
                    btnAddPerson.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    btnPick.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    btnDelPicks.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    break;

                case 1:
                    btnPick.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    btnAddPerson.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    btnDelPicks.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;
            }
        }

        private void btnDelPicks_Click(object sender, RoutedEventArgs e)
        {
            _pickDb.DeleteAllPicks();
            ReinitPicks();
        }

        private void listPeopleItem_Holding(object sender, HoldingRoutedEventArgs args)
        {
            // this event is fired multiple times. We do not want to show the menu twice
            if (args.HoldingState != HoldingState.Started) return;

            FrameworkElement element = sender as FrameworkElement;
            if (element == null) return;

            // If the menu was attached properly, we just need to call this handy method
            FlyoutBase.ShowAttachedFlyout(element);
        }

        private void menuFlyoutListPeopleItem_Click(object sender, RoutedEventArgs args)
        {
            FrameworkElement element = (FrameworkElement)args.OriginalSource;
            if (element.DataContext != null)
            {
                var displayablePerson = element.DataContext as DisplayablePerson;
                DeletePerson(displayablePerson);
            }
        }

        private void DeletePerson(DisplayablePerson displayablePerson)
        {
            _pickDb.DeletePersonById(displayablePerson.Id);
            _displayablePeople.Remove(displayablePerson);
            CheckPickButtonEnableStatus();
        }

        private void CheckPickButtonEnableStatus()
        {
            btnPick.IsEnabled = _displayablePeople.Count > 0;
        }
    }

    static class AnimUtils
    {
        public static Task BeginAsyncAnimation(this Storyboard storyboard)
        {
            var taskSource = new TaskCompletionSource<object>();

            EventHandler<object> completed = null;

            completed += (s, e) =>
            {
                storyboard.Completed -= completed;

                taskSource.SetResult(null);
            };

            storyboard.Completed += completed;

            storyboard.Begin();

            return taskSource.Task;
        }
    }
}
