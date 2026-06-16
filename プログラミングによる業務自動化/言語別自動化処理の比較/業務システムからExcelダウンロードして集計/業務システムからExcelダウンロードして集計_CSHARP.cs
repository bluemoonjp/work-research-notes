// =============================================================================
// 業務システムからExcelダウンロードして集計
//
// 処理の流れ:
//   1. Playwright でブラウザを操作し、業務システムへログインする
//   2. 売上データ Excel をダウンロードする
//   3. ClosedXML でダウンロードした Excel を読み込み、SalesRecord のリストに変換する
//   4. LINQ でカテゴリ別に集計する
//   5. ClosedXML で集計結果を新しい Excel に書き出す
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

// record = 作成後に値を変更できない不変データ型（C# 9.0 以降）
//
// class との主な違い:
//   - record は一度作成したらプロパティを変更できない → データの誤変更を防ぐ
//   - 値の等値比較が自動で使える（同じ値なら同じオブジェクトとみなせる）
//   - ToString() が自動で見やすい形になる
//
// Excel から読み込んだ売上 1 行分を表す。読み込んだ後は変更しないので record が適切。
record SalesRecord(
    string OrderDate,    // 注文日
    string ProductName,  // 商品名
    string Category,     // カテゴリ
    decimal UnitPrice,   // 単価
    int Quantity         // 数量
)
{
    // record の中にも計算プロパティを追加できる
    // 単価 × 数量 = 合計金額
    public decimal TotalAmount => UnitPrice * Quantity;
}

// LINQ の集計結果を格納するクラス
//
// SalesRecord と異なり class にしている理由:
//   - record はデータの「実体」（注文 1 件）に向いている
//   - class はプログラムが計算・生成した「結果」に向いている
//   - こうすると「これは元データか、計算結果か」がコードを読んだだけで分かる
//
// init: コンストラクタまたはオブジェクト初期化子でのみ設定できる（その後は変更不可）
class CategorySummary
{
    public string Category { get; init; } = "";  // カテゴリ名
    public decimal TotalAmount { get; init; }     // 合計金額
    public int TotalQuantity { get; init; }       // 合計数量
    public int OrderCount { get; init; }          // 注文件数
}

// ─────────────────────────────────────────────────────────────
// ブラウザ操作: Playwright でログインしてダウンロード
// ─────────────────────────────────────────────────────────────
class BusinessSystemDownloader
{
    // 実際の環境では設定ファイルや環境変数から読み込むこと
    private const string LoginUrl     = "https://example.com/login";
    private const string DashboardUrl = "**/dashboard";
    private const string DownloadUrl  = "https://example.com/sales/export";
    private const string UserName     = "user@example.com";
    private const string Password     = "password";

    // 先月の期間（開始日・終了日）を計算して返す
    //
    // タプル (DateTime Start, DateTime End):
    //   C# では複数の値をひとつの戻り値にまとめられる。
    //   out パラメータより書き方がシンプルで、受け取り側も読みやすい。
    private static (DateTime Start, DateTime End) GetLastMonthRange()
    {
        var today = DateTime.Now;

        // 今月1日を起点にして先月の範囲を求める。
        // new DateTime(year, month, day) で任意の日付を作れる。
        var firstOfThisMonth = new DateTime(today.Year, today.Month, 1);

        // AddMonths(-1): 月を1つ戻す。月末の計算を自分でしなくてよい。
        // AddDays(-1):   今月1日の前日 = 先月末。うるう年や月ごとの日数差を意識しなくてよい。
        var start = firstOfThisMonth.AddMonths(-1); // 先月1日
        var end   = firstOfThisMonth.AddDays(-1);   // 先月末

        return (start, end);
    }

