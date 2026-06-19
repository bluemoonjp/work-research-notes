// =============================================================================
// 1: 型と変数宣言 - コードサンプル
// =============================================================================

using System.Text;

// エントリポイント（トップレベルステートメント）
TypeSamples.RunAll();

// =============================================================================
// サンプルクラス
// =============================================================================

class TypeSamples
{
    public static void RunAll()
    {
        Console.WriteLine("=== 1: 型と変数宣言 ===\n");

        DemonstrateBuiltInTypes();
        DemonstrateVar();
        DemonstrateValueVsReference();
        DemonstrateConstAndReadonly();
        DemonstrateStringAndStringBuilder();
        DemonstrateTuples();
    }

    // -------------------------------------------------------------------------
    // 1. 組み込み型
    // -------------------------------------------------------------------------
    static void DemonstrateBuiltInTypes()
    {
        Console.WriteLine("--- 組み込み型 ---");

        bool isActive = true;
        byte byteVal = 255;            // 0〜255
        int count = 42;
        long bigNumber = 9_000_000_000L; // L サフィックスで long リテラル
        float floatVal = 3.14f;         // f サフィックスで float リテラル
        double doubleVal = 3.14159265358979;
        decimal price = 1234.56m;       // m サフィックスで decimal リテラル
        char letter = 'A';
        string text = "Hello, C#!";
        object anything = 42;           // すべての型を格納できる（ボクシングが発生）

        Console.WriteLine($"bool:    {isActive}");
        Console.WriteLine($"byte:    {byteVal}");
        Console.WriteLine($"int:     {count}");
        Console.WriteLine($"long:    {bigNumber}");
        Console.WriteLine($"float:   {floatVal}");
        Console.WriteLine($"double:  {doubleVal}");
        Console.WriteLine($"decimal: {price}");
        Console.WriteLine($"char:    {letter}");
        Console.WriteLine($"string:  {text}");
        Console.WriteLine($"object:  {anything} (型: {anything.GetType().Name})");

        // 浮動小数点誤差のデモ
        Console.WriteLine("\n-- 浮動小数点誤差 --");
        double d = 0.1 + 0.2;
        decimal dec = 0.1m + 0.2m;
        Console.WriteLine($"double:  0.1 + 0.2 = {d}");    // 0.30000000000000004
        Console.WriteLine($"decimal: 0.1 + 0.2 = {dec}");  // 0.3

        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    // 2. var による型推論
    // -------------------------------------------------------------------------
    static void DemonstrateVar()
    {
        Console.WriteLine("--- var による型推論 ---");

        // var はコンパイル時に型が確定する（動的型付けではない）
        var n = 100;              // int に推論
        var s = "Hello";          // string に推論
        var list = new List<int> { 1, 2, 3 }; // List<int> に推論
        var pi = 3.14;            // double に推論

        Console.WriteLine($"n の型:    {n.GetType().Name}");    // Int32
        Console.WriteLine($"s の型:    {s.GetType().Name}");    // String
        Console.WriteLine($"list の型: {list.GetType().Name}"); // List`1
        Console.WriteLine($"pi の型:   {pi.GetType().Name}");   // Double

        // var は「型が右辺から明らか」な場合に使うのが推奨スタイル
        var user = new UserRecord("Alice", 30);  // UserRecord と明らか
        Console.WriteLine($"user: {user}");

        // 型が右辺から不明な場合は明示的な型宣言を使う
        string? input = Console.IsInputRedirected ? "test input" : null;
        Console.WriteLine($"input: {input ?? "(null)"}");

        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    // 3. 値型 vs 参照型
    // -------------------------------------------------------------------------
    static void DemonstrateValueVsReference()
    {
        Console.WriteLine("--- 値型 vs 参照型 ---");

        // 値型: コピーが発生する
        int a = 10;
        int b = a;   // 値がコピーされる
        b = 20;
        Console.WriteLine($"値型  a={a}, b={b}"); // a=10, b=20（a は変わらない）

        // 参照型: 参照がコピーされる（同じオブジェクトを指す）
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = list1;  // 参照がコピーされる
        list2.Add(4);
        Console.WriteLine($"参照型 list1.Count={list1.Count}"); // 4（list1 も影響を受ける）

        // string は参照型だが不変（変更操作は新しいオブジェクトを返す）
        string s1 = "Hello";
        string s2 = s1;
        s1 = s1 + " World"; // 新しい string が生成される
        Console.WriteLine($"string s1={s1}, s2={s2}"); // s1="Hello World", s2="Hello"

        // 値型の struct: コピーが発生する
        var p1 = new PointStruct(1, 2);
        var p2 = p1;        // 値がコピーされる
        p2 = new PointStruct(9, 9);
        Console.WriteLine($"struct p1={p1}, p2={p2}"); // p1 は変わらない

        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    // 4. const と readonly
    // -------------------------------------------------------------------------
    static void DemonstrateConstAndReadonly()
    {
        Console.WriteLine("--- const と readonly ---");

        Console.WriteLine($"MaxRetry:  {AppConfig.MaxRetry}");    // コンパイル時定数
        Console.WriteLine($"AppName:   {AppConfig.AppName}");
        Console.WriteLine($"Version:   {AppConfig.Version}");     // 実行時定数

        var config1 = new AppConfig();
        var config2 = new AppConfig();

        // インスタンスごとに異なる readonly フィールド
        Console.WriteLine($"config1.CreatedAt: {config1.CreatedAt:HH:mm:ss.fff}");
        Console.WriteLine($"config2.CreatedAt: {config2.CreatedAt:HH:mm:ss.fff}");

        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    // 5. string と StringBuilder
    // -------------------------------------------------------------------------
    static void DemonstrateStringAndStringBuilder()
    {
        Console.WriteLine("--- string と StringBuilder ---");

        // 少数の連結: string 補間が読みやすい
        string firstName = "太郎";
        string lastName = "山田";
        string fullName = $"{lastName} {firstName}"; // 文字列補間
        Console.WriteLine($"フルネーム: {fullName}");

        // string.Join: コレクションの結合に便利
        string[] words = { "apple", "banana", "cherry" };
        string joined = string.Join(", ", words);
        Console.WriteLine($"Join: {joined}");

        // 大量連結: StringBuilder を使う
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var sb = new StringBuilder();
        for (int i = 0; i < 10_000; i++)
        {
            sb.Append(i);
            sb.Append(',');
        }
        string sbResult = sb.ToString();
        sw.Stop();
        Console.WriteLine($"StringBuilder 10000回連結: {sw.ElapsedMilliseconds}ms, 長さ={sbResult.Length}");

        // StringBuilder の主要メソッド
        var sb2 = new StringBuilder("Hello");
        sb2.Append(" World");        // 末尾に追加
        sb2.Insert(5, ",");          // 指定位置に挿入
        sb2.Replace("World", "C#"); // 置換
        Console.WriteLine($"StringBuilder 操作: {sb2}");

        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    // 6. タプル
    // -------------------------------------------------------------------------
    static void DemonstrateTuples()
    {
        Console.WriteLine("--- タプル ---");

        // 名前付きタプルの宣言
        (int Min, int Max) range = (1, 100);
        Console.WriteLine($"Min={range.Min}, Max={range.Max}");

        // メソッドの戻り値としてのタプル
        var result = Divide(10, 3);
        Console.WriteLine($"10 ÷ 3 = {result.Quotient} 余り {result.Remainder}");

        // デコンストラクション（分解）
        var (quotient, remainder) = Divide(17, 5);
        Console.WriteLine($"17 ÷ 5 = {quotient} 余り {remainder}");

        // _ でタプルの不要な要素を捨てる
        var (_, rem) = Divide(20, 7);
        Console.WriteLine($"20 ÷ 7 の余り: {rem}");

        // タプルは値型（System.ValueTuple）であることの確認
        var t1 = (X: 1, Y: 2);
        var t2 = t1;  // 値がコピーされる
        t2.X = 99;
        Console.WriteLine($"t1.X={t1.X}, t2.X={t2.X}"); // t1.X=1（コピーなので影響なし）

        Console.WriteLine();
    }

    // タプルを戻り値に使うメソッド
    static (int Quotient, int Remainder) Divide(int dividend, int divisor)
    {
        return (dividend / divisor, dividend % divisor);
    }
}

// -------------------------------------------------------------------------
// サポート型の定義
// -------------------------------------------------------------------------

// const と readonly のデモ用クラス
class AppConfig
{
    // const: コンパイル時定数。暗黙的に static。プリミティブ/string のみ
    public const int MaxRetry = 3;
    public const string AppName = "MyApp";

    // static readonly: 実行時に一度だけ評価される定数
    public static readonly string Version = LoadVersion();

    // インスタンス readonly: コンストラクタで設定できる
    public readonly DateTime CreatedAt;

    public AppConfig()
    {
        CreatedAt = DateTime.Now; // コンストラクタで設定可能
        // CreatedAt = DateTime.Now; ← コンストラクタ以外では代入不可
    }

    static string LoadVersion()
    {
        // 実際にはファイルや設定から読み込む場合がある
        return "1.0.0";
    }
}

// 値型（struct）のデモ用
struct PointStruct
{
    public int X { get; }
    public int Y { get; }

    public PointStruct(int x, int y) { X = x; Y = y; }

    public override string ToString() => $"({X}, {Y})";
}

// var のデモで使うシンプルな record
record UserRecord(string Name, int Age);
