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

namespace SubwayFinder
{
    public partial class MapPage : PhoneApplicationPage
    {
        public MapPage()
        {
            InitializeComponent();

            App.AppGeoCoordinateWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(AppGeoCoordinateWatcher_PositionChanged);
            App.AppGeoCoordinateWatcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(AppGeoCoordinateWatcher_StatusChanged);

            this.MapViewPort.SetView(App.AppGeoCoordinateWatcher.Position.Location, 15.0);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            AppGeoCoordinateWatcher_PositionChanged(new object(), new GeoPositionChangedEventArgs<GeoCoordinate>(App.AppGeoCoordinateWatcher.Position));
        }

        void AppGeoCoordinateWatcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            if (App.AppGeoCoordinateWatcher.Status == GeoPositionStatus.Ready && e.Position.Location.IsUnknown == false)
            {
                App.EnableProgressBar();

                // Clear the map
                this.MapViewPort.Children.Clear();

                // Calculate distances
                foreach (LocationInfo loc in App.AppDatabase)
                {
                    loc.Distance = Utilities.ConvertToMiles(Utilities.Distance(e.Position.Location.Latitude, e.Position.Location.Longitude, loc.Latitude, loc.Longitude));
                }

                App.RefinedLocations = App.AppDatabase.OrderBy(loc => loc.Distance).Take(15).ToList();

                foreach (LocationInfo loc in App.RefinedLocations.Reverse<LocationInfo>())
                {
                    Pushpin pin = new Pushpin();
                    pin.Location = new GeoCoordinate(loc.Latitude, loc.Longitude);
                    pin.Background = (SolidColorBrush)Resources["PhoneAccentBrush"];
                    pin.Content = loc.Name;
                    pin.DataContext = loc;
                    pin.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(PushPin_Tap);

                    this.MapViewPort.Children.Add(pin);
                }

                // Show current location
                Pushpin me = new Pushpin();
                me.Location = e.Position.Location;
                me.Content = "My Location";

                this.MapViewPort.Children.Add(me);

                MapViewPort_MapPan(null, null);
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

        private void MapViewPort_MapPan(object sender, Microsoft.Phone.Controls.Maps.MapDragEventArgs e)
        {
            if (this.MapViewPort.IsDownloading == false)
                App.DisableProgressBar();
            else
                App.EnableProgressBar();
        }

        private void MapViewPort_MapResolved(object sender, EventArgs e)
        {
            App.DisableProgressBar();
        }

        void PushPin_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LocationInfo loc = (LocationInfo)((Pushpin)sender).DataContext;

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
        private void AppBarIconMe_Click(object sender, EventArgs e)
        {
            this.MapViewPort.SetView(App.AppGeoCoordinateWatcher.Position.Location, 15.0);
        }
        #endregion AppBar
    }
}