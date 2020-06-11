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
                      DisplayAlert("Device discovered", "", "Ok");
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

        private async void btnKnowConnect_Clicked(object sender, EventArgs e)
        {
            try
            {
                await adapter.ConnectToKnownDeviceAsync(new Guid("e0cbf06c-cd8b-4647-bb8a-263b43f0f974"));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Notice", ex.Message.ToString(), "Ok");
            }
        }

        IList<IService> Services;
        IService Service;
        private async void btnGetServices_Clicked(object sender, EventArgs e)
        {
            Services = (IList<IService>)await device.GetServicesAsync();
            Service = await device.GetServiceAsync(device.Id);
        }

        IList<ICharacteristic> Characteristics;
        ICharacteristic Characteristic;
        private async void btnGetcharacters_Clicked(object sender, EventArgs e)
        {
            var characteristics = await Service.GetCharacteristicsAsync();
            Guid idGuid = Guid.Parse("e0cbf06c-cd8b-4647-bb8a-263b43f0f974");
            Characteristic = await Service.GetCharacteristicAsync(idGuid);
        }

        private async void btnGetRW_Clicked(object sender, EventArgs e)
        {
            var bytes = await Characteristic.ReadAsync();
            await Characteristic.WriteAsync(bytes);
        }

        private async void btnUpdate_Clicked(object sender, EventArgs e)
        {
            Characteristic.ValueUpdated += (o, args) =>
            {
                var bytes = args.Characteristic.Value;
            };
            await Characteristic.StartUpdatesAsync();
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