    // async Task<string> = 非同期メソッド。完了後に string を返す。
    // Playwright は「await」を使う非同期処理が前提になっている。
    // await = その処理が完了するまで次の行に進まずに待つ、という意味。
    public async Task<string> DownloadAsync(string saveDirectory)
    {
        // Playwright の初期化（ブラウザを操作するための仕組みを準備する）
        using var playwright = await Playwright.CreateAsync();

        // Chromium ブラウザを起動する
        // Headless = false にするとブラウザが画面に表示される（動作確認に便利）
        // Headless = true にするとバックグラウンドで動作する（本番運用向け）
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false
        });

        var page = await browser.NewPageAsync();

        // ── ログイン ──
        // GotoAsync: 指定した URL をブラウザで開く
        await page.GotoAsync(LoginUrl);

        // FillAsync: CSS セレクタで要素を指定し、テキストを入力する
        await page.FillAsync("#username", UserName);
        await page.FillAsync("#password", Password);

        // ClickAsync: 指定した要素をクリックする
        await page.ClickAsync("button[type=submit]");

        // WaitForURLAsync: URL がパターンに一致するまで待機する
        // ポーリング（一定間隔で確認）ではなくブラウザのイベントを監視するため無駄がない
        await page.WaitForURLAsync(DashboardUrl);

        // ── 日付範囲の設定 ──
        // 先月の期間を計算し、ToString で文字列に変換してフォームに入力する。
        // "yyyy/MM/dd" は書式文字列: yyyy=年4桁, MM=月2桁, dd=日2桁
        // 例: 2024年5月1日 → "2024/05/01"（書式はシステムに合わせて変更すること）
        var (startDate, endDate) = GetLastMonthRange();
        await page.FillAsync("#start-date", startDate.ToString("yyyy/MM/dd"));
        await page.FillAsync("#end-date",   endDate.ToString("yyyy/MM/dd"));
        await page.ClickAsync("#search-btn");
        // WaitForSelectorAsync: 指定した要素が DOM に現れるまで待機する
        await page.WaitForSelectorAsync(".result-table");

        // ── ダウンロード ──
        // RunAndWaitForDownloadAsync: クリックとダウンロード完了の待機を同時に行う
        // Selenium と異なりダウンロード先フォルダの事前設定が不要で、
        // ダウンロードファイルをプログラムから直接扱える
        var download = await page.RunAndWaitForDownloadAsync(async () =>
        {
            await page.GotoAsync(DownloadUrl);
        });

        // ダウンロードしたファイルを指定フォルダに保存する
        // SuggestedFilename: サーバーが提示したファイル名（Content-Disposition ヘッダから取得）
        var filePath = Path.Combine(saveDirectory, download.SuggestedFilename);
        await download.SaveAsAsync(filePath);

        Console.WriteLine($"ダウンロード完了: {filePath}");
        return filePath;
    }
}

// ─────────────────────────────────────────────────────────────
// Excel 読み込み: ClosedXML でダウンロードした Excel を解析
// ─────────────────────────────────────────────────────────────
static class ExcelParser
{
    // ダウンロードした Excel ファイルを SalesRecord のリストに変換する
    public static List<SalesRecord> Parse(string filePath)
    {
        var records = new List<SalesRecord>();

        // using: ファイルを使い終わったら自動でリソースを解放する（ファイルのロックを防ぐ）
        using var workbook = new XLWorkbook(filePath);

        // 最初のシートを取得する（ClosedXML のシート番号は 1 始まり）
        var sheet = workbook.Worksheet(1);

        // データが入っている最終行を自動検出する
        // ?? 1 は「null なら 1 を使う」という意味（データがゼロ行の場合の保険）
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        // 1 行目はヘッダなので 2 行目からループする
        for (int row = 2; row <= lastRow; row++)
        {
            // Cell(行番号, 列番号).GetValue<型>() でセルの値を指定した型で取得する
            var record = new SalesRecord(
                OrderDate:   sheet.Cell(row, 1).GetValue<string>(),
                ProductName: sheet.Cell(row, 2).GetValue<string>(),
                Category:    sheet.Cell(row, 3).GetValue<string>(),
                UnitPrice:   sheet.Cell(row, 4).GetValue<decimal>(),
                Quantity:    sheet.Cell(row, 5).GetValue<int>()
            );
            records.Add(record);
        }

        Console.WriteLine($"{records.Count} 件のデータを読み込みました");
        return records;
    }
}

