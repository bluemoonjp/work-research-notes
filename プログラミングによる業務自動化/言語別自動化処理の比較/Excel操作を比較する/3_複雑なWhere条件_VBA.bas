Attribute VB_Name = "複雑なWhere条件"
Option Explicit

'==============================================================================
' プログラム3: 複雑なWhere条件
' 商品マスタから複合条件で対象商品を抽出し、
' 「イレギュラー発注商品」シートに出力する。
'
' 抽出条件:
'   (仕入元 = "A社" AND 調達区分 = "国内調達")
'   OR
'   (仕入元 = "B社" AND 調達区分 = "受注生産")
'==============================================================================
Sub 実行_複雑なWhere条件()

    Dim wsマスタ    As Worksheet
    Dim ws出力      As Worksheet
    Dim LastRow     As Long
    Dim i           As Long
    Dim 出力行       As Long
    Dim 仕入元       As String
    Dim 調達区分     As String

    ' --- 入力シートの参照 ---
    Set wsマスタ = ThisWorkbook.Worksheets("商品マスタ")

    ' --- 出力シートが既に存在する場合は削除して再作成 ---
    Application.DisplayAlerts = False
    On Error Resume Next
    ThisWorkbook.Worksheets("イレギュラー発注商品").Delete
    On Error GoTo 0
    Application.DisplayAlerts = True

    Set ws出力 = ThisWorkbook.Worksheets.Add
    ws出力.Name = "イレギュラー発注商品"

    ' --- ヘッダー行の出力 ---
    With ws出力
        .Cells(1, 1).Value = "商品コード"
        .Cells(1, 2).Value = "商品名"
        .Cells(1, 3).Value = "型番"
        .Cells(1, 4).Value = "発売日"
        .Cells(1, 5).Value = "仕入元"
        .Cells(1, 6).Value = "調達区分"
        .Cells(1, 7).Value = "単品原価"
        .Cells(1, 8).Value = "単品売価"

        ' ヘッダー行を太字にする
        .Rows(1).Font.Bold = True
    End With

    ' --- 商品マスタの最終行を取得 ---
    LastRow = wsマスタ.Cells(wsマスタ.Rows.Count, 1).End(xlUp).Row

    ' --- データ行を1行ずつ確認し、条件を満たす行を出力シートへコピー ---
    出力行 = 2  ' 出力開始行（2行目からデータ）

    For i = 2 To LastRow

        仕入元   = CStr(wsマスタ.Cells(i, 5).Value)
        調達区分 = CStr(wsマスタ.Cells(i, 6).Value)

        ' --- Where条件（複合条件）:
        '   ① A社 かつ 国内調達
        '   ② B社 かつ 受注生産
        '   のどちらか一方を満たす行を抽出
        If (仕入元 = "A社" And 調達区分 = "国内調達") Or _
           (仕入元 = "B社" And 調達区分 = "受注生産") Then

            ' 条件を満たした行を出力シートへ転記
            ws出力.Cells(出力行, 1).Value = wsマスタ.Cells(i, 1).Value  ' 商品コード
            ws出力.Cells(出力行, 2).Value = wsマスタ.Cells(i, 2).Value  ' 商品名
            ws出力.Cells(出力行, 3).Value = wsマスタ.Cells(i, 3).Value  ' 型番
            ws出力.Cells(出力行, 4).Value = wsマスタ.Cells(i, 4).Value  ' 発売日
            ws出力.Cells(出力行, 5).Value = wsマスタ.Cells(i, 5).Value  ' 仕入元
            ws出力.Cells(出力行, 6).Value = wsマスタ.Cells(i, 6).Value  ' 調達区分
            ws出力.Cells(出力行, 7).Value = wsマスタ.Cells(i, 7).Value  ' 単品原価
            ws出力.Cells(出力行, 8).Value = wsマスタ.Cells(i, 8).Value  ' 単品売価

            出力行 = 出力行 + 1
        End If

    Next i

    ' --- 列幅を内容に合わせて自動調整 ---
    ws出力.Columns("A:H").AutoFit

    MsgBox "プログラム3 完了: 「イレギュラー発注商品」シートに " & (出力行 - 2) & " 件出力しました。", vbInformation

End Sub
