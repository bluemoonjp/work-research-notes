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

    Dim dicマスタ As Object
    Dim dic集計   As Object
    Dim データ    As Variant
    Dim 件数      As Long
    Dim ws出力    As Worksheet

    Set dicマスタ = マスタをDicに読み込む(ThisWorkbook.Worksheets("商品マスタ"))
    Set dic集計   = 注文を集計してDicに格納する(ThisWorkbook.Worksheets("注文"), dicマスタ)

    件数 = dic集計.Count
    If 件数 = 0 Then
        MsgBox "集計対象のデータがありません。", vbExclamation
        Exit Sub
    End If

    データ = DicをDataに変換する(dic集計, 件数)  ' 利益額・利益率も算出
    バブルソート データ, 件数                      ' 販売年月昇順・利益額降順

    Set ws出力 = 出力シートを準備する("販売集計")
    集計ヘッダーを書き込む ws出力
    集計データを書き込む ws出力, データ, 件数
    ws出力.Columns("A:J").AutoFit

    MsgBox "プログラム2 完了: 「販売集計」シートに " & 件数 & " 件出力しました。", vbInformation

End Sub

'--------------------------------------------------------------
' 既存シートを削除して新規作成し、Worksheetオブジェクトを返す
'--------------------------------------------------------------
Private Function 出力シートを準備する(シート名 As String) As Worksheet

    Dim ws As Worksheet

    Application.DisplayAlerts = False
    On Error Resume Next
    ThisWorkbook.Worksheets(シート名).Delete
    On Error GoTo 0
    Application.DisplayAlerts = True

    Set ws = ThisWorkbook.Worksheets.Add
    ws.Name = シート名
    Set 出力シートを準備する = ws

End Function

'--------------------------------------------------------------
' 商品マスタを Dictionary に読み込んで返す
' key=商品コード, value=Array(0..6)
'   (0)商品名 (1)型番 (2)発売日 (3)仕入元 (4)調達区分 (5)単品原価 (6)単品売価
'--------------------------------------------------------------
Private Function マスタをDicに読み込む(ws As Worksheet) As Object

    Dim dic     As Object
    Dim LastRow As Long
    Dim i       As Long
    Dim コード  As String
    Dim 情報(6) As Variant

    Set dic = CreateObject("Scripting.Dictionary")
    dic.CompareMode = 1
    LastRow = ws.Cells(ws.Rows.Count, 1).End(xlUp).Row

    For i = 2 To LastRow
        コード = CStr(ws.Cells(i, 1).Value)
        If コード = "" Then GoTo 次のマスタ行

        情報(0) = ws.Cells(i, 2).Value          ' 商品名
        情報(1) = ws.Cells(i, 3).Value          ' 型番
        情報(2) = ws.Cells(i, 4).Value          ' 発売日
        情報(3) = ws.Cells(i, 5).Value          ' 仕入元
        情報(4) = ws.Cells(i, 6).Value          ' 調達区分
        情報(5) = CDbl(ws.Cells(i, 7).Value)    ' 単品原価
        情報(6) = CDbl(ws.Cells(i, 8).Value)    ' 単品売価
        dic(コード) = 情報
次のマスタ行:
    Next i

    Set マスタをDicに読み込む = dic

End Function

'--------------------------------------------------------------
' 注文を1行ずつ読んでジョイン・集計し Dictionary に格納して返す
' key=販売年月_商品コード, value=Array(0..7)
'   (0)販売年月 (1)商品コード (2)商品名 (3)単品原価 (4)単品売価
'   (5)売上個数 (6)売上金額  (7)原価合計
'--------------------------------------------------------------
Private Function 注文を集計してDicに格納する( _
    ws As Worksheet, dicマスタ As Object) As Object

    Dim dic      As Object
    Dim LastRow  As Long
    Dim i        As Long
    Dim コード   As String
    Dim 注文日   As Date
    Dim 販売年月 As String
    Dim 集計キー As String
    Dim 数量     As Long
    Dim m        As Variant
    Dim 原価     As Double
    Dim 売価     As Double
    Dim 既存     As Variant
    Dim 新規(7)  As Variant

    Set dic = CreateObject("Scripting.Dictionary")
    dic.CompareMode = 1
    LastRow = ws.Cells(ws.Rows.Count, 1).End(xlUp).Row

    For i = 2 To LastRow
        コード = CStr(ws.Cells(i, 3).Value)
        If Not dicマスタ.Exists(コード) Then GoTo 次の注文行
        If Not IsDate(ws.Cells(i, 2).Value) Then GoTo 次の注文行

        注文日   = CDate(ws.Cells(i, 2).Value)
        販売年月 = Format(注文日, "yyyy/mm")
        数量     = CLng(ws.Cells(i, 4).Value)
        集計キー = 販売年月 & "_" & コード

        m    = dicマスタ(コード)
        原価 = CDbl(m(5))
        売価 = CDbl(m(6))

        If dic.Exists(集計キー) Then
            ' 既存エントリに数量・金額を加算する
            ' ※ VBAのDictionaryはValueを直接変更できないため、いったん取り出して戻す
            既存    = dic(集計キー)
            既存(5) = 既存(5) + 数量
            既存(6) = 既存(6) + 数量 * 売価
            既存(7) = 既存(7) + 数量 * 原価
            dic(集計キー) = 既存
        Else
            新規(0) = 販売年月 : 新規(1) = コード
            新規(2) = m(0)            ' 商品名
            新規(3) = 原価            ' 単品原価
            新規(4) = 売価            ' 単品売価
            新規(5) = 数量            ' 売上個数
            新規(6) = 数量 * 売価     ' 売上金額
            新規(7) = 数量 * 原価     ' 原価合計
            dic(集計キー) = 新規
        End If
