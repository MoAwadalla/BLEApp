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

            
        }

        
        private async void connect(object sender, EventArgs e)
        {
            try
            {
                if(device != null)
                {
                    await adapter.ConnectToDeviceAsync(device);
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
                
                deviceList.Clear();
                
                adapter.DeviceDiscovered += (s, a) =>
                  {
                      deviceList.Add(a.Device);
                      //DisplayAlert("Device discovered", "", "Ok");
                  };

                await adapter.StartScanningForDevicesAsync();

                lv.ItemsSource = deviceList;
                if (!adapter.IsScanning)
                {
                    await adapter.StartScanningForDevicesAsync();
                   
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Notice", ex.Message.ToString(), "Error");
            }
        }
        private void getStatus(object sender, EventArgs e)
        {
            this.DisplayAlert("Notice", ble.State.ToString(), "Ok");
            if (ble.State == BluetoothState.Off)
            {
                txtErrorBle.Text = "Your Bluetooth is off";
            } else
            {
                txtErrorBle.Text = "";
            }
        }

        

        IList<IService> Services;
        IService Service;
        private async void btnGetServices_Clicked(object sender, EventArgs e)
        {
            Services = (IList<IService>)await device.GetServicesAsync();
            
            getServicesPage(); //generate new layout

            return;
        }

        ListView ServicesList;
        private void getServicesPage()
        {
            ServicesList = new ListView
            {
                ItemsSource = Services,
                SelectionMode = ListViewSelectionMode.Single,

            };
            ServicesList.ItemSelected += (sender, e) =>
            {
                int index = e.SelectedItemIndex;
                DisplayAlert(Services[index].Name, Services[index].Device.State.ToString(), "Ok");
            };
            oldPage = Content;
            Content =
                new StackLayout
                {
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Start,

                    Children = {
                        new Label
                        {
                            Text = "Services"
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

        private void txtErrorBle_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

        }
    }
}
