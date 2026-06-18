// =============================================================================
// 業務システムからExcelダウンロードして集計 [バリアント 2/2]
// ─ Windows DPAPI によるローカル暗号化資格情報管理版 ─
//
// 処理の流れ:
//   0. 初回のみ: コンソールで ID・パスワードを入力し、DPAPI で暗号化して PC に保存する
//   1. Playwright でブラウザを操作し、業務システムへログインする
//   2. 売上データ Excel をダウンロードする
//   3. ClosedXML でダウンロードした Excel を読み込み、SalesRecord のリストに変換する
//   4. LINQ でカテゴリ別に集計する
//   5. ClosedXML で集計結果を新しい Excel に書き出す
//
// 1番との違い:
//   - 1番: ログイン ID・パスワードをコード内の定数として管理する
//   - 2番: 初回起動時に入力した ID・パスワードを Windows DPAPI で暗号化して
//          この PC に保存する。以降の実行では保存済みデータから自動復号する。
//
// 必要な NuGet パッケージ:
//   - Microsoft.Playwright                         (ブラウザ操作)
//   - ClosedXML                                    (Excel 読み書き)
//   - System.Security.Cryptography.ProtectedData   (Windows DPAPI)
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Playwright;

// ─────────────────────────────────────────────────────────────
// データ定義（1番と同一）
// ─────────────────────────────────────────────────────────────

record SalesRecord(
    string  OrderDate,
    string  ProductName,
    string  Category,
    decimal UnitPrice,
    int     Quantity
)
{
    public decimal TotalAmount => UnitPrice * Quantity;
}

class CategorySummary
{
    public string  Category      { get; init; } = "";
    public decimal TotalAmount   { get; init; }
    public int     TotalQuantity { get; init; }
    public int     OrderCount    { get; init; }
}

// ─────────────────────────────────────────────────────────────
// エントリポイント
// ─────────────────────────────────────────────────────────────
class Program
{
    static async Task Main(string[] args)
    {
        var workDir    = AppContext.BaseDirectory;
        var outputPath = Path.Combine(workDir, "集計結果.xlsx");

        Console.WriteLine("=== 業務システムからExcelダウンロードして集計 (DPAPI版) ===");

        // 0. 認証情報の取得（初回のみ入力を求める）
        // CredentialStore.Exists() = 暗号化済みファイルが存在するかを確認する
        // ファイルがなければ初回起動とみなし、入力を求めて暗号化保存する
        if (!CredentialStore.Exists())
        {
            Console.WriteLine("\n初回のみ認証情報を入力してください（暗号化してこの PC に保存されます）。");
            Console.WriteLine("次回以降は自動で読み込まれます。変更したい場合は credentials.dat を削除してください。\n");

            Console.Write("ログイン ID : ");
            var inputUser = Console.ReadLine()!;

            Console.Write("パスワード  : ");
            var inputPass = ReadMaskedPassword();

            CredentialStore.Save(inputUser, inputPass);
            Console.WriteLine("\n認証情報を暗号化して保存しました。\n");
        }

        var (userName, password) = CredentialStore.Load();

        // 1. ブラウザでログインしてダウンロード
        Console.WriteLine("\n[1/4] ブラウザでログイン・ダウンロード中...");
        var downloader         = new BusinessSystemDownloader(userName, password);
        var downloadedFilePath = await downloader.DownloadAsync(workDir);

        // 2. ダウンロードした Excel を読み込む
        Console.WriteLine("\n[2/4] Excel を読み込み中...");
        var records = ExcelParser.Parse(downloadedFilePath);

        // 3. LINQ でカテゴリ別に集計する
        Console.WriteLine("\n[3/4] 集計中...");
        var summary = SalesAggregator.Aggregate(records);

        Console.WriteLine("\nカテゴリ別集計結果:");
        foreach (var s in summary)
            Console.WriteLine($"  {s.Category,-15} 合計金額: {s.TotalAmount,10:N0}円  件数: {s.OrderCount}");

        // 4. Excel に書き出す
        Console.WriteLine("\n[4/4] Excel に書き出し中...");
        ExcelWriter.Write(records, summary, outputPath);

        Console.WriteLine("\n完了しました。");
    }

    // パスワード入力時に文字を「*」でマスク表示する
    //
    // Console.ReadKey(intercept: true):
    //   intercept = true にするとキー入力をコンソールに表示しない
    //   戻り値の ConsoleKeyInfo からキーの種類と文字を取得できる
    private static string ReadMaskedPassword()
    {
        var sb = new StringBuilder();
        ConsoleKeyInfo key;

        while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
            {
                // Backspace: 最後の文字を削除し、画面の「*」も消す
                sb.Remove(sb.Length - 1, 1);
                Console.Write("\b \b"); // カーソルを1つ戻して空白で上書きし、さらに1つ戻す
            }
            else if (key.Key != ConsoleKey.Backspace)
            {
                sb.Append(key.KeyChar);
                Console.Write("*");
            }
        }

