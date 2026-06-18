// =============================================================================
// ジョイントグループ化 — 注文と商品マスタを結合して販売年月×商品コード別に集計する
//
// 処理の流れ:
//   1. データ.xlsx の「商品マスタ」「注文」シートを読み込む
//   2. LINQ の Join で 2 シートを商品コードで結合する（INNER JOIN）
//   3. 販売年月（注文日の yyyy/MM）と商品コードでグループ化して集計する
//   4. 販売年月昇順・利益額降順に並べ替える
//   5. 結果を同じブック内の「販売集計」シートに書き出す
//
// 必要な NuGet パッケージ:
//   - ClosedXML             (Excel 読み書き)
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

// ─────────────────────────────────────────────────────────────
// データ定義
// ─────────────────────────────────────────────────────────────

// Excel から読み込んだ商品マスタ 1 行分
// record にすることで「読み込んだ後は変更しない」ことをコードで表明する
record ProductMaster(
    string   商品コード,
    string   商品名,
    string   型番,
    DateTime 発売日,
    string   仕入元,
    string   調達区分,
    decimal  単品原価,
    decimal  単品売価
);

// Excel から読み込んだ注文 1 行分
record OrderRow(
    string   注文番号,
    DateTime 注文日,
    string   商品コード,
    int      数量
);

// Join 後の結合データ 1 行分
// 商品マスタと注文の両方の情報を持つ中間データ
record JoinedOrder(
    string   注文番号,
    DateTime 注文日,
    string   商品コード,
    string   商品名,
    decimal  単品原価,
    decimal  単品売価,
    int      数量
);

// 販売年月 × 商品コード別の集計結果
// LINQ で計算した結果なので class で表す
// （record は「読み込んだ不変データ」、class は「計算して作ったデータ」として使い分ける）
class SalesSummary
{
    public string  販売年月   { get; init; } = "";
    public string  商品コード { get; init; } = "";
    public string  商品名     { get; init; } = "";
    public decimal 単品原価   { get; init; }
    public decimal 単品売価   { get; init; }
    public int     売上個数   { get; init; }
    public decimal 売上金額   { get; init; }
    public decimal 原価       { get; init; }
    public decimal 利益額     { get; init; }
    public decimal 利益率     { get; init; }  // 0.35 = 35%
}

// ─────────────────────────────────────────────────────────────
// エントリポイント
// ─────────────────────────────────────────────────────────────
class Program
{
    static void Main(string[] args)
    {
        // AppContext.BaseDirectory: 実行ファイルと同じディレクトリのパスを取得する
        var filePath = Path.Combine(AppContext.BaseDirectory, "データ.xlsx");

        Console.WriteLine("=== ジョイントグループ化 ===");

        // XLWorkbook(filePath): 既存の Excel ファイルを開く
        using var workbook = new XLWorkbook(filePath);

        // 1. 商品マスタを読み込む
        Console.WriteLine("\n[1/4] 商品マスタを読み込み中...");
        var masters = MasterSheetReader.Read(workbook);

        // 2. 注文を読み込む
        Console.WriteLine("\n[2/4] 注文を読み込み中...");
        var orders = OrderSheetReader.Read(workbook);

        // 3. Join してグループ化集計する
        Console.WriteLine("\n[3/4] Join・集計中...");
        var summary = Prog2Processor.Run(masters, orders);

        // 4. 結果を同じブックに書き出す
        Console.WriteLine("\n[4/4] Excel に書き出し中...");
        Prog2SheetWriter.Write(workbook, summary);

        // workbook.Save(): 開いたファイルを上書き保存する
        workbook.Save();
        Console.WriteLine("\n完了しました。");
    }
}

// ─────────────────────────────────────────────────────────────
// Excel 読み込み: 商品マスタシート
// ─────────────────────────────────────────────────────────────
static class MasterSheetReader
{
    public static List<ProductMaster> Read(XLWorkbook workbook)
    {
        var records = new List<ProductMaster>();
        var sheet   = workbook.Worksheet("商品マスタ");
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            records.Add(new ProductMaster(
                商品コード: sheet.Cell(row, 1).GetValue<string>(),
                商品名:     sheet.Cell(row, 2).GetValue<string>(),
                型番:       sheet.Cell(row, 3).GetValue<string>(),
                発売日:     sheet.Cell(row, 4).GetValue<DateTime>(),
                仕入元:     sheet.Cell(row, 5).GetValue<string>(),
                調達区分:   sheet.Cell(row, 6).GetValue<string>(),
                単品原価:   sheet.Cell(row, 7).GetValue<decimal>(),
                単品売価:   sheet.Cell(row, 8).GetValue<decimal>()
            ));
        }
        Console.WriteLine($"商品マスタ: {records.Count} 件読み込み");
        return records;
    }
}

