using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileYearSrt
{
    class FileYearSrtModel
    {
        int copied;
        int processed;
        volatile bool running;
        int fileCount;
        volatile bool monthly;

        public int FileCount { get => fileCount; }

        public int Processed { get => processed; }

        public int Copyed { get => copied; }

        public string SourceFolder { get; private set; }

        public string DestFolder { get; private set; }

        public bool Running { get => running;   }

        public List<string> FileExtensions { get; private set; } = new List<string>();

        public List<string> SelectedFileExtensions { get; private set; } = new List<string>();

        public string ActualFile { get; private set; }

        public bool IsMonthlySorting { get=>monthly; set { monthly = value; } }


        public FileYearSrtModel(string src, string dest)
        {
            if (Directory.Exists(src) && !File.Exists(src))
            {
                SourceFolder = src;
            }
            else
            {
                throw new ArgumentException("Invalid source directory!");
            }
            if (Directory.Exists(dest) && !File.Exists(dest))
            {
                DestFolder = dest;
            }
            else
            {
                throw new ArgumentException("Invalid destination directory!");
            }            
        }

        public void CalculateFiles()
        {
            Task.Factory.StartNew(() =>
            {
                running = true;
                int fc = CalculateFiles(SourceFolder);
                Interlocked.Exchange(ref fileCount, fc);
                running = false;
            });
        }

        public int CalculateFiles(string path)
        {
            
            int result = Directory.GetFiles(path).Length;
            foreach (var item in Directory.GetFiles(path))
            {
                ActualFile = item;
                string ext = Path.GetExtension(item);
                if (!FileExtensions.Contains(ext))
                {
                    FileExtensions.Add(ext);
                }
            }
            foreach (var item in Directory.GetDirectories(path))
            {
                result += CalculateFiles(item);
            }
            return result;
        }

        public void CopyDirectory()
        {
            Task.Factory.StartNew(() => {
                running = true;
                Interlocked.Exchange(ref copied, 0);
                Interlocked.Exchange(ref processed, 0);
                CopyDirectory(SourceFolder);
                running = false;
            });
        }

        protected void CopyDirectory(string path)
        {
            bool mm = monthly;
            foreach (var item in Directory.GetFiles(path))
            {
                int year = File.GetLastWriteTime(item).Year;
                string destPath = Path.Combine(DestFolder, year.ToString());
                if(mm)
                {
                    string month = File.GetLastWriteTime(item).ToString("MM");
                    destPath = Path.Combine(destPath, month);
                }
                string fileName = Path.GetFileName(item);
                string ext = Path.GetExtension(item);
                if (SelectedFileExtensions.Contains(ext))
                {
                    if (!Directory.Exists(destPath))
                    {
                        Directory.CreateDirectory(destPath);
                    }
                    string target = Path.Combine(destPath, fileName);
                    if (!File.Exists(target))
                    {
                        File.Copy(item, target);
                        Interlocked.Increment(ref copied);
                    }
                    else
                    {
                        long sizeSrc = new FileInfo(item).Length;
                        long sizeDest = new FileInfo(target).Length;
                        if (sizeSrc != sizeDest)
                        {
                            int counter = 0;
                            bool needMore = false;
                            do
                            {
                                needMore = false;
                                ++counter;
                                string fnameGen = fileName.TrimEnd(ext.ToCharArray());
                                fnameGen += "(" + counter + ")" + ext;
                                target = Path.Combine(destPath, fnameGen);
                                if (File.Exists(target))
                                {
                                    sizeDest = new FileInfo(target).Length;
                                    needMore = sizeSrc != sizeDest;
                                }
                            } while (needMore);
                            if (!File.Exists(target))
                            {
                                File.Copy(item, target);
                                Interlocked.Increment(ref copied);
                            }
                        }
                    }
                }
                Interlocked.Increment(ref processed);
            }
            foreach (var item in Directory.GetDirectories(path))
            {
                CopyDirectory(item);
            }
        }

    }
}