        Console.WriteLine();
        return sb.ToString();
    }
}

// ─────────────────────────────────────────────────────────────
// 資格情報の暗号化保存・読み込み: Windows DPAPI を使用
// ─────────────────────────────────────────────────────────────
static class CredentialStore
{
    // 保存先: %APPDATA%\SalesAutoTool\credentials.dat
    //
    // %APPDATA% はユーザーごとに異なるフォルダ（例: C:\Users\yamada\AppData\Roaming）
    // ユーザーが違えば保存先が異なるため、他ユーザーのファイルを読み込めない
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SalesAutoTool",
        "credentials.dat");

    // アプリ固有のエントロピー（DPAPI の暗号強度を高める追加データ）
    //
    // DPAPI は Windows ユーザーのキーで暗号化するが、エントロピーを加えることで
    // 「このアプリが保存したデータ」以外は復号できなくなる。
    // 同じ Windows ユーザーでも、エントロピーが異なれば復号失敗になる。
    private static readonly byte[] Entropy =
        { 0x53, 0x61, 0x6C, 0x65, 0x73, 0x41, 0x75, 0x74, 0x6F };

    public static bool Exists() => File.Exists(FilePath);

    public static void Save(string userName, string password)
    {
        // 保存形式: "ユーザー名\nパスワード" を UTF-8 バイト列に変換してから暗号化する
        var plain = Encoding.UTF8.GetBytes($"{userName}\n{password}");

        // ProtectedData.Protect:
        //   DataProtectionScope.CurrentUser = 現在の Windows ユーザーのみが復号できる
        //   同じ PC でも別の Windows ユーザーでログインした場合は復号不可
        //   credentials.dat を別 PC にコピーしても復号不可
        var encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);

        // ディレクトリが存在しない場合は作成してからファイルを書き込む
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllBytes(FilePath, encrypted);

        Console.WriteLine($"保存先: {FilePath}");
    }

    public static (string UserName, string Password) Load()
    {
        var encrypted = File.ReadAllBytes(FilePath);

        // ProtectedData.Unprotect:
        //   暗号化時と同じユーザー・同じエントロピーでのみ復号できる
        var plain = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);

        // Split('\n', 2): 最初の改行文字でのみ分割し、最大 2 要素にする
        // パスワードに改行文字が含まれていた場合の誤分割を防ぐ
        var parts = Encoding.UTF8.GetString(plain).Split('\n', 2);
        return (parts[0], parts[1]);
    }
}

// ─────────────────────────────────────────────────────────────
// ブラウザ操作: Playwright でログインしてダウンロード
// ─────────────────────────────────────────────────────────────

// 1番との違い: UserName・Password の定数を廃止し、コンストラクタで受け取る
//
// class ClassName(型 引数名) の書き方は「プライマリコンストラクタ」（C# 12 以降）
// コンストラクタの引数がそのままクラス全体で使えるフィールドになる
class BusinessSystemDownloader(string userName, string password)
{
    private const string LoginUrl     = "https://example.com/login";
    private const string DashboardUrl = "**/dashboard";
    private const string DownloadUrl  = "https://example.com/sales/export";

    private static (DateTime Start, DateTime End) GetLastMonthRange()
    {
        var today            = DateTime.Now;
        var firstOfThisMonth = new DateTime(today.Year, today.Month, 1);
        var start            = firstOfThisMonth.AddMonths(-1);
        var end              = firstOfThisMonth.AddDays(-1);
        return (start, end);
    }

    public async Task<string> DownloadAsync(string saveDirectory)
    {
        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false
        });

        var page = await browser.NewPageAsync();

        // ── ログイン ──
        await page.GotoAsync(LoginUrl);
        await page.FillAsync("#username", userName);    // コンストラクタ引数を使用
        await page.FillAsync("#password", password);    // コンストラクタ引数を使用
        await page.ClickAsync("button[type=submit]");
        await page.WaitForURLAsync(DashboardUrl);

        // ── 日付範囲の設定 ──
        var (startDate, endDate) = GetLastMonthRange();
        await page.FillAsync("#start-date", startDate.ToString("yyyy/MM/dd"));
        await page.FillAsync("#end-date",   endDate.ToString("yyyy/MM/dd"));
        await page.ClickAsync("#search-btn");
        await page.WaitForSelectorAsync(".result-table");

        // ── ダウンロード ──
        var download = await page.RunAndWaitForDownloadAsync(async () =>
        {
            await page.GotoAsync(DownloadUrl);
        });

        var filePath = Path.Combine(saveDirectory, download.SuggestedFilename);
        await download.SaveAsAsync(filePath);

        Console.WriteLine($"ダウンロード完了: {filePath}");
        return filePath;
    }
}

