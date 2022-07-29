using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace MagicImageSorter
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Fields

        private string _source;
        private string _target;
        private bool _generateDateTimeStructure;
        private bool _structureByYear;
        private bool _structureByMonth;
        private bool _structureByDay;
        private bool _resultVisible;
        private int _successfullCount = 0;
        private ObservableCollection<string> _alreadyExistFiles = new ObservableCollection<string>();
        private ObservableCollection<string> _unknownErrors = new ObservableCollection<string>();

        #endregion

        #region Properties

        /// <summary>
        /// Command for Selecting the Source Directory
        /// </summary>
        public RelayCommand SearchSourceCommand
        {
            get => new RelayCommand(o =>
            {
                var sel = SelectDir();
                if (!string.IsNullOrEmpty(sel))
                {
                    Source = sel;
                }
            });
        }
        
        /// <summary>
        /// Command for Selecting the Target Directory
        /// </summary>
        public RelayCommand SearchTargetCommand
        {
            get => new RelayCommand(o =>
            {
                var sel = SelectDir();
                if (!string.IsNullOrEmpty(sel))
                {
                    Target = sel;
                }
            });
        }
        
        /// <summary>
        /// Command for Starting the Copy-Process
        /// </summary>
        public RelayCommand CopyCommand
        {
            get => new RelayCommand(o => Copy(), CanCopy);
        }
        
        /// <summary>
        /// Command for Starting a New Copy Process
        /// </summary>
        public RelayCommand StartNewCommand
        {
            get => new RelayCommand((o) => ResultVisible = false);
        }

        /// <summary>
        /// Command for Closing the Window
        /// </summary>
        public RelayCommand CloseCommand
        {
            get => new RelayCommand(o => Close());
        }

        /// <summary>
        /// Selected Source Directory
        /// </summary>
        public string Source
        {
            get => _source;
            private set
            {
                _source = value;
                OnPropertyChanged(nameof(Source));
            }
        }

        /// <summary>
        /// Selected Target Directory
        /// </summary>
        public string Target
        {
            get => _target;
            private set
            {
                _target = value;
                OnPropertyChanged(nameof(Target));
            }
        }

        /// <summary>
        /// Determines if underlying directories need to be included
        /// </summary>
        public bool SearchRecursive { get; set; }
        
        /// <summary>
        /// Determines if a Structure for DateTimes should be generated
        /// </summary>
        public bool GenerateDateTimeStructure
        {
            get => _generateDateTimeStructure;
            set
            {
                _generateDateTimeStructure = value;
                OnPropertyChanged(nameof(GenerateDateTimeStructure));

                if (value == false && StructureByYear != false)
                {
                    StructureByYear = false;
                }
            }
        }

        /// <summary>
        /// Determines if Files should be structured by year
        /// </summary>
        public bool StructureByYear
        {
            get => _structureByYear;
            set
            {
                _structureByYear = value;

                if (value == false)
                {
                    StructureByMonth = StructureByDay = false;

                    if (GenerateDateTimeStructure != false)
                    {
                        GenerateDateTimeStructure = false;
                    }
                }
            }
        }

        /// <summary>
        /// Determines if Files should be structured by month
        /// </summary>
        public bool StructureByMonth
        {
            get => _structureByMonth;
            set
            {
                _structureByMonth = value;
                OnPropertyChanged(nameof(StructureByMonth));

                if (value == false)
                {
                    StructureByDay = false;
                }
            }
        }

        /// <summary>
        /// Determines if Files should be structured by day
        /// </summary>
        public bool StructureByDay
        {
            get => _structureByDay;
            set
            {
                _structureByDay = value;
                OnPropertyChanged(nameof(StructureByDay));
            }
        }
        
        /// <summary>
        /// Determines if originals should be deleted after copying
        /// </summary>
        public bool DeleteOriginals { get; set; }

        /// <summary>
        /// Determines if Dialog with results is visible
        /// </summary>
        public bool ResultVisible
        {
            get => _resultVisible;
            set
            {
                _resultVisible = value;
                OnPropertyChanged(nameof(ResultVisible));
            }
        }

        /// <summary>
        /// Count of successfull copied files
        /// </summary>
        public int SuccessfullCount
        {
            get => _successfullCount;
            set
            {
                _successfullCount = value;
                OnPropertyChanged(nameof(SuccessfullCount));
            }
        }

        /// <summary>
        /// List of Files which already exists
        /// </summary>
        public ObservableCollection<string> AlreadyExistFiles
        {
            get => _alreadyExistFiles;
            set
            {
                _alreadyExistFiles = value;
                OnPropertyChanged(nameof(AlreadyExistFiles));
            }
        }

        /// <summary>
        /// List of unknown errors
        /// </summary>
        public ObservableCollection<string> UnknownErrors
        {
            get => _unknownErrors;
            set
            {
                _alreadyExistFiles = value;
                OnPropertyChanged(nameof(UnknownErrors));
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new Instance of <see cref="MainWindow"/>
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        #endregion

        #region EventHandler

        private void OnPropertyChanged(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        private void Copier_Completed(object sender, EventArgs e) => ResultVisible = true;

        private void Copier_Success(object sender, string e) => SuccessfullCount++;

        private void Copier_UnknownError(object sender, string e) => Dispatcher.Invoke(() => UnknownErrors.Add(e));

        private void Copier_FileAlreadyExists(object sender, string e) => Dispatcher.Invoke(() => AlreadyExistFiles.Add(e));

        #endregion

        #region Methods

        #region METHOD :: COPY

        private string SelectDir()
        {
            string dir = null;

            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                dir = dialog.SelectedPath;
            }

            return dir;
        }

        private async void Copy()
        {
            var copier = new FileCopier(Source, Target, SearchRecursive, GenerateDateTimeStructure, StructureByYear, StructureByMonth, StructureByDay, DeleteOriginals);
            copier.FileAlreadyExists += Copier_FileAlreadyExists;
            copier.UnknownError += Copier_UnknownError;
            copier.Success += Copier_Success;
            copier.Completed += Copier_Completed;

            await copier.StartProgress();
        }

        #endregion

        #region Helpers

        private bool CanCopy(object obj) => !string.IsNullOrEmpty(Source) && !string.IsNullOrEmpty(Target) && Directory.Exists(Source) && Directory.Exists(Target);

        #endregion

        #endregion
    }
}