次の注文行:
    Next i

    Set 注文を集計してDicに格納する = dic

End Function

'--------------------------------------------------------------
' Dictionary を2次元配列に変換して返す（利益額・利益率も算出する）
' 列: (0)販売年月 (1)商品コード (2)商品名 (3)単品原価 (4)単品売価
'     (5)売上個数 (6)売上金額  (7)原価合計 (8)利益額  (9)利益率
'--------------------------------------------------------------
Private Function DicをDataに変換する(dic As Object, 件数 As Long) As Variant

    Dim data()   As Variant
    Dim キー一覧 As Variant
    Dim エントリ As Variant
    Dim j        As Long
    Dim 売上金額 As Double
    Dim 原価合計 As Double
    Dim 利益額   As Double

    ReDim data(件数 - 1, 9)
    キー一覧 = dic.Keys

    For j = 0 To 件数 - 1
        エントリ = dic(キー一覧(j))
        売上金額 = CDbl(エントリ(6))
        原価合計 = CDbl(エントリ(7))
        利益額   = 売上金額 - 原価合計

        data(j, 0) = エントリ(0)  ' 販売年月
        data(j, 1) = エントリ(1)  ' 商品コード
        data(j, 2) = エントリ(2)  ' 商品名
        data(j, 3) = エントリ(3)  ' 単品原価
        data(j, 4) = エントリ(4)  ' 単品売価
        data(j, 5) = エントリ(5)  ' 売上個数
        data(j, 6) = 売上金額
        data(j, 7) = 原価合計
        data(j, 8) = 利益額
        data(j, 9) = IIf(売上金額 <> 0, 利益額 / 売上金額, 0)  ' 利益率
    Next j

    DicをDataに変換する = data

End Function

'--------------------------------------------------------------
' バブルソート: 販売年月昇順 → 同月内は利益額降順
' ByRef により呼び出し元の配列を直接書き換える
'--------------------------------------------------------------
Private Sub バブルソート(ByRef data As Variant, 件数 As Long)

    Dim p         As Long
    Dim q         As Long
    Dim k         As Long
    Dim 入替      As Boolean
    Dim 一時行(9) As Variant

    For p = 0 To 件数 - 2
        For q = 0 To 件数 - 2 - p
            入替 = False
            If CStr(data(q, 0)) > CStr(data(q + 1, 0)) Then
                入替 = True
            ElseIf CStr(data(q, 0)) = CStr(data(q + 1, 0)) Then
                ' 同じ販売年月なら利益額の降順（大きい方を前に）
                If CDbl(data(q, 8)) < CDbl(data(q + 1, 8)) Then 入替 = True
            End If

            If 入替 Then
                For k = 0 To 9
                    一時行(k)        = data(q, k)
                    data(q, k)       = data(q + 1, k)
                    data(q + 1, k)   = 一時行(k)
                Next k
            End If
        Next q
    Next p

End Sub

'--------------------------------------------------------------
' 集計シートのヘッダー行を書き込む
'--------------------------------------------------------------
Private Sub 集計ヘッダーを書き込む(ws As Worksheet)

    With ws
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
        .Rows(1).Font.Bold  = True
    End With

End Sub

'--------------------------------------------------------------
' ソート済みの集計データを出力シートに書き込む
'--------------------------------------------------------------
Private Sub 集計データを書き込む(ws As Worksheet, data As Variant, 件数 As Long)

    Dim i      As Long
    Dim 出力行 As Long

    For i = 0 To 件数 - 1
        出力行 = i + 2
        ws.Cells(出力行, 1).Value  = data(i, 0)  ' 販売年月
        ws.Cells(出力行, 2).Value  = data(i, 1)  ' 商品コード
        ws.Cells(出力行, 3).Value  = data(i, 2)  ' 商品名
        ws.Cells(出力行, 4).Value  = data(i, 3)  ' 単品原価
        ws.Cells(出力行, 5).Value  = data(i, 4)  ' 単品売価
        ws.Cells(出力行, 6).Value  = data(i, 5)  ' 売上個数
        ws.Cells(出力行, 7).Value  = data(i, 6)  ' 売上金額
        ws.Cells(出力行, 8).Value  = data(i, 7)  ' 原価
        ws.Cells(出力行, 9).Value  = data(i, 8)  ' 利益額
        ws.Cells(出力行, 10).Value        = data(i, 9)  ' 利益率（小数: 0.35 = 35%）
        ws.Cells(出力行, 10).NumberFormat = "0.00%"
    Next i

End Sub
