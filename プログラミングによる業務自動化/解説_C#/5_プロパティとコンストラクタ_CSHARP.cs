// 5_プロパティとコンストラクタ_CSHARP.cs
// 「プロパティとコンストラクタ」記事のコードサンプル

using System;

// ============================================================
// 1. プロパティとフィールドの比較
// ============================================================

class Temperature
{
    // private フィールド（バッキングフィールド）
    private double _celsius;

    // バリデーション付きプロパティ
    public double Celsius
    {
        get => _celsius;
        set
        {
            if (value < -273.15)
                throw new ArgumentOutOfRangeException(nameof(value), "絶対零度より低い温度は設定できません");
            _celsius = value;
        }
    }

    // 計算プロパティ（バッキングフィールドなし）
    public double Fahrenheit => _celsius * 9 / 5 + 32;
    public double Kelvin     => _celsius + 273.15;

    public Temperature(double celsius) => Celsius = celsius; // set を通してバリデーション

    public override string ToString()
        => $"{Celsius:F1}°C / {Fahrenheit:F1}°F / {Kelvin:F2}K";
}

// ============================================================
// 2. 自動実装プロパティ
// ============================================================

class Person
{
    public string Name     { get; set; } = string.Empty;
    public int    Age      { get; set; }

    // コンストラクタ内のみ書き込み可能
    public DateTime CreatedAt { get; } = DateTime.Now;

    public override string ToString() => $"{Name}({Age}歳) 作成:{CreatedAt:yyyy-MM-dd}";
}

// ============================================================
// 3. init アクセサ（C# 9+）
// ============================================================

class ServerConfig
{
    public string Host     { get; init; } = "localhost";
    public int    Port     { get; init; } = 8080;
    public bool   UseTls   { get; init; } = false;

    public string BaseUrl => $"{(UseTls ? "https" : "http")}://{Host}:{Port}";
}

// ============================================================
// 4. コンストラクタ委譲（this(...)）
// ============================================================

class Rectangle
{
    public double Width  { get; }
    public double Height { get; }

    // 共通コンストラクタ
    public Rectangle(double width, double height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width と Height は正の値にしてください");
        Width  = width;
        Height = height;
    }

    // 正方形用ショートカット: this で共通コンストラクタへ委譲
    public Rectangle(double side) : this(side, side) { }

    // デフォルト（1×1 の正方形）
    public Rectangle() : this(1.0) { }

    public double Area      => Width * Height;
    public double Perimeter => 2 * (Width + Height);

    public override string ToString()
        => $"Rectangle({Width} x {Height}): 面積={Area}, 周長={Perimeter}";
}

// ============================================================
// 5. プライマリコンストラクタ（C# 12+）
// ============================================================

// 従来の書き方
class PointLegacy
{
    public double X { get; }
    public double Y { get; }

    public PointLegacy(double x, double y) { X = x; Y = y; }

    public double DistanceTo(PointLegacy other)
        => Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));

    public override string ToString() => $"({X}, {Y})";
}

// プライマリコンストラクタ（C# 12+）: 宣言がシンプルになる
class Point(double x, double y)
{
    // プライマリコンストラクタのパラメータをプロパティへ代入（イミュータブルにするため明示する）
    public double X { get; } = x;
    public double Y { get; } = y;

    public double DistanceTo(Point other)
        => Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));

    public override string ToString() => $"({X}, {Y})";
}

// サービスクラスでの典型的な使い方: 依存を受け取るだけ
class Logger(string prefix)
{
    // パラメータを直接フィールドとして利用（プロパティへの代入不要）
    public void Log(string message) => Console.WriteLine($"[{prefix}] {message}");
}

// ============================================================
// エントリーポイント
// ============================================================
class Program
{
    static void Main()
    {
        Console.WriteLine("=== 1. プロパティとフィールドの比較 ===");

        var temp = new Temperature(100.0);
        Console.WriteLine(temp); // 100°C / 212°F / 373.15K

        temp.Celsius = -10.0;
        Console.WriteLine(temp);

        try
        {
            temp.Celsius = -300; // バリデーションで例外
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine($"例外: {ex.Message.Split('\r')[0]}");
        }

        Console.WriteLine();
        Console.WriteLine("=== 2. 自動実装プロパティ ===");

        var person = new Person { Name = "田中花子", Age = 28 };
        Console.WriteLine(person);

        Console.WriteLine();
        Console.WriteLine("=== 3. init アクセサ（C# 9+） ===");

        // オブジェクト初期化子で設定
        var config = new ServerConfig { Host = "api.example.com", Port = 443, UseTls = true };
        Console.WriteLine($"BaseUrl: {config.BaseUrl}");

        // config.Host = "other.com"; // コンパイルエラー: init 後は変更不可

        // デフォルト値でも作れる
        var devConfig = new ServerConfig();
        Console.WriteLine($"BaseUrl: {devConfig.BaseUrl}");

        Console.WriteLine();
        Console.WriteLine("=== 4. コンストラクタ委譲 ===");

        var r1 = new Rectangle(4.0, 3.0); // 引数2つ
        var r2 = new Rectangle(5.0);      // 引数1つ（正方形）: this(5.0, 5.0) へ委譲
        var r3 = new Rectangle();         // 引数なし（1×1）: this(1.0) → this(1.0, 1.0) へ委譲

        Console.WriteLine(r1);
        Console.WriteLine(r2);
        Console.WriteLine(r3);

        Console.WriteLine();
        Console.WriteLine("=== 5. プライマリコンストラクタ（C# 12+） ===");

        var p1 = new Point(0.0, 0.0);
        var p2 = new Point(3.0, 4.0);
        Console.WriteLine($"{p1} → {p2}: 距離 = {p1.DistanceTo(p2):F2}");

        var logger = new Logger("INFO");
        logger.Log("アプリケーション起動");
        logger.Log("処理完了");
    }
}
