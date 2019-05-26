using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using DocReader.Core.Models;
using DocReader.Core.Services;
using DocReader.Helpers;
using DocReader.Services;

using Microsoft.Toolkit.Uwp.UI.Animations;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace DocReader.Views
{
    public sealed partial class ImageGalleryDetailPage : Page, INotifyPropertyChanged
    {
        private object _selectedImage;
        private ObservableCollection<ImageTaken> _source;

        public object SelectedImage
        {
            get => _selectedImage;
            set
            {
                Set(ref _selectedImage, value);
                if (_selectedImage != null)
                {
                    ImagesNavigationHelper.UpdateImageId(ImageGalleryPage.ImageGallerySelectedIdKey, ((ImageTaken)SelectedImage).ID);
                }
            }
        }

        public ObservableCollection<ImageTaken> Source
        {
            get => _source;
            set => Set(ref _source, value);
        }

        public ImageGalleryDetailPage()
        {
            // TODO WTS: Replace this with your actual data
            ObservableCollection<ImageTaken> ocIT = new ObservableCollection<ImageTaken>();
            foreach (ImageTaken it in PhotoDataService.GetGalleryImageData())
            {
                
                    ocIT.Add(it);
                
            }
            Source = ocIT;
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var selectedImageID = e.Parameter as string;
            if (!string.IsNullOrEmpty(selectedImageID) && e.NavigationMode == NavigationMode.New)
            {
                SelectedImage = Source.FirstOrDefault(i => i.ID == selectedImageID);
                
            }
            else
            {
                selectedImageID = ImagesNavigationHelper.GetImageId(ImageGalleryPage.ImageGallerySelectedIdKey);
                if (!string.IsNullOrEmpty(selectedImageID))
                {
                    SelectedImage = Source.FirstOrDefault(i => i.ID == selectedImageID);
                }
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            if (e.NavigationMode == NavigationMode.Back)
            {
                NavigationService.Frame.SetListDataItemForNextConnectedAnimation(SelectedImage);
                ImagesNavigationHelper.RemoveImageId(ImageGalleryPage.ImageGallerySelectedIdKey);
            }
        }

        private void OnPageKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
                e.Handled = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void FlipView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (SelectedImage != null)
            {
                ImageTaken it = (ImageTaken)SelectedImage;
                NavigationService.Navigate(typeof(TabbedDetailsPage), it.Source);
            }
        }

    }
}
