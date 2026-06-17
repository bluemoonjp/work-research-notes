// =============================================================================
// 単純なWhere条件 — 商品マスタから「今年発売のハイグレードモデル」を抽出する
//
// 処理の流れ:
//   1. データ.xlsx の「商品マスタ」シートを読み込む
//   2. LINQ の Where で「今年発売かつ型番末尾が HG」に絞り込む
//   3. 結果を同じブック内の「最新ハイグレードモデル一覧」シートに書き出す
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
// 絞り込み: 今年発売 かつ 型番末尾が "HG"
// ─────────────────────────────────────────────────────────────
static class Prog1Processor
{
    public static List<ProductMaster> Filter(List<ProductMaster> masters)
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
        // return result;

        // ── 【LINQ Where を使う場合】条件をラムダ式で簡潔に書ける ──
        //
        // m => ... : 「各要素を m として受け取り、右辺の条件を評価する」ラムダ式
        // EndsWith("HG"): 文字列が指定の文字列で終わるかどうかを bool で返す
        var result = masters
            .Where(m => m.発売日.Year == DateTime.Now.Year
                     && m.型番.EndsWith("HG"))
            .ToList();

        Console.WriteLine($"絞り込み後: {result.Count} 件");
        return result;
    }
}

// ─────────────────────────────────────────────────────────────
// Excel 出力: 「最新ハイグレードモデル一覧」シートに書き出す
// ─────────────────────────────────────────────────────────────
static class Prog1SheetWriter
{
    private const string SheetName = "最新ハイグレードモデル一覧";

    public static void Write(XLWorkbook workbook, List<ProductMaster> data)
    {
        // 同名シートが既に存在する場合は削除してから作り直す
        if (workbook.Worksheets.TryGetWorksheet(SheetName, out var existing))
            existing.Delete();

        var sheet = workbook.Worksheets.Add(SheetName);

        // ── ヘッダ行 ──
        string[] headers = { "商品コード", "商品名", "型番", "発売日", "仕入元", "調達区分", "単品原価", "単品売価" };
        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = sheet.Cell(1, col);
            cell.Value                      = headers[col - 1];
            cell.Style.Fill.BackgroundColor = XLColor.SteelBlue;
            cell.Style.Font.FontColor       = XLColor.White;
            cell.Style.Font.Bold            = true;
        }

        // ── データ行 ──
        for (int i = 0; i < data.Count; i++)
        {
            var m   = data[i];
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

        // 列幅をコンテンツに合わせて自動調整する
        sheet.Columns().AdjustToContents();
        Console.WriteLine($"シート「{SheetName}」に {data.Count} 件書き出し");
    }
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

        Console.WriteLine("=== 単純なWhere条件 ===");

        // XLWorkbook(filePath): 既存の Excel ファイルを開く
        // using: ブロックを抜けるときに自動で Dispose（ファイルを閉じる）する
        using var workbook = new XLWorkbook(filePath);

        // 1. 商品マスタを読み込む
        Console.WriteLine("\n[1/3] 商品マスタを読み込み中...");
        var masters = MasterSheetReader.Read(workbook);

        // 2. Where で絞り込む
        Console.WriteLine("\n[2/3] 絞り込み中...");
        var filtered = Prog1Processor.Filter(masters);

        // 3. 結果を同じブックに書き出す
        Console.WriteLine("\n[3/3] Excel に書き出し中...");
        Prog1SheetWriter.Write(workbook, filtered);

        // workbook.Save(): 開いたファイルを上書き保存する（SaveAs と違いパス指定不要）
        workbook.Save();
        Console.WriteLine("\n完了しました。");
    }
}
