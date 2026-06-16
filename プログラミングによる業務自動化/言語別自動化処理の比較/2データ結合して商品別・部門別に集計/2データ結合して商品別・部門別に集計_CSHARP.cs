// =============================================================================
// 2データを結合して商品別・部門別に集計
//
// 処理の流れ:
//   1. Playwright でブラウザを操作し、業務システムへログインする
//   2. 日付範囲（先月1日〜先月末）をフォームに設定する
//   3. 商品マスタと売上データの Excel を 2 つダウンロードする
//   4. ClosedXML で各 Excel を読み込む
//   5. LINQ の Join で 2 データを結合する
//   6. 商品別・部門別にそれぞれ集計する
//   7. ClosedXML で 3 シートの Excel に書き出す
//
// 必要な NuGet パッケージ:
//   - Microsoft.Playwright  (ブラウザ操作)
//   - ClosedXML             (Excel 読み書き)
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Playwright;

// ─────────────────────────────────────────────────────────────
// データ定義
// ─────────────────────────────────────────────────────────────

// 商品マスタ 1 行分
// record にすることで「読み込んだ後は変更しない」ことをコードで表明する
record ProductMaster(
    string ProductCode,   // 商品コード（売上データとの結合キー）
    string ProductName,   // 商品名
    string Department,    // 部門カテゴリ
    int    UnitsPerCase   // 入り数（1ケース何個入りか）
)

// 売上データ 1 行分
record SalesRow(
    string  ReceiptId,    // レシートID（客単価の客数カウントに使用）
    string  OrderDate,    // 売上日
    string  ProductCode,  // 商品コード（商品マスタとの結合キー）
    int     Quantity,     // 販売個数
    decimal Amount        // 売上金額
)

// Join 後の結合データ 1 行分
// 商品マスタと売上データの両方の情報を持つ
record JoinedSales(
    string  ReceiptId,
    string  OrderDate,
    string  ProductCode,
    string  ProductName,
    string  Department,
    int     UnitsPerCase,
    int     Quantity,
    decimal Amount
)

// 商品別集計結果
// LINQ で計算した結果なので class で表す（record との使い分けは前回サンプル参照）
class ProductSummary
{
    public string  ProductCode   { get; init; } = "";
    public string  ProductName   { get; init; } = "";
    public string  Department    { get; init; } = "";
    public decimal TotalAmount   { get; init; }   // 売上金額合計
    public int     OrderCount    { get; init; }   // 件数
    public int     TotalQuantity { get; init; }   // 販売個数
    public int     UnitsPerCase  { get; init; }   // 入り数
    public int     Cases         { get; init; }   // 販売ケース数
    public int     Remainder     { get; init; }   // 端数（ケースに満たない個数）
}

// 部門別集計結果（客単価含む）
class DepartmentSummary
{
    public string  Department     { get; init; } = "";
    public decimal TotalAmount    { get; init; }  // 売上合計
    public int     CustomerCount  { get; init; }  // 客数（ユニークレシートID数）
    public decimal AvgPerCustomer { get; init; }  // 客単価
}

// ─────────────────────────────────────────────────────────────
// ブラウザ操作: Playwright でログイン・日付設定・2ファイルダウンロード
// ─────────────────────────────────────────────────────────────
class BusinessSystemDownloader
{
    private const string LoginUrl          = "https://example.com/login";
    private const string DashboardUrl      = "**/dashboard";
    private const string UserName          = "user@example.com";
    private const string Password          = "password";

    // 先月の期間を計算する（1本目サンプルと同じパターン）
    //
    // タプル戻り値 (DateTime Start, DateTime End):
    //   複数の値をひとつの戻り値にまとめる C# の機能。
    //   呼び出し側では var (start, end) = GetLastMonthRange(); のように受け取れる。
    private static (DateTime Start, DateTime End) GetLastMonthRange()
    {
        var today            = DateTime.Now;
        var firstOfThisMonth = new DateTime(today.Year, today.Month, 1);
        var start            = firstOfThisMonth.AddMonths(-1); // 先月1日
        var end              = firstOfThisMonth.AddDays(-1);   // 先月末
        return (start, end);
    }

