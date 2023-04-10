using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Forms;
using System.Threading;

namespace FileYearSrt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FileYearSrtModel fileYearSrtModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SrcButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.ShowDialog();
                sourceText.Text = dialog.SelectedPath;
            }
        }

        private void DestButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.ShowDialog();
                destText.Text = dialog.SelectedPath;
            }
        }

        private void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() => {
                try
                {
                    enable(false);
                    if (fileYearSrtModel.FileCount == 0)
                    {
                        writeOutText("Preparation not succes!", true);
                        return;
                    }
                    Dispatcher.Invoke(() => {
                        fileYearSrtModel.IsMonthlySorting = (bool)monthlyChk.IsChecked;
                        foreach (var item in exList.SelectedItems)
                        {
                            fileYearSrtModel.SelectedFileExtensions.Add(item.ToString());
                        }
                    });
                    Dispatcher.Invoke(() => progress1.Maximum = fileYearSrtModel.FileCount);
                    writeOutText("Copy " + fileYearSrtModel.FileCount + " file to: " + fileYearSrtModel.DestFolder);
                    fileYearSrtModel.CopyDirectory();
                    do
                    {
                        writeOutText(fileYearSrtModel.Processed +  " / " + fileYearSrtModel.FileCount + " file processed!");
                        Dispatcher.Invoke(() => progress1.Value = fileYearSrtModel.Processed);
                        Thread.Sleep(100);
                    }
                    while (fileYearSrtModel.Running);
                    writeOutText( fileYearSrtModel.Processed + 
                        " / " + fileYearSrtModel.FileCount + " file processed! " + fileYearSrtModel.Copyed + " copied. Ready!");
                }
                catch (Exception ex)
                {
                    writeOutText(ex.Message, true);
                }
                finally
                {
                    enable(true);
                }
            });
        }

        private void PrepBtn_Click(object sender, RoutedEventArgs e)
        {
            string src = sourceText.Text;
            string dst = destText.Text;
            Task.Factory.StartNew(() =>
            {
                try
                {
                    enable(false);
                    writeOutText("Prepare!");
                    fileYearSrtModel = new FileYearSrtModel(src, dst);
                    fileYearSrtModel.CalculateFiles();
                    do
                    {
                        writeOutText(fileYearSrtModel.ActualFile);
                        Thread.Sleep(10);
                    }
                    while (fileYearSrtModel.Running);
                    Dispatcher.Invoke(() =>
                    {
                        foreach (var item in fileYearSrtModel.FileExtensions)
                        {
                            exList.Items.Add(item);
                        }
                        exList.SelectAll();
                    });

                    writeOutText("Ready to process!");
                }
                catch (Exception ex)
                {
                    writeOutText(ex.Message, true);
                }
                finally
                {
                    enable(true);
                }
            });
        }

        private void enable(bool state)
        {
            Dispatcher.Invoke(() => {
                prepBtn.IsEnabled = state;
                processButton.IsEnabled = state;
            });
        }

        private void writeOutText(string text, bool inRed = false)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                resultText.Text = text;
                if (inRed) resultText.Foreground = Brushes.Red;
                else resultText.Foreground = Brushes.Black;
            }));
        }
    }
}