// ─────────────────────────────────────────────────────────────
// Excel 読み込み: 注文シート
// ─────────────────────────────────────────────────────────────
static class OrderSheetReader
{
    public static List<OrderRow> Read(XLWorkbook workbook)
    {
        var records = new List<OrderRow>();
        var sheet   = workbook.Worksheet("注文");
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            records.Add(new OrderRow(
                注文番号: sheet.Cell(row, 1).GetValue<string>(),
                注文日:   sheet.Cell(row, 2).GetValue<DateTime>(),
                商品コード: sheet.Cell(row, 3).GetValue<string>(),
                数量:     sheet.Cell(row, 4).GetValue<int>()
            ));
        }
        Console.WriteLine($"注文: {records.Count} 件読み込み");
        return records;
    }
}

// ─────────────────────────────────────────────────────────────
// Join + GroupBy: 結合してから集計する
// ─────────────────────────────────────────────────────────────
static class Prog2Processor
{
    public static List<SalesSummary> Run(
        List<ProductMaster> masters,
        List<OrderRow>      orders)
    {
        // ════════════════════════════════════════════════════════════════
        // STEP 1 — LINQ の Join: 商品コードで 2 つのリストを結合する
        //          （SQL の INNER JOIN と同じ。マスタに存在しない商品コードは除外される）
        // ════════════════════════════════════════════════════════════════

        // ── 【LINQ を使わない場合】Dictionary で商品マスタを索引化してから foreach ──
        //
        // var masterDict = masters.ToDictionary(m => m.商品コード);
        // var joinedList = new List<JoinedOrder>();
        // foreach (var o in orders)
        // {
        //     if (masterDict.TryGetValue(o.商品コード, out var m))
        //     {
        //         joinedList.Add(new JoinedOrder(
        //             o.注文番号, o.注文日, o.商品コード,
        //             m.商品名, m.単品原価, m.単品売価, o.数量));
        //     }
        // }

        // ── 【LINQ Join を使う場合】結合条件と変換を 1 式で書ける ──
        //
        // 引数の読み方:
        //   第 1 引数 masters      : 結合する相手のリスト（商品マスタ）
        //   第 2 引数 o => o.商品コード: 注文側の結合キー
        //   第 3 引数 m => m.商品コード: 商品マスタ側の結合キー
        //   第 4 引数 (o, m) => new ...: 結合後に作るオブジェクト
        var joined = orders
            .Join(
                masters,
                o => o.商品コード,
                m => m.商品コード,
                (o, m) => new JoinedOrder(
                    注文番号:  o.注文番号,
                    注文日:    o.注文日,
                    商品コード: o.商品コード,
                    商品名:    m.商品名,
                    単品原価:  m.単品原価,
                    単品売価:  m.単品売価,
                    数量:      o.数量
                )
            )
            .ToList();

        Console.WriteLine($"Join 後: {joined.Count} 件");

        // ════════════════════════════════════════════════════════════════
        // STEP 2 — LINQ の GroupBy: 販売年月 × 商品コードでグループ化して集計する
        //          （SQL の GROUP BY と同じ）
        // ════════════════════════════════════════════════════════════════

        // ── 【LINQ を使わない場合】Dictionary でグループを手動管理 ──
        //
        // var groupDict = new Dictionary<(string, string), List<JoinedOrder>>();
        // foreach (var r in joined)
        // {
        //     var key = (r.注文日.ToString("yyyy/MM"), r.商品コード);
        //     if (!groupDict.ContainsKey(key)) groupDict[key] = new List<JoinedOrder>();
        //     groupDict[key].Add(r);
        // }
        // var summaryList = new List<SalesSummary>();
        // foreach (var kv in groupDict)
        // {
        //     var g          = kv.Value;
        //     var 売上金額   = g.Sum(r => r.数量 * r.単品売価);
        //     var 原価       = g.Sum(r => r.数量 * r.単品原価);
        //     var 利益額     = 売上金額 - 原価;
        //     summaryList.Add(new SalesSummary { ... });
        // }
        // summaryList = summaryList.OrderBy(...).ThenByDescending(...).ToList();

        // ── 【LINQ GroupBy を使う場合】グループ化・集計・並べ替えを連鎖できる ──
        //
        // GroupBy(r => new { r.販売年月, r.商品コード }):
        //   匿名型をキーにして複数列でグループ化する。
        //   各グループは g.Key.販売年月 / g.Key.商品コード でキーを参照できる。
        var result = joined
            .GroupBy(r => new
            {
                販売年月   = r.注文日.ToString("yyyy/MM"),
                r.商品コード
            })
            .Select(g =>
            {
                var 売上金額 = g.Sum(r => (decimal)r.数量 * r.単品売価);
                var 原価     = g.Sum(r => (decimal)r.数量 * r.単品原価);
                var 利益額   = 売上金額 - 原価;

                // ゼロ除算を避けるため売上金額 > 0 のときだけ除算する
                var 利益率 = 売上金額 > 0 ? 利益額 / 売上金額 : 0m;

                return new SalesSummary
                {
                    販売年月   = g.Key.販売年月,
                    商品コード = g.Key.商品コード,
                    // グループ内は同じ商品名なので First() で取得する
                    商品名     = g.First().商品名,
                    単品原価   = g.First().単品原価,
                    単品売価   = g.First().単品売価,
                    売上個数   = g.Sum(r => r.数量),
                    売上金額   = 売上金額,
                    原価       = 原価,
                    利益額     = 利益額,
                    利益率     = 利益率
                };
            })
            // 販売年月の昇順、同一月内は利益額の降順で並べる
            .OrderBy(r => r.販売年月)
            .ThenByDescending(r => r.利益額)
            .ToList();

        Console.WriteLine($"集計後: {result.Count} 件");
        return result;
    }
}

