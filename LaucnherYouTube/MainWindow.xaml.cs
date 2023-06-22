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
        private readonly string xmlFile = "Assets/version.xml";
        private string _stateLocateVersionXML;
        private string _stateServerVersionXML;
        private int? idProcessApp = null;
        private bool appIsStarting = false;
        private bool checkUpdate = true;
        private bool isStartUnzipUpdateFileApp = true;
        private Process processApp;
        public static bool UserAllowUpdateApp { get; set; } = false;
        private DispatcherTimer dispatcherTimer;
        private Point scrollPointMouse = new Point();
        private double speedScrollHorizontalOffset = 0.3;
        private Stopwatch stopWatch = new Stopwatch();
        public bool IsOpenWindowSetting = false;
        public static string ArgumentsAppString { get; set; }
        public static int ArgumentsAppSpeedDownload { get; set; } = 81920;

        WebClient clientDownloadApp = new WebClient();
        HttpClient httpClient = new HttpClient();

        public MainWindow()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExcepctionEventApp);

            InitializeComponent();

            UpdateUI();
            ServerXMLDownload();
            UpdateContentSever();
            LocateVersionXML();
        }
        #region XMLREAD
        private void LocateVersionXML()
        {
            XmlReader reader = XmlReader.Create(xmlFile);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "Main")
                    {
                        reader.ReadToFollowing("version");
                        _stateLocateVersionXML = reader.ReadElementContentAsString();
                    }
                }
            }
        }
        public async void ServerXMLDownload()
        {
            try
            {
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
                ButtonInLauncher_CheckUpdate.IsEnabled = false;
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
        private void ButtonFoundNewVersion(object sender, RoutedEventArgs e)
        {
            ChildWindow.AllowUpdate allowUpdateWindow = new ChildWindow.AllowUpdate();
            allowUpdateWindow.Show();
        }
        private void UpdateUI()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(BackgroundUIFunction);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();
        }
        private void ButtonUpdateDialogWindow(object sender, RoutedEventArgs rea)
        {
            ServerDownloadChacheGameAsync();
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
            ButtonInLauncher_ReinstallApp.IsEnabled = true;
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
                ButtonInLauncher_KillStartedGame.IsEnabled = true;
            }
            catch (Exception e)
            {
                LoggingProcessJobs("EXCEPTION" + e.Message.ToString());
            }
        }
        private void ButtonKillProcessApp(object sender, RoutedEventArgs e)
        {
            if (appIsStarting == true)
            {
                processApp.Kill();
                processApp.Dispose();
                ButtonInLauncher_KillStartedGame.IsEnabled = false;
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
        #endregion
        #region BACKGROUNDFUNC
        public void BackgroundUIFunction(object sender, EventArgs ea)
        {
            if (_stateLocateVersionXML != _stateServerVersionXML & checkUpdate == true)
            {
                ButtonInLauncher_CheckUpdate.IsEnabled = true;
                checkUpdate = false;
            }
            if (UserAllowUpdateApp == true)
            {
                ButtonInLauncher_CheckUpdate.IsEnabled = false;
                ServerDownloadChacheGameAsync();
                UserAllowUpdateApp = false;
            }
            ProgressBarExtractFile.Minimum = 0;
            //_textCurrentVersion.Text = "Current version: " + _stateLocateVersionXML;
            _textServerVersion.Text = "Server version: " + _stateServerVersionXML;
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
            }
            else
            {
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
                ButtonInLauncher_ReinstallApp.IsEnabled = false;
                ButtonInLauncher_CheckUpdate.IsEnabled = false;
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
                        ButtonInLauncher_ReinstallApp.Dispatcher.Invoke(() => ButtonInLauncher_ReinstallApp.IsEnabled = true);
                        ButtonInLauncher_CheckUpdate.Dispatcher.Invoke(() => ButtonInLauncher_CheckUpdate.IsEnabled = true);
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
                    ButtonInLauncher_ReinstallApp.Dispatcher.Invoke(() => ButtonInLauncher_ReinstallApp.IsEnabled = true);
                    ButtonInLauncher_CheckUpdate.Dispatcher.Invoke(() => ButtonInLauncher_CheckUpdate.IsEnabled = true);
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
        private void OpenSettingsWindiowButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            if (IsOpenWindowSetting == false)
            {
                settingsWindow.Show();
                IsOpenWindowSetting = true;
            }
        }
    }
}