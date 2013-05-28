using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MegaApi;
using MegaApi.Comms;
using MegaApi.Utility;
using MegaDesktop.MVVM.Core;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace MegaDesktop.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private static string TITLE = "Mega Desktop (beta)";
        private Mega api;
        private IDictionary<string, MegaListNode> _flatNodes = new Dictionary<string, MegaListNode>();
        private ObservableCollection<TransferHandle> _transfers = new ObservableCollection<TransferHandle>();

        public ObservableCollection<MegaListNode> Nodes { get; set; }
        public ObservableCollection<TransferHandle> Transfers
        {
            get
            {
                return _transfers;
            }
        }

        private MegaListNode _currentNode;
        public MegaListNode CurrentNode
        {
            get { return _currentNode; }
            set
            {
                if (value == _currentNode)
                    return;

                _currentNode = value;

                OnPropertyChanged("CurrentNode");
            }
        }

        private MegaListNode _selectedNode;
        public MegaListNode SelectedNode
        {
            get { return _selectedNode; }
            set
            {
                if (value == _selectedNode)
                    return;

                _selectedNode = value;

                OnPropertyChanged("SelectedNode");
                OnPropertyChanged("CanDownload");
                OnPropertyChanged("CanDelete");
            }
        }

        private int _lastApiError;
        public int LastApiError
        {
            get { return _lastApiError; }
            set
            {
                if (value == _lastApiError)
                    return;

                _lastApiError = value;

                OnPropertyChanged("LastApiError");
            }
        }

        public string StatusText
        {
            get { return GetMessage(Status); }
        }

        private ConfigurationViewModel _configModel = new ConfigurationViewModel();
        public ConfigurationViewModel ConfigurationModel
        {
            get { return _configModel; }
            set
            {
                if (value == _configModel)
                    return;

                _configModel = value;

                OnPropertyChanged("ConfigurationModel");
            }
        }

        private ConnectivityStatus _status = ConnectivityStatus.Offline;
        public ConnectivityStatus Status
        {
            get { return _status; }
            set
            {
                if (value == _status)
                    return;

                _status = value;

                OnPropertyChanged("StatusText");
                OnPropertyChanged("CanOperate");
                OnPropertyChanged("IsBusy");
                OnPropertyChanged("PickNodeEnabled");
                OnPropertyChanged("Status");
            }
        }

        private string _title = TITLE;
        public string Title
        {
            get { return _title; }
            set
            {
                if (value == _title)
                    return;

                _title = value;

                OnPropertyChanged("Title");
            }
        }

        private bool _isNewVersionAvailable = false;
        public bool IsNewVersionAvailable
        {
            get { return _isNewVersionAvailable; }
            set
            {
                if (value == _isNewVersionAvailable)
                    return;

                _isNewVersionAvailable = value;

                OnPropertyChanged("IsNewVersionAvailable");
            }
        }

        public bool IsBusy
        {
            get { return _status == ConnectivityStatus.LogginIn; }
        }

        public bool PickNodeEnabled
        {
            get
            {
                return _status != ConnectivityStatus.RetrievingList;
            }
        }

        public bool CanOperate
        {
            get { return Status == ConnectivityStatus.AccountLoaded || Status == ConnectivityStatus.Online; }
        }
        public bool CanDownload
        {
            get
            {
                return SelectedNode != null;
            }
        }
        public bool CanDelete
        {
            get
            {
                return SelectedNode != null;
            }
        }

        public int FileNodesCount
        {
            get
            {
                if (CurrentNode == null)
                    return 0;

                return CurrentNode.Children.Count(n => n.Type == MegaNodeType.File);
            }
        }

        public string FileNodesSize
        {
            get
            {
                if (CurrentNode == null)
                    return "-";

                var total = CurrentNode.Children.Sum(n => n.Size) / 1024;

                if (total > 1000000)
                {
                    return string.Format("{0:F2} GB", (total / 1024) / 1024);
                }
                if (total > 1000)
                {
                    return string.Format("{0:F2} MB", total / 1024);
                }
                return string.Format("{0:F2} kB", total);
            }
        }

        public ICommand BackNodeCommand
        {
            get;
            set;
        }

        public ICommand CancelAllTransferCommand
        {
            get;
            set;
        }

        public ICommand CancelTransferCommand
        {
            get;
            set;
        }

        public ICommand DeleteCommand { get; set; }

        public ICommand DownloadNodesCommand { get; set; }

        public ICommand GoToAuthorSiteCommand { get; set; }

        public ICommand GoToFeedbackSiteCommand { get; set; }

        public ICommand MoveIntoNodeCommand
        {
            get;
            set;
        }

        public ICommand RefreshNodesCommand
        {
            get;
            set;
        }

        public ICommand RemoveAllTransfersCommand { get; set; }

        public ICommand RemoveTransferCommand { get; set; }

        public ICommand ShowConfigurationCommand { get; set; }

        public ICommand UploadCommand { get; set; }

        public ICommand UploadBatchCommand
        {
            get;
            set;
        }

        public MainWindowViewModel()
        {
            Nodes = new ObservableCollection<MegaListNode>();
            Nodes.CollectionChanged += OnNodesCollectionChanged;
            CheckTos();
            CheckFirstRun();

            UpdateCheck();

            MoveIntoNodeCommand = new RelayCommand(p => ChangeCurrentNode((MegaListNode)p));
            CancelTransferCommand = new RelayCommand(p => CancelTransfer((TransferHandle)p, false));
            UploadBatchCommand = new RelayCommand(p => UploadBatch((string[])p));
            BackNodeCommand = new RelayCommand(p => ChangeCurrentNode(CurrentNode.Parent), p => CurrentNode != null && CurrentNode.Parent != null);
            RefreshNodesCommand = new RelayCommand(p => LoadNodes());
            ShowConfigurationCommand = new RelayCommand(p => ShowConfiguration());
            DeleteCommand = new RelayCommand(p => DeleteNode((MegaListNode)p), p => CanDelete);
            DownloadNodesCommand = new RelayCommand(p => DownloadNodes((MegaListNode)p), p => CanDownload);
            RemoveTransferCommand = new RelayCommand(p => Transfers.Remove((TransferHandle)p));
            CancelAllTransferCommand = new RelayCommand(p => CancelAllTransfers(), p => Transfers.Count > 0);
            RemoveAllTransfersCommand = new RelayCommand(p => RemoveAllTransfers(), p => Transfers.Count > 0);
            UploadCommand = new RelayCommand(p => Upload());
            GoToAuthorSiteCommand = new RelayCommand(p => OpenAuthorSite());
            GoToFeedbackSiteCommand = new RelayCommand(p => OpenFeedbackSite());

            System.Net.ServicePointManager.DefaultConnectionLimit = 50;

            var save = false;
            var userAccountFile = GetUserKeyFilePath();
            Login(save, userAccountFile);
        }
        
        private void AddDownloadHandle(TransferHandle h)
        {
            Invoke(() => Transfers.Add(h));
            h.TransferEnded += (s1, e1) =>
            {
                if (Transfers.All(t => t.Progress >= 100))
                {
                    Status = ConnectivityStatus.Online;
                }
            };
        }

        private void AddFilesToParentNode(string parentNodeId, string[] files)
        {
            foreach (var file in files)
            {
                if (Directory.Exists(file))
                {
                    var dirBreakUp = file.Split(Path.DirectorySeparatorChar);
                    var d = api.CreateFolderSync(parentNodeId, dirBreakUp.Last());
                    AppendNode(d);
                    AddFilesToParentNode(d.Id, Directory.GetFileSystemEntries(file));
                }
                else
                {
                    var fileName = Path.GetFileName(file);
                    if (_flatNodes[parentNodeId].Children.Any(mln => mln.Name == fileName))
                    {
                        if (Properties.Settings.Default.SkipDuplicatedFiles || MessageBox.Show(string.Format("The file '{0}' was already uploaded. Do you want to overwrite it?", fileName), "FILE CONFLICT", MessageBoxButton.YesNo) == MessageBoxResult.No)
                        {
                            continue;
                        }
                    }

                    api.UploadFile(parentNodeId, file, h => AddUploadHandle(h), e => DefaultErrorHandler(e));
                }
            }
        }

        private void AddUploadHandle(TransferHandle h)
        {
            Invoke(() => Transfers.Add(h));
            h.PropertyChanged += (source, e) =>
            {
                if (e.PropertyName == "Status")
                {
                    if (((TransferHandle)source).Status == TransferHandleStatus.Uploading)
                    {
                        Status = ConnectivityStatus.Uploading;
                    }
                    else
                    {
                        if (Transfers.All(t => t.Status == TransferHandleStatus.Success || t.Status == TransferHandleStatus.Cancelled || t.Status == TransferHandleStatus.Error))
                        {
                            Status = ConnectivityStatus.Online;
                        }
                    }
                }
            };
            h.TransferEnded += (s1, e1) =>
            {
                AppendNode(h.Node);
            };
        }

        private void AppendNode(MegaNode node)
        {
            var newNode = new MegaListNode(node) { Parent = _flatNodes[node.ParentId] };
            _flatNodes.Add(node.Id, newNode);
            Invoke(() => _flatNodes[node.ParentId].Children.Add(newNode));
            OnPropertyChanged("CurrentNode");
            OnPropertyChanged("FileNodesCount");
            OnPropertyChanged("FileNodesSize");
        }

        private void CancelAllTransfers()
        {
            foreach (var transfer in Transfers)
            {
                if (transfer.Status == TransferHandleStatus.Downloading ||
                    transfer.Status == TransferHandleStatus.Uploading ||
                    transfer.Status == TransferHandleStatus.Pending ||
                    transfer.Status == TransferHandleStatus.Paused)
                {
                    transfer.CancelTransfer();
                }
            }
        }

        private void CancelTransfer(TransferHandle handle, bool warn = true)
        {
            if (warn && (handle.Status == TransferHandleStatus.Downloading || handle.Status == TransferHandleStatus.Uploading))
            {
                var type = (handle.Status == TransferHandleStatus.Downloading ? "download" : "upload");
                var text = String.Format("Are you sure to cancel the {0} process for {1}?", type, handle.Node.Attributes.Name);
                if (MessageBox.Show(text, "Cancel " + type, MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    return;
                }
            }
            handle.CancelTransfer();
        }

        private void CancelTransfers()
        {
            lock (_transfers)
            {
                foreach (var transfer in _transfers)
                {
                    transfer.CancelTransfer();
                }
            }
        }

        private void ChangeCurrentNode(MegaListNode newCurrent)
        {
            if (newCurrent.Type == MegaNodeType.Folder || newCurrent.Type == MegaNodeType.RootFolder)
            {
                CurrentNode = newCurrent;
                OnPropertyChanged("FileNodesCount");
                OnPropertyChanged("FileNodesSize");
            }
        }

        private void CheckFirstRun()
        {
            if (MegaDesktop.Properties.Settings.Default.FirstRunLatestVer !=
                GoogleAnalytics.AppVersion)
            {
                GoogleAnalytics.SendTrackingRequest("FirstRun_Desktop");
                MegaDesktop.Properties.Settings.Default.FirstRunLatestVer =
                GoogleAnalytics.AppVersion;
                MegaDesktop.Properties.Settings.Default.Save();
            }
            else
            {
                GoogleAnalytics.SendTrackingRequest("Run_Desktop");
            }
        }

        private static void CheckTos()
        {
            if (MegaDesktop.Properties.Settings.Default.TosAccepted) { return; }
            else
            {
                TermsOfServiceWindow tos = new TermsOfServiceWindow();
                var res = tos.ShowDialog();
                if (!res.Value)
                {
                    Process.GetCurrentProcess().Kill();
                }
                else
                {
                    MegaDesktop.Properties.Settings.Default.TosAccepted = true;
                    MegaDesktop.Properties.Settings.Default.Save();
                }
            }
        }

        private void DefaultErrorHandler(int e)
        {
            Status = ConnectivityStatus.Failed;
            LastApiError = e;
        }

        private void DeleteNode(MegaListNode node)
        {
            var type = (node.Type == MegaNodeType.Folder ? "folder" : "file");
            var text = String.Format("Are you sure to delete the {0} {1}?", type, node.Name);
            if (MessageBox.Show(text, "Deleting " + type, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                api.RemoveNode(node.Id, () => LoadNodes(), err => DefaultErrorHandler(err));
            }
        }

        private void DownloadNodes(MegaListNode megaListNode)
        {
            string destination;
            Status = ConnectivityStatus.Downloading;
            if (ShowPickFolder(out destination))
            {
                if (megaListNode.Type == MegaNodeType.Folder)
                {
                    foreach (var node in megaListNode.Children)
                    {
                        api.DownloadFile(node.BaseNode, Path.Combine(destination, node.Name), AddDownloadHandle, DefaultErrorHandler);
                    }
                }
                else
                {
                    api.DownloadFile(megaListNode.BaseNode, Path.Combine(destination, megaListNode.Name), AddDownloadHandle, DefaultErrorHandler);
                }
            }
        }

        private string GetUserKeyFilePath()
        {
            string userDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            userDir += System.IO.Path.DirectorySeparatorChar + "MegaDesktop";
            if (!Directory.Exists(userDir)) { Directory.CreateDirectory(userDir); }
            userDir += System.IO.Path.DirectorySeparatorChar;
            return userDir + "user.dat";
        }

        private string GetMessage(ConnectivityStatus connectivityStatus)
        {
            switch (connectivityStatus)
            {
                case ConnectivityStatus.LogginIn:
                    return "Logging in...";
                case ConnectivityStatus.RetrievingList:
                    return "Retrieving the list...";
                case ConnectivityStatus.Online:
                    return "Ready";
                case ConnectivityStatus.Uploading:
                    return "Uploading...";
                case ConnectivityStatus.Downloading:
                    return "Downloading...";
                case ConnectivityStatus.Failed:
                    return string.Format("Exception Found ({0})", LastApiError);
            }
            return string.Empty;
        }

        private void Invoke(Action fn)
        {
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (Delegate)fn);
        }

        private void LoadNodes()
        {
            string previousCurrentNode = null;

            if (CurrentNode != null)
            {
                previousCurrentNode = CurrentNode.Id;
            }
            Status = ConnectivityStatus.RetrievingList;
            _flatNodes.Clear();
            Invoke(() => Nodes.Clear());
            api.GetNodes((list) =>
            {
                Status = ConnectivityStatus.Online;
                Invoke(() =>
                {
                    foreach (var node in list)
                    {
                        var mlnNew = new MegaListNode(node);
                        if (!_flatNodes.ContainsKey(node.Id))
                        {
                            _flatNodes.Add(mlnNew.Id, mlnNew);
                        }
                        if (!string.IsNullOrEmpty(node.ParentId))
                        {
                            if (!_flatNodes.ContainsKey(node.ParentId))
                            {
                                var nodeParent = list.FirstOrDefault(mn => mn.Id == node.ParentId);

                                _flatNodes.Add(nodeParent.Id, new MegaListNode(nodeParent));
                            }
                            mlnNew.Parent = _flatNodes[node.ParentId];
                            _flatNodes[node.ParentId].Children.Add(mlnNew);
                        }
                        else
                        {
                            Nodes.Add(mlnNew);
                        }

                    }
                    if (!string.IsNullOrEmpty(previousCurrentNode))
                    {
                        CurrentNode = _flatNodes[previousCurrentNode];
                    }
                    else
                    {
                        CurrentNode = Nodes.FirstOrDefault(mln => mln.Name == "Root");
                    }
                });
            }, DefaultErrorHandler);
        }

        private MegaUser Login(bool save, string userAccountFile)
        {
            MegaUser u;
            if ((u = Mega.LoadAccount(userAccountFile)) == null) { save = true; }

            Status = ConnectivityStatus.LogginIn;

            Mega.Init(u, (m) =>
            {
                api = m;
                if (save)
                {
                    SaveAccount(userAccountFile, "user.anon.dat");
                }

                Status = ConnectivityStatus.AccountLoaded;

                if (api.User.Status == MegaUserStatus.Anonymous)
                {
                    Title = TITLE + " - anonymous account";
                }
                else
                {
                    Title = TITLE + " - " + m.User.Email;
                }
            }, (e) => { MessageBox.Show("Error while loading account: " + e); Application.Current.Shutdown(); });
            return u;
        }

        private void OpenAuthorSite()
        {
            Process.Start("http://megadesktop.com/");
        }

        private void OpenFeedbackSite()
        {
            Process.Start("http://megadesktop.uservoice.com/forums/191321-general");
        }

        private void RemoveAllTransfers()
        {
            var removeList = new List<TransferHandle>();
            foreach (var transfer in Transfers)
            {
                if (transfer.Status == TransferHandleStatus.Cancelled ||
                    transfer.Status == TransferHandleStatus.Error ||
                    transfer.Status == TransferHandleStatus.Success)
                {
                    removeList.Add(transfer);
                }
            }
            removeList.ForEach(t => Transfers.Remove(t));
        }

        private void SaveAccount(string userAccountFile, string backupFileName)
        {
            if (File.Exists(userAccountFile))
            {
                var backupFile = System.IO.Path.GetDirectoryName(userAccountFile) +
                                System.IO.Path.DirectorySeparatorChar +
                                backupFileName;
                File.Copy(userAccountFile, backupFile, true);
            }
            api.SaveAccount(GetUserKeyFilePath());
        }

        private void ScheduleUpload(string[] files, string parentId)
        {
            AddFilesToParentNode(parentId, files);
        }

        private void ShowConfiguration()
        {
            ConfigurationModel.IsConfigurationDialogVisible = true;
        }

        private bool ShowPickFolder(out string folderPicked)
        {
            bool success = false;
            if (Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog.IsPlatformSupported)
            {
                var dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;
                CommonFileDialogResult result = dialog.ShowDialog();

                success = (result == CommonFileDialogResult.Ok);
                folderPicked = dialog.FileName;
            }
            else
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                success = (result == System.Windows.Forms.DialogResult.OK);
                folderPicked = dialog.SelectedPath;
            }

            return success;
        }

        private void UpdateCheck()
        {
            CustomWC wc = new CustomWC(false, 30000);
            wc.DownloadStringCompleted += wc_DownloadStringCompleted;
            wc.DownloadStringAsync(new Uri("http://megadesktop.com/version.txt?rnd=" + (new Random()).Next()));
        }

        private void Upload()
        {
            var dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                Task.Factory.StartNew(() => ScheduleUpload(dialog.FileNames, CurrentNode.Id));
            }
        }

        private void UploadBatch(string[] files)
        {
            var task = Task.Factory.StartNew(() => ScheduleUpload(files, CurrentNode.Id));
        }

        protected void wc_DownloadStringCompleted(object sender, System.Net.DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                return;
            }

            try
            {
                if (e.Result.StartsWith("MD_VER:") && e.Result.Trim().Substring(7) != GoogleAnalytics.AppVersion)
                {
                    //Invoke(() =>
                    //{
                    //    HomeLink.Content = "http://megadesktop.com/ - New Version Available!";
                    //    HomeLink.Foreground = System.Windows.Media.Brushes.Red;
                    //});
                }
            }
            catch (Exception)
            {
            }
        }

        protected void OnNodesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("FileNodesCount");
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == "Status")
            {
                if (this.Status == ConnectivityStatus.AccountLoaded)
                {
                    LoadNodes();
                }
            }
        }

        public class MegaListNode
        {
            private MegaNode _baseNode;

            public MegaListNode(MegaNode baseNode)
            {
                Children = new ObservableCollection<MegaListNode>();
                _baseNode = baseNode;
            }

            public string Id
            {
                get
                {
                    return _baseNode.Id;
                }
            }
            public string Name
            {
                get
                {
                    return _baseNode.Attributes.Name;
                }
            }
            public int Type
            {
                get
                {
                    return _baseNode.Type;
                }
            }
            public MegaNode BaseNode
            {
                get
                {
                    return _baseNode;
                }
            }
            public MegaListNode Parent { get; set; }
            public ObservableCollection<MegaListNode> Children { get; set; }
            public long? Size
            {
                get
                {
                    return _baseNode.Size;
                }
            }
        }

        public enum ConnectivityStatus
        {
            Offline,
            LogginIn,
            AccountLoaded,
            Online,
            RetrievingList,
            Uploading,
            Downloading,
            Failed
        }
    }
}