// ─────────────────────────────────────────────────────────────
// 集計: LINQ でカテゴリ別に集計する
// ─────────────────────────────────────────────────────────────
static class SalesAggregator
{
    public static List<CategorySummary> Aggregate(List<SalesRecord> records)
    {
        // ════════════════════════════════════════════════════════════════
        // LINQ の利点: カテゴリ別の合計金額・数量・件数を一度に集計する
        // ════════════════════════════════════════════════════════════════

        // ── 【LINQ を使わない場合】ループと Dictionary で書くと冗長になる ──
        //
        // var totalDict    = new Dictionary<string, decimal>();
        // var quantityDict = new Dictionary<string, int>();
        // var countDict    = new Dictionary<string, int>();
        //
        // foreach (var r in records)
        // {
        //     if (!totalDict.ContainsKey(r.Category))
        //     {
        //         totalDict[r.Category]    = 0;
        //         quantityDict[r.Category] = 0;
        //         countDict[r.Category]    = 0;
        //     }
        //     totalDict[r.Category]    += r.TotalAmount;
        //     quantityDict[r.Category] += r.Quantity;
        //     countDict[r.Category]    += 1;
        // }
        // さらに並べ替えや CategorySummary への変換も別途必要になる

        // ── 【LINQ を使う場合】操作を連鎖させて簡潔に書ける ──
        //
        // GroupBy:          同じ Category を持つレコードをグループにまとめる
        //                   例: { "電子機器": [record1, record3], "食品": [record2] }
        // Select:           各グループを別の型に変換する（ここでは CategorySummary に変換）
        //                   g.Key = グループのキー（カテゴリ名）
        // g.Sum(r => ...):  グループ内のレコードの指定プロパティを合計する
        // g.Count():        グループ内のレコード件数を返す
        // OrderByDescending: 指定プロパティの降順（大きい順）に並べる
        // ToList():         ここまでの処理を実行し、結果を List に確定させる
        var summary = records
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

        return summary;
    }
}

// ─────────────────────────────────────────────────────────────
// Excel 出力: ClosedXML で集計結果を書き出す
// ─────────────────────────────────────────────────────────────
static class ExcelWriter
{
    public static void Write(
        List<SalesRecord> records,
        List<CategorySummary> summary,
        string outputPath)
    {
        // 新しい Excel ブックを作成する
        using var workbook = new XLWorkbook();

        WriteRawDataSheet(workbook, records);
        WriteSummarySheet(workbook, summary);

        workbook.SaveAs(outputPath);
        Console.WriteLine($"Excel を保存しました: {outputPath}");
    }

    // シート 1: 生データ（ダウンロードした全レコードをそのまま転記）
    private static void WriteRawDataSheet(XLWorkbook workbook, List<SalesRecord> records)
    {
        var sheet = workbook.Worksheets.Add("生データ");

        // ヘッダ行を書き込む
        string[] headers = { "注文日", "商品名", "カテゴリ", "単価", "数量", "合計金額" };
        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = sheet.Cell(1, col);
            cell.Value = headers[col - 1];
            cell.Style.Fill.BackgroundColor = XLColor.SteelBlue;
            cell.Style.Font.FontColor       = XLColor.White;
            cell.Style.Font.Bold            = true;
        }

