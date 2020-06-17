using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;

namespace BLEApp
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        IBluetoothLE ble;
        IAdapter adapter;
        ObservableCollection<IDevice> deviceList;
        IDevice device;
        View oldPage;
        public MainPage()
        {
            InitializeComponent();
            ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;
            deviceList = new ObservableCollection<IDevice>();
            lv.ItemsSource = deviceList;
            lv.RefreshCommand = new Command(() =>
                {
                    lv.IsRefreshing = false;

                    scan(new object(), new EventArgs());


                }); 
            
        }

        
        private async void connect(object sender, EventArgs e)
        {
            try
            {
                if(device != null)
                {
                    await adapter.ConnectToDeviceAsync(device);
                    DisplayAlert("Connected to:", device.Name.ToString(), "Ok");
                }
                else
                {
                    await DisplayAlert("Notice", "No Device Selected", "Ok");
                }
            }
            catch(Exception ex)
            {
               await DisplayAlert("Notice", ex.Message.ToString(), "Ok");
            }

        }

        

        private async void scan(object sender, EventArgs e)
        {
            try
            {
                lv.IsRefreshing = false;
                await adapter.StartScanningForDevicesAsync();

                deviceList.Clear();
                
               adapter.DeviceDiscovered += (s, a) =>
                  {
                      if (!deviceList.Contains(a.Device) && a.Device.Name != null)
                      {
                          deviceList.Add(a.Device);
                          lv.ItemsSource = deviceList;
                      }
                      
                      
                     // DisplayAlert("Device discovered", a.Device.Name, "Ok");
                  };

                

                
                if (!adapter.IsScanning)
                {
                    await adapter.StartScanningForDevicesAsync();
                   
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Notice", ex.Message.ToString(), "Error");
            }
            lv.ItemsSource = deviceList;
        }
        private void getStatus(object sender, EventArgs e)
        {
            this.DisplayAlert("Status", ble.State.ToString(), "Ok");
        }

        

        IList<IService> Services;
     //   IService Service;
        private async void btnGetServices_Clicked(object sender, EventArgs e)
        {
            if (device == null)
            {
                return;
            }
            Services = (IList<IService>)await device.GetServicesAsync();
            
            getServicesPage(); //generate new layout
            
            
        }

        ListView ServicesList;
        List<String> ServicesListNames;
        ICharacteristic characteristic;
        IList<ICharacteristic> Characteristics;
        private void getServicesPage()
        {
            ServicesListNames = new List<string>();
            foreach (IService i in Services)
            {
                ServicesListNames.Add(i.Name);
            }




            ServicesList = new ListView
            {
                ItemsSource = ServicesListNames,
                SelectionMode = ListViewSelectionMode.Single,
                SeparatorColor = Color.Black,
                BackgroundColor = Color.Transparent,
                
            };

            ServicesList.ItemSelected += async (sender, e) =>
            {
                int index = e.SelectedItemIndex;
                Characteristics = (IList<ICharacteristic>) await Services[index].GetCharacteristicsAsync();
                //characteristic = await Services[index].GetCharacteristicAsync(Services[index].Id);
                //var bytes = characteristic.Value;
                //List<string> charStrings = new List<string>();
                foreach (var i in Characteristics){
                    try
                    {
                        byte[] bytes = await i.ReadAsync();
                        DisplayAlert(Services[index].Name, Encoding.UTF8.GetString(bytes, 0, bytes.Length), "Ok");
                    }
                    catch
                    {
                        List<IDescriptor> descriptors = (List<IDescriptor>) await i.GetDescriptorsAsync();

                        DisplayAlert(Services[index].Name, descriptors.ToString(), "Ok");
                    }
                    
                    

                }

            };

            oldPage = Content;

            Content =

                new StackLayout
                {
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    VerticalOptions = LayoutOptions.Start,

                    Children = {
                        new Label
                        {
                            Text = "Services",
                            FontSize = 20,
                            TextColor = Color.Black,
                        },

                        new Button
                        {
                            Text = "Go Back",
                            Command = new Command(() =>
                            {
                                Content = oldPage;
                            })
                        },
                        ServicesList
                    }
                };
        }

       

        
        


        private void lv_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (lv.SelectedItem == null)
            {
                return;
            }
            device = lv.SelectedItem as IDevice;
        }

        


    }
}
