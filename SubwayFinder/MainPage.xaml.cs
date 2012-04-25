using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

using System.Device.Location;
using SubwayFinder.Common;
using SubwayFinder.Model;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Tasks;
using System.Text.RegularExpressions;

namespace SubwayFinder
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Enable progress indicator
            App.EnableProgressBar();

            // Initialize the location data
            App.AppGeoCoordinateWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            App.AppGeoCoordinateWatcher.MovementThreshold = 200;
            App.AppGeoCoordinateWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(AppGeoCoordinateWatcher_PositionChanged);
            App.AppGeoCoordinateWatcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(AppGeoCoordinateWatcher_StatusChanged);

            // Begin acquisition of data from location service
            if ((bool)IsolatedStorageSettings.ApplicationSettings["IsLocationEnabled"] == true)
            {
                // Location service is enabled
                App.AppGeoCoordinateWatcher.Start();
            }
            else
            {
                // Location service is disabled
            }
        }

        void AppGeoCoordinateWatcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            if (App.AppGeoCoordinateWatcher.Status == GeoPositionStatus.Ready && e.Position.Location.IsUnknown == false)
            {
                App.EnableProgressBar();

                // Clear collection
                if (App.RefinedLocations != null)
                {
                    App.RefinedLocations.Clear();
                }
                
                // Calculate distances
                foreach (LocationInfo loc in App.AppDatabase)
                {
                    loc.Distance = Utilities.ConvertToMiles(Utilities.Distance(e.Position.Location.Latitude, e.Position.Location.Longitude, loc.Latitude, loc.Longitude));
                }

                App.RefinedLocations = App.AppDatabase.OrderBy(loc => loc.Distance).Take(15).ToList();

                string streetPattern = "([\\w\\d\\s.]*),";
                string citystatezipPattern = "[\\w\\d\\s.]*, ([\\w\\d\\s.,]*)";
                Match streetMatch;
                Match citystatezipMatch;
                foreach (LocationInfo loc in App.RefinedLocations)
                {
                    // Format distance string
                    if (loc.Distance > 10.0)
                        loc.DistanceString = Math.Round(loc.Distance).ToString();
                    else
                        loc.DistanceString = loc.Distance.ToString("0.0");

                    // Match regex for street
                    streetMatch = Regex.Match(loc.FullLocationString, streetPattern);
                    loc.Street = streetMatch.Groups[1].Value.ToString();

                    // Match regex for city, state and zip
                    citystatezipMatch = Regex.Match(loc.FullLocationString, citystatezipPattern);
                    loc.CityStateZip = citystatezipMatch.Groups[1].Value.ToString();
                }
                
                this.ListViewPort.ItemsSource = App.RefinedLocations;

                App.DisableProgressBar();
            }
        }

        void AppGeoCoordinateWatcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            if (e.Status == GeoPositionStatus.NoData)
            {
                MessageBox.Show("Your location could not be determined, please try again later.", 
                    "Location Unavailable", 
                    MessageBoxButton.OK);
            }
        }

        private void LocationGrid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LocationInfo loc = (LocationInfo)((TextBlock)e.OriginalSource).DataContext;

            BingMapsDirectionsTask bingMapsDirectionsTask = new BingMapsDirectionsTask();

            LabeledMapLocation start = 
                new LabeledMapLocation("My Location", 
                    new GeoCoordinate(App.AppGeoCoordinateWatcher.Position.Location.Latitude, App.AppGeoCoordinateWatcher.Position.Location.Longitude));
            bingMapsDirectionsTask.Start = start;

            LabeledMapLocation end = 
                new LabeledMapLocation("Subway", 
                    new GeoCoordinate(loc.Latitude, loc.Longitude));
            bingMapsDirectionsTask.End = end;

            bingMapsDirectionsTask.Show();
        }

        #region AppBar
        private void AppBarIconMap_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/MapPage.xaml", UriKind.Relative));
        }
        #endregion AppBar
    }
}