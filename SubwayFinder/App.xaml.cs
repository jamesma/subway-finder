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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

using System.Device.Location;
using SubwayFinder.Common;
using SubwayFinder.Model;
using System.IO.IsolatedStorage;

namespace SubwayFinder
{
    public partial class App : Application
    {
        public static GeoCoordinateWatcher AppGeoCoordinateWatcher = null;
        public static List<LocationInfo> AppDatabase = null;
        public static List<LocationInfo> RefinedLocations = null;

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();
        }

        public static void EnableProgressBar()
        {
            if (GlobalLoading.Instance.IsLoading == false)
                GlobalLoading.Instance.IsLoading = true;
        }

        public static void DisableProgressBar()
        {
            if (GlobalLoading.Instance.IsLoading == true)
                GlobalLoading.Instance.IsLoading = false;
        }

        private void LoadDatabase()
        {
            if (AppDatabase == null)
                AppDatabase = new List<LocationInfo>();

            var temp = CSVReader.FromStream(Application.GetResourceStream(new Uri("Model/subwaypoi.csv", UriKind.Relative)).Stream);

            foreach (List<string> row in temp)
            {
                LocationInfo loc = new LocationInfo();

                loc.Longitude = Convert.ToDouble(row[0]);
                loc.Latitude = Convert.ToDouble(row[1]);
                loc.Name = row[2].Trim();
                loc.FullLocationString = row[3].Trim();

                App.AppDatabase.Add(loc);
            }
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains("IsLocationEnabled") == false)
            {
                // This is the first run of the application
                IsolatedStorageSettings.ApplicationSettings["IsLocationEnabled"] = true;
            }

            this.LoadDatabase();
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            this.LoadDatabase();
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            // Ensure that required application state is persisted here.
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            // Let Page navigation transitions to work automatically
            RootFrame = new TransitionFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Initialize the global loading system
            GlobalLoading.Instance.Initialize(RootFrame);

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion
    }
}