Attribute VB_Name = "複雑なWhere条件"
Option Explicit

'==============================================================================
' プログラム3: 複雑なWhere条件
' 商品マスタから複合OR条件で対象商品を抽出し、
' 「イレギュラー発注商品」シートに出力する。
'
' 抽出条件:
'   (仕入元 = "A社" AND 調達区分 = "国内調達")
'   OR (仕入元 = "B社" AND 調達区分 = "受注生産")
'==============================================================================
Sub 実行_複雑なWhere条件()

    Dim wsマスタ As Worksheet
    Dim ws出力   As Worksheet
    Dim 件数     As Long

    Set wsマスタ = ThisWorkbook.Worksheets("商品マスタ")
    Set ws出力   = 出力シートを準備する("イレギュラー発注商品")

    商品マスタのヘッダーを書き込む ws出力
    件数 = OR条件で絞り込んで転記する(wsマスタ, ws出力)

    ws出力.Columns("A:H").AutoFit
    MsgBox "プログラム3 完了: 「イレギュラー発注商品」シートに " & 件数 & " 件出力しました。", vbInformation

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
' 商品マスタ共通のヘッダー行（8列）を書き込む
'--------------------------------------------------------------
Private Sub 商品マスタのヘッダーを書き込む(ws As Worksheet)

    With ws
        .Cells(1, 1).Value = "商品コード"
        .Cells(1, 2).Value = "商品名"
        .Cells(1, 3).Value = "型番"
        .Cells(1, 4).Value = "発売日"
        .Cells(1, 5).Value = "仕入元"
        .Cells(1, 6).Value = "調達区分"
        .Cells(1, 7).Value = "単品原価"
        .Cells(1, 8).Value = "単品売価"
        .Rows(1).Font.Bold = True
    End With

End Sub

'--------------------------------------------------------------
' Where条件: (A社+国内調達) OR (B社+受注生産) → 転記して件数を返す
'--------------------------------------------------------------
Private Function OR条件で絞り込んで転記する( _
    wsマスタ As Worksheet, ws出力 As Worksheet) As Long

    Dim LastRow  As Long
    Dim i        As Long
    Dim 出力行   As Long
    Dim 仕入元   As String
    Dim 調達区分 As String

    LastRow = wsマスタ.Cells(wsマスタ.Rows.Count, 1).End(xlUp).Row
    出力行  = 2

    For i = 2 To LastRow
        仕入元   = CStr(wsマスタ.Cells(i, 5).Value)
        調達区分 = CStr(wsマスタ.Cells(i, 6).Value)

        If (仕入元 = "A社" And 調達区分 = "国内調達") Or _
           (仕入元 = "B社" And 調達区分 = "受注生産") Then
            マスタ行を転記する wsマスタ, ws出力, i, 出力行
            出力行 = 出力行 + 1
        End If
    Next i

    OR条件で絞り込んで転記する = 出力行 - 2

End Function

'--------------------------------------------------------------
' 商品マスタの1行（8列分）を出力シートへ転記する
'--------------------------------------------------------------
Private Sub マスタ行を転記する( _
    wsFrom As Worksheet, wsTo As Worksheet, _
    元行 As Long, 先行 As Long)

    Dim col As Integer
    For col = 1 To 8
        wsTo.Cells(先行, col).Value = wsFrom.Cells(元行, col).Value
    Next col

End Sub