        // データ行を書き込む（2 行目から）
        for (int i = 0; i < records.Count; i++)
        {
            var r   = records[i];
            int row = i + 2;
            sheet.Cell(row, 1).Value = r.OrderDate;
            sheet.Cell(row, 2).Value = r.ProductName;
            sheet.Cell(row, 3).Value = r.Category;
            sheet.Cell(row, 4).Value = r.UnitPrice;
            sheet.Cell(row, 5).Value = r.Quantity;
            sheet.Cell(row, 6).Value = r.TotalAmount;
        }

        // 列幅をセルの内容に合わせて自動調整する
        sheet.Columns().AdjustToContents();
    }

    // シート 2: カテゴリ集計
    private static void WriteSummarySheet(XLWorkbook workbook, List<CategorySummary> summary)
    {
        var sheet = workbook.Worksheets.Add("カテゴリ集計");

        // ヘッダ行
        string[] headers = { "カテゴリ", "合計金額", "合計数量", "注文件数" };
        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = sheet.Cell(1, col);
            cell.Value = headers[col - 1];
            cell.Style.Fill.BackgroundColor = XLColor.DarkGreen;
            cell.Style.Font.FontColor       = XLColor.White;
            cell.Style.Font.Bold            = true;
        }

        // データ行
        for (int i = 0; i < summary.Count; i++)
        {
            var s   = summary[i];
            int row = i + 2;
            sheet.Cell(row, 1).Value = s.Category;
            sheet.Cell(row, 2).Value = s.TotalAmount;
            sheet.Cell(row, 3).Value = s.TotalQuantity;
            sheet.Cell(row, 4).Value = s.OrderCount;
        }

        // 合計行を末尾に追加する
        // LINQ の Sum でリスト全体の合計を計算する
        int totalRow = summary.Count + 2;
        var totalLabel = sheet.Cell(totalRow, 1);
        totalLabel.Value          = "合計";
        totalLabel.Style.Font.Bold = true;
        sheet.Cell(totalRow, 2).Value = summary.Sum(s => s.TotalAmount);
        sheet.Cell(totalRow, 3).Value = summary.Sum(s => s.TotalQuantity);
        sheet.Cell(totalRow, 4).Value = summary.Sum(s => s.OrderCount);

        sheet.Columns().AdjustToContents();
    }
}

// ─────────────────────────────────────────────────────────────
// エントリポイント
// ─────────────────────────────────────────────────────────────
class Program
{
    // async Task Main = Playwright など非同期処理を使う場合のエントリポイントの書き方
    // 通常の void Main と異なり、await を使った非同期処理を呼び出せる
    static async Task Main(string[] args)
    {
        // 実行ファイルと同じフォルダをダウンロード先・出力先にする
        var workDir    = AppContext.BaseDirectory;
        var outputPath = Path.Combine(workDir, "集計結果.xlsx");

        Console.WriteLine("=== 業務システムからExcelダウンロードして集計 ===");

        // 1. ブラウザでログインしてダウンロード
        Console.WriteLine("\n[1/4] ブラウザでログイン・ダウンロード中...");
        var downloader         = new BusinessSystemDownloader();
        var downloadedFilePath = await downloader.DownloadAsync(workDir);

        // 2. ダウンロードした Excel を読み込む
        Console.WriteLine("\n[2/4] Excel を読み込み中...");
        var records = ExcelParser.Parse(downloadedFilePath);

        // 3. LINQ でカテゴリ別に集計する
        Console.WriteLine("\n[3/4] 集計中...");
        var summary = SalesAggregator.Aggregate(records);

        // 集計結果をコンソールにも出力する
        Console.WriteLine("\nカテゴリ別集計結果:");
        foreach (var s in summary)
        {
            Console.WriteLine($"  {s.Category,-15} 合計金額: {s.TotalAmount,10:N0}円  件数: {s.OrderCount}");
        }

        // 4. Excel に書き出す
        Console.WriteLine("\n[4/4] Excel に書き出し中...");
        ExcelWriter.Write(records, summary, outputPath);

        Console.WriteLine("\n完了しました。");
    }
}
