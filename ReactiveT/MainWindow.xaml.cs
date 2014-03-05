using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Threading;
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
                Proxy.On<Customer>("addMessage", (t) =>
                {

                    var data = DataGrid.ItemsSource.Cast<Customer>().ToArray();
                    data[t.Index] = t;
                    Application.Current.Dispatcher.BeginInvoke((Action)
                        (() => {
                                   DataGrid.ItemsSource = data;
                        }));
//                    MessageBox.Show(@"Someone updated Table:
//> " + t.OrderId+@"
//> " + t.CustomerId + @"
//> " + t.EmployeeId)
                });
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
                    var item = gridData[i];
                    var itemToRemove = _dataList[i];
                    _dataList.Remove(itemToRemove);
                    item.Index = i;
                    _dataList.Insert(i,item);
                    changedRecord.Add(item);
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


    public class Customer :INotifyPropertyChanged
    {
        private int _orderId;
        private string _customerId;
        private int _employeeId;
        private DateTime _orderDate;
        private double _freight;
        private string _shipName;
        private string _shipAdress;

        public int Index { get; set; }

        public int OrderId
        {
            get { return _orderId; }
            set { _orderId = value; NotifyPropertyChanged("OrderId"); }
        }

        public string CustomerId
        {
            get { return _customerId; }
            set { _customerId = value; NotifyPropertyChanged("CustomerId"); }
        }

        public int EmployeeId
        {
            get { return _employeeId; }
            set { _employeeId = value; NotifyPropertyChanged("EmployeeId"); }
        }

        public DateTime OrderDate
        {
            get { return _orderDate; }
            set { _orderDate = value; NotifyPropertyChanged("OrderDate"); }
        }

        public double Freight { get; set; }

        public string ShipName { get; set; }

        public string ShipAdress { get; set; }

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        
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

        public event PropertyChangedEventHandler PropertyChanged;
    }

}
