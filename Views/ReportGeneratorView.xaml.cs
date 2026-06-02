using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Drawing.Chart;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SystemUI.Views
{
    public partial class ReportGeneratorView : UserControl
    {
        private DataTable _currentData = null;
        private static readonly string FixedReportsDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "reports");

        public ReportGeneratorView()
        {
            InitializeComponent();
            txtSavePath.Text = FixedReportsDirectory;
            UpdateSuggestedFileName();
        }

        private void CmbReportType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSuggestedFileName();
        }

        private void UpdateSuggestedFileName()
        {
            if (txtFileName == null || cmbReportType == null) return;
            string type = (cmbReportType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Report";
            string shortType = RemoveTrailingReportWord(type)
                .Trim()
                .Replace(" ", "-");
            // e.g. MusicLib-Songs-20260602-200731
            txtFileName.Text = $"MusicLib-{shortType}-{DateTime.Now:yyyyMMdd-HHmmss}";
        }

        private static string RemoveTrailingReportWord(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            // net472-safe equivalent of Replace(" Report", "", OrdinalIgnoreCase) at end.
            const string suffix = " report";
            var trimmed = value.TrimEnd();
            if (trimmed.Length >= suffix.Length &&
                string.Equals(trimmed.Substring(trimmed.Length - suffix.Length), suffix, StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring(0, trimmed.Length - suffix.Length);
            }
            return value;
        }

        private void BtnPreview_Click(object sender, RoutedEventArgs e)
        {
            LoadPreviewData();
        }

        private void LoadPreviewData()
        {
            string type = (cmbReportType.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
            string query = type switch
            {
                // Preview should always be arranged by id number
                "Songs Report" => "SELECT id, title, artist, album, genre, duration, year_released FROM songs ORDER BY id",
                "Playlists Report" => "SELECT id, name, owner, description, created_at FROM playlists ORDER BY id",
                "Borrowing Report" => "SELECT id, borrower_name, song_title, borrow_date, return_date, status, remarks FROM borrowing ORDER BY id",
                _ => "SELECT id, title, artist, album, genre, duration, year_released FROM songs ORDER BY id"
            };

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    var adapter = new MySqlDataAdapter(query, conn);
                    _currentData = new DataTable();
                    adapter.Fill(_currentData);
                    dgPreview.ItemsSource = _currentData.DefaultView;
                    lblStatus.Text = $"Loaded {_currentData.Rows.Count} records.";
                    lblStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                    lblStatus.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error loading data: " + ex.Message;
                lblStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
                lblStatus.Visibility = Visibility.Visible;
            }
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            string outputDir = FixedReportsDirectory;
            string signedBy = txtSignedBy.Text.Trim();
            string reportType = (cmbReportType.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Songs Report";

            Directory.CreateDirectory(outputDir);

            string baseName = (txtFileName.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = $"MusicLib-{DateTime.Now:yyyyMMdd-HHmmss}";

            baseName = SanitizeFileName(baseName);
            string path = Path.Combine(outputDir, baseName + ".xlsx");

            if (_currentData == null || _currentData.Rows.Count == 0)
                LoadPreviewData();

            if (_currentData == null) return;

            try
            {
                using (var package = new ExcelPackage())
                {
                    // ===== SHEET 1: DATA =====
                    var ws = package.Workbook.Worksheets.Add("Report");

                    // Header block
                    ws.Cells[1, 1].Value = "MusicLib - Music Library Information System";
                    ws.Cells[1, 1].Style.Font.Bold = true;
                    ws.Cells[1, 1].Style.Font.Size = 16;
                    ws.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.ColorTranslator.FromHtml("#a855f7"));

                    ws.Cells[2, 1].Value = "Bicol University - College of Science (IT Department)";
                    ws.Cells[2, 1].Style.Font.Italic = true;
                    ws.Cells[2, 1].Style.Font.Color.SetColor(System.Drawing.Color.DarkSlateGray);

                    ws.Cells[3, 1].Value = reportType;
                    ws.Cells[3, 1].Style.Font.Bold = true;
                    ws.Cells[3, 1].Style.Font.Size = 13;

                    ws.Cells[4, 1].Value = "Generated: " + DateTime.Now.ToString("MMMM dd, yyyy hh:mm tt");
                    ws.Cells[4, 1].Style.Font.Color.SetColor(System.Drawing.Color.Gray);

                    int colCount = _currentData.Columns.Count;
                    if (colCount > 1)
                    {
                        ws.Cells[1, 1, 1, colCount].Merge = true;
                        ws.Cells[2, 1, 2, colCount].Merge = true;
                        ws.Cells[3, 1, 3, colCount].Merge = true;
                        ws.Cells[4, 1, 4, colCount].Merge = true;
                    }

                    int headerRow = 6;
                    for (int c = 0; c < _currentData.Columns.Count; c++)
                    {
                        var cell = ws.Cells[headerRow, c + 1];
                        cell.Value = _currentData.Columns[c].ColumnName.ToUpper().Replace("_", " ");
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#2A1B3D"));
                        cell.Style.Font.Color.SetColor(System.Drawing.ColorTranslator.FromHtml("#a855f7"));
                        cell.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                        cell.Style.Border.Bottom.Color.SetColor(System.Drawing.ColorTranslator.FromHtml("#a855f7"));
                    }

                    // Data rows
                    for (int r = 0; r < _currentData.Rows.Count; r++)
                    {
                        for (int c = 0; c < _currentData.Columns.Count; c++)
                        {
                            var cell = ws.Cells[r + headerRow + 1, c + 1];
                            cell.Value = _currentData.Rows[r][c]?.ToString() ?? "";
                            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            if (r % 2 == 0)
                            {
                                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.White);
                            }
                            else
                            {
                                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f3e8ff"));
                            }
                        }
                    }

                    int sigRow = _currentData.Rows.Count + headerRow + 3;
                    ws.Cells[sigRow, 1].Value = "Prepared by:";
                    ws.Cells[sigRow, 1].Style.Font.Bold = true;
                    ws.Cells[sigRow + 2, 1].Value = signedBy;
                    ws.Cells[sigRow + 2, 1].Style.Font.Bold = true;
                    ws.Cells[sigRow + 2, 1].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    ws.Cells[sigRow + 2, 1].Style.Border.Bottom.Color.SetColor(System.Drawing.Color.Black);
                    ws.Cells[sigRow + 3, 1].Value = "Authorized Signatory";
                    ws.Cells[sigRow + 3, 1].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
                    ws.Cells[sigRow + 3, 1].Style.Font.Italic = true;

                    ws.Cells.AutoFitColumns();

                    var wsChart = package.Workbook.Worksheets.Add("Summary Chart");
                    wsChart.Cells[1, 1].Value = "Summary for: " + reportType;
                    wsChart.Cells[1, 1].Style.Font.Bold = true;
                    wsChart.Cells[1, 1].Style.Font.Size = 14;
                    wsChart.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.ColorTranslator.FromHtml("#a855f7"));

                    wsChart.Cells[3, 1].Value = "Category";
                    wsChart.Cells[3, 2].Value = "Count";
                    wsChart.Cells[3, 1].Style.Font.Bold = true;
                    wsChart.Cells[3, 2].Style.Font.Bold = true;

                    if (reportType == "Songs Report")
                    {
                        var genreCounts = new System.Collections.Generic.Dictionary<string, int>();
                        foreach (DataRow dr in _currentData.Rows)
                        {
                            string g = dr["genre"]?.ToString() ?? "Unknown";
                            if (string.IsNullOrEmpty(g)) g = "Unknown";
                            genreCounts[g] = genreCounts.ContainsKey(g) ? genreCounts[g] + 1 : 1;
                        }
                        wsChart.Cells[3, 1].Value = "Genre";
                        int row = 4;
                        foreach (var kv in genreCounts)
                        {
                            wsChart.Cells[row, 1].Value = kv.Key;
                            wsChart.Cells[row, 2].Value = kv.Value;
                            row++;
                        }

                        // Add chart
                        var chart = wsChart.Drawings.AddChart("GenreChart", eChartType.BarClustered);
                        chart.Title.Text = "Songs by Genre";
                        chart.SetPosition(4, 0, 4, 0);
                        chart.SetSize(480, 280);
                        var series = chart.Series.Add(wsChart.Cells[4, 2, row - 1, 2], wsChart.Cells[4, 1, row - 1, 1]);
                        series.Header = "Count";
                    }
                    else if (reportType == "Borrowing Report")
                    {
                        var statusCounts = new System.Collections.Generic.Dictionary<string, int>();
                        foreach (DataRow dr in _currentData.Rows)
                        {
                            string st = dr["status"]?.ToString() ?? "unknown";
                            statusCounts[st] = statusCounts.ContainsKey(st) ? statusCounts[st] + 1 : 1;
                        }
                        wsChart.Cells[3, 1].Value = "Status";
                        int row = 4;
                        foreach (var kv in statusCounts)
                        {
                            wsChart.Cells[row, 1].Value = kv.Key;
                            wsChart.Cells[row, 2].Value = kv.Value;
                            row++;
                        }

                        var chart = wsChart.Drawings.AddChart("StatusChart", eChartType.BarClustered);
                        chart.Title.Text = "Borrowing by Status";
                        chart.SetPosition(4, 0, 4, 0);
                        chart.SetSize(480, 280);
                        var series = chart.Series.Add(wsChart.Cells[4, 2, row - 1, 2], wsChart.Cells[4, 1, row - 1, 1]);
                        series.Header = "Count";
                    }
                    else
                    {
                        var ownerCounts = new System.Collections.Generic.Dictionary<string, int>();
                        foreach (DataRow dr in _currentData.Rows)
                        {
                            string o = dr["owner"]?.ToString() ?? "Unknown";
                            if (string.IsNullOrEmpty(o)) o = "Unknown";
                            ownerCounts[o] = ownerCounts.ContainsKey(o) ? ownerCounts[o] + 1 : 1;
                        }
                        wsChart.Cells[3, 1].Value = "Owner";
                        int row = 4;
                        foreach (var kv in ownerCounts)
                        {
                            wsChart.Cells[row, 1].Value = kv.Key;
                            wsChart.Cells[row, 2].Value = kv.Value;
                            row++;
                        }

                        var chart = wsChart.Drawings.AddChart("OwnerChart", eChartType.BarClustered);
                        chart.Title.Text = "Playlists by Owner";
                        chart.SetPosition(4, 0, 4, 0);
                        chart.SetSize(480, 280);
                        var series = chart.Series.Add(wsChart.Cells[4, 2, row - 1, 2], wsChart.Cells[4, 1, row - 1, 1]);
                        series.Header = "Count";
                    }

                    wsChart.Cells.AutoFitColumns();

                    package.SaveAs(new System.IO.FileInfo(path));
                }

                lblStatus.Text = $"✓ Saved to: {path}";
                lblStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                lblStatus.Visibility = Visibility.Visible;
                MessageBox.Show($"Excel report saved successfully!\n{path}", "Export Complete");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
                lblStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
                lblStatus.Visibility = Visibility.Visible;
                MessageBox.Show("Error generating report:\n" + ex.Message, "Error");
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(fileName.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            cleaned = cleaned.Trim().TrimEnd('.');
            return string.IsNullOrWhiteSpace(cleaned) ? "Report" : cleaned;
        }

        private void Nav_Dashboard(object sender, MouseButtonEventArgs e)
            => ((MainWindow)Window.GetWindow(this)).MainContent.Content = new DashboardView();
        private void Nav_Songs(object sender, MouseButtonEventArgs e)
            => ((MainWindow)Window.GetWindow(this)).MainContent.Content = new SongManagementView();
        private void Nav_Playlists(object sender, MouseButtonEventArgs e)
            => ((MainWindow)Window.GetWindow(this)).MainContent.Content = new PlaylistManagementView();
        private void Nav_Borrowing(object sender, MouseButtonEventArgs e)
            => ((MainWindow)Window.GetWindow(this)).MainContent.Content = new BorrowingView();
        private void Nav_Reports(object sender, MouseButtonEventArgs e)
            => ((MainWindow)Window.GetWindow(this)).MainContent.Content = new ReportGeneratorView();
        private void Nav_Users(object sender, MouseButtonEventArgs e)
            => ((MainWindow)Window.GetWindow(this)).MainContent.Content = new UserManagementView();
        private void Nav_Logout(object sender, MouseButtonEventArgs e)
        {
            var main = (MainWindow)Window.GetWindow(this);
            main.ShowMainNav(true);
            main.MainContent.Content = null;
        }
    }
}