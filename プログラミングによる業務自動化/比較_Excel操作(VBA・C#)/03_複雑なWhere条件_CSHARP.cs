// =============================================================================
// 複雑なWhere条件 — OR を含む複合条件で商品マスタを絞り込む
//
// 処理の流れ:
//   1. データ.xlsx の「商品マスタ」シートを読み込む
//   2. LINQ の Where で以下の OR 条件に合う商品を抽出する
//        (仕入元 == "A社" かつ 調達区分 == "国内調達")
//        または
//        (仕入元 == "B社" かつ 調達区分 == "受注生産")
//   3. 結果を同じブック内の「イレギュラー発注商品」シートに書き出す
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
// エントリポイント
// ─────────────────────────────────────────────────────────────
class Program
{
    static void Main(string[] args)
    {
        // AppContext.BaseDirectory: 実行ファイルと同じディレクトリのパスを取得する
        var filePath = Path.Combine(AppContext.BaseDirectory, "データ.xlsx");

        Console.WriteLine("=== 複雑なWhere条件 ===");

        // XLWorkbook(filePath): 既存の Excel ファイルを開く
        // using: ブロックを抜けるときに自動で Dispose（ファイルを閉じる）する
        using var workbook = new XLWorkbook(filePath);

        // 1. 商品マスタを読み込む
        Console.WriteLine("\n[1/3] 商品マスタを読み込み中...");
        var masters = MasterSheetReader.Read(workbook);

        // 2. Where で絞り込む
        Console.WriteLine("\n[2/3] 絞り込み中...");
        var filtered = Prog3Processor.Filter(masters);

        // 3. 結果を同じブックに書き出す
        Console.WriteLine("\n[3/3] Excel に書き出し中...");
        Prog3SheetWriter.Write(workbook, filtered);

        // workbook.Save(): 開いたファイルを上書き保存する（SaveAs と違いパス指定不要）
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
// 絞り込み: OR を含む複合条件
// ─────────────────────────────────────────────────────────────
static class Prog3Processor
{
    public static List<ProductMaster> Filter(List<ProductMaster> masters)
    {
        // ════════════════════════════════════════════════════════════════
        // LINQ の Where: OR（||）と AND（&&）を組み合わせた複合条件
        //
        // 条件の読み方:
        //   (A社 かつ 国内調達) または (B社 かつ 受注生産)
        //
        // A社 × 国内調達: 本来 A社 は海外調達が多いのに国内調達になっている
        // B社 × 受注生産: 本来 B社 は定番品が多いのに受注生産になっている
        // → どちらも通常の発注パターンと異なるイレギュラーな組み合わせ
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
        // return result;

        // ── 【LINQ Where を使う場合】OR を含む条件もラムダ式で 1 行にまとめられる ──
        //
        // && は AND（両方が true のとき true）
        // || は OR（どちらか一方が true のとき true）
        // 括弧で AND を先にグループ化することで、OR の評価順序を明確にする
        var result = masters
            .Where(m =>
                (m.仕入元 == "A社" && m.調達区分 == "国内調達") ||
                (m.仕入元 == "B社" && m.調達区分 == "受注生産"))
            .ToList();

        Console.WriteLine($"絞り込み後: {result.Count} 件");
        return result;
    }
}

// ─────────────────────────────────────────────────────────────
// Excel 出力: 「イレギュラー発注商品」シートに書き出す
// ─────────────────────────────────────────────────────────────
static class Prog3SheetWriter
{
    private const string SheetName = "イレギュラー発注商品";

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
            cell.Style.Fill.BackgroundColor = XLColor.DarkOrange;
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
