// ============================================================
// ブラウザ操作データをExcelに転記するサンプル (C#)
// ============================================================
// 前提パッケージ（NuGet）:
//   Selenium.WebDriver
//   Selenium.WebDriver.ChromeDriver
//   EPPlus (6.x 以降は非商用ライセンス LicenseContext 設定が必要)
//
// COM参照（プロジェクト参照に追加）:
//   Microsoft Excel xx.x Object Library
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using OfficeOpenXml;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace BrowserToExcelSample
{
    // データ行を表す値オブジェクト。
    // string[] の配列渡しより列の意味が明確になるため class 化した。
    internal sealed class RecordRow
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Status { get; init; } = "";
        public string UpdatedAt { get; init; } = "";
    }

    public static class BrowserToExcelSample
    {
        // ---- 定数 -------------------------------------------------------

        // ダミー URL・セレクタ（実環境に合わせて差し替える）
        private const string LoginUrl = "https://intranet.example.local/login";
        private const string ListUrl  = "https://intranet.example.local/items/list";

        private const string UsernameSelector = "#username";
        private const string PasswordSelector = "#password";
        private const string LoginButtonSelector = "button[type='submit']";
        private const string TableSelector = "table#item-list";
        private const string TableRowSelector = "table#item-list tbody tr";

        // ---- エントリポイント -------------------------------------------

        public static async Task Main(string[] args)
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"output_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            // ① ブラウザ操作でデータ取得
            var records = await FetchRecordsAsync("user01", "password01");

            Console.WriteLine($"{records.Count} 件取得しました。");

            // ② Excel 書き込み（2 通りを切り替えられるようにしてある）
            WriteWithEPPlus(records, outputPath);
            // WriteWithInterop(records, outputPath);  // COM版はこちら

            Console.WriteLine($"書き込み完了: {outputPath}");
        }

        // ---- ブラウザ操作 -----------------------------------------------

        private static async Task<List<RecordRow>> FetchRecordsAsync(
            string username, string password)
        {
            // ChromeOptions でヘッドレスにすると CI や定時実行でも使いやすい
            var options = new ChromeOptions();
            options.AddArgument("--headless=new");
            options.AddArgument("--no-sandbox");

            using var driver = new ChromeDriver(options);

            // WebDriverWait は暗黙的待機より要素単位で制御できるため明示的に使う
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

            // ログイン
            await LoginAsync(driver, wait, username, password);

            // 一覧ページへ遷移してテーブル取得
            driver.Navigate().GoToUrl(ListUrl);
            wait.Until(d => d.FindElement(By.CssSelector(TableSelector)));

            return ParseTable(driver);
        }

        private static async Task LoginAsync(
            IWebDriver driver, WebDriverWait wait,
            string username, string password)
        {
            driver.Navigate().GoToUrl(LoginUrl);

            // ログインフォームが描画されるまで待つ
            wait.Until(d => d.FindElement(By.CssSelector(UsernameSelector)));

            driver.FindElement(By.CssSelector(UsernameSelector)).SendKeys(username);
            driver.FindElement(By.CssSelector(PasswordSelector)).SendKeys(password);
            driver.FindElement(By.CssSelector(LoginButtonSelector)).Click();

            // ページ遷移完了を URL 変化で判定
            wait.Until(d => !d.Url.Contains("/login"));

            // ログイン後処理を非同期で挟める余地として await を入れている
            await Task.CompletedTask;
        }

        private static List<RecordRow> ParseTable(IWebDriver driver)
        {
            // LINQ で tbody の各行をオブジェクトに射影する
            // VBA のようなセル単位ループより意図が読みやすい
            return driver
                .FindElements(By.CssSelector(TableRowSelector))
                .Select(row =>
                {
                    var cells = row.FindElements(By.TagName("td"));
                    return new RecordRow
                    {
                        Id        = CellText(cells, 0),
                        Name      = CellText(cells, 1),
                        Status    = CellText(cells, 2),
                        UpdatedAt = CellText(cells, 3),
                    };
                })
                .ToList();
        }

        private static string CellText(IReadOnlyList<IWebElement> cells, int index)
            => index < cells.Count ? cells[index].Text.Trim() : "";

        // ---- Excel 書き込み: EPPlus 版 ----------------------------------

        private static void WriteWithEPPlus(List<RecordRow> records, string path)
        {
            // 非商用利用の場合は NonCommercial を設定する（商用は Commercial）
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("データ");

            // ヘッダ行
            ws.Cells[1, 1].Value = "ID";
            ws.Cells[1, 2].Value = "名前";
            ws.Cells[1, 3].Value = "ステータス";
            ws.Cells[1, 4].Value = "更新日時";

            // データ行: LoadFromCollection を使うと DTO の各プロパティを自動展開できるが、
            // 列順を明示したいため手動で書いている
            for (int i = 0; i < records.Count; i++)
            {
                int row = i + 2;
                var rec = records[i];
                ws.Cells[row, 1].Value = rec.Id;
                ws.Cells[row, 2].Value = rec.Name;
                ws.Cells[row, 3].Value = rec.Status;
                ws.Cells[row, 4].Value = rec.UpdatedAt;
            }

            // 列幅の自動調整
            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var file = new FileInfo(path);
            package.SaveAs(file);
        }

        // ---- Excel 書き込み: COM Interop 版 -----------------------------

        private static void WriteWithInterop(List<RecordRow> records, string path)
        {
            Microsoft.Office.Interop.Excel.Application? excel = null;
            Workbook? wb = null;

            try
            {
                excel = new Microsoft.Office.Interop.Excel.Application
                {
                    Visible = false,
                    DisplayAlerts = false,
                };

                wb = excel.Workbooks.Add();
                var ws = (Worksheet)wb.Worksheets[1];
                ws.Name = "データ";

                // ヘッダ
                ws.Cells[1, 1] = "ID";
                ws.Cells[1, 2] = "名前";
                ws.Cells[1, 3] = "ステータス";
                ws.Cells[1, 4] = "更新日時";

                // COM の Range オブジェクトへまとめて配置する方が
                // セル単位 Set よりも桁違いに速い（数百行で顕著になる）
                var data = new object[records.Count, 4];
                for (int i = 0; i < records.Count; i++)
                {
                    data[i, 0] = records[i].Id;
                    data[i, 1] = records[i].Name;
                    data[i, 2] = records[i].Status;
                    data[i, 3] = records[i].UpdatedAt;
                }

                var startCell = ws.Cells[2, 1];
                var endCell   = ws.Cells[records.Count + 1, 4];
                var range     = ws.Range[startCell, endCell];
                range.Value2  = data;  // 配列一括代入でラウンドトリップを最小化

                // 列幅自動調整
                ws.UsedRange.EntireColumn.AutoFit();

                wb.SaveAs(path,
                    XlFileFormat.xlOpenXMLWorkbook,
                    Type.Missing, Type.Missing, false, false,
                    XlSaveAsAccessMode.xlNoChange,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            }
            finally
            {
                // COM オブジェクトは GC に任せず明示的に解放しないとプロセスが残る
                wb?.Close(false);
                excel?.Quit();
                if (wb    != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(wb);
                if (excel != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(excel);
            }
        }
    }
}
