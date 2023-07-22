using LaucnherYouTube.ChildWindow;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;

namespace LaucnherYouTube
{
    public partial class MainWindow : Window
    {
        private readonly string zipPath = @".\ChacheDownloadGame.zip";
        private readonly string appTemlPath = "tempDirectoryUnzip";
        private string _stateLocateVersionXML = "1.1";
        private string _stateServerVersionXML;
        private int? idProcessApp = null;
        private bool appIsStarting = false;
        private bool checkUpdate = true;
        private bool isStartUnzipUpdateFileApp = true;
        private Process processApp;
        public static bool isActiveSettingWindow { get; set; } = false;
        public static bool isActiveUpdateWindow { get; set; } = false;
        public static bool UserAllowUpdateApp { get; set; } = false;
        private DispatcherTimer dispatcherTimer;
        private Point scrollPointMouse = new Point();
        private double speedScrollHorizontalOffset = 0.3;
        private Stopwatch stopWatch = new Stopwatch();
        public static string ArgumentsAppString { get; set; }
        public static int ArgumentsAppSpeedDownload { get; set; } = 81920;

        WebClient clientDownloadApp = new WebClient();
        HttpClient httpClient = new HttpClient();
        Window settingsWindow;
        Window allowUpdateWindow;
        public MainWindow()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExcepctionEventApp);

            InitializeComponent();

