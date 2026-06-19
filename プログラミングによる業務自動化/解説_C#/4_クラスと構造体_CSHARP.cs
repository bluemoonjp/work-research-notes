// 4_クラスと構造体_CSHARP.cs
// 「クラスと構造体」記事のコードサンプル

using System;
using System.Collections.Generic;

// ============================================================
// 1. class と struct のコピーセマンティクス比較
// ============================================================

// 参照型（class）
class PointClass
{
    public int X { get; set; }
    public int Y { get; set; }

    public PointClass(int x, int y) { X = x; Y = y; }

    public override string ToString() => $"PointClass({X}, {Y})";
}

// 値型（struct）
struct PointStruct
{
    public int X { get; set; }
    public int Y { get; set; }

    public PointStruct(int x, int y) { X = x; Y = y; }

    public override string ToString() => $"PointStruct({X}, {Y})";
}

// ============================================================
// 2. record のサンプル（C# 9+）
// ============================================================

// record は値等値性と with 式（非破壊的変更）を持つ
record Person(string Name, int Age);

// record struct（C# 10+）は struct ベースの record
record struct Coordinate(double Latitude, double Longitude);

// ============================================================
// 3. sealed クラス
// ============================================================
sealed class Singleton
{
    private static Singleton? _instance;

    private Singleton() { }

    public static Singleton Instance => _instance ??= new Singleton();

    public string GetInfo() => "Singleton インスタンス";
}

// ============================================================
// 4. アクセス修飾子のデモ
// ============================================================
class BankAccount
{
    // フィールドは private: 外部から直接変更不可
    private decimal _balance;

    // public: 外部から読み書き可
    public string Owner { get; }

    // internal: 同一アセンブリ内のみ
    internal string InternalCode { get; } = "ACCT-001";

    public BankAccount(string owner, decimal initialBalance)
    {
        Owner = owner;
        _balance = initialBalance;
    }

    // protected: 派生クラスからもアクセス可
    protected decimal Balance => _balance;

    public void Deposit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("金額は正の値にしてください");
        _balance += amount;
    }

    public override string ToString() => $"{Owner}: {_balance:C}";
}

// ============================================================
// 5. 静的メンバーと静的クラス
// ============================================================

// 静的クラス: ユーティリティメソッドのまとまり
static class MathUtils
{
    public static int Square(int x) => x * x;
    public static double CircleArea(double radius) => Math.PI * radius * radius;
}

// 静的フィールドで全インスタンス共通のカウンタ
class Counter
{
    private static int _count = 0;
    public static int Count => _count;

    public Counter() => _count++;
}

// ============================================================
// 6. オブジェクト初期化子
// ============================================================
class Product
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }

    public override string ToString() => $"{Name} ¥{Price:N0} 在庫:{Stock}";
}

// ============================================================
// エントリーポイント
// ============================================================
class Program
{
    static void Main()
    {
        Console.WriteLine("=== 1. class vs struct のコピーセマンティクス ===");

        // class: 代入は参照のコピー → 一方を変更すると両方変わる
        var classA = new PointClass(1, 2);
        var classB = classA;       // 同じオブジェクトを参照
        classB.X = 99;
        Console.WriteLine($"classA: {classA}"); // X=99 に変わっている
        Console.WriteLine($"classB: {classB}");

        Console.WriteLine();

        // struct: 代入は値のコピー → 独立した別のコピー
        var structA = new PointStruct(1, 2);
        var structB = structA;     // 独立したコピー
        structB.X = 99;
        Console.WriteLine($"structA: {structA}"); // X=1 のまま
        Console.WriteLine($"structB: {structB}");

        Console.WriteLine();
        Console.WriteLine("=== 2. record のサンプル ===");

        var alice = new Person("Alice", 30);
        var bob = alice with { Name = "Bob" }; // 新しいインスタンスを作成（with 式）

        Console.WriteLine($"alice: {alice}");
        Console.WriteLine($"bob:   {bob}");

        // record は値等値性: プロパティが同じなら等しい
        var alice2 = new Person("Alice", 30);
        Console.WriteLine($"alice == alice2: {alice == alice2}"); // True
        Console.WriteLine($"alice == bob:    {alice == bob}");    // False

        // record struct のサンプル
        var tokyo = new Coordinate(35.6895, 139.6917);
        var osaka = tokyo with { Latitude = 34.6937, Longitude = 135.5023 };
        Console.WriteLine($"tokyo: {tokyo}");
        Console.WriteLine($"osaka: {osaka}");

        Console.WriteLine();
        Console.WriteLine("=== 3. sealed クラス（Singleton） ===");

        var s1 = Singleton.Instance;
        var s2 = Singleton.Instance;
        Console.WriteLine($"同一インスタンス: {ReferenceEquals(s1, s2)}"); // True
        Console.WriteLine(s1.GetInfo());

        Console.WriteLine();
        Console.WriteLine("=== 4. アクセス修飾子 ===");

        var account = new BankAccount("山田太郎", 10_000m);
        account.Deposit(5_000m);
        Console.WriteLine(account);
        // account._balance = 0; // コンパイルエラー: private フィールドはアクセス不可

        Console.WriteLine();
        Console.WriteLine("=== 5. 静的メンバー ===");

        Console.WriteLine($"Square(7):       {MathUtils.Square(7)}");
        Console.WriteLine($"CircleArea(3.0): {MathUtils.CircleArea(3.0):F4}");

        _ = new Counter();
        _ = new Counter();
        _ = new Counter();
        Console.WriteLine($"Counter.Count: {Counter.Count}"); // 3

        Console.WriteLine();
        Console.WriteLine("=== 6. オブジェクト初期化子 ===");

        // コンストラクタ引数なしでプロパティをまとめて初期化
        var product = new Product
        {
            Name  = "C# 入門書",
            Price = 3_500m,
            Stock = 20,
        };
        Console.WriteLine(product);

        // コレクション初期化子
        var products = new List<Product>
        {
            new Product { Name = "キーボード", Price = 12_000m, Stock = 5  },
            new Product { Name = "マウス",     Price =  3_500m, Stock = 10 },
        };

        foreach (var p in products)
            Console.WriteLine($"  {p}");
    }
}
