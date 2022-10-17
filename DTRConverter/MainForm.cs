using ExcelDataReader;
using System.Data;
using System.IO;
using System;
using System.Reflection;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas;
using Rectangle = iText.Kernel.Geom.Rectangle;
using Path = System.IO.Path;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Collections;
using System.Text;
using iText.Forms;
using System.IO.Compression;
using iText.Layout.Element;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.ExtendedProperties;
using Syncfusion.DocIO.DLS;
using Syncfusion.CompoundFile.DocIO.Net;
using Directory = System.IO.Directory;
using Syncfusion.DocIO;
using DocumentFormat.OpenXml.Spreadsheet;

namespace DTRConverter
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            Reset();
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            progressBarConvert.Value = 0;
            if (e.Data != null)
            {
                while (true)
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files.Length > 1)
                    {
                        MessageBox.Show("The program only accepts a single excel file.");
                        break;
                    }
                    if (files.Length < 1)
                    {
                        MessageBox.Show("File does not exists.");
                        break;
                    }
                    SetFile(files[0]);
                    return;
                }
            }
            Reset();
        }

        private void ButtonBrowse_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                SetFile(openFileDialog.FileName);
            }
            else
            {
                Reset();
            }
        }

        private async void ButtonConvert_Click(object sender, EventArgs e)
        {
            buttonBrowse.Enabled = false;
            buttonConvert.Enabled = false;
            panelDragFile.Enabled = false;
            await Task.Run(delegate
            {
                try
                {
                    if (!File.Exists(openFileDialog.FileName))
                    {
                        Invoke(delegate
                        {
                            Reset();
                            MessageBox.Show("File does not exists.");
                        });
                        return;
                    }

                    if (!openFileDialog.FileName.ToLower().EndsWith(".xls") &&
                        !openFileDialog.FileName.ToLower().EndsWith(".xlsx"))
                    {
                        Invoke(delegate
                        {
                            Reset();
                            MessageBox.Show("File is invalid.");
                        });
                        return;
                    }

                    Invoke(delegate
                    {
                        progressBarConvert.Value = 10;
                    });

                    string filename = openFileDialog.FileName;
                    string directory = filename[..filename.LastIndexOf('.')];

                    DataSet? result;
                    using (var stream = File.Open(filename, FileMode.Open, FileAccess.Read))
                    {
                        using var reader = ExcelReaderFactory.CreateReader(stream);
                        result = reader.AsDataSet();
                    }

                    if (result == null || result.Tables.Count == 0)
                    {
                        Invoke(delegate
                        {
                            Reset();
                            MessageBox.Show("File is invalid.");
                        });
                        return;
                    }

                    var table = result.Tables[0];

                    var columns = table.Columns;
                    var rows = table.Rows;

                    var nameColumn = columns[1];
                    var numberColumn = columns[2];
                    var dateTimeColumn = columns[3];
                    var dtrTypeColumn = columns[4];
                    var verifyCodeColumn = columns[8];

                    if (nameColumn == null ||
                        numberColumn == null ||
                        dateTimeColumn == null ||
                        dtrTypeColumn == null ||
                        verifyCodeColumn == null)
                    {
                        Invoke(delegate
                        {
                            Reset();
                            MessageBox.Show("File is invalid.");
                        });
                        return;
                    }

                    Dictionary<int, EmployeeDtr> employeeDtrs = new();
                    Dictionary<int, int> monthsCount = new();
                    Dictionary<int, int> yearsCount = new();
                    int firstDayInMonth = 0;
                    int lastDayInMonth = 0;

                    bool isFirstIteration = true;

                    foreach (DataRow row in rows)
                    {
                        if (isFirstIteration)
                        {
                            isFirstIteration = false;
                            continue;
                        }

                        var rawName = row[nameColumn].ToString();
                        var rawNumber = row[numberColumn].ToString();
                        var rawDateTime = row[dateTimeColumn].ToString();
                        var rawDtrType = row[dtrTypeColumn].ToString();
                        var rawVerifyCode = row[verifyCodeColumn].ToString();

                        if (rawName == null || string.IsNullOrEmpty(rawName) ||
                            rawNumber == null || string.IsNullOrEmpty(rawNumber) ||
                            rawDateTime == null || string.IsNullOrEmpty(rawDateTime) ||
                            rawDtrType == null || string.IsNullOrEmpty(rawDtrType) ||
                            rawVerifyCode == null || string.IsNullOrEmpty(rawVerifyCode))
                        {
                            Invoke(delegate
                            {
                                Reset();
                                MessageBox.Show("File is invalid.");
                            });
                            return;
                        }

                        string name = rawName;
                        if (name[..^1].Contains('.'))
                        {
                            name = string.Concat(name[..^1].Replace('.', ' '), name.AsSpan(name.Length - 1));
                        }
                        if (name[..^1].Contains(','))
                        {
                            name = string.Concat(name[..^1].Replace(",", ", "), name.AsSpan(name.Length - 1));
                        }
                        string[] separatedName1 = name.Split(' ');
                        name = "";
                        for (int j = 0; j < separatedName1.Length; j++)
                        {
                            name += separatedName1[j][0].ToString().ToUpperInvariant();
                            if (separatedName1[j].Length > 1)
                            {
                                name += separatedName1[j][1..].ToLowerInvariant();
                            }
                            if (separatedName1.Length - 1 != j)
                            {
                                name += ' ';
                            }
                        }

                        if (!int.TryParse(rawNumber, out int number))
                        {
                            Invoke(delegate
                            {
                                Reset();
                                MessageBox.Show("File is invalid.");
                            });
                            return;
                        }

                        if (!double.TryParse(rawDateTime, out double doubleDateTime))
                        {
                            Invoke(delegate
                            {
                                Reset();
                                MessageBox.Show("File is invalid.");
                            });
                            return;
                        }
                        DateTime dateTime = DateTime.FromOADate(doubleDateTime);

                        VerifyCode verifyCode;
                        switch (rawVerifyCode.ToLower())
                        {
                            case "fingerpint":
                                verifyCode = VerifyCode.Fingerprint;
                                break;
                            case "face":
                                verifyCode = VerifyCode.Face;
                                break;
                            case "idcard":
                                verifyCode = VerifyCode.IDCard;
                                break;
                            default:
                                Invoke(delegate
                                {
                                    Reset();
                                    MessageBox.Show("File is invalid.");
                                });
                                return;
                        }

                        if (!employeeDtrs.TryGetValue(number, out EmployeeDtr? employeeDtr))
                        {
                            employeeDtr = new EmployeeDtr(number, name);
                            employeeDtrs.Add(number, employeeDtr);
                        }

                        Dtr dtr = new(dateTime, verifyCode);

                        employeeDtr.Dtrs.Add(dtr);

                        if (!yearsCount.ContainsKey(dateTime.Year))
                        {
                            yearsCount.Add(dateTime.Year, 1);
                        }
                        else
                        {
                            yearsCount[dateTime.Year] = yearsCount[dateTime.Year] + 1;
                        }
                        if (!monthsCount.ContainsKey(dateTime.Month))
                        {
                            monthsCount.Add(dateTime.Month, 1);
                        }
                        else
                        {
                            monthsCount[dateTime.Month] = monthsCount[dateTime.Month] + 1;
                        }

                        if (firstDayInMonth == 0 || firstDayInMonth > dateTime.Day)
                        {
                            firstDayInMonth = dateTime.Day;
                        }

                        if (lastDayInMonth == 0 || lastDayInMonth < dateTime.Day)
                        {
                            lastDayInMonth = dateTime.Day;
                        }
                    }

                    if (!yearsCount.Any() || !monthsCount.Any())
                    {
                        Invoke(delegate
                        {
                            Reset();
                            MessageBox.Show("File is invalid.");
                        });
                        return;
                    }

                    int year = yearsCount.MaxBy(i => i.Value).Key;
                    int month = monthsCount.MaxBy(i => i.Value).Key;

                    if (yearsCount.Count != 1)
                    {
                        Invoke(delegate
                        {
                            Reset();
                            MessageBox.Show("File contains multiple years.");
                        });
                        return;
                    }

                    if (monthsCount.Count != 1)
                    {
                        Invoke(delegate
                        {
                            Reset();
                            MessageBox.Show("File contains multiple months.");
                        });
                        return;
                    }

                    foreach (var employeeDtr in employeeDtrs.Values)
                    {
                        var perDay = employeeDtr.Dtrs.GroupBy(i => (i.DateTime.Year, i.DateTime.Month, i.DateTime.Day));
                        foreach (var day in perDay)
                        {
                            if (day.Key.Day == 14)
                            {

                            }

                            Dtr? amIn = day
                                .Where(i => i.DateTime.Hour < 12)
                                .MinBy(i => i.DateTime);
                            Dtr? pmOut = day
                                .Where(i => i.DateTime.Hour > 12)
                                .MaxBy(i => i.DateTime);

                            Dtr? amOut = day
                                .Where(i => i.DateTime.Hour == 12)
                                .MinBy(i => i.DateTime);
                            Dtr? pmIn = day
                                .Where(i => i.DateTime.Hour == 12 && i != amOut)
                                .MaxBy(i => i.DateTime);

                            if (amOut == null)
                            {
                                if (amIn == null)
                                {
                                    amOut = day
                                        .Where(i => i.DateTime.Hour < 12)
                                        .MaxBy(i => i.DateTime);
                                }
                                else
                                {
                                    amOut = day
                                        .Where(i => i.DateTime.Hour < 12 && !CheckDateDiff(i.DateTime, amIn.DateTime, TimeSpan.FromHours(1)))
                                        .MaxBy(i => i.DateTime);
                                }
                            }
                            if (pmIn == null)
                            {
                                if (pmOut == null)
                                {
                                    pmIn = day
                                        .Where(i => i.DateTime.Hour > 12)
                                        .MinBy(i => i.DateTime);
                                }
                                else
                                {
                                    pmIn = day
                                        .Where(i => i.DateTime.Hour > 12 && !CheckDateDiff(i.DateTime, pmOut.DateTime, TimeSpan.FromHours(1)))
                                        .MinBy(i => i.DateTime);
                                }
                            }

                            if (amOut == pmIn && amOut != null && pmIn != null)
                            {
                                if (amIn != null && pmOut != null)
                                {
                                    //amOut = null;
                                    //pmIn = null;
                                }
                                else if (amIn == null && pmOut != null)
                                {
                                    amOut = null;
                                }
                                else if (amIn != null && pmOut == null)
                                {
                                    pmIn = null;
                                }
                                else if (amIn == null && pmOut == null)
                                {
                                    //amOut = null;
                                }
                            }

                            PairedDtr pairedDtr = new(day.Key.Year, day.Key.Month, day.Key.Day, amIn, amOut, pmIn, pmOut);

                            employeeDtr.PairedDtrs.Add(pairedDtr);
                        }
                    }
                    string monthName = (new DateTime(year, month, 1)).ToString("MMMM");
                    string dateRange = $"{monthName} {firstDayInMonth} - {lastDayInMonth}, {year}";

                    Invoke(delegate
                    {
                        progressBarConvert.Value = 20;
                    });

                    var assembly = Assembly.GetExecutingAssembly();
                    string resourceName1 = assembly.GetManifestResourceNames()
                        .Single(str => str.EndsWith("template1.doc"));
                    string resourceName2 = assembly.GetManifestResourceNames()
                        .Single(str => str.EndsWith("template2.doc"));

                    Invoke(delegate
                    {
                        progressBarConvert.Value = 30;
                    });

                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    Stream? templateStreamResource1 = assembly.GetManifestResourceStream(resourceName1);
                    Stream? templateStreamResource2 = assembly.GetManifestResourceStream(resourceName2);

                    WordDocument documentTemplate1 = new(templateStreamResource1);
                    WordDocument documentTemplate2 = new(templateStreamResource2);
                    int index = 0;
                    foreach (var employeeDtr in employeeDtrs.Values)
                    {
                        WordDocument document;
                        if (employeeDtr.Number == 054)
                        {
                            document = documentTemplate1.Clone();
                        }
                        else
                        {
                            document = documentTemplate2.Clone();
                        }

                        var employeeNameCells = document.FindAll("{EmployeeName}", true, true);
                        foreach (var c in employeeNameCells)
                        {
                            var cell = c.GetAsOneRange();
                            cell.Text = employeeDtr.Name.ToUpperInvariant();
                        }

                        var dateTimeSpanCells = document.FindAll("{DateTimeSpan}", true, true);
                        foreach (var c in dateTimeSpanCells)
                        {
                            var cell = c.GetAsOneRange();
                            cell.Text = dateRange;
                        }

                        for (int i = 0; i < 31; i++)
                        {
                            PairedDtr? dtr = employeeDtr.PairedDtrs.FirstOrDefault(dtr => dtr.Day == (i + 1));
                            var cells1 = document.FindAll("{" + (i + 1).ToString() + ",1}", true, true);
                            var cells2 = document.FindAll("{" + (i + 1).ToString() + ",2}", true, true);
                            var cells3 = document.FindAll("{" + (i + 1).ToString() + ",3}", true, true);
                            var cells4 = document.FindAll("{" + (i + 1).ToString() + ",4}", true, true);
                            var cells5 = document.FindAll("{" + (i + 1).ToString() + ",5}", true, true);
                            var cells6 = document.FindAll("{" + (i + 1).ToString() + ",6}", true, true);
                            foreach (var c in cells1)
                            {
                                var cell = c.GetAsOneRange();
                                cell.CharacterFormat.FontSize = 10;
                                if (dtr != null && dtr.AMInDtr != null)
                                {
                                    cell.Text = dtr.AMInDtr.DateTime.ToString("hh:mm");
                                }
                                else
                                {
                                    cell.Text = "";
                                }
                            }
                            foreach (var c in cells2)
                            {
                                var cell = c.GetAsOneRange();
                                cell.CharacterFormat.FontSize = 10;
                                if (dtr != null && dtr.AMOutDtr != null)
                                {
                                    cell.Text = dtr.AMOutDtr.DateTime.ToString("hh:mm");
                                }
                                else
                                {
                                    cell.Text = "";
                                }
                            }
                            foreach (var c in cells3)
                            {
                                var cell = c.GetAsOneRange();
                                cell.CharacterFormat.FontSize = 10;
                                if (dtr != null && dtr.PMInDtr != null)
                                {
                                    cell.Text = dtr.PMInDtr.DateTime.ToString("hh:mm");
                                }
                                else
                                {
                                    cell.Text = "";
                                }
                            }
                            foreach (var c in cells4)
                            {
                                var cell = c.GetAsOneRange();
                                cell.CharacterFormat.FontSize = 10;
                                if (dtr != null && dtr.PMOutDtr != null)
                                {
                                    cell.Text = dtr.PMOutDtr.DateTime.ToString("hh:mm");
                                }
                                else
                                {
                                    cell.Text = "";
                                }
                            }
                            foreach (var c in cells5)
                            {
                                var cell = c.GetAsOneRange();
                                cell.CharacterFormat.FontSize = 10;
                                if (dtr != null)
                                {
                                    cell.Text = "";
                                }
                                else
                                {
                                    cell.Text = "";
                                }
                            }
                            foreach (var c in cells6)
                            {
                                var cell = c.GetAsOneRange();
                                cell.CharacterFormat.FontSize = 10;
                                if (dtr != null)
                                {
                                    cell.Text = "";
                                }
                                else
                                {
                                    cell.Text = "";
                                }
                            }
                        }

                        document.Protect(ProtectionType.AllowOnlyReading, "PBCCAdmin");

                        string employeeDtrFileName = $"{Path.Combine(directory, employeeDtr.Name.Replace(".", ""))} ({monthName} 1-{lastDayInMonth}).doc";
                        document.Save(employeeDtrFileName);

                        int progress = (int)(((index + 1) / (double)employeeDtrs.Count) * (100 - 30));

                        Invoke(delegate
                        {
                            progressBarConvert.Value = progress + 30;
                        });

                        index++;
                    }

                    templateStreamResource1?.Close();
                    templateStreamResource2?.Close();

                    Invoke(delegate
                    {
                        progressBarConvert.Value = 100;

                        MessageBox.Show($"File converted and saved to \"{directory}\" folder");
                    });
                }
                catch (Exception ex)
                {
                    Invoke(delegate
                    {
                        MessageBox.Show($"Conversion error: " + ex.Message);
                    });
                }
            });
            buttonBrowse.Enabled = true;
            buttonConvert.Enabled = true;
            panelDragFile.Enabled = true;
        }

        private void Reset()
        {
            progressBarConvert.Value = 0;
            buttonBrowse.Focus();
        }

        private void SetFile(string fileName)
        {
            textBoxFileName.Text = fileName;
            openFileDialog.FileName = fileName;
            progressBarConvert.Value = 0;
            buttonConvert.Focus();
        }

        private static bool CheckDateDiff(DateTime dateTime1, DateTime dateTime2, TimeSpan timeSpan)
        {
            TimeSpan diff = dateTime1 - dateTime2;
            double absDiff = Math.Abs(diff.TotalHours);
            return absDiff < timeSpan.TotalHours;
        }
    }
}