    // 2 ファイルをダウンロードして、それぞれのパスをタプルで返す
    public async Task<(string masterPath, string salesPath)> DownloadBothAsync(string saveDirectory)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false
        });
        var page = await browser.NewPageAsync();

        // ── ログイン（1 回だけ行う） ──
        await page.GotoAsync(LoginUrl);
        await page.FillAsync("#username", UserName);
        await page.FillAsync("#password", Password);
        await page.ClickAsync("button[type=submit]");
        await page.WaitForURLAsync(DashboardUrl);

        // ── 日付範囲の設定 ──
        // ToString("yyyy/MM/dd"): DateTime を "2024/05/01" のような文字列に変換する
        // 書式指定子の意味: yyyy=4桁年, MM=2桁月, dd=2桁日
        var (startDate, endDate) = GetLastMonthRange();
        await page.FillAsync("#start-date", startDate.ToString("yyyy/MM/dd"));
        await page.FillAsync("#end-date",   endDate.ToString("yyyy/MM/dd"));
        await page.ClickAsync("#search-btn");
        await page.WaitForSelectorAsync(".result-table");

        // ── 商品マスタのダウンロード ──
        var dl1        = await page.RunAndWaitForDownloadAsync(
            () => page.ClickAsync("#download-master-btn"));
        var masterPath = Path.Combine(saveDirectory, dl1.SuggestedFilename);
        await dl1.SaveAsAsync(masterPath);
        Console.WriteLine($"商品マスタ ダウンロード完了: {masterPath}");

        // ── 売上データのダウンロード ──
        // 同じブラウザセッション内でそのまま 2 つ目のダウンロードを行う
        var dl2       = await page.RunAndWaitForDownloadAsync(
            () => page.ClickAsync("#download-sales-btn"));
        var salesPath = Path.Combine(saveDirectory, dl2.SuggestedFilename);
        await dl2.SaveAsAsync(salesPath);
        Console.WriteLine($"売上データ ダウンロード完了: {salesPath}");

        return (masterPath, salesPath);
    }
}

// ─────────────────────────────────────────────────────────────
// Excel 読み込み: 商品マスタ
// ─────────────────────────────────────────────────────────────
static class MasterExcelParser
{
    public static List<ProductMaster> Parse(string filePath)
    {
        var records = new List<ProductMaster>();
        using var workbook = new XLWorkbook(filePath);
        var sheet   = workbook.Worksheet(1);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            records.Add(new ProductMaster(
                ProductCode:  sheet.Cell(row, 1).GetValue<string>(),
                ProductName:  sheet.Cell(row, 2).GetValue<string>(),
                Department:   sheet.Cell(row, 3).GetValue<string>(),
                UnitsPerCase: sheet.Cell(row, 4).GetValue<int>()
            ));
        }
        Console.WriteLine($"商品マスタ: {records.Count} 件");
        return records;
    }
}

// ─────────────────────────────────────────────────────────────
// Excel 読み込み: 売上データ
// ─────────────────────────────────────────────────────────────
static class SalesExcelParser
{
    public static List<SalesRow> Parse(string filePath)
    {
        var records = new List<SalesRow>();
        using var workbook = new XLWorkbook(filePath);
        var sheet   = workbook.Worksheet(1);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            records.Add(new SalesRow(
                ReceiptId:   sheet.Cell(row, 1).GetValue<string>(),
                OrderDate:   sheet.Cell(row, 2).GetValue<string>(),
                ProductCode: sheet.Cell(row, 3).GetValue<string>(),
                Quantity:    sheet.Cell(row, 4).GetValue<int>(),
                Amount:      sheet.Cell(row, 5).GetValue<decimal>()
            ));
        }
        Console.WriteLine($"売上データ: {records.Count} 件");
        return records;
    }
}

// ─────────────────────────────────────────────────────────────
// Join: LINQ で 2 データを結合する
// ─────────────────────────────────────────────────────────────
static class DataJoiner
{
    public static List<JoinedSales> Join(
        List<SalesRow>      salesRows,
        List<ProductMaster> productMasters)
    {
        // ════════════════════════════════════════════════════════════════
        // LINQ の Join: 共通キーで 2 つのリストを結合する（SQL の INNER JOIN と同じ）
        // ════════════════════════════════════════════════════════════════

        // ── 【LINQ を使わない場合】foreach と Dictionary でルックアップ ──
        //
        // var masterDict = productMasters.ToDictionary(p => p.ProductCode);
        // var joinedList = new List<JoinedSales>();
        // foreach (var s in salesRows)
        // {
        //     if (masterDict.TryGetValue(s.ProductCode, out var m))
        //     {
        //         joinedList.Add(new JoinedSales(
        //             s.ReceiptId, s.OrderDate, s.ProductCode,
        //             m.ProductName, m.Department, m.UnitsPerCase,
        //             s.Quantity, s.Amount));
        //     }
        // }
        // Dictionary の生成・null チェック・要素追加と 3 つの操作が分散してしまう

        // ── 【LINQ Join を使う場合】結合条件と変換を 1 つにまとめられる ──
        //
        // 引数の読み方:
        //   第 1 引数 productMasters: 結合する相手のリスト
        //   第 2 引数 s => s.ProductCode: 売上データ側の結合キー
        //   第 3 引数 p => p.ProductCode: 商品マスタ側の結合キー
        //   第 4 引数 (s, p) => new ...: 結合後に作るオブジェクト
        //
        // ※ INNER JOIN のため商品マスタに存在しない ProductCode の行は除外される
        var joined = salesRows
            .Join(
                productMasters,
                s => s.ProductCode,
                p => p.ProductCode,
                (s, p) => new JoinedSales(
                    ReceiptId:    s.ReceiptId,
                    OrderDate:    s.OrderDate,
                    ProductCode:  s.ProductCode,
                    ProductName:  p.ProductName,
                    Department:   p.Department,
                    UnitsPerCase: p.UnitsPerCase,
                    Quantity:     s.Quantity,
                    Amount:       s.Amount
                )
            )
            .ToList();

        Console.WriteLine($"結合後: {joined.Count} 件");
        return joined;
    }
}

// ─────────────────────────────────────────────────────────────
// 集計: 商品別・部門別に集計する
// ─────────────────────────────────────────────────────────────
static class SalesAggregator
{
    // 商品別集計: 売上合計・件数・個数・ケース数・端数
    public static List<ProductSummary> ByProduct(List<JoinedSales> joined)
    {
        return joined
            .GroupBy(r => r.ProductCode)
            .Select(g =>
            {
                var totalQty     = g.Sum(r => r.Quantity);
                // グループ内は同じ商品なので入り数は全行同じ → First() で取得
                var unitsPerCase = g.First().UnitsPerCase;

                // ケース数と端数の計算
                // C# では int / int の結果は int（小数点以下切り捨て）= ケース数
                // % は剰余演算子: 割り切れなかった余り = 端数
                // 例: 25個、入り数6 → 25 / 6 = 4ケース、25 % 6 = 1個端数
                return new ProductSummary
                {
                    ProductCode   = g.Key,
                    ProductName   = g.First().ProductName,
                    Department    = g.First().Department,
                    TotalAmount   = g.Sum(r => r.Amount),
                    OrderCount    = g.Count(),
                    TotalQuantity = totalQty,
                    UnitsPerCase  = unitsPerCase,
                    Cases         = totalQty / unitsPerCase,
                    Remainder     = totalQty % unitsPerCase
                };
            })
            .OrderByDescending(s => s.TotalAmount)
            .ToList();
    }

