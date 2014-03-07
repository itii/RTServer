using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
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
using Timer = System.Timers.Timer;

namespace ReactiveT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string name = Guid.NewGuid().ToString();
        public const string Host = "http://www.dota2picks.somee.com";
        private DispatcherTimer _timer;

        public IHubProxy Proxy { get; set; }
        public HubConnection Connection { get; set; }
        
        public MainWindow()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Tick += OnTick;
            _timer.Interval = TimeSpan.FromMilliseconds(500);
          
            _dataList = new List<SamplePortfolio>();
            temp = new List<SamplePortfolio>();
        }

        private void OnTick(object sender, EventArgs e)
        {
            ProgressBar.Value++;
            if (ProgressBar.Value == 99)
                ProgressBar.Value = 0;
        }

        private void OnTick(object state)
        {
           
        }

        

        private List<SamplePortfolio> _dataList;
        private List<SamplePortfolio> temp; 

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            GetData();

            Task.Factory.StartNew(() =>
            {
                Connection = new HubConnection(Host);
                Proxy = Connection.CreateHubProxy("RTServerHub");
                Proxy.On<SamplePortfolio>("addMessage", (t) =>
                {

                    var data = DataGrid.ItemsSource.Cast<SamplePortfolio>().ToArray();
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
            _timer.Start();
            var task = Task.Factory.StartNew(() =>
            {
                var client = new HttpClient {BaseAddress = new Uri("http://www.dota2picks.somee.com")};
                // <- -- - - - - - - - --  - - - webapi
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = client.GetAsync("/api/values").Result;
                var records = response.Content.ReadAsAsync<SamplePortfolio[]>().Result;

                _dataList.AddRange(records);


                _dataList.ForEach(f => temp.Add(f.Clone()));
                DataGrid.ItemsSource = temp;
            }
            );
            task.ContinueWith((t) => _timer.Stop());

        }


        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            var changedRecord = new List<SamplePortfolio>();
            // post to webapi
            var client = new HttpClient { BaseAddress = new Uri("http://www.dota2picks.somee.com") }; // <- -- - - - - - - - --  - - - webapi

            var gridData = DataGrid.ItemsSource.Cast<SamplePortfolio>().ToList();

            for (int i = 0; i < gridData.Count(); i++)
            {
                if (!gridData[i].Equals(_dataList[i]))
                {
                   // var response = client.PutAsJsonAsync("api/Values/" + gridData[i]._id, gridData[i]).Result; //<-------- change string con
                    var item = gridData[i];
                    var itemToRemove = _dataList[i];
                    _dataList.Remove(itemToRemove);
                    item.Index = gridData.IndexOf(item);
                    _dataList.Insert(i,item);
                    changedRecord.Add(item);
                }
            }

            // signalr
            SendMessage(changedRecord);
        }

        private void SendMessage(List<SamplePortfolio> changedRecord)
        {
            foreach (var record in changedRecord)
            {
                Proxy.Invoke("Send", record);
            }
           
        }
    }

    public class SamplePortfolio
    {
        internal string _id { get; set; }

        public int Index { get; set; }

        public string StuffID { get; set; }

        public double BidPrice { get; set; }

        public double OfferPrice { get; set; }

        public double PriceC { get; set; }

        public double TVol { get; set; }

        public double TValue { get; set; }

        public double IRate { get; set; }

        public override bool Equals(object obj)
        {
            var record = obj as SamplePortfolio;
            if (record == null) return false;
            if (this.StuffID != record.StuffID ||
                this.BidPrice != record.BidPrice ||
                this.OfferPrice != record.OfferPrice ||
                this.PriceC != record.PriceC ||
                this.TVol != record.TVol ||
                this.TValue != record.TValue ||
                this.IRate != record.IRate)
                return false;
            return true;
        }

        public SamplePortfolio Clone()
        {
            var temp = new SamplePortfolio()
            {
                _id = _id,
                Index = Index,
                StuffID = StuffID,
                BidPrice = BidPrice,
                OfferPrice = OfferPrice,
                PriceC = PriceC,
                TVol = TVol,
                TValue = TValue,
                IRate = IRate
            };
            return temp;
        }
    }

    public class Customer :INotifyPropertyChanged
    {
        private int _index;
        private int _orderId;
        private string _customerId;
        private int _employeeId;
        private DateTime _orderDate;
        private double _freight;
        private string _shipName;
        private string _shipAdress;

        public int Index { get { return _index; } set { _index = value; } }

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
            if (record == null) return false;
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
                Index = Index,
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
