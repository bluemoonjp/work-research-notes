Attribute VB_Name = "1から3まとめ"
Option Explicit

'==============================================================================
' プログラム4: 1〜3まとめ
' このファイル単体でプログラム1〜3の処理をすべて実行する（外部モジュール不要）。
'==============================================================================
Sub 実行_1から3まとめ()

    P1_実行  ' 今年発売のHGモデルを「最新ハイグレードモデル一覧」へ
    P2_実行  ' 注文×商品マスタをジョインして「販売集計」へ
    P3_実行  ' OR条件のイレギュラー商品を「イレギュラー発注商品」へ

    MsgBox "プログラム1〜3 すべて完了しました。", vbInformation

End Sub

'==============================================================================
' プログラム1: 単純なWhere条件
'==============================================================================
Private Sub P1_実行()

    Dim wsマスタ As Worksheet
    Dim ws出力   As Worksheet
    Dim 件数     As Long

    Set wsマスタ = ThisWorkbook.Worksheets("商品マスタ")
    Set ws出力   = 出力シートを準備する("最新ハイグレードモデル一覧")

    商品マスタのヘッダーを書き込む ws出力
    件数 = 今年HGで絞り込んで転記する(wsマスタ, ws出力)
    ws出力.Columns("A:H").AutoFit

    Debug.Print "P1 完了: " & 件数 & " 件"

End Sub

Private Function 今年HGで絞り込んで転記する( _
    wsマスタ As Worksheet, ws出力 As Worksheet) As Long

    Dim LastRow As Long
    Dim i       As Long
    Dim 出力行  As Long
    Dim 今年    As Integer
    Dim 発売日  As Date
    Dim 型番    As String

    今年    = Year(Now)
    LastRow = wsマスタ.Cells(wsマスタ.Rows.Count, 1).End(xlUp).Row
    出力行  = 2

    For i = 2 To LastRow
        If Not IsDate(wsマスタ.Cells(i, 4).Value) Then GoTo 次の行
        発売日 = CDate(wsマスタ.Cells(i, 4).Value)
        型番   = CStr(wsマスタ.Cells(i, 3).Value)

        If Year(発売日) = 今年 And Right(型番, 2) = "HG" Then
            マスタ行を転記する wsマスタ, ws出力, i, 出力行
            出力行 = 出力行 + 1
        End If
次の行:
    Next i

    今年HGで絞り込んで転記する = 出力行 - 2

End Function

'==============================================================================
' プログラム2: ジョイントグループ化
'==============================================================================
Private Sub P2_実行()

    Dim dicマスタ As Object
    Dim dic集計   As Object
    Dim データ    As Variant
    Dim 件数      As Long
    Dim ws出力    As Worksheet

    Set dicマスタ = マスタをDicに読み込む(ThisWorkbook.Worksheets("商品マスタ"))
    Set dic集計   = 注文を集計してDicに格納する(ThisWorkbook.Worksheets("注文"), dicマスタ)

    件数 = dic集計.Count
    If 件数 = 0 Then
        MsgBox "P2: 集計対象のデータがありません。", vbExclamation
        Exit Sub
    End If

    データ = DicをDataに変換する(dic集計, 件数)
    バブルソート データ, 件数

    Set ws出力 = 出力シートを準備する("販売集計")
    集計ヘッダーを書き込む ws出力
    集計データを書き込む ws出力, データ, 件数
    ws出力.Columns("A:J").AutoFit

    Debug.Print "P2 完了: " & 件数 & " 件"

End Sub

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

        data(j, 0) = エントリ(0)
        data(j, 1) = エントリ(1)
        data(j, 2) = エントリ(2)
        data(j, 3) = エントリ(3)
        data(j, 4) = エントリ(4)
        data(j, 5) = エントリ(5)
        data(j, 6) = 売上金額
        data(j, 7) = 原価合計
        data(j, 8) = 利益額
        data(j, 9) = IIf(売上金額 <> 0, 利益額 / 売上金額, 0)
    Next j

    DicをDataに変換する = data

End Function

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

Private Sub 集計データを書き込む(ws As Worksheet, data As Variant, 件数 As Long)

    Dim i      As Long
    Dim 出力行 As Long

    For i = 0 To 件数 - 1
        出力行 = i + 2
        ws.Cells(出力行, 1).Value  = data(i, 0)
        ws.Cells(出力行, 2).Value  = data(i, 1)
        ws.Cells(出力行, 3).Value  = data(i, 2)
        ws.Cells(出力行, 4).Value  = data(i, 3)
        ws.Cells(出力行, 5).Value  = data(i, 4)
        ws.Cells(出力行, 6).Value  = data(i, 5)
        ws.Cells(出力行, 7).Value  = data(i, 6)
        ws.Cells(出力行, 8).Value  = data(i, 7)
        ws.Cells(出力行, 9).Value  = data(i, 8)
        ws.Cells(出力行, 10).Value        = data(i, 9)
        ws.Cells(出力行, 10).NumberFormat = "0.00%"
    Next i

End Sub

'==============================================================================
' プログラム3: 複雑なWhere条件
'==============================================================================
Private Sub P3_実行()

    Dim wsマスタ As Worksheet
    Dim ws出力   As Worksheet
    Dim 件数     As Long

    Set wsマスタ = ThisWorkbook.Worksheets("商品マスタ")
    Set ws出力   = 出力シートを準備する("イレギュラー発注商品")

    商品マスタのヘッダーを書き込む ws出力
    件数 = OR条件で絞り込んで転記する(wsマスタ, ws出力)
    ws出力.Columns("A:H").AutoFit

    Debug.Print "P3 完了: " & 件数 & " 件"

End Sub

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

'==============================================================================
' 共通ヘルパー（P1・P2・P3 すべてで使用）
'==============================================================================

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

Private Sub マスタ行を転記する( _
    wsFrom As Worksheet, wsTo As Worksheet, _
    元行 As Long, 先行 As Long)

    Dim col As Integer
    For col = 1 To 8
        wsTo.Cells(先行, col).Value = wsFrom.Cells(元行, col).Value
    Next col

End Sub