    // 部門別集計: 売上合計・客数・客単価
    public static List<DepartmentSummary> ByDepartment(List<JoinedSales> joined)
    {
        return joined
            .GroupBy(r => r.Department)
            .Select(g =>
            {
                var totalAmount = g.Sum(r => r.Amount);

                // 客単価 = 売上合計 ÷ 客数
                // 客数 = ユニークなレシートID の数（同じレシートの複数商品を 1 客として数える）
                //
                // Select で ReceiptId だけを取り出す
                // Distinct で重複を除去する
                // Count で件数を数える
                var customerCount = g.Select(r => r.ReceiptId).Distinct().Count();

                return new DepartmentSummary
                {
                    Department     = g.Key,
                    TotalAmount    = totalAmount,
                    CustomerCount  = customerCount,
                    // ゼロ除算を避けるため customerCount > 0 のときだけ除算する
                    AvgPerCustomer = customerCount > 0 ? totalAmount / customerCount : 0
                };
            })
            .OrderByDescending(d => d.TotalAmount)
            .ToList();
    }
}

// ─────────────────────────────────────────────────────────────
// Excel 出力: ClosedXML で 3 シートに書き出す
// ─────────────────────────────────────────────────────────────
static class ExcelWriter
{
    public static void Write(
        List<JoinedSales>      joinedData,
        List<ProductSummary>   productSummary,
        List<DepartmentSummary> deptSummary,
        string                 outputPath)
    {
        using var workbook = new XLWorkbook();
        WriteJoinedSheet(workbook, joinedData);
        WriteProductSheet(workbook, productSummary);
        WriteDeptSheet(workbook, deptSummary);
        workbook.SaveAs(outputPath);
        Console.WriteLine($"Excel を保存しました: {outputPath}");
    }

    // シート 1: 結合データ（Join 後の全明細）
    private static void WriteJoinedSheet(XLWorkbook workbook, List<JoinedSales> data)
    {
        var sheet = workbook.Worksheets.Add("結合データ");
        string[] headers = { "レシートID", "売上日", "商品コード", "商品名", "部門カテゴリ", "入り数", "販売個数", "売上金額" };
        WriteHeader(sheet, headers, XLColor.SteelBlue);

        for (int i = 0; i < data.Count; i++)
        {
            var r = data[i]; int row = i + 2;
            sheet.Cell(row, 1).Value = r.ReceiptId;
            sheet.Cell(row, 2).Value = r.OrderDate;
            sheet.Cell(row, 3).Value = r.ProductCode;
            sheet.Cell(row, 4).Value = r.ProductName;
            sheet.Cell(row, 5).Value = r.Department;
            sheet.Cell(row, 6).Value = r.UnitsPerCase;
            sheet.Cell(row, 7).Value = r.Quantity;
            sheet.Cell(row, 8).Value = r.Amount;
        }
        sheet.Columns().AdjustToContents();
    }

    // シート 2: 商品別集計
    private static void WriteProductSheet(XLWorkbook workbook, List<ProductSummary> summary)
    {
        var sheet = workbook.Worksheets.Add("商品別集計");
        string[] headers = { "商品コード", "商品名", "部門カテゴリ", "売上合計", "件数", "販売個数", "ケース数", "端数", "入り数" };
        WriteHeader(sheet, headers, XLColor.DarkGreen);

        for (int i = 0; i < summary.Count; i++)
        {
            var s = summary[i]; int row = i + 2;
            sheet.Cell(row, 1).Value = s.ProductCode;
            sheet.Cell(row, 2).Value = s.ProductName;
            sheet.Cell(row, 3).Value = s.Department;
            sheet.Cell(row, 4).Value = s.TotalAmount;
            sheet.Cell(row, 5).Value = s.OrderCount;
            sheet.Cell(row, 6).Value = s.TotalQuantity;
            sheet.Cell(row, 7).Value = s.Cases;
            sheet.Cell(row, 8).Value = s.Remainder;
            sheet.Cell(row, 9).Value = s.UnitsPerCase;
        }

        // 合計行
        int totalRow = summary.Count + 2;
        var label = sheet.Cell(totalRow, 1);
        label.Value = "合計"; label.Style.Font.Bold = true;
        sheet.Cell(totalRow, 4).Value = summary.Sum(s => s.TotalAmount);
        sheet.Cell(totalRow, 5).Value = summary.Sum(s => s.OrderCount);
        sheet.Cell(totalRow, 6).Value = summary.Sum(s => s.TotalQuantity);

        sheet.Columns().AdjustToContents();
    }

