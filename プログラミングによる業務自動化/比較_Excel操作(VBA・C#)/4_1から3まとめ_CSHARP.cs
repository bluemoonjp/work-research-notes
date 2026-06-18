// =============================================================================
// 1〜3まとめ — プログラム 1・2・3 を 1 回の起動でまとめて実行する
//
// ※ このファイルはプログラム 1〜3 の処理クラスをすべて統合したバージョンである。
//    ブックを 1 度だけ開き、3 つの処理を順番に実行してから 1 度だけ保存する。
//
// 処理の流れ:
//   1. データ.xlsx を 1 度だけ開く
//   2. プログラム 1: 今年発売のハイグレードモデルを「最新ハイグレードモデル一覧」に書き出す
//   3. プログラム 2: 注文と商品マスタを Join して「販売集計」に書き出す
//   4. プログラム 3: OR 条件でイレギュラー商品を「イレギュラー発注商品」に書き出す
//   5. ブックを 1 度だけ保存する
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

// Excel から読み込んだ注文 1 行分（プログラム 2 で使用）
record OrderRow(
    string   注文番号,
    DateTime 注文日,
    string   商品コード,
    int      数量
);

// Join 後の結合データ 1 行分（プログラム 2 内部で使用する中間データ）
record JoinedOrder(
    string   注文番号,
    DateTime 注文日,
    string   商品コード,
    string   商品名,
    decimal  単品原価,
    decimal  単品売価,
    int      数量
);

// 販売年月 × 商品コード別の集計結果（プログラム 2 で使用）
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
// エントリポイント: ブックを 1 度だけ開いて 3 つの処理を順番に実行する
// ─────────────────────────────────────────────────────────────
class Program
{
    static void Main(string[] args)
    {
        // AppContext.BaseDirectory: 実行ファイルと同じディレクトリのパスを取得する
        var filePath = Path.Combine(AppContext.BaseDirectory, "データ.xlsx");

        Console.WriteLine("=== 1〜3まとめ ===");

        // XLWorkbook(filePath): 既存の Excel ファイルを開く
        // using: ブロックを抜けるときに自動で Dispose（ファイルを閉じる）する
        using var workbook = new XLWorkbook(filePath);

        // 商品マスタは 3 プログラム共通で使うので最初に 1 回だけ読み込む
        Console.WriteLine("\n[1/5] 商品マスタを読み込み中...");
        var masters = MasterSheetReader.Read(workbook);

        // 注文はプログラム 2 だけで使う
        Console.WriteLine("\n[2/5] 注文を読み込み中...");
        var orders = OrderSheetReader.Read(workbook);

        // プログラム 1: 今年発売のハイグレードモデルを抽出してシートに書き出す
        Console.WriteLine("\n[3/5] プログラム 1 実行中...");
        Prog1Processor.Run(workbook, masters);

        // プログラム 2: Join してグループ化集計してシートに書き出す
        Console.WriteLine("\n[4/5] プログラム 2 実行中...");
        Prog2Processor.Run(workbook, masters, orders);

        // プログラム 3: OR 条件でイレギュラー商品を抽出してシートに書き出す
        Console.WriteLine("\n[5/5] プログラム 3 実行中...");
        Prog3Processor.Run(workbook, masters);

        // workbook.Save(): 開いたファイルを 1 度だけ上書き保存する
        workbook.Save();
        Console.WriteLine("\n完了しました。");
    }
}

// ─────────────────────────────────────────────────────────────
// Excel 読み込み: 商品マスタシート（プログラム 1・2・3 共通）
// ─────────────────────────────────────────────────────────────
static class MasterSheetReader
{
    public static List<ProductMaster> Read(XLWorkbook workbook)
    {
        var records = new List<ProductMaster>();
        var sheet   = workbook.Worksheet("商品マスタ");
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        // 1 行目はヘッダなので 2 行目からデータを読み込む
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
// Excel 読み込み: 注文シート（プログラム 2 で使用）
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
                注文番号:   sheet.Cell(row, 1).GetValue<string>(),
                注文日:     sheet.Cell(row, 2).GetValue<DateTime>(),
                商品コード: sheet.Cell(row, 3).GetValue<string>(),
                数量:       sheet.Cell(row, 4).GetValue<int>()
            ));
        }
        Console.WriteLine($"注文: {records.Count} 件読み込み");
        return records;
    }
}

// ─────────────────────────────────────────────────────────────
// プログラム 1: 今年発売のハイグレードモデルを抽出する
// ─────────────────────────────────────────────────────────────
static class Prog1Processor
{
    private const string SheetName = "最新ハイグレードモデル一覧";

    public static void Run(XLWorkbook workbook, List<ProductMaster> masters)
    {
        // ════════════════════════════════════════════════════════════════
        // LINQ の Where: 条件に合う要素だけを残す（SQL の WHERE 句と同じ）
        // ════════════════════════════════════════════════════════════════

        // ── 【LINQ を使わない場合】foreach で条件を確認しながら手動でリストに追加 ──
        //
        // var result = new List<ProductMaster>();
        // foreach (var m in masters)
        // {
        //     bool isThisYear  = m.発売日.Year == DateTime.Now.Year;
        //     bool isHighGrade = m.型番.EndsWith("HG");
        //     if (isThisYear && isHighGrade)
        //         result.Add(m);
        // }

        // ── 【LINQ Where を使う場合】条件をラムダ式で簡潔に書ける ──
        //
        // m => ... : 「各要素を m として受け取り、右辺の条件を評価する」ラムダ式
        // EndsWith("HG"): 文字列が指定の文字列で終わるかどうかを bool で返す
        var filtered = masters
            .Where(m => m.発売日.Year == DateTime.Now.Year
                     && m.型番.EndsWith("HG"))
            .ToList();

        Console.WriteLine($"  [Prog1] 絞り込み後: {filtered.Count} 件");

        // 同名シートが既に存在する場合は削除してから作り直す
        if (workbook.Worksheets.TryGetWorksheet(SheetName, out var existing))
            existing.Delete();

        var sheet = workbook.Worksheets.Add(SheetName);
        string[] headers = { "商品コード", "商品名", "型番", "発売日", "仕入元", "調達区分", "単品原価", "単品売価" };
        WriteHeader(sheet, headers, XLColor.SteelBlue);

        for (int i = 0; i < filtered.Count; i++)
        {
            var m   = filtered[i];
            int row = i + 2;
            sheet.Cell(row, 1).Value = m.商品コード;
            sheet.Cell(row, 2).Value = m.商品名;
            sheet.Cell(row, 3).Value = m.型番;
            sheet.Cell(row, 4).Value = m.発売日;
            sheet.Cell(row, 4).Style.NumberFormat.Format = "yyyy/MM/dd";
            sheet.Cell(row, 5).Value = m.仕入元;
            sheet.Cell(row, 6).Value = m.調達区分;
            sheet.Cell(row, 7).Value = m.単品原価;
            sheet.Cell(row, 8).Value = m.単品売価;
        }
        sheet.Columns().AdjustToContents();
        Console.WriteLine($"  [Prog1] シート「{SheetName}」に書き出し完了");
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

// ─────────────────────────────────────────────────────────────
// プログラム 2: 注文と商品マスタを Join してグループ化集計する
// ─────────────────────────────────────────────────────────────
static class Prog2Processor
{
    private const string SheetName = "販売集計";

    public static void Run(
        XLWorkbook          workbook,
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
        //   第 1 引数 masters        : 結合する相手のリスト（商品マスタ）
        //   第 2 引数 o => o.商品コード: 注文側の結合キー
        //   第 3 引数 m => m.商品コード: 商品マスタ側の結合キー
        //   第 4 引数 (o, m) => new ...: 結合後に作るオブジェクト
        var joined = orders
            .Join(
                masters,
                o => o.商品コード,
                m => m.商品コード,
                (o, m) => new JoinedOrder(
                    注文番号:   o.注文番号,
                    注文日:     o.注文日,
                    商品コード: o.商品コード,
                    商品名:     m.商品名,
                    単品原価:   m.単品原価,
                    単品売価:   m.単品売価,
                    数量:       o.数量
                )
            )
            .ToList();

        Console.WriteLine($"  [Prog2] Join 後: {joined.Count} 件");

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
        //     var g        = kv.Value;
        //     var 売上金額 = g.Sum(r => r.数量 * r.単品売価);
        //     var 原価     = g.Sum(r => r.数量 * r.単品原価);
        //     var 利益額   = 売上金額 - 原価;
        //     summaryList.Add(new SalesSummary { ... });
        // }
        // summaryList = summaryList.OrderBy(...).ThenByDescending(...).ToList();

        // ── 【LINQ GroupBy を使う場合】グループ化・集計・並べ替えを連鎖できる ──
        //
        // GroupBy(r => new { r.販売年月, r.商品コード }):
        //   匿名型をキーにして複数列でグループ化する。
        //   各グループは g.Key.販売年月 / g.Key.商品コード でキーを参照できる。
        var summary = joined
            .GroupBy(r => new
            {
                販売年月    = r.注文日.ToString("yyyy/MM"),
                r.商品コード
            })
            .Select(g =>
            {
                var 売上金額 = g.Sum(r => (decimal)r.数量 * r.単品売価);
                var 原価     = g.Sum(r => (decimal)r.数量 * r.単品原価);
                var 利益額   = 売上金額 - 原価;
                // ゼロ除算を避けるため売上金額 > 0 のときだけ除算する
                var 利益率   = 売上金額 > 0 ? 利益額 / 売上金額 : 0m;

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

        Console.WriteLine($"  [Prog2] 集計後: {summary.Count} 件");

        // 同名シートが既に存在する場合は削除してから作り直す
        if (workbook.Worksheets.TryGetWorksheet(SheetName, out var existing))
            existing.Delete();

        var sheet = workbook.Worksheets.Add(SheetName);
        string[] headers =
        {
            "販売年月", "商品コード", "商品名",
            "単品原価", "単品売価", "売上個数", "売上金額", "原価", "利益額", "利益率"
        };
        WriteHeader(sheet, headers, XLColor.DarkGreen);

        for (int i = 0; i < summary.Count; i++)
        {
            var r   = summary[i];
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
        Console.WriteLine($"  [Prog2] シート「{SheetName}」に書き出し完了");
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

// ─────────────────────────────────────────────────────────────
// プログラム 3: OR を含む複合条件でイレギュラー商品を抽出する
// ─────────────────────────────────────────────────────────────
static class Prog3Processor
{
    private const string SheetName = "イレギュラー発注商品";

    public static void Run(XLWorkbook workbook, List<ProductMaster> masters)
    {
        // ════════════════════════════════════════════════════════════════
        // LINQ の Where: OR（||）と AND（&&）を組み合わせた複合条件
        //
        // 条件の読み方:
        //   (A社 かつ 国内調達) または (B社 かつ 受注生産)
        // ════════════════════════════════════════════════════════════════

        // ── 【LINQ を使わない場合】foreach で各条件を個別に評価 ──
        //
        // var result = new List<ProductMaster>();
        // foreach (var m in masters)
        // {
        //     bool isPatternA = m.仕入元 == "A社" && m.調達区分 == "国内調達";
        //     bool isPatternB = m.仕入元 == "B社" && m.調達区分 == "受注生産";
        //     if (isPatternA || isPatternB)
        //         result.Add(m);
        // }

        // ── 【LINQ Where を使う場合】OR を含む条件もラムダ式で 1 行にまとめられる ──
        //
        // && は AND（両方が true のとき true）
        // || は OR（どちらか一方が true のとき true）
        // 括弧で AND を先にグループ化することで、OR の評価順序を明確にする
        var filtered = masters
            .Where(m =>
                (m.仕入元 == "A社" && m.調達区分 == "国内調達") ||
                (m.仕入元 == "B社" && m.調達区分 == "受注生産"))
            .ToList();

        Console.WriteLine($"  [Prog3] 絞り込み後: {filtered.Count} 件");

        // 同名シートが既に存在する場合は削除してから作り直す
        if (workbook.Worksheets.TryGetWorksheet(SheetName, out var existing))
            existing.Delete();

        var sheet = workbook.Worksheets.Add(SheetName);
        string[] headers = { "商品コード", "商品名", "型番", "発売日", "仕入元", "調達区分", "単品原価", "単品売価" };
        WriteHeader(sheet, headers, XLColor.DarkOrange);

        for (int i = 0; i < filtered.Count; i++)
        {
            var m   = filtered[i];
            int row = i + 2;
            sheet.Cell(row, 1).Value = m.商品コード;
            sheet.Cell(row, 2).Value = m.商品名;
            sheet.Cell(row, 3).Value = m.型番;
            sheet.Cell(row, 4).Value = m.発売日;
            sheet.Cell(row, 4).Style.NumberFormat.Format = "yyyy/MM/dd";
            sheet.Cell(row, 5).Value = m.仕入元;
            sheet.Cell(row, 6).Value = m.調達区分;
            sheet.Cell(row, 7).Value = m.単品原価;
            sheet.Cell(row, 8).Value = m.単品売価;
        }
        sheet.Columns().AdjustToContents();
        Console.WriteLine($"  [Prog3] シート「{SheetName}」に書き出し完了");
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
