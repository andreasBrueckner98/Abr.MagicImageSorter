using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MagicImageSorter
{
    public class FileCopier
    {
        #region Events

        public event EventHandler<string> FileAlreadyExists;
        public event EventHandler<string> UnknownError;
        public event EventHandler<string> Success;
        public event EventHandler Completed;

        #endregion

        #region Fields

        private readonly string _source;
        private readonly string _target;
        private readonly bool _recursive;
        private readonly bool _genStructure;
        private readonly bool _structuredByYear;
        private readonly bool _structuredByMonth;
        private readonly bool _structuredByDay;
        private readonly bool _deleteOrig;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new Instance of <see cref="FileCopier"/>
        /// </summary>
        /// <param name="source">Source-Directory</param>
        /// <param name="target">Target-Directory</param>
        /// <param name="recursive">Recursive Search</param>
        /// <param name="genStructure">Generate a Structure</param>
        /// <param name="structureByYear">Generate a Structure by year</param>
        /// <param name="structureByMonth">Generate a Structure by month</param>
        /// <param name="structureByDay">Generate a Structure by day</param>
        /// <param name="deleteOrig">Delete the original files</param>
        public FileCopier(string source, string target, bool recursive, bool genStructure, bool structureByYear, bool structureByMonth, bool structureByDay, bool deleteOrig)
        {
            _source = source;
            _target = target;
            _recursive = recursive;
            _genStructure = genStructure;
            _structuredByYear = structureByYear;
            _structuredByMonth = structureByMonth;
            _structuredByDay = structureByDay;
            _deleteOrig = deleteOrig;
        }

        #endregion

        #region EventHandler

        private void OnFileAlreadyExists(string fileName) => FileAlreadyExists?.Invoke(this, fileName);
        private void OnUnknownError(string fileName) => UnknownError?.Invoke(this, fileName);
        private void OnSuccess(string fileName) => Success?.Invoke(this, fileName);
        private void OnCompleted() => Completed?.Invoke(this, null);

        #endregion

        #region Methods

        public async Task StartProgress()
        {
            await Task.Run(() =>
            {
                var files = ListFiles();

                if (_genStructure)
                {
                    foreach (var file in files)
                        GenerateDirectoryAndCopy(file);
                    //GenerateDirectoriesAndCopyFiles(files);
                }
                //else
                //{
                //    OnlyCopyFiles(files);
                //}

                if (_deleteOrig)
                {
                    DeleteOriginals(files.Select(x => x.FullName));
                }

                OnCompleted();
            });
        }

        private IEnumerable<FileInfo> ListFiles() => GetFiles(_source, ".jpg", ".png");

        private List<FileInfo> GetFiles(string path, params string[] filters)
        {
            var files = new List<FileInfo>();

            try
            {
                var currentDir = new DirectoryInfo(path);
                var allFiles = currentDir.GetFiles();

                foreach (var filter in filters)
                {
                    files.AddRange(allFiles.Where(x => x.Name.EndsWith(filter)));
                }

                if (_recursive)
                    foreach (var directory in currentDir.GetDirectories())
                        files.AddRange(GetFiles(currentDir.FullName, filters));
            }
            catch (UnauthorizedAccessException) { }

            return files;
        }

        private void GenerateDirectoriesAndCopyFiles(IEnumerable<string> files)
        {
            var information = RetrieveInformation(files);

            var yearGrouped = information.GroupBy(x => x.CreationTime.Year);
            string baseDir = string.Empty;
            foreach (var yearGroupedItem in yearGrouped)
            {
                var tmpTarget = _target;
                tmpTarget = Path.Combine(tmpTarget, $"{yearGroupedItem.Key}");

                if (!Directory.Exists(tmpTarget))
                    Directory.CreateDirectory(tmpTarget);

                if (_structuredByMonth)
                {
                    var monthGroupedFiles = yearGroupedItem.ToList().GroupBy(x => x.CreationTime.Month);

                    foreach (var monthGroupedFile in monthGroupedFiles)
                    {
                        var month = monthGroupedFile.Key;
                        baseDir = Path.Combine(baseDir, $"{month}");

                        if (!Directory.Exists(baseDir))
                            Directory.CreateDirectory(baseDir);

                        if (_structuredByDay)
                        {
                            var dayGroupedFiles = monthGroupedFile.ToList().GroupBy(x => x.CreationTime.Day);

                            foreach (var dayGroupedFile in dayGroupedFiles)
                            {
                                var day = monthGroupedFile.Key;
                                baseDir = Path.Combine(baseDir, $"{day}");

                                if (!Directory.Exists(baseDir))
                                    Directory.CreateDirectory(baseDir);

                                CopyFile(dayGroupedFile, baseDir);
                            }
                        }
                        else
                        {
                            CopyFile(monthGroupedFile, baseDir);
                        }
                    }
                }
                else
                {
                    CopyFile(yearGroupedItem, baseDir);
                }
            }
        }

        private void GenerateDirectoryAndCopy(FileInfo file)
        {
            var tmpTarget = _target;

            if (_genStructure || _structuredByYear)
            {
                var creationTime = file.CreationTime;
                var year = creationTime.Year;

                tmpTarget = Path.Combine(tmpTarget, $"{year}");
                if (!Directory.Exists(tmpTarget))
                    Directory.CreateDirectory(tmpTarget);

                if (_structuredByMonth)
                {
                    var month = creationTime.Month;

                    tmpTarget = Path.Combine(tmpTarget, $"{month}");
                    if (!Directory.Exists(tmpTarget))
                        Directory.CreateDirectory(tmpTarget);

                    if (_structuredByDay)
                    {
                        var day = creationTime.Day;

                        tmpTarget = Path.Combine(tmpTarget, $"{day}");
                        if (!Directory.Exists(tmpTarget))
                            Directory.CreateDirectory(tmpTarget);

                        CopyFile(file.FullName, Path.Combine(tmpTarget, file.Name));
                    }
                    else
                    {
                        CopyFile(file.FullName, Path.Combine(tmpTarget, file.Name));
                    }
                }
                else
                {
                    CopyFile(file.FullName, Path.Combine(tmpTarget, file.Name));
                }
            }
            else
            {
                CopyFile(file.FullName, Path.Combine(tmpTarget, file.Name));
            }
        }

        private void CopyFile(string sourceFile, string targetFile)
        {
            if (File.Exists(targetFile)) 
            {
                OnFileAlreadyExists(targetFile);
            }
            else
            {
                try
                {
                    File.Copy(sourceFile, targetFile);
                    OnSuccess(sourceFile);
                }
                catch (Exception ex)
                {
                    OnUnknownError(ex.ToString());
                }
            }
        }

        private void CopyFile(IGrouping<int, FileInfo> groupedFile, string targetDir)
        {
            var filePath = groupedFile.Select(x => x).FirstOrDefault().FullName;
            var file = groupedFile.Select(x => x).FirstOrDefault().Name;

            File.Copy(filePath, Path.Combine(targetDir, file));
        }

        private IEnumerable<FileInfo> RetrieveInformation(IEnumerable<string> files)
        {
            var res = new List<FileInfo>();

            foreach (var file in files)
            {
                res.Add(new FileInfo(file));
            }

            return res;
        }

        private void OnlyCopyFiles(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                File.Copy(file, Path.Combine(_target, new FileInfo(file).Name));
            }
        }

        private void DeleteOriginals(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        #endregion
    }
}
