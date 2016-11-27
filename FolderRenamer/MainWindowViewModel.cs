using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using System.Windows.Input;

namespace FolderRenamer
{
    public class MainWindowViewModel : ViewModelBase
    {
        public Func<object, string> SelectFolderAction;

        private string _folderPath;
        private string _notification;
        private bool _canRename;

        public bool CanRename
        {
            get { return _canRename; }
            set { SetProperty(ref _canRename, value, () => CanRename); }
        }
        public string Notification
        {
            get { return _notification; }
            set { SetProperty(ref _notification, value, () => Notification); }
        }
        public string FolderPath
        {
            get { return _folderPath; }
            set
            {
                SetProperty(ref _folderPath, value, () => FolderPath);
                if (!String.IsNullOrEmpty(FolderPath))
                    CanRename = true;
            }
        }


        public ICommand SelectFolderCommand { get; set; }
        public ICommand RenameCommand { get; set; }

        public ICommand FindWithoutTagsCommand { get; set; }

        public MainWindowViewModel()
        {
            SelectFolderCommand = new DelegateCommand(SelectFolder);
            RenameCommand = new AsyncCommand(this.Rename);
            FindWithoutTagsCommand = new AsyncCommand(this.FindWithoutTags);

            CanRename = false;
        }

        private async Task FindWithoutTags()
        {
            await Task.Run(() =>
            {
                Logic logic = new Logic();
                this.Notification = "Getting Information...";
                var vm = this;
                string filePath = logic.FindWithoutTags(this.FolderPath, ref vm);
                this.Notification = "Done";
                Process.Start(filePath);
            });
        }

        private async Task Rename()
        {
            if (CanRename == false)
                return;

            await Task.Run(() =>
            {
                var vm = this;
                Logic logic = new Logic();
                this.Notification = "Processing ...";
                logic.Rename(this.FolderPath, ref vm);

                if (logic.failedRenames.Count > 0)
                {
                    this.Notification = "Done, with errors.Check ErroLog.txt for more info";
                    using (var file = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\ErroLog.txt", true))
                    {
                        file.WriteLine(String.Join(Environment.NewLine, logic.failedRenames));
                    }
                }
                else
                    this.Notification = "Done";
            });
        }

        private void SelectFolder()
        {
            FolderPath = SelectFolderAction.Invoke(null);
            this.Notification = String.IsNullOrEmpty(FolderPath)
                                ? "No Folder selected"
                                : this.FolderPath;
        }
    }
}
