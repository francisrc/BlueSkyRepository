using System;
using System.Linq;
using System.IO;
using BSky.Lifetime;
using BSky.Lifetime.Interfaces;


namespace BSky.Statistics.Common
{
    public class Journal : ILogDevice
    {
        ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();//17Dec2012
        private string _fileName;//fullpathfilename
        private int _maxfilesize;
        private int _maxbackupfiles;
        #region ILogDevice Members

        public void WriteLine(string text)
        {
            if (!String.IsNullOrEmpty(_fileName) && null == _writer)
                OpenLogFile(_fileName);

            if (null != _writer)
            {
                if (CheckFileSize(_fileName))
                {
                    DateTime uanow = DateTime.Now;//Added by Anil to get current date time
                    _writer.WriteLine(uanow.ToLocalTime() + " :: " + text);
                }
            }
        }

        public string FileName
        {
            get{ return _fileName; }
            set{ _fileName = value;}
        }
        public int MaxFileSize
        {
            get { return _maxfilesize; }
            set { _maxfilesize = value; }
        }
        public int MaxBackupFiles
        {
            get { return _maxbackupfiles; }
            set { _maxbackupfiles = value; }
        }

        #endregion
        StreamWriter _writer;
        private void OpenLogFile(string fileName)
        {
            Close();

            string dirPath = Path.GetDirectoryName(fileName);

            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            _writer = File.AppendText(fileName);
            _writer.AutoFlush = true;

        }
        private void Close()
        {
            if (null == _writer) return;

            _writer.Close();
            _writer = null;
        }

        // check R Log filesize and if its more than 500KB then rename 
        // current log for backup and create new log file with same name as current log.
        // If Max number of backup files is reached then start deleting oldest & create backup
        // of current file with same name.
        private bool CheckFileSize(string rlogfname)//AD
        {
            int maxFilesizeinKB = 50;
            int maxBackupFiles = 10;
            string bkupfname = string.Empty;//rlogfname + ".1";

            //// Backup logic ///
            try
            {
                if (File.Exists(rlogfname))
                {
                    FileInfo f = new FileInfo(rlogfname);
                    long s1 = f.Length;
                    if (s1 > maxFilesizeinKB * 1024)// Max file size 
                    {

                        /// Generate Backup filename ///
                        for (int i = 1; i <= maxBackupFiles; i++)
                        {
                            if (!File.Exists(rlogfname + "." + i.ToString()))//if file does not exists
                            {
                                bkupfname = rlogfname + "." + i.ToString();
                                break;
                            }
                        }
                        if (bkupfname.Trim().Length < 1)//there are already max backup files.
                        {
                            ////delete oldest and use that name for new backup file. ie delete .1 then .2 then .3
                            string dirname = Path.GetDirectoryName(rlogfname);
                            DirectoryInfo dirInfo = new DirectoryInfo(dirname);
                            FileInfo[] allFiles = dirInfo.GetFiles();
                            if (allFiles.Length == 0)
                                return false;

                            FileInfo oldestfile = allFiles[0];
                            foreach (var currfile in allFiles.Skip(1))
                            {
                                if (currfile.LastWriteTime < oldestfile.LastWriteTime)
                                    oldestfile = currfile;
                            }
                            bkupfname = oldestfile.FullName;
                        }

                        /// close current log file///
                        Close();

                        /// delete old backup file ///
                        File.Delete(bkupfname);

                        /// rename current log file to backup filename ///
                        File.Move(rlogfname, bkupfname);

                        /// create and open new log file ///
                        OpenLogFile(rlogfname);
                    }

                }
            }
            catch (Exception ex)
            {
                string err = ex.Message;
                logService.WriteToLogLevel(ex.Message, LogLevelEnum.Error);
                return false;
            }
            return true;
        }

    }
}
