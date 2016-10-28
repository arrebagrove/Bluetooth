using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Bluetooth;
using Java.Util;

namespace Bluetooth.Droid.Activities.Bluetooth
{
    [Activity(Label = "BluetoothActivity", MainLauncher=true)]
    public class BluetoothActivity : Activity
    {
        string macAddress = string.Empty;
        private static ArrayAdapter<string> newDevicesArrayAdapter;
        private static ArrayAdapter<string> pairedDevicesArrayAdapter;
        private static List<BluetoothDevice> newlyDiscoveredBTDevices = new List<BluetoothDevice>();
        private Receiver receiver;
        BluetoothAdapter mBluetoothAdapter;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            RequestWindowFeature(WindowFeatures.IndeterminateProgress);
            SetContentView(Resource.Layout.device_list);

            receiver = new Receiver();
            var filter = new IntentFilter(BluetoothDevice.ActionFound);
            RegisterReceiver(receiver, filter);

            // Register for broadcasts when discovery has finished
            filter = new IntentFilter(BluetoothAdapter.ActionDiscoveryFinished);

            RegisterReceiver(receiver, filter);

            pairedDevicesArrayAdapter = new ArrayAdapter<string>(this, Resource.Layout.device_name);
            var pairedDevicesListView = FindViewById<ListView>(Resource.Id.paired_devices);
            pairedDevicesListView.Adapter = pairedDevicesArrayAdapter;
            pairedDevicesListView.ItemClick += PairedDevicesListView_ItemClick;

            newDevicesArrayAdapter = new ArrayAdapter<string>(this, Resource.Layout.device_name);

            var newDevicesListView = FindViewById<ListView>(Resource.Id.new_devices);
            newDevicesListView.Adapter = newDevicesArrayAdapter;
            newDevicesListView.ItemClick += DeviceListClick;

            mBluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            if (mBluetoothAdapter == null)
            {
                Toast.MakeText(this, "Bluetooth is not available", ToastLength.Long).Show();
            }
            else
            {
                if (!mBluetoothAdapter.IsEnabled)
                {
                    Intent enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                    StartActivityForResult(enableBtIntent, 1);
                }
            }

            var scanButton = FindViewById<Button>(Resource.Id.button_scan);
            scanButton.Click += (sender, e) =>
            {
                DoDiscovery();
                (sender as View).Visibility = ViewStates.Gone;
            };

        }

        private void PairedDevicesListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            mBluetoothAdapter.CancelDiscovery();

            // Get the device MAC address, which is the last 17 chars in the View
            var info = (e.View as TextView).Text.ToString();
            var address = info.Substring(info.Length - 17);

            string[] lines = info.ToString().Split(new string[] { "\n" }, StringSplitOptions.None);
            var device_Name = lines[0];

            BluetoothDevice device = mBluetoothAdapter.BondedDevices.Where(x => x.Name == device_Name).SingleOrDefault();
            try
            {
                BluetoothSocket btSocket = device.CreateRfcommSocketToServiceRecord(UUID.FromString("00001101-0000-1000-8000-00805F9B34FB"));
                btSocket.Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ex: " + ex.Message);
            }

        }

        void DeviceDiscovery(BluetoothAdapter mBluetoothAdapter)
        {
            ICollection<BluetoothDevice> pairedDevices = mBluetoothAdapter.BondedDevices;
            if (pairedDevices.Count > 0)
            {
                foreach (BluetoothDevice device in pairedDevices)
                {
                    Console.WriteLine("Name: " + device.Name + "\n Address: " + device.Address);
                    macAddress = device.Address;
                }
            }
        }

        private void DoDiscovery()
        {
            foreach (var device in mBluetoothAdapter.BondedDevices)
            {
                pairedDevicesArrayAdapter.Add(device.Name + "\n" + device.Address);
            }
            // Indicate scanning in the title
            SetProgressBarIndeterminateVisibility(true);
            SetTitle(Resource.String.scanning);

            // Turn on sub-title for new devices
            FindViewById<View>(Resource.Id.title_new_devices).Visibility = ViewStates.Visible;

            // If we're already discovering, stop it
            if (mBluetoothAdapter.IsDiscovering)
            {
                mBluetoothAdapter.CancelDiscovery();
            }

            // Request discover from BluetoothAdapter
            mBluetoothAdapter.StartDiscovery();
        }

        void DeviceListClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            // Cancel discovery because it's costly and we're about to connect
            mBluetoothAdapter.CancelDiscovery();

            // Get the device MAC address, which is the last 17 chars in the View
            var info = (e.View as TextView).Text.ToString();
            var address = info.Substring(info.Length - 17);

            string[] lines = info.ToString().Split(new string[] { "\n" }, StringSplitOptions.None);
            var device_Name = lines[0];

            Toast.MakeText(this, device_Name, ToastLength.Long).Show();


            BluetoothDevice btDevice = newlyDiscoveredBTDevices.Where(x => x.Name == device_Name).SingleOrDefault();
            try
            {
                BluetoothSocket btSocket = btDevice.CreateRfcommSocketToServiceRecord(UUID.FromString("00001101-0000-1000-8000-00805F9B34FB"));
                btSocket.Connect();
            }
            catch(Exception ex)
            {
                Console.WriteLine("ex: " + ex.Message);
            }

            //Intent intent = new Intent(this, typeof(BluetoothChat));
            //intent.PutExtra(BluetoothChat.DEVICE_NAME, device_Name);
            //StartActivity(intent);

        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Make sure we're not doing discovery anymore
            if (mBluetoothAdapter != null)
            {
                mBluetoothAdapter.CancelDiscovery();
            }

            // Unregister broadcast listeners
            UnregisterReceiver(receiver);
        }
        public class Receiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                string action = intent.Action;

                // When discovery finds a device
                if (action == BluetoothDevice.ActionFound)
                {
                    // Get the BluetoothDevice object from the Intent
                    BluetoothDevice device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                    // If it's already paired, skip it, because it's been listed already
                    if (device.BondState != Bond.Bonded)
                    {
                        newlyDiscoveredBTDevices.Add(device);
                        newDevicesArrayAdapter.Add(device.Name + "\n" + device.Address);
                    }
                    // When discovery is finished, change the Activity title
                }
                else if (action == BluetoothAdapter.ActionDiscoveryFinished)
                {
                }
            }
        }
    }
}