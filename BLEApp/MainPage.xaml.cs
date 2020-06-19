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
using Xamarin.Essentials;

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
        View oldServicesPage;
        public MainPage()
        {
            InitializeComponent();
            ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;
            deviceList = new ObservableCollection<IDevice>();

            //var status = Permissions.RequestAsync<Permissions.LocationAlways>();

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
        ListView charList;
        List<String> CharacteristicsListNames;
        Picker picker;
        List<String> DataFormats;
        private void getServicesPage()
        {
            //get and print each service
            ServicesListNames = new List<string>();
            foreach (IService i in Services)
            {
                ServicesListNames.Add(i.Name);
            }

            //User chooses output type
            DataFormats = new List<String>();
            DataFormats.Add("UTF-8 String");
            DataFormats.Add("Int32");
            DataFormats.Add("Hex");
            picker = new Picker
            {
                Title = "Data Format",
                TitleColor = Color.Black,
                ItemsSource = DataFormats,
                BackgroundColor = Color.White,
            };

            //Creats list to view services
            ServicesList = new ListView
            {
                ItemsSource = ServicesListNames,
                SelectionMode = ListViewSelectionMode.Single,
                SeparatorColor = Color.Black,
                BackgroundColor = Color.Transparent,
                
            };
            //when a service is tapped
            ServicesList.ItemTapped += async (sender, e) =>
            {
                //create backup for previous screen
                oldServicesPage = Content;

                //get index of service
                int index = e.ItemIndex;                
                
                //gather characteristics and put in list
                Characteristics = (IList<ICharacteristic>) await Services[index].GetCharacteristicsAsync();

                CharacteristicsListNames = new List<string>();
                foreach (ICharacteristic i in Characteristics)
                {
                    CharacteristicsListNames.Add(i.Name);
                    var test = await i.GetDescriptorsAsync();
                }
                
                //listview for each characteristic
                charList = new ListView
                {
                    ItemsSource = CharacteristicsListNames,
                    SelectionMode = ListViewSelectionMode.Single,
                    SeparatorColor = Color.Black,
                    BackgroundColor = Color.Transparent
                };
                //when a characteristic is tapped
                charList.ItemTapped += async (s, o) =>
                {
                    //get characteristic through index
                    int indexChar = o.ItemIndex;                    
                    var currentChar = Characteristics[indexChar];
                    
                    //if the characteristic is readable...
                    if (currentChar.CanRead)
                    {
                        try
                        {
                            //print characteristic based on chosen data
                            var bytes = await currentChar.ReadAsync();
                            if (picker.SelectedItem.Equals("UTF-8 String"))
                            {
                                await DisplayAlert(currentChar.Name, Encoding.UTF8.GetString(bytes, 0, bytes.Length), "Ok");
                            } else if (picker.SelectedItem.Equals("Int32"))
                            {
                                await DisplayAlert(currentChar.Name, Convert.ToInt32(bytes).ToString(), "Ok");
                            } else if (picker.SelectedItem.Equals("Hex"))
                            {
                                await DisplayAlert(currentChar.Name, BitConverter.ToString(bytes), "Ok");
                            } else
                            {
                                await DisplayAlert(currentChar.Name, Encoding.UTF8.GetString(bytes, 0, bytes.Length), "Ok");
                            }
                        }
                        catch
                        {
                            //defaults to string if error occurs
                            await DisplayAlert(currentChar.Name, currentChar.Value.ToString(), "Ok");
                        }
                    //if characteristic is updatable
                    } else if (currentChar.CanUpdate)
                    {
                        //NOT YET FINISHED...should create a subscription system
                        await currentChar.StartUpdatesAsync();
                        var desc = currentChar.GetDescriptorAsync(Services[index].Id);
                        DisplayAlert(currentChar.Name, desc.ToString() , "Ok");
                        await currentChar.StopUpdatesAsync();
                    }



                };
                //characteristics page
                Content = new StackLayout
                {
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    VerticalOptions = LayoutOptions.Start,

                    Children = {
                        new Label
                        {
                            Text = Services[index].Name,
                            FontSize = 20,
                            TextColor = Color.Black,
                        },

                        new Button
                        {
                            Text = "Go Back",
                            Command = new Command(() =>
                            {
                                Content = oldServicesPage;
                            })
                        },
                        charList,
                        picker,
                    }
                };           
                    
                    

                

            };
            
            //services page
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
