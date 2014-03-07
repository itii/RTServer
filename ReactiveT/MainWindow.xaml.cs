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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ReactiveT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string connectionString = "mongodb://itii:falconsoft@ds037737.mongolab.com:37737/rt_mongo";

        private string name = Guid.NewGuid().ToString();
        public const string Host = "http://www.dota2picks.somee.com";

        public IHubProxy Proxy { get; set; }
        public HubConnection Connection { get; set; }

        public MongoCollection<SamplePortfolio> MongoCollection { get; set; }

        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += _timer_Tick;
            
          
            _dataList = new List<SamplePortfolio>();
            temp = new List<SamplePortfolio>();
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            ProgressBar.Value++;
            if (ProgressBar.Value >= 100)
                ProgressBar.Value = 0;
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
                        (() =>
                        {
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
            //var client = new HttpClient { BaseAddress = new Uri("http://www.dota2picks.somee.com") }; // <- -- - - - - - - - --  - - - webapi
            //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            //var response = client.GetAsync("/api/values").Result;
            //var records = response.Content.ReadAsAsync<SamplePortfolio[]>().Result;

            //_dataList.AddRange(records);
            SaveButton.IsEnabled = false;
            _timer.Start();
            Task.Factory.StartNew(() =>
            {
                var server = MongoServer.Create(connectionString);
                var db = server.GetDatabase("rt_mongo");
                MongoCollection = db.GetCollection<SamplePortfolio>("SamplePortfolio");
                var list = MongoCollection.FindAll().AsEnumerable();
                _dataList.AddRange(list);
                _dataList.ForEach(f => temp.Add(f.Clone()));
                Dispatcher.Invoke(()=>DataGrid.ItemsSource = temp);
            }).ContinueWith(t =>
            {
                _timer.Stop();
                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Value = 0;
                    SaveButton.IsEnabled = true;
                });
                StartAggregateData();
            });
        }

        private void StartAggregateData()
        {
          //  MongoCollection

            var group = new BsonDocument 
                { 
                    { "$group", new BsonDocument 
                            { 
                            {  "_id","$StuffID" }, 
                                { 
                                    "Count", new BsonDocument 
                                                 { 
                                                     { 
                                                         "$sum", 1 
                                                     } 
                                                 } 
                                } 
                            }
                  } 
                };
            var result = MongoCollection.Aggregate(group).ResultDocuments.Select(s=>ToDynamic(s));
            
            Application.Current.Dispatcher.BeginInvoke((Action)(() => DataGridAggregation.ItemsSource = result));
        }

        public static dynamic ToDynamic(BsonDocument doc)
        {
            var json = doc.ToJson();
            dynamic obj = JToken.Parse(json);
            return obj;
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
                    var response = client.PutAsJsonAsync("api/Values/" + gridData[i]._id, gridData[i]).Result; //<-------- change string con
                    var item = gridData[i];
                    var itemToRemove = _dataList[i];
                    _dataList.Remove(itemToRemove);
                    item.Index = gridData.IndexOf(item);
                    _dataList.Insert(i,item.Clone());
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

    public class SamplePortfolio :INotifyPropertyChanged
    {
        private string _stuffID;
        private double _bidPrice;
        private double _offerPrice;
        private double _priceC;
        private double _tVol;
        private double _tValue;
        private double _iRate;
        [BsonId]
        internal ObjectId _id { get; set; }

        public int Index { get; set; }

        public string StuffID
        {
            get { return _stuffID; } 
            set { _stuffID = value; NotifyPropertyChanged("StuffID"); }
        }

        public double BidPrice
        {
            get { return _bidPrice; }
            set { _bidPrice = value; NotifyPropertyChanged("BidPrice"); }
        }

        public double OfferPrice
        {
            get { return _offerPrice; }
            set { _offerPrice = value; NotifyPropertyChanged("OfferPrice"); }
        }

        public double PriceC
        {
            get { return _priceC; }
            set { _priceC = value; NotifyPropertyChanged("PriceC"); }
        }

        public double TVol
        {
            get { return _tVol; } 
            set { _tVol = value; NotifyPropertyChanged("TVol"); }
        }

        public double TValue
        {
            get { return _tValue; } 
            set { _tValue = value; NotifyPropertyChanged("TValue"); }
        }

        public double IRate
        {
            get { return _iRate; } 
            set { _iRate = value; NotifyPropertyChanged("IRate"); }
        }

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

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;
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