// ─────────────────────────────────────────────────────────────
// Excel 読み込み・集計・出力（1番と同一）
// ─────────────────────────────────────────────────────────────
static class ExcelParser
{
    public static List<SalesRecord> Parse(string filePath)
    {
        var records = new List<SalesRecord>();

        using var workbook = new XLWorkbook(filePath);
        var sheet   = workbook.Worksheet(1);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            records.Add(new SalesRecord(
                OrderDate:   sheet.Cell(row, 1).GetValue<string>(),
                ProductName: sheet.Cell(row, 2).GetValue<string>(),
                Category:    sheet.Cell(row, 3).GetValue<string>(),
                UnitPrice:   sheet.Cell(row, 4).GetValue<decimal>(),
                Quantity:    sheet.Cell(row, 5).GetValue<int>()
            ));
        }

        Console.WriteLine($"{records.Count} 件のデータを読み込みました");
        return records;
    }
}

static class SalesAggregator
{
    public static List<CategorySummary> Aggregate(List<SalesRecord> records)
    {
        return records
            .GroupBy(r => r.Category)
            .Select(g => new CategorySummary
            {
                Category      = g.Key,
                TotalAmount   = g.Sum(r => r.TotalAmount),
                TotalQuantity = g.Sum(r => r.Quantity),
                OrderCount    = g.Count()
            })
            .OrderByDescending(s => s.TotalAmount)
            .ToList();
    }
}

static class ExcelWriter
{
    public static void Write(
        List<SalesRecord>     records,
        List<CategorySummary> summary,
        string                outputPath)
    {
        using var workbook = new XLWorkbook();

        WriteRawDataSheet(workbook, records);
        WriteSummarySheet(workbook, summary);

        workbook.SaveAs(outputPath);
        Console.WriteLine($"Excel を保存しました: {outputPath}");
    }

    private static void WriteRawDataSheet(XLWorkbook workbook, List<SalesRecord> records)
    {
        var sheet = workbook.Worksheets.Add("生データ");
        WriteHeader(sheet, new[] { "注文日", "商品名", "カテゴリ", "単価", "数量", "合計金額" }, XLColor.SteelBlue);

        for (int i = 0; i < records.Count; i++)
        {
            var r = records[i]; int row = i + 2;
            sheet.Cell(row, 1).Value = r.OrderDate;
            sheet.Cell(row, 2).Value = r.ProductName;
            sheet.Cell(row, 3).Value = r.Category;
            sheet.Cell(row, 4).Value = r.UnitPrice;
            sheet.Cell(row, 5).Value = r.Quantity;
            sheet.Cell(row, 6).Value = r.TotalAmount;
        }

        sheet.Columns().AdjustToContents();
    }

    private static void WriteSummarySheet(XLWorkbook workbook, List<CategorySummary> summary)
    {
        var sheet = workbook.Worksheets.Add("カテゴリ集計");
        WriteHeader(sheet, new[] { "カテゴリ", "合計金額", "合計数量", "注文件数" }, XLColor.DarkGreen);

        for (int i = 0; i < summary.Count; i++)
        {
            var s = summary[i]; int row = i + 2;
            sheet.Cell(row, 1).Value = s.Category;
            sheet.Cell(row, 2).Value = s.TotalAmount;
            sheet.Cell(row, 3).Value = s.TotalQuantity;
            sheet.Cell(row, 4).Value = s.OrderCount;
        }

        int totalRow = summary.Count + 2;
        var label = sheet.Cell(totalRow, 1);
        label.Value = "合計"; label.Style.Font.Bold = true;
        sheet.Cell(totalRow, 2).Value = summary.Sum(s => s.TotalAmount);
        sheet.Cell(totalRow, 3).Value = summary.Sum(s => s.TotalQuantity);
        sheet.Cell(totalRow, 4).Value = summary.Sum(s => s.OrderCount);

        sheet.Columns().AdjustToContents();
    }

    private static void WriteHeader(IXLWorksheet sheet, string[] headers, XLColor bgColor)
    {
        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = sheet.Cell(1, col);
            cell.Value                      = headers[col - 1];
            cell.Style.Fill.BackgroundColor = bgColor;
            cell.Style.Font.FontColor       = XLColor.White;
            cell.Style.Font.Bold            = true;
        }
    }
}
