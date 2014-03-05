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
        private string name = Guid.NewGuid().ToString();
        public const string Host = "http://www.dota2picks.somee.com";

        public IHubProxy Proxy { get; set; }
        public HubConnection Connection { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            _dataList = new List<Customer>();
            temp = new List<Customer>();
        }

        private List<Customer> _dataList;
        private List<Customer> temp; 

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            GetData();

            Task.Factory.StartNew(() =>
            {
                Connection = new HubConnection(Host);
                Proxy = Connection.CreateHubProxy("RTServerHub");
                Proxy.On<Customer>("addMessage", (t) => MessageBox.Show(@"Someone updated Table:
> " + t.OrderId+@"
> " + t.CustomerId + @"
> " + t.EmployeeId));
                Connection.Start().ContinueWith((t) =>
                {
                    MessageBox.Show(Connection.State.ToString() +@"
" + Connection.ConnectionId);
                    Proxy.Invoke("Registration");
                });
               
            });

        }

        private void GetData()
        {
            var client = new HttpClient { BaseAddress = new Uri("http://www.dota2picks.somee.com") }; // <- -- - - - - - - - --  - - - webapi
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = client.GetAsync("/api/values").Result;
            var records = response.Content.ReadAsAsync<Customer[]>().Result;

            _dataList.AddRange(records);

         
            _dataList.ForEach(f => temp.Add(f.Clone()));
            DataGrid.ItemsSource = temp;
        }


        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            var changedRecord = new List<Customer>();
            // post to webapi
            var client = new HttpClient { BaseAddress = new Uri("http://www.dota2picks.somee.com") }; // <- -- - - - - - - - --  - - - webapi

            var gridData = DataGrid.ItemsSource.Cast<Customer>().ToArray();

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
        }

        private void SendMessage(List<Customer> changedRecord)
        {
            foreach (var record in changedRecord)
            {
                Proxy.Invoke("Send", record);
            }
           
        }
    }


    public class Customer
    {
        public int OrderId { get; set; }

        public string CustomerId { get; set; }

        public int EmployeeId { get; set; }

        public DateTime OrderDate { get; set; }

        public double Freight { get; set; }

        public string ShipName { get; set; }

        public string ShipAdress { get; set; }
       
        
        public override bool Equals(object obj)
        {
            var record = obj as Customer;

            if (this.OrderId != record.OrderId || 
                this.CustomerId != record.CustomerId ||
                this.EmployeeId != record.EmployeeId ||
                this.OrderDate != record.OrderDate ||
                this.Freight != record.Freight ||
                this.ShipName != record.ShipName ||
                this.ShipAdress != record.ShipAdress)
                return false;
            return true;
        }

        public Customer Clone()
        {
            var temp = new Customer()
            {
                OrderId = OrderId,
                CustomerId = CustomerId,
                EmployeeId = EmployeeId,
                OrderDate = OrderDate,
                Freight = Freight,
                ShipAdress = ShipAdress,
                ShipName = ShipName
            };
            return temp;
        }
    }

}
