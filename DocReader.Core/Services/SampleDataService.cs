using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using DocReader.Core.Models;
using Windows.Storage;

namespace DocReader.Core.Services
{
    // This class holds sample data used by some generated pages to show how they can be used.
    // TODO WTS: Delete this file once your app is using real data.
    public static class PhotoDataService
    {
       
        private static string _localResourcesPath;

        private static ObservableCollection<ImageTaken> _gallerySampleData;

        public static void Initialize(string localResourcesPath)
        {
            _localResourcesPath = localResourcesPath;
        }

        // TODO WTS: Remove this once your image gallery page is displaying real data.
        public static ObservableCollection<ImageTaken> GetGalleryImageData()
        {


            _gallerySampleData = new ObservableCollection<ImageTaken>();


            var files = ApplicationData.Current.LocalFolder.GetFilesAsync();
            START:
            Thread.Sleep(100);
            if(files.Status == Windows.Foundation.AsyncStatus.Started)
            {
                goto START;
            }
            
            
                foreach (IStorageFile isf in files.GetResults())
                {
                    var istNew = ApplicationData.Current.LocalFolder.TryGetItemAsync(isf.Name);
                    if (istNew != null)
                    {
                        _gallerySampleData.Add(new ImageTaken()
                        {
                            ID = isf.Name,
                            Source = $"{_localResourcesPath}\\" + isf.Name,
                            Name = isf.Name

                        });

                    }
                }
            
            
                           
            
            return _gallerySampleData;
        }
    }
}
