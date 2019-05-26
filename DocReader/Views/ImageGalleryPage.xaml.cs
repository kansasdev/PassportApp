using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using DocReader.Core.Models;
using DocReader.Core.Services;
using DocReader.Helpers;
using DocReader.Services;

using Microsoft.Toolkit.Uwp.UI.Animations;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Microsoft.Toolkit.Uwp.Helpers;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DocReader.Views
{
    public sealed partial class ImageGalleryPage : Page, INotifyPropertyChanged
    {
        public const string ImageGallerySelectedIdKey = "ImageGallerySelectedIdKey";

        private ObservableCollection<ImageTaken> _source;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            UpdateImageSource();
            
            base.OnNavigatedFrom(e);
        }

        public ObservableCollection<ImageTaken> Source
        {
            get => _source;
            set => Set(ref _source, value);
        }

        public ImageGalleryPage()
        {
            InitializeComponent();
            UpdateImageSource();
        }

        private void UpdateImageSource()
        {
            if (Source == null)
            {
                Source = new ObservableCollection<ImageTaken>();
            }
            else
            {
                ObservableCollection<ImageTaken> ocIT = PhotoDataService.GetGalleryImageData();
                foreach (ImageTaken it in ocIT)
                {
                    if (Source.Where(q => q.ID == it.ID).FirstOrDefault() == null)
                    {
                        Source.Add(it);
                    }

                }
                int index = 0;
                List<int> lstToDelete = new List<int>();
                foreach(ImageTaken it in Source)
                {
                    if(ocIT.Where(q=>q.ID == it.ID).FirstOrDefault() == null)
                    {
                        lstToDelete.Add(index);
                    }
                    index++;
                }

                foreach(int d in lstToDelete)
                {
                    Source.RemoveAt(d);
                }
            }
           
            
        }

        private void ImagesGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selected = e.ClickedItem as ImageTaken;
            ImagesNavigationHelper.AddImageId(ImageGallerySelectedIdKey, selected.ID);
            NavigationService.Frame.SetListDataItemForNextConnectedAnimation(selected);
            NavigationService.Navigate<ImageGalleryDetailPage>(selected.ID);
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
              

        private void ThumbnailImage_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            ImageTaken it = (e.OriginalSource as Image).DataContext as ImageTaken;
            FrameworkElement senderElement = sender as FrameworkElement;
            // If you need the clicked element:
            // Item whichOne = senderElement.DataContext as Item;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            flyoutBase.ShowAt(senderElement);
        }

        private void ThumbnailImage_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            // If you need the clicked element:
            // Item whichOne = senderElement.DataContext as Item;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            flyoutBase.ShowAt(senderElement);
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ImageTaken it = (e.OriginalSource as MenuFlyoutItem).DataContext as ImageTaken;
            if (it != null)
            {
                StorageFile sf = await StorageFile.GetFileFromPathAsync(it.Source);
                await sf.DeleteAsync(StorageDeleteOption.PermanentDelete);
                UpdateImageSource();
                await Task.CompletedTask;
            }
        }
    }
}
