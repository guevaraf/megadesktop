using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MegaDesktop.MVVM.Core;

namespace MegaDesktop.ViewModel
{
    public class ConfigurationViewModel: ViewModelBase
    {
        private bool _skipDuplicatedFiles = MegaDesktop.Properties.Settings.Default.SkipDuplicatedFiles;
        public bool SkipDuplicated
        {
            get { return _skipDuplicatedFiles; }
            set
            {
                if (value == _skipDuplicatedFiles)
                    return;

                _skipDuplicatedFiles = value;

                OnPropertyChanged("SkipDuplicated");
            }
        }

        private bool _isConfigurationDialogVisible = false;
        public bool IsConfigurationDialogVisible
        {
            get { return _isConfigurationDialogVisible; }
            set
            {
                if (value == _isConfigurationDialogVisible)
                    return;

                _isConfigurationDialogVisible = value;

                OnPropertyChanged("IsConfigurationDialogVisible");
            }
        }

        public ICommand SaveCommand { get; set; }

        public ConfigurationViewModel()
        {
            SaveCommand = new RelayCommand(p => Save());
        }

        private void Save()
        {
            MegaDesktop.Properties.Settings.Default.SkipDuplicatedFiles = SkipDuplicated;
            MegaDesktop.Properties.Settings.Default.Save();
            IsConfigurationDialogVisible = false;
        }
    }
}
