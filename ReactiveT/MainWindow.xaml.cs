using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;

namespace ReactiveT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public const string Host = "http://www.dota2picks.somee.com";

        public IHubProxy Proxy { get; set; }
        public HubConnection Connection { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            _dataList = new List<Record>();
            temp = new List<Record>();
        }

        private List<Record> _dataList;
        private List<Record> temp; 

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            GetData();

            Task.Factory.StartNew(() =>
            {
                Connection = new HubConnection(Host);
                Proxy = Connection.CreateHubProxy("RTServerHub");

                Proxy.On<Record>("addMessage", (t) => MessageBox.Show(@"Someone updated Table:
> " + t.RecordKey+@"
> " + t.Value + @"
> " + t.Description));
                Connection.Start().ContinueWith((t) => { MessageBox.Show(Connection.State.ToString()); });

            });

        }

        private void GetData()
        {
            var client = new HttpClient { BaseAddress = new Uri("http://www.dota2picks.somee.com") }; // <- -- - - - - - - - --  - - - webapi
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = client.GetAsync("/api/values").Result;
            var records = response.Content.ReadAsAsync<Record[]>().Result;

            _dataList.AddRange(records);

         
            _dataList.ForEach(f => temp.Add(f.Clone()));
            DataGrid.ItemsSource = temp;
        }


        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            var changedRecord = new List<Record>();
            // post to webapi
            var client = new HttpClient { BaseAddress = new Uri("http://www.dota2picks.somee.com") }; // <- -- - - - - - - - --  - - - webapi

            var gridData = DataGrid.ItemsSource.Cast<Record>().ToArray();

            for (int i = 0; i < gridData.Count(); i++)
            {
                if (!gridData[i].Equals(_dataList[i]))
                {
                    var response = client.PostAsJsonAsync("api/User", gridData[i]).Result; //<-------- change string con
                    changedRecord.Add(gridData[i]);
                }
            }

            // signalr
            SendMessage(changedRecord);
            //hello Yarik
           // Proxy.Invoke("ActivateRandomizeData");
        }

        private void SendMessage(List<Record> changedRecord)
        {
            foreach (var record in changedRecord)
            {
                Proxy.Invoke("Send", record);
            }
           
        }
    }

    public class Record
    {
        public int RecordKey { get; set; }

        public string Value { get; set; }

        public string Description { get; set; }

        public override bool Equals(object obj)
        {
            var record = obj as Record;

            if (this.RecordKey != record.RecordKey || this.Value != record.Value ||
                this.Description != record.Description)
                return false;
            return true;
        }

        public Record Clone()
        {
            var temp = new Record()
            {
                RecordKey = RecordKey,
                Value = Value,
                Description = Description
            };
            return temp;
        }
    }
}