    // シート 3: 部門別集計（客単価）
    private static void WriteDeptSheet(XLWorkbook workbook, List<DepartmentSummary> summary)
    {
        var sheet = workbook.Worksheets.Add("部門別集計");
        string[] headers = { "部門カテゴリ", "売上合計", "客数", "客単価" };
        WriteHeader(sheet, headers, XLColor.DarkOrange);

        for (int i = 0; i < summary.Count; i++)
        {
            var s = summary[i]; int row = i + 2;
            sheet.Cell(row, 1).Value = s.Department;
            sheet.Cell(row, 2).Value = s.TotalAmount;
            sheet.Cell(row, 3).Value = s.CustomerCount;
            sheet.Cell(row, 4).Value = s.AvgPerCustomer;
        }

        // 合計行（客単価は合計行には出さない）
        int totalRow = summary.Count + 2;
        var label = sheet.Cell(totalRow, 1);
        label.Value = "合計"; label.Style.Font.Bold = true;
        sheet.Cell(totalRow, 2).Value = summary.Sum(s => s.TotalAmount);
        sheet.Cell(totalRow, 3).Value = summary.Sum(s => s.CustomerCount);

        sheet.Columns().AdjustToContents();
    }

    // ヘッダ行の書き込み（共通処理）
    private static void WriteHeader(IXLWorksheet sheet, string[] headers, XLColor bgColor)
    {
        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = sheet.Cell(1, col);
            cell.Value = headers[col - 1];
            cell.Style.Fill.BackgroundColor = bgColor;
            cell.Style.Font.FontColor       = XLColor.White;
            cell.Style.Font.Bold            = true;
        }
    }
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

        Console.WriteLine("=== 2データを結合して商品別・部門別に集計 ===");

        // 1. ブラウザでログイン・日付設定・2ファイルダウンロード
        Console.WriteLine("\n[1/5] ブラウザでログイン・ダウンロード中...");
        var downloader = new BusinessSystemDownloader();
        var (masterPath, salesPath) = await downloader.DownloadBothAsync(workDir);

        // 2. 商品マスタを読み込む
        Console.WriteLine("\n[2/5] 商品マスタを読み込み中...");
        var masters = MasterExcelParser.Parse(masterPath);

        // 3. 売上データを読み込む
        Console.WriteLine("\n[3/5] 売上データを読み込み中...");
        var salesRows = SalesExcelParser.Parse(salesPath);

        // 4. 2 データを Join する
        Console.WriteLine("\n[4/5] データを結合・集計中...");
        var joined         = DataJoiner.Join(salesRows, masters);
        var productSummary = SalesAggregator.ByProduct(joined);
        var deptSummary    = SalesAggregator.ByDepartment(joined);

        // 集計結果をコンソールに出力する
        Console.WriteLine("\n── 部門別集計（客単価）──");
        foreach (var d in deptSummary)
            Console.WriteLine($"  {d.Department,-12} 売上: {d.TotalAmount,10:N0}円  客数: {d.CustomerCount,5}  客単価: {d.AvgPerCustomer:N0}円");

        // 5. Excel に書き出す
        Console.WriteLine("\n[5/5] Excel に書き出し中...");
        ExcelWriter.Write(joined, productSummary, deptSummary, outputPath);

        Console.WriteLine("\n完了しました。");
    }
}