            TextBlockInLauncherComboBoxChooseGame_CurrentVersion.Text = "Version app: " + _stateLocateVersionXML;
            UpdateUI();
            ServerXMLDownload();
            UpdateContentSever();
        }
        #region XMLREAD
        public async void ServerXMLDownload()
        {
            try
            {
                if (Directory.Exists("assets") == false)
                {
                    Directory.CreateDirectory("assets");
                }
                HttpClient serverClient = new HttpClient();
                var responseServerClient = await serverClient.GetAsync(new Uri("https://raw.githubusercontent.com"));
                if (responseServerClient.IsSuccessStatusCode)
                {
                    WebClient client = new WebClient();
                    client.DownloadFileCompleted += CompleteDownloadVersionXMLServer;
                    client.DownloadFileAsync(new Uri("https://raw.githubusercontent.com/VarionDrakon/LauncherGame/main/versionServer.xml"), "Assets/versionServer.xml");
                    ServerConnecting.Text = "SERVER ONLINE!";
                }
            }
            catch
            {
                ServerConnecting.Text = "SERVER OFFLINE!";
            }
        }
        private void CompleteDownloadVersionXMLServer(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled)
            {
                MessageBox.Show("Download error: " + e.Error);
            }
            else
            {
                ServerVersionXML();
            }
        }
        public void ServerVersionXML()
        {
            XmlTextReader reader = new XmlTextReader("Assets/versionServer.xml");
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "Main")
                    {
                        reader.ReadToFollowing("version");
                        _stateServerVersionXML = reader.ReadElementContentAsString();
                    }
                }
            }
        }
        #endregion
        #region UIFUNC
        private void UpdateUI()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(BackgroundUIFunction);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 2);
            dispatcherTimer.Start();
        }
        private void ButtonCancelDownloadApp(object sender, RoutedEventArgs e)
        {
            clientDownloadApp.CancelAsync();
            try
            {
                cancelTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                DownloadAppState.Dispatcher.Invoke(() => DownloadAppState.Text = "State: " + ex.Message.ToString());
            }
            ButtonInLauncher_StopDownloadGame.IsEnabled = false;
        }
        private void ButtonLaunchGame(object sender, RoutedEventArgs rea)
        {
            try
            {
                processApp = new Process();
                processApp.StartInfo.UseShellExecute = false;
                processApp.StartInfo.FileName = @"Game\Steampunk Edge - IcePunk.exe";
                processApp.StartInfo.Arguments = ArgumentsAppString;
                processApp.Start();
                idProcessApp = processApp.Id;
            }
            catch (Exception e)
            {
                LoggingProcessJobs("EXCEPTION" + e.Message.ToString());
            }
        }
        private void ProgressMessageHandler_HttpReceiveProgress(object sender, HttpProgressEventArgs e)
        {
            var calculateBytesSpeedWrite = e.BytesTransferred / 1024d / (stopWatch.ElapsedMilliseconds / 1000d);
            DownloadAppState.Dispatcher.Invoke(() => DownloadAppState.Text = "Progress download: " + e.ProgressPercentage + " Average speed download: " + (int)calculateBytesSpeedWrite + " kb/s");
            ProgressBarExtractFile.Dispatcher.Invoke(() => ProgressBarExtractFile.Value = e.ProgressPercentage);
        }
        private void ScrollViewerContent_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ScrollViewerContent.ReleaseMouseCapture();
        }

        private void ScrollViewerContent_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            scrollPointMouse = e.GetPosition(ScrollViewerContent);
            speedScrollHorizontalOffset = ScrollViewerContent.HorizontalOffset;
            ScrollViewerContent.CaptureMouse();
        }
        private void ScrollViewerContent_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewerContent.ScrollToHorizontalOffset(ScrollViewerContent.HorizontalOffset - e.Delta);
        }
        private void ScrollViewerContent_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (ScrollViewerContent.IsMouseCaptured)
            {
                ScrollViewerContent.ScrollToHorizontalOffset(speedScrollHorizontalOffset + (scrollPointMouse.X - e.GetPosition(ScrollViewerContent).X));
            }
        }
        private void OpenSettingsWindiowButton_Click(object sender, RoutedEventArgs e)
        {
            if (isActiveSettingWindow == false)
            {
                settingsWindow = new SettingsWindow();
                isActiveSettingWindow = true;
                settingsWindow.Show();
            }
        }
        private bool ComboBoxChooseGameInLauncherHandle = true;
        private void ComboBoxChooseGameInLauncher_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxChooseGameInLauncherHandle) ComboBoxChooseGameInLauncher_Handle();
            ComboBoxChooseGameInLauncherHandle = true;
        }
        private void ComboBoxChooseGameInLauncher_DropDownClosed(object sender, EventArgs e)
        {
            ComboBox ComboBoxChooseGameInLauncher = sender as ComboBox;
            ComboBoxChooseGameInLauncherHandle = !ComboBoxChooseGameInLauncher.IsDropDownOpen;
            ComboBoxChooseGameInLauncher_Handle();
        }
        private void ComboBoxChooseGameInLauncher_Handle()
        {
            switch (ComboBoxChooseGameInLauncher.SelectedIndex)
            {
                case 0:
                    ComboBoxChooseGameInLauncher.SelectedIndex = -1;
                    break;
                case 1:
                    Process.Start(@".\");
                    ComboBoxChooseGameInLauncher.SelectedIndex = -1;
                    break;
                case 2:
                    ComboBoxChooseGameInLauncher.SelectedIndex = -1;
                    break;
                case 3:
                    ComboBoxChooseGameInLauncher.SelectedIndex = -1;
                    break;
                case 4:
                    ComboBoxChooseGameInLauncher.SelectedIndex = -1;
                    break;
                case 5:
                    ComboBoxChooseGameInLauncher.SelectedIndex = -1;
                    break;
                case 6:
                    ComboBoxChooseGameInLauncher.SelectedIndex = -1;
                    break;
            }
        }
        private bool ComboBoxChooseGameInLauncherAddOptionsHandle = true;
        private void ComboBoxChooseGameInLauncherAddOptions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxChooseGameInLauncherAddOptionsHandle) ComboBoxChooseGameInLauncherAddOptions_Handle();
            ComboBoxChooseGameInLauncherAddOptionsHandle = true;
        }
        private void ComboBoxChooseGameInLauncherAddOptions_DropDownClosed(object sender, EventArgs e)
        {
            ComboBox ComboBoxChooseGameInLauncherAddOptions = sender as ComboBox;
            ComboBoxChooseGameInLauncherAddOptionsHandle = !ComboBoxChooseGameInLauncherAddOptions.IsDropDownOpen;
            ComboBoxChooseGameInLauncherAddOptions_Handle();
        }
        private void ComboBoxChooseGameInLauncherAddOptions_Handle()
        {
            switch (ComboBoxChooseGameInLauncherAddOptions.SelectedIndex)
            {
                case 0:
                    if (appIsStarting == true)
                    {
                        processApp.Kill();
                        processApp.Dispose();
                    }
                    ComboBoxChooseGameInLauncherAddOptions.SelectedIndex = -1;
                    break;
                case 1:
                    ServerDownloadChacheGameAsync();
                    ComboBoxChooseGameInLauncherAddOptions.SelectedIndex = -1;
                    break;
                case 2:
                    if (isActiveUpdateWindow == false)
                    {
                        allowUpdateWindow = new AllowUpdate();
                        isActiveUpdateWindow = true;
                        allowUpdateWindow.Show();
                    }
                    ComboBoxChooseGameInLauncherAddOptions.SelectedIndex = -1;
                    break;
            }
        }
        #endregion
        #region BACKGROUNDFUNC
        public void BackgroundUIFunction(object sender, EventArgs ea)
        {
            if (_stateLocateVersionXML != _stateServerVersionXML & checkUpdate == true)
            {
                checkUpdate = false;
            }
            if (UserAllowUpdateApp == true)
            {
                ServerDownloadChacheGameAsync();
                UserAllowUpdateApp = false;
            }
            ProgressBarExtractFile.Minimum = 0;
            TextBlockInLauncherComboBoxChooseGame_ServerVersion.Text = "Server version: " + _stateServerVersionXML;
            Process[] processedUsers = Process.GetProcesses();
            foreach (Process allprocessed in processedUsers)
            {
                if (allprocessed.Id == idProcessApp)
                {
                    appIsStarting = true;
                    break;
                }
                else if (allprocessed.Id != idProcessApp) appIsStarting = false;
            }
            if (appIsStarting == false)
            {
                AppState.Text = "App is not starting!";
                LaunchGameButton.IsEnabled = true;
                LaunchGameButton.Content = "Launch";
            }
            else
            {
                LaunchGameButton.IsEnabled = false;
                LaunchGameButton.Content = "Game is run";
                AppState.Text = "App is starting!";
            }
        }
        private static void ExcepctionEventApp(object sender, UnhandledExceptionEventArgs ueea)
        {
            Exception e = (Exception)ueea.ExceptionObject;
            LoggingProcessJobs("EXCEPTION" + e.Message.ToString());
        }
        private static void LoggingProcessJobs(string s)
        {
            if (Directory.Exists(@"Log/") == false)
            {
                Directory.CreateDirectory(@"Log/");
            }
            DateTime now = DateTime.Now;
            string todayTimeLog = now.ToString("mmHHddMMyyyyy");
            string nameFileLog = @"Log/" + "Log" + todayTimeLog + ".txt";

            using (StreamWriter sw = new StreamWriter(nameFileLog, false))
            {
                sw.WriteLineAsync(s);
            }
        }
        #endregion
        #region UPDATEFUNC
        CancellationTokenSource cancelTokenSource;
        public void ServerDownloadChacheGameAsync()
        {
            ButtonInLauncher_StopDownloadGame.IsEnabled = true;
            try
            {
                if (cancelTokenSource == null || cancelTokenSource.IsCancellationRequested)
                {
                    cancelTokenSource = new CancellationTokenSource();
                }
                CancellationToken cancellationToken = cancelTokenSource.Token;
                Task downloadFileHTTP = Task.Run(async () =>
                {
                    HttpRequestMessage httpRequestMessage = new HttpRequestMessage() { Method = HttpMethod.Get, RequestUri = new Uri("https://getfile.dokpub.com/yandex/get/https://disk.yandex.ru/d/ULuueEdGY9RIeA") };
                    ProgressMessageHandler progressMessageHandler = new ProgressMessageHandler(new HttpClientHandler() { AllowAutoRedirect = true });
                    httpClient = new HttpClient(progressMessageHandler) { Timeout = Timeout.InfiniteTimeSpan };
                    stopWatch.Start();
                    progressMessageHandler.HttpReceiveProgress += ProgressMessageHandler_HttpReceiveProgress;
                    Stream streamFileServer = await httpClient.GetStreamAsync(httpRequestMessage.RequestUri);
                    Stream fileStreamServer = new FileStream(zipPath, FileMode.OpenOrCreate, FileAccess.Write);
                    try
                    {
                        await streamFileServer.CopyToAsync(fileStreamServer, ArgumentsAppSpeedDownload, cancellationToken);
                        cancelTokenSource.Dispose();
                        streamFileServer.Dispose();
                        fileStreamServer.Dispose();
                        return;
                    }
                    catch (Exception e)
                    {
                        DownloadAppState.Dispatcher.Invoke(() => DownloadAppState.Text = "State: " + e.Message.ToString());
                        cancelTokenSource.Dispose();
                        streamFileServer.Dispose();
                        fileStreamServer.Dispose();
                        return;
                    }
                }, cancellationToken);
                downloadFileHTTP.ContinueWith(obj =>
                {
                    DownloadAppState.Dispatcher.Invoke(() => DownloadAppState.Text = "Unzip...");
                    using (ZipArchive zipFileServer = ZipFile.OpenRead(zipPath))
                    {
                        ProgressBarExtractFile.Dispatcher.Invoke(() => ProgressBarExtractFile.Value = 0);
                        int zipFilesCount = zipFileServer.Entries.Count;
                        ProgressBarExtractFile.Dispatcher.Invoke(() => ProgressBarExtractFile.Maximum = zipFilesCount);
                        foreach (var zip in zipFileServer.Entries)
                        {
                            if (isStartUnzipUpdateFileApp == true)
                            {
                                zip.Archive.ExtractToDirectory(appTemlPath);
                                isStartUnzipUpdateFileApp = false;
                                break;
                            }
                        }
                        ProgressBarExtractFile.Dispatcher.Invoke(() => ProgressBarExtractFile.Value = zipFilesCount);
                    }
                    File.Delete(zipPath);
                    foreach (string dgfse in Directory.GetFileSystemEntries(appTemlPath + "/Game"))
                    {
                        FileAttributes attributes = File.GetAttributes(dgfse);
                        DirectoryInfo dirInf = new DirectoryInfo(dgfse);
                        FileInfo fileInf = new FileInfo(dgfse);
                        if (Directory.Exists(@"Game/") == false)
                        {
                            Directory.CreateDirectory("Game");
                        }
                        if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            if (Directory.Exists(@"Game/" + dirInf.Name) == true)
                            {
                                Directory.Delete(@"Game/" + dirInf.Name, true);
                            }
                            dirInf.MoveTo(@"Game/" + dirInf.Name);
                        }
                        else if ((attributes & FileAttributes.Directory) != FileAttributes.Directory)
                        {
                            if (File.Exists(@"Game/" + fileInf.Name) == true)
                            {
                                File.Delete(@"Game/" + fileInf.Name);
                            }
                            fileInf.MoveTo(@"Game/" + fileInf.Name);
                        }
                    }
                    DownloadAppState.Dispatcher.Invoke(() => DownloadAppState.Text = "Game install!");
                    ProgressBarExtractFile.Dispatcher.Invoke(() => ProgressBarExtractFile.Value = 0);
                    return;
                }, cancellationToken);
            }
            catch (Exception e)
            {
                LoggingProcessJobs("EXCEPTION E: " + e.Message.ToString());
                DownloadAppState.Dispatcher.Invoke(() => DownloadAppState.Text = "State task: " + e.Message.ToString());
            }
        }
        #endregion
        #region WINDOWMANAGMENT 
        private void UnwrapApp_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void RollUpApp_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        #endregion
        #region WEBCONTENT
        private void UpdateContentSever()
        {
            var brushContentServer = new ImageBrush();
            brushContentServer.ImageSource = new BitmapImage(new Uri("https://raw.githubusercontent.com/VarionDrakon/VarionDrakon.github.io/4c68931bdfa5acf7a6ef0cb56d13797b7493d523/otherFiles/img/electrods.png", UriKind.Absolute));
            broochButtonContentServer_Content1.Background = brushContentServer;
            broochButtonContentServer_Content2.Background = brushContentServer;
            broochButtonContentServer_Content3.Background = brushContentServer;
            broochButtonContentServer_Content4.Background = brushContentServer;
            broochButtonContentServer_Content5.Background = brushContentServer;
            broochButtonContentServer_Content6.Background = brushContentServer;
        }
        #endregion
    }
}