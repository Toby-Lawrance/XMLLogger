using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ookii.Dialogs.Wpf;
using CsvHelper;
using CsvHelper.Configuration;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace XMLLogger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private UdpClient client = new();
        private string folder = "";
        private string file = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new VistaFolderBrowserDialog();
                dialog.RootFolder = Environment.SpecialFolder.Desktop;
                dialog.Multiselect = false;
                if ((bool) dialog.ShowDialog())
                {
                    folder = dialog.SelectedPath;
                }
            }
            catch (Exception exception)
            {
                Errors.Text = exception.ToString();
            }
        }

        private void StopBtn_OnClick(object sender, RoutedEventArgs e)
        {
            Running = false;
            Errors.Text += "Stopped";
            writer.Dispose();
        }

        private int port = 59152;

        private void StartBtn_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folder))
                {
                    folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }

                if (string.IsNullOrWhiteSpace(file))
                {
                    file = $"RecordingData.csv";
                }

                receiver = ReceiveData;

                port = int.Parse(PortBox.Text);
                client = new UdpClient(port);

                Running = true;


                fs = new FileStream(System.IO.Path.Join(folder, file), FileMode.Append);
                writer = new StreamWriter(fs);
                client.BeginReceive(receiver, null);
                Errors.Text += "Started";
            }
            catch (Exception exception)
            {
                Errors.Text = exception.ToString();
            }
        }

        private AsyncCallback receiver;
        private FileStream fs;
        private StreamWriter writer;
        private bool Running = false;

        private void ReceiveData(IAsyncResult ar)
        {
            try
            {
                var ep = new IPEndPoint(IPAddress.Any, port);
                var data = client.EndReceive(ar, ref ep);
                var stringed = Encoding.ASCII.GetString(data);
                Dispatcher.Invoke(() => { DataDisplay.Text = stringed; });

                var doc = XDocument.Parse(stringed);

                var dp = doc.Descendants("DataPoint").First();
                writer.WriteLine(dp.Value);

                if (Running)
                {
                    client.BeginReceive(receiver, null);
                }
            }
            catch (Exception exception)
            {
                Dispatcher.Invoke(() => { Errors.Text = exception.ToString(); });
            }
        }

        private void AddToConsider_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DataPointName.Text))
            {
                return;
            }

            var identicalExists =
                DataPointNames.Items.SourceCollection.OfType<string>().Any(s => s == DataPointName.Text);
            if (identicalExists)
            {
                DataPointName.Text += "1";
                return;
            }

            DataPointNames.SelectedIndex = DataPointNames.Items.Add(DataPointName.Text);
        }

        private void UpdateToConsider_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DataPointName.Text))
            {
                return;
            }

            if (DataPointNames.SelectedItem is not string)
            {
                return;
            }
            
            var selectedIndex = DataPointNames.SelectedIndex;
            DataPointNames.Items[selectedIndex] = DataPointName.Text;
            DataPointNames.SelectedIndex = selectedIndex;
        }

        private void RemoveToConsider_OnClick(object sender, RoutedEventArgs e)
        {
            if (DataPointNames.SelectedItem is string s)
            {
                DataPointNames.Items.Remove(s);
                DataPointNames.SelectedIndex = DataPointNames.Items.Count - 1;
            }
        }

        private void DataPointNames_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataPointNames.SelectedItem is string s)
            {
                DataPointName.Text = s;
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            DataPointNames.SelectedIndex = DataPointNames.Items.Add("DataPoint");
        }
    }
}