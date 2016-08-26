using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTools;
using CommonTools.Utils;
using System.IO;

namespace FolderRenamer
{
    public class Logic
    {
        private static string _path;
        private StringBuilder log;
        public List<string> failedRenames;

        public Logic()
        {
            failedRenames = new List<string>();
        }


        public Result Rename(string path, ref MainWindowViewModel vm)
        {
            log = new StringBuilder();
            var subFolders = this.GetSubfolders(path);

            foreach (var folder in subFolders)
            {
                vm.Notification = folder;
                _path = folder;
                string folderName = folder.Split('\\').Last();
                string newName = "";
                string newDir = "";

                switch (folderName.Split('-').Length)
                {
                    // name und artists werden aus tags geholt
                    case 1:
                        newName = this.GenerateNewNameWithArtistAndYear(folderName);
                        break;

                    // nur das jahr wird aus dem tag geholt
                    case 2:
                        newName = this.GenerateNewNameWithYear(folderName);
                        break;

                    case 3:
                        newName = this.GenerateNewNameWithArtistAndYear(folderName);
                        break;
                }

                // der name des neuen Pfades (directory) wird hier erzeugt, indem im alten pfad der letzte teil entfernt und der neue hinzuefügt wird
                var splitted = folder.Split('\\').ToList();
                splitted.RemoveAt(splitted.IndexOf(splitted.Last()));
                splitted.Add(newName);
                newDir = string.Join("\\", splitted);

                RenamingProcedure(folder, newDir);

            }

            /*
             Ablauf:
             In dem übergebenen Ordner, muss ganz nach unten navigiert werden, bis zu dem Ort wo die untersten Ordner sind
             Dabei sollen unterordern, die noch weiter unten sind als andere unterste Ordner ignoriert werden:

                Root
                --> Sub 1
                    --> Sub 1.1
                    --> Sub 1.2
                        -->Sub 1.2.1
                    --> Sub 1.3

              d.h. Sub 1.1, 1.2, 1.3 werden verwendet, Sub 1.2.1 wird ignoriert

            Dann soll der Name des Ordners analysiert werden.
            Wenn zuerst 4 Zahlen und danach ein bindestrich kommt: mit dem nächsten weitermachen           2014 - 
            Wenn am ende 4 zahlen in runden oder eckigen Klammern sind: klammern entfernen, 4 zahlen ganz nach vorne schieben und bindestrich einfügen:     (2014) --> 2014 -
 
            Zu beachten: 
            *Jahrlose Alben: "Slipknot - All Hope is Gone"
                --> In den Ordner rein und versuchen ID3 Tag für "YEAR" auszulesen, dann zurück navigieren und entsprechen umbennen: "2008 - Slipknot - All Hope is Gone"
                --> Wenn Tag für "YEAR" nicht gefüllt ist, dann weglassen (muss dann von Hand umbennant werden) !UND EINE MELDUNG ANZEGEN DASS DER ORDNER NCITH UMBENANNT WURDE!

            *Alben ohne Artist: "Total Annihilation"
                --> In den Ordner rein und versuchen ID3 Tag für "ARTIST" auzulesen, dann zurück navigieren und entsprechend umbennen: "2008 - Total Annihilation - Total Annihilation"
            */

            return new Result("", ResultStatus.Success);
        }

        private string GenerateNewNameWithArtistAndYear(string folderName)
        {
            string artist = GetArtistsFromTag();
            string year = GetYearFromTag();
            string album = GetAlbumFromTag();
            if (year == "0")
            {
                //log.AppendLine(_path);
                //log.AppendLine($"{artist} - {album}");
                //log.AppendLine(Environment.NewLine);

                failedRenames.Add($"{artist} - {album}");

                return $"{artist} - {album}";
            }

            return $"{year} - {artist} - {album}";
        }

        public IEnumerable<string> GetSubfolders(string sourcePath)
        {
            IEnumerable<string> subfolders = Directory.
                GetDirectories(sourcePath, "*", SearchOption.AllDirectories).
                Where(f => !Directory.EnumerateDirectories(f).Any());

            return subfolders;
        }

        private string GenerateNewNameWithYear(string folderName)
        {
            var yearName = folderName.Split('(').Last()
                                     .Split('[').Last()
                                     .Split('{').Last()
                                     .TrimEnd(')').TrimEnd(']');
            var albumName = folderName.Split('(').First()
                                      .Split('[').First()
                                      .Split('{').First();

            int temp = 0;
            if (!Int32.TryParse(yearName, out temp))
            {
                yearName = GetYearFromTag();
            }

            if (yearName == "0" || yearName.Length != 4)
            {
                failedRenames.Add(albumName);
                return albumName;
            }

            return $"{yearName} - {albumName}";
        }

        private string GetYearFromTag()
        {
            var file = Directory.GetFiles(_path, "*.mp3").FirstOrDefault();

            if (file == null)
                return "0";

            TagLib.File f = TagLib.File.Create(file);
            return f.Tag.Year.ToString();
        }

        private string GetArtistsFromTag()
        {
            var file = Directory.GetFiles(_path, "*.mp3").FirstOrDefault();

            if (file == null)
                return "";

            TagLib.File f = TagLib.File.Create(file);
            if (f.Tag.AlbumArtists.Length == 0)
                return f.Tag.Performers.ElementAt(0);

            return f.Tag.AlbumArtists.ElementAt(0);
        }

        private string GetAlbumFromTag()
        {
            var file = Directory.GetFiles(_path, "*.mp3").FirstOrDefault();
            if (file == null)
                return "";

            TagLib.File f = TagLib.File.Create(file);
            return f.Tag.Album;
        }

        private bool CheckStringFormat(string newName)
        {
            var splitted = newName.Split('-');
            if (splitted.Length == 3)
                return true;

            return false;
        }

        private void RenamingProcedure(string oldDirName, string newDirName)
        {
            try
            {
                if (oldDirName == newDirName)
                    return;
                if (Directory.Exists(newDirName))
                    return;

                Directory.CreateDirectory(newDirName);
                var files = Directory.GetFiles(oldDirName);
                foreach (var file in files)
                {
                    string newFilePath = String.Concat(newDirName, "\\", file.Split('\\').Last());
                    File.Move(file, newFilePath);
                }
                Directory.Delete(oldDirName);
            }
            catch (Exception ex)
            {
                using (var file = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\Log.txt", true))
                {
                    failedRenames.Add($"Error at: {oldDirName}\t{ex.Message}");
                    file.WriteLine($"Error at: {oldDirName}.{Environment.NewLine}{ex.Message}{Environment.NewLine}");
                }
            }
        }

        public string FindWithoutTags(string folderPath)
        {
            var subFolders = this.GetSubfolders(folderPath);
            foreach (var folder in subFolders)
            {
                var file = Directory.GetFiles(folder, "*.mp3").FirstOrDefault();
                if (file == null)
                {
                    using (var log = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\FindWithoutName_Error.txt", true))
                    {
                        log.WriteLine($"Couldn't find files for {folder}");
                        continue;
                    }
                }
                else
                {
                    TagLib.File tag = TagLib.File.Create(file);
                    if (String.IsNullOrEmpty(tag.Tag.Album)
                        || String.IsNullOrEmpty(tag.Tag.FirstAlbumArtist)
                        || String.IsNullOrEmpty(tag.Tag.FirstGenre)
                        || String.IsNullOrEmpty(tag.Tag.FirstPerformer)
                        || String.IsNullOrEmpty(tag.Tag.Title)
                        || tag.Tag.Year == 0)
                    {
                        using (var log = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\FindWithoutName.txt", true))
                        {
                            log.WriteLine($"{folder}");
                            continue;
                        }
                    }
                }
            }


            return AppDomain.CurrentDomain.BaseDirectory + "\\FindWithoutName.txt";
        }
    }
}