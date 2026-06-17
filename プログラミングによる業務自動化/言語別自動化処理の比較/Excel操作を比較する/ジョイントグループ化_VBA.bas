Attribute VB_Name = "ジョイントグループ化"
Option Explicit

'==============================================================================
' プログラム2: ジョイントグループ化
' 注文シートと商品マスタをジョインして「販売年月×商品コード」でグループ化し、
' 売上集計（売上個数・売上金額・原価・利益額・利益率）を
' 「販売集計」シートに出力する。
' 並び順: 販売年月 昇順、利益額 降順（バブルソートで実装）
'==============================================================================
Sub 実行_ジョイントグループ化()

    ' --- 変数宣言（VBAのDimは関数スコープ。ループ内に書いても有効範囲は変わらないため先頭にまとめる） ---
    Dim wsマスタ    As Worksheet
    Dim ws注文      As Worksheet
    Dim ws出力      As Worksheet
    Dim dicマスタ   As Object   ' key=商品コード, value=マスタ情報配列
    Dim dic集計     As Object   ' key=yyyy/mm_商品コード, value=集計情報配列
    Dim LastRow     As Long
    Dim i           As Long
    Dim j           As Long
    Dim p           As Long
    Dim q           As Long
    Dim k           As Long

    ' Step1で使用
    Dim 商品コード   As String
    Dim マスタ情報(6) As Variant

    ' Step2で使用
    Dim 注文コード  As String
    Dim 注文日      As Date
    Dim 数量        As Long
    Dim 販売年月    As String
    Dim 集計キー    As String
    Dim m()         As Variant
    Dim 単品原価    As Double
    Dim 単品売価    As Double
    Dim 既存()      As Variant
    Dim 新規(7)     As Variant

    ' Step3で使用
    Dim キー一覧    As Variant
    Dim 件数        As Long
    Dim ソートデータ() As Variant
    Dim エントリ()  As Variant
    Dim 売上金額    As Double
    Dim 原価合計    As Double
    Dim 利益額      As Double
    Dim 利益率      As Double

    ' Step4で使用
    Dim 入替        As Boolean
    Dim 一時行(9)   As Variant
    Dim 年月A       As String
    Dim 年月B       As String

    ' Step5で使用
    Dim 出力行      As Long

    ' --- 入力シートの参照 ---
    Set wsマスタ = ThisWorkbook.Worksheets("商品マスタ")
    Set ws注文   = ThisWorkbook.Worksheets("注文")

    ' ============================================================
    ' Step1: 商品マスタをDictionaryに読み込む（key=商品コード）
    ' ============================================================
    Set dicマスタ = CreateObject("Scripting.Dictionary")
    dicマスタ.CompareMode = 1  ' テキスト比較（大文字小文字を区別しない）

    LastRow = wsマスタ.Cells(wsマスタ.Rows.Count, 1).End(xlUp).Row

    For i = 2 To LastRow
        商品コード = CStr(wsマスタ.Cells(i, 1).Value)

        If 商品コード <> "" Then
            ' 配列: (0)商品名 (1)型番 (2)発売日 (3)仕入元 (4)調達区分 (5)単品原価 (6)単品売価
            マスタ情報(0) = wsマスタ.Cells(i, 2).Value  ' 商品名
            マスタ情報(1) = wsマスタ.Cells(i, 3).Value  ' 型番
            マスタ情報(2) = wsマスタ.Cells(i, 4).Value  ' 発売日
            マスタ情報(3) = wsマスタ.Cells(i, 5).Value  ' 仕入元
            マスタ情報(4) = wsマスタ.Cells(i, 6).Value  ' 調達区分
            マスタ情報(5) = CDbl(wsマスタ.Cells(i, 7).Value)  ' 単品原価
            マスタ情報(6) = CDbl(wsマスタ.Cells(i, 8).Value)  ' 単品売価

            dicマスタ(商品コード) = マスタ情報
        End If
    Next i

    ' ============================================================
    ' Step2: 注文を1行ずつ読み込み、商品マスタとジョインして集計
    ' ============================================================
    Set dic集計 = CreateObject("Scripting.Dictionary")
    dic集計.CompareMode = 1

    LastRow = ws注文.Cells(ws注文.Rows.Count, 1).End(xlUp).Row

    For i = 2 To LastRow
        注文コード = CStr(ws注文.Cells(i, 3).Value)  ' 商品コード列（C列）

        ' 商品マスタに存在しない場合はスキップ
        If Not dicマスタ.Exists(注文コード) Then GoTo 次の注文

        ' 注文日から販売年月（yyyy/mm）を生成
        If IsDate(ws注文.Cells(i, 2).Value) Then
            注文日 = CDate(ws注文.Cells(i, 2).Value)
        Else
            GoTo 次の注文
        End If
        販売年月 = Format(注文日, "yyyy/mm")

        数量 = CLng(ws注文.Cells(i, 4).Value)

        ' 集計用のキーを「yyyy/mm_商品コード」で生成
        集計キー = 販売年月 & "_" & 注文コード

        ' マスタ情報を取得
        m = dicマスタ(注文コード)
        単品原価 = CDbl(m(5))
        単品売価 = CDbl(m(6))

        ' Dictionaryに既存エントリがあれば累積、なければ新規作成
        ' 配列: (0)販売年月 (1)商品コード (2)商品名 (3)単品原価 (4)単品売価
        '        (5)売上個数 (6)売上金額  (7)原価合計
        If dic集計.Exists(集計キー) Then
            既存 = dic集計(集計キー)
            既存(5) = 既存(5) + 数量               ' 売上個数
            既存(6) = 既存(6) + (数量 * 単品売価)   ' 売上金額
            既存(7) = 既存(7) + (数量 * 単品原価)   ' 原価合計
            dic集計(集計キー) = 既存
        Else
            新規(0) = 販売年月
            新規(1) = 注文コード
            新規(2) = m(0)                          ' 商品名
            新規(3) = 単品原価
            新規(4) = 単品売価
            新規(5) = 数量                           ' 売上個数
            新規(6) = 数量 * 単品売価                ' 売上金額
            新規(7) = 数量 * 単品原価                ' 原価合計
            dic集計(集計キー) = 新規
        End If

次の注文:
    Next i

    ' ============================================================
    ' Step3: Dictionaryを2次元配列に変換してソート準備
    ' ============================================================
    キー一覧 = dic集計.Keys
    件数 = dic集計.Count

    If 件数 = 0 Then
        MsgBox "集計対象のデータがありません。", vbExclamation
        Exit Sub
    End If

    ' ソート用2次元配列
    ' 列: 0=販売年月 1=商品コード 2=商品名 3=単品原価 4=単品売価
    '      5=売上個数 6=売上金額 7=原価合計 8=利益額 9=利益率
    ReDim ソートデータ(件数 - 1, 9)

    For j = 0 To 件数 - 1
        エントリ = dic集計(キー一覧(j))

        売上金額 = CDbl(エントリ(6))
        原価合計 = CDbl(エントリ(7))
        利益額   = 売上金額 - 原価合計
        If 売上金額 <> 0 Then
            利益率 = 利益額 / 売上金額
        Else
            利益率 = 0
        End If

        ソートデータ(j, 0) = エントリ(0)  ' 販売年月
        ソートデータ(j, 1) = エントリ(1)  ' 商品コード
        ソートデータ(j, 2) = エントリ(2)  ' 商品名
        ソートデータ(j, 3) = エントリ(3)  ' 単品原価
        ソートデータ(j, 4) = エントリ(4)  ' 単品売価
        ソートデータ(j, 5) = エントリ(5)  ' 売上個数
        ソートデータ(j, 6) = 売上金額      ' 売上金額
        ソートデータ(j, 7) = 原価合計      ' 原価合計
        ソートデータ(j, 8) = 利益額        ' 利益額
        ソートデータ(j, 9) = 利益率        ' 利益率
    Next j

    ' ============================================================
    ' Step4: バブルソート（販売年月 昇順 → 利益額 降順）
    ' ============================================================
    For p = 0 To 件数 - 2
        For q = 0 To 件数 - 2 - p
            入替 = False

            年月A = CStr(ソートデータ(q, 0))
            年月B = CStr(ソートデータ(q + 1, 0))

            ' 販売年月が同じ場合は利益額の降順（大きい方が前）
            If 年月A > 年月B Then
                入替 = True
            ElseIf 年月A = 年月B Then
                If CDbl(ソートデータ(q, 8)) < CDbl(ソートデータ(q + 1, 8)) Then
                    入替 = True
                End If
            End If

            If 入替 Then
                For k = 0 To 9
                    一時行(k) = ソートデータ(q, k)
                    ソートデータ(q, k) = ソートデータ(q + 1, k)
                    ソートデータ(q + 1, k) = 一時行(k)
                Next k
            End If
        Next q
    Next p

    ' ============================================================
    ' Step5: 出力シートの準備と書き込み
    ' ============================================================
    Application.DisplayAlerts = False
    On Error Resume Next
    ThisWorkbook.Worksheets("販売集計").Delete
    On Error GoTo 0
    Application.DisplayAlerts = True

    Set ws出力 = ThisWorkbook.Worksheets.Add
    ws出力.Name = "販売集計"

    ' ヘッダー行の書き込み
    With ws出力
        .Cells(1, 1).Value  = "販売年月"
        .Cells(1, 2).Value  = "商品コード"
        .Cells(1, 3).Value  = "商品名"
        .Cells(1, 4).Value  = "単品原価"
        .Cells(1, 5).Value  = "単品売価"
        .Cells(1, 6).Value  = "売上個数"
        .Cells(1, 7).Value  = "売上金額"
        .Cells(1, 8).Value  = "原価"
        .Cells(1, 9).Value  = "利益額"
        .Cells(1, 10).Value = "利益率"

        ' ヘッダーを太字に
        .Rows(1).Font.Bold = True
    End With

    ' データ行の書き込み
    For i = 0 To 件数 - 1
        出力行 = i + 2

        ws出力.Cells(出力行, 1).Value  = ソートデータ(i, 0)  ' 販売年月
        ws出力.Cells(出力行, 2).Value  = ソートデータ(i, 1)  ' 商品コード
        ws出力.Cells(出力行, 3).Value  = ソートデータ(i, 2)  ' 商品名
        ws出力.Cells(出力行, 4).Value  = ソートデータ(i, 3)  ' 単品原価
        ws出力.Cells(出力行, 5).Value  = ソートデータ(i, 4)  ' 単品売価
        ws出力.Cells(出力行, 6).Value  = ソートデータ(i, 5)  ' 売上個数
        ws出力.Cells(出力行, 7).Value  = ソートデータ(i, 6)  ' 売上金額
        ws出力.Cells(出力行, 8).Value  = ソートデータ(i, 7)  ' 原価
        ws出力.Cells(出力行, 9).Value  = ソートデータ(i, 8)  ' 利益額

        ' 利益率はパーセント書式で2桁小数
        ws出力.Cells(出力行, 10).Value        = ソートデータ(i, 9)
        ws出力.Cells(出力行, 10).NumberFormat = "0.00%"
    Next i

    ' 列幅を自動調整
    ws出力.Columns("A:J").AutoFit

    MsgBox "プログラム2 完了: 「販売集計」シートに " & 件数 & " 件出力しました。", vbInformation

End Sub
