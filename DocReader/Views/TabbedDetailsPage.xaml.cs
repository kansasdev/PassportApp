using DocReader.Services;
using DocReader.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Helpers;
using System.Runtime.InteropServices.WindowsRuntime;

namespace DocReader.Views
{
    public sealed partial class TabbedDetailsPage : Page, INotifyPropertyChanged,PassService.IPassCallback
    {
        private PassService.IPass _proxy;
        DuplexChannelFactory<PassService.IPass> _factory;



        public TabbedDetailsPage()
        {
            InitializeComponent();
            //chkbxDOBVIZ.IsChecked = false;
            //chkbxValidityPeriodVIZ.IsChecked = false;

        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string _imgUrl;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if(e.Parameter!=null)
            {
                if(e.Parameter.GetType()==typeof(string))
                {
                    if (!string.IsNullOrEmpty(e.Parameter.ToString()))
                    {
                        _imgUrl = e.Parameter.ToString();

                        imgVIZ.Source = new BitmapImage(new Uri(_imgUrl));
                    }
                }
            }
            if (_proxy == null)
            {
                CreateProxy();                               
            }
            lstBoxMsg.ItemsSource = new List<string>();

            base.OnNavigatedTo(e);
        }

        private void CreateProxy()
        {
            EndpointAddress endpoint = new EndpointAddress(new Uri("net.tcp://localhost:8000/Passport/service"));

            InstanceContext context = new InstanceContext(this);
            PassService.PassClient client = new PassService.PassClient(context, BuildBinding(), endpoint);
            _factory = (DuplexChannelFactory<PassService.IPass>)client.ChannelFactory;

            _factory.Faulted += Factory_Faulted;

            _proxy = _factory.CreateChannel();
        }

        private void Factory_Faulted(object sender, EventArgs e)
        {
            CreateProxy();
        }

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