// ─────────────────────────────────────────────────────────────
// Excel 出力: 「販売集計」シートに書き出す
// ─────────────────────────────────────────────────────────────
static class Prog2SheetWriter
{
    private const string SheetName = "販売集計";

    public static void Write(XLWorkbook workbook, List<SalesSummary> data)
    {
        // 同名シートが既に存在する場合は削除してから作り直す
        if (workbook.Worksheets.TryGetWorksheet(SheetName, out var existing))
            existing.Delete();

        var sheet = workbook.Worksheets.Add(SheetName);

        // ── ヘッダ行 ──
        string[] headers =
        {
            "販売年月", "商品コード", "商品名",
            "単品原価", "単品売価", "売上個数", "売上金額", "原価", "利益額", "利益率"
        };
        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = sheet.Cell(1, col);
            cell.Value                      = headers[col - 1];
            cell.Style.Fill.BackgroundColor = XLColor.DarkGreen;
            cell.Style.Font.FontColor       = XLColor.White;
            cell.Style.Font.Bold            = true;
        }

        // ── データ行 ──
        for (int i = 0; i < data.Count; i++)
        {
            var r   = data[i];
            int row = i + 2;
            sheet.Cell(row,  1).Value = r.販売年月;
            sheet.Cell(row,  2).Value = r.商品コード;
            sheet.Cell(row,  3).Value = r.商品名;
            sheet.Cell(row,  4).Value = r.単品原価;
            sheet.Cell(row,  5).Value = r.単品売価;
            sheet.Cell(row,  6).Value = r.売上個数;
            sheet.Cell(row,  7).Value = r.売上金額;
            sheet.Cell(row,  8).Value = r.原価;
            sheet.Cell(row,  9).Value = r.利益額;

            // 利益率はセルの値に小数（例: 0.35）を設定し、
            // 書式を "0.00%" にすることで Excel 上で「35.00%」と表示される
            sheet.Cell(row, 10).Value                     = r.利益率;
            sheet.Cell(row, 10).Style.NumberFormat.Format = "0.00%";
        }

        sheet.Columns().AdjustToContents();
        Console.WriteLine($"シート「{SheetName}」に {data.Count} 件書き出し");
    }
}
