// デバッグ練習用サンプルコード
// 以下のシナリオを試す:
//   1. F9 でブレークポイントを設置して F5 で実行
//   2. F10（ステップ オーバー）で 1 行ずつ進める
//   3. F11（ステップ イン）で Multiply メソッドの中に入る
//   4. 条件付き BP: i == 5 のときだけ止まるよう設定して実行

using System.Collections.Generic;

var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
var results = new List<int>();

// ← ここにブレークポイントを設置してみる
foreach (var i in numbers)
{
    int doubled = Multiply(i, 2);   // F11 でこのメソッドに入れる
    results.Add(doubled);
}

Console.WriteLine("結果:");
foreach (var r in results)
{
    Console.WriteLine(r);
}

// ローカルウィンドウで results の中身を確認
// イミディエイトウィンドウで ? results.Count と入力してみる
Console.ReadLine();

static int Multiply(int a, int b)
{
    // F11 で入ると、ここで一時停止する
    int result = a * b;
    return result;
}