        private async void BtnDoRfid_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                ctrlLoading.IsLoading = true;
                ClearMessages();
                string message = string.Empty;
                PassService.CheckJavaPrerequisiteResponse cjpresp = await _proxy.CheckJavaPrerequisiteAsync(new PassService.CheckJavaPrerequisiteRequest(message));
                if (cjpresp.CheckJavaPrerequisiteResult)
                {
                    AddMessageToList("JavaPrereqOK".GetLocalized() + cjpresp.message);
                    PopToast();
                    PassService.KeyData kd = new PassService.KeyData();
                    kd.DOB = DOBVIZ.Text;
                    kd.DocNumber = docNumberVIZ.Text;
                    kd.ValidityDate = ValidityPeriodVIZ.Text;
                    await _proxy.ReadChipAsync(kd);
                }
                else
                {
                    AddMessageToList("JavaPrereqNOK".GetLocalized()+cjpresp.message);
                }
            }
            catch (CommunicationException ce)
            {
                CreateProxy();
            }
            catch (Exception ex)
            {
                AddMessageToList("Error reading RFID: " + ex.Message);
            }
            finally
            {
                ctrlLoading.IsLoading = false;
                await Task.CompletedTask;
            }
        }

        private void BtnDoPhotoAgain_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            NavigationService.Navigate(typeof(CameraPage));
        }

        private NetTcpBinding BuildBinding()
        {
            var binding = new NetTcpBinding()
            {

                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferPoolSize = int.MaxValue,
                ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas()
                {
                    MaxArrayLength = int.MaxValue,
                    MaxBytesPerRead = int.MaxValue,
                    MaxDepth = int.MaxValue,
                    MaxNameTableCharCount = int.MaxValue,
                    MaxStringContentLength = int.MaxValue
                }
                


            };
            binding.Security.Mode =SecurityMode.None;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.None;
                        
            return binding;
        }

        private async void BtnDoOCR_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            
            try
            {
                
                ctrlLoading.IsLoading = true;
                ClearMessages();
                string message = string.Empty;
                PassService.CheckPythonPrerequisiteRequest pythonRequest = new PassService.CheckPythonPrerequisiteRequest(message);
                PassService.CheckPythonPrerequisiteResponse pythonResp = await _proxy.CheckPythonPrerequisiteAsync(pythonRequest);
                if(pythonResp.CheckPythonPrerequisiteResult)
                {
                    AddMessageToList("Prerequisites for Python OK: "+pythonResp.message);
                    if(string.IsNullOrEmpty(_imgUrl))
                    {
                        throw new ApplicationException("No image for OCR provided");
                    }
                    else
                    {
                        StorageFile sf = await StorageFile.GetFileFromPathAsync(_imgUrl);
                        byte[] bytes = null;
                        using (var stream = await sf.OpenReadAsync())
                        {
                            bytes = new byte[stream.Size];
                            using (var reader = new DataReader(stream))
                            {
                                await reader.LoadAsync((uint)stream.Size);
                                reader.ReadBytes(bytes);
                            }
                        }
                        if(bytes!=null && bytes.Length>=1)
                        {
                            PassService.PersonalDataVIZ pdv = await _proxy.ExtractMrzDataAsync(bytes);
                            if (string.IsNullOrEmpty(pdv.Errors))
                            {
                                docNumberVIZ.Text = pdv.DocumentNumber;
                                docNumberCRC.IsChecked = pdv.DocNumberChecksum;
                                DOBVIZ.Text = pdv.DateOfBirth;
                                dobCRC.IsChecked = pdv.DOBChecksum;
                                ValidityPeriodVIZ.Text = pdv.ExpirationDate;
                                valPeriodCRC.IsChecked = pdv.ExpirationChecksum;
                                AddMessageToList("Extraction finished, check checksums");
                                //if(pdv.DocNumberChecksum&&pdv.DOBChecksum&&pdv.ExpirationChecksum)
                                {
                                    btnDoRfid.IsEnabled = true;

                                }
                            }
                            else
                            {
                                AddMessageToList("Error while OCR: "+pdv.Errors);
                            }
                        }
                    }
                    ctrlLoading.IsLoading = false;
                    await Task.CompletedTask;
                }
                else
                {
                    throw new ApplicationException("No Python prerequisites found!");
                }


            }
            catch(CommunicationException ce)
            {
                CreateProxy();
                ctrlLoading.IsLoading = false;
                AddMessageToList("No service running, run service again and try again" + Environment.NewLine + ce.Message);
            }
            catch (Exception ex)
            {
                ctrlLoading.IsLoading = false;
                AddMessageToList("Error doing OCR: " + ex.Message);
                await Task.CompletedTask;
            }
           
        }

        private async void BtnDoCSCAUpdate_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                ctrlLoading.IsLoading = true;
                ClearMessages();
                string msg = string.Empty;
                PassService.InvokeMasterlistupdateRequest imur = new PassService.InvokeMasterlistupdateRequest(msg);
                PassService.InvokeMasterlistupdateResponse imuresp = await _proxy.InvokeMasterlistupdateAsync(imur);
                if (imuresp.InvokeMasterlistupdateResult)
                {
                    AddMessageToList("Task finished: "+imuresp.message);
                }
                else
                {
                    AddMessageToList("Error during task: " + imuresp.message);
                }

            }
            catch(Exception ex)
            {
                AddMessageToList("Exception during CSCA update: " + ex.Message);
            }
            finally
            {
                ctrlLoading.IsLoading = false;
                await Task.CompletedTask;
            }
        }

        public void SessionOpened()
        {
            IAsyncAction act = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                AddMessageToList("RFID session opened");
            });

            Task t = act.AsTask();
        }

        public void SessionFinished(string messages)
        {
            IAsyncAction act = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                AddMessageToList(messages);
                if(!messages.Contains("error"))
                {
                    Items.SelectedIndex = 1;
                }

            });

            Task t = act.AsTask();
            
        }

        public void ErrorRaised(string err)
        {
            IAsyncAction act = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                AddMessageToList("Error during RFID"+Environment.NewLine+err);
            });

            Task t = act.AsTask();
        }

        public void MrzFromChipTaken(string mrz)
        {
            IAsyncAction act = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                AddMessageToList("MRZ taken");
                tbxMrz.Text = mrz;

                Items.SelectedIndex = 1;
            });

            Task t = act.AsTask();
        }

        public async void ImageFromChipTaken(byte[] img)
        {
            if (img != null && img.Length >= 1)
            {
                IAsyncAction act = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    AddMessageToList("Face image taken");

                });

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                 {

                    BitmapImage BI = await BytesToBitmapImage(img);
                    imgFace.Source = BI;
                 });
                Task t = act.AsTask();
            }
            
        }

        public void DocChecksPerformed()
        {
            IAsyncAction act = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                AddMessageToList("Document checked electronically");

               
            });

            Task t = act.AsTask();
        }

        public void ResultChip(DocReader.PassService.ChipData cd)
        {
            if (cd.MessageLOG != null)
            {
                IAsyncAction act = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (cd.CAStatus != null)
                    {
                        CAStatus.IsChecked = cd.CAStatus;
                    }
                    if (!string.IsNullOrEmpty(cd.FirstName))
                    {
                        firstName.Text = cd.FirstName.Trim('<');
                    }
                    if (!string.IsNullOrEmpty(cd.LastName))
                    {
                        lastName.Text = cd.LastName.Trim('<');
                    }
                    if (!string.IsNullOrEmpty(cd.Citizenship))
                    {
                        citizenship.Text = cd.Citizenship;
                    }
                    if (cd.DG14Integrity != null)
                    {
                        dg14CRC.IsChecked = cd.DG14Integrity;
                    }
                    if (cd.DG1Integrity != null)
                    {
                        dg1CRC.IsChecked = cd.DG1Integrity;
                    }

                    dg2CRC.IsChecked = cd.DG2Integrity;
                    if (!string.IsNullOrEmpty(cd.IssuingCountry))
                    {
                        issuingCountry.Text = cd.IssuingCountry;
                    }
                   
                    SODIntegrity.IsChecked = cd.SODIntegrity;
                    CSCATrusted.IsChecked = cd.IsDSTrusted;

                    if(!string.IsNullOrEmpty(cd.ExpirationDate))
                    {
                        validityPeriod.Text = cd.ExpirationDate;
                    }
                    if(!string.IsNullOrEmpty(cd.DOB))
                    {
                        dob.Text = cd.DOB;
                    }
                    if(!string.IsNullOrEmpty(cd.Citizenship))
                    {
                        citizenship.Text = cd.Citizenship;
                    }
                    if(!string.IsNullOrEmpty(cd.IssuingCountry))
                    {
                        issuingCountry.Text = cd.IssuingCountry;
                    }
                    if(!string.IsNullOrEmpty(cd.DocumentNumber))
                    {
                        docNumber.Text = cd.DocumentNumber;
                    }

                    IsPACE.IsChecked = cd.IsPACEReadingProc;
                    JPEG2000.IsChecked = cd.IsJPEG2000;

                    foreach (string m in cd.MessageLOG)
                    {
                        AddMessageToList(m);
                    }

                });

                Task t = act.AsTask();
            }
        }

        public void CSCADownloaded(string messages)
        {
            IAsyncAction act = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
             {
                 AddMessageToList("File downloaded as: "+messages);
             });

            Task t = act.AsTask();
                       

        }

        public void CSCAUnzipped(string message)
        {
            IAsyncAction act = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
             {
                 AddMessageToList("File unzipped to: "+message);
             });
            Task t = act.AsTask();
           
        }

        public void CSCAParsed(string message)
        {
            IAsyncAction act = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                AddMessageToList("Certificates parsed to: "+message);
            });

            Task t = act.AsTask();
            
        }

        private void AddMessageToList(string msg)
        {
                        
            if (lstBoxMsg.ItemsSource != null)
            {
               
                List<string> lstTemp = (List<string>)lstBoxMsg.ItemsSource;
                List<string> lstCurrent = new List<string>(lstTemp);
                lstCurrent.Add(msg);
                lstCurrent.Reverse();
                lstBoxMsg.ItemsSource = lstCurrent;
            }
            else
            {
                List<string> lstTemp = new List<string>();
                lstTemp.Add(msg);
                lstBoxMsg.ItemsSource = lstTemp;
            }
        }

        private void ClearMessages()
        {
            lstBoxMsg.ItemsSource = null;
        }

        private void PopToast()
        {
            // Generate the toast notification content and pop the toast
            ToastContent content = GenerateToastContent();
            ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(content.GetXml()));
        }


        private static ToastContent GenerateToastContent()
        {
            return new ToastContent()
            {
                Launch = "action=viewEvent&eventId=1983",
                Scenario = ToastScenario.IncomingCall,

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                {
                    new AdaptiveText()
                    {
                        Text = "Put document on a reader"
                    }

                    
                }
                    }
                },

                Actions = new ToastActionsCustom()
                {
                    Inputs =
            {
                new ToastSelectionBox("snoozeTime")
                {
                    DefaultSelectionBoxItemId = "1",
                    Items =
                    {
                        new ToastSelectionBoxItem("1", "waiting for 5 seconds"),
                        
                    }
                }
            },

                    Buttons =
            {
                
                new ToastButtonDismiss(){}
            }
                }
            };
        }

        private async Task<BitmapImage> BytesToBitmapImage(byte[] bytes)
        {

            BitmapImage image = new BitmapImage();
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                await stream.WriteAsync(bytes.AsBuffer());

                stream.Seek(0);

                await image.SetSourceAsync(stream);

            }

            return image;

        }

        private void ValidityPeriodVIZ_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (chkbxValidityPeriodVIZ.IsChecked != null && chkbxDOBVIZ.IsChecked != null)
            {
                if ((bool)chkbxDOBVIZ.IsChecked && (bool)chkbxDOBVIZ.IsChecked)
                {
                    btnDoRfid.IsEnabled = true;
                }
                else
                {
                    btnDoRfid.IsEnabled = false;
                }
            }
        }

        private void DOBVIZ_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (chkbxValidityPeriodVIZ.IsChecked != null && chkbxDOBVIZ.IsChecked != null)
            {
                if ((bool)chkbxDOBVIZ.IsChecked && (bool)chkbxDOBVIZ.IsChecked)
                {
                    btnDoRfid.IsEnabled = true;
                }
                else
                {
                    btnDoRfid.IsEnabled = false;
                }
            }
        }
    }
}
