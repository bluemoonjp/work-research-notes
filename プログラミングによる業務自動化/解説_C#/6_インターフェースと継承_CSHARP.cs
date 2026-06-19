// 6_インターフェースと継承_CSHARP.cs
// 「インターフェースと継承」記事のコードサンプル

using System;
using System.Collections.Generic;

// ============================================================
// 1. インターフェースの定義と実装
// ============================================================

interface IShape
{
    double Area { get; }
    double Perimeter { get; }

    // デフォルトインターフェースメソッド（C# 8+）: 省略可
    string Describe() => $"面積={Area:F2}, 周長={Perimeter:F2}";
}

// 複数インターフェース
interface IPrintable
{
    void Print();
}

class Circle : IShape, IPrintable
{
    public double Radius { get; }

    public Circle(double radius) => Radius = radius;

    public double Area      => Math.PI * Radius * Radius;
    public double Perimeter => 2 * Math.PI * Radius;

    // デフォルト実装を使わず明示的に実装
    public string Describe() => $"Circle(r={Radius:F1}): 面積={Area:F2}";

    public void Print() => Console.WriteLine(Describe());
}

class RightTriangle : IShape
{
    public double Base   { get; }
    public double Height { get; }
    private double Hypotenuse => Math.Sqrt(Base * Base + Height * Height);

    public RightTriangle(double b, double h) { Base = b; Height = h; }

    public double Area      => Base * Height / 2;
    public double Perimeter => Base + Height + Hypotenuse;

    // Describe() はデフォルト実装を利用（明示的な実装なし）
}

// ============================================================
// 2. 抽象クラスと継承
// ============================================================

// 抽象クラス: 共通の状態と一部の実装を持つ基底型
abstract class Animal
{
    public string Name { get; }

    protected Animal(string name) => Name = name;

    // abstract: サブクラスで必ず実装する
    public abstract string Speak();

    // virtual: サブクラスでオーバーライド可（しなくてもよい）
    public virtual string Describe() => $"{GetType().Name}「{Name}」";

    public override string ToString() => Describe();
}

class Dog : Animal
{
    public Dog(string name) : base(name) { }

    public override string Speak() => "ワン！";
}

class Cat : Animal
{
    public Cat(string name) : base(name) { }

    public override string Speak() => "ニャー！";

    // override + 独自の追加情報
    public override string Describe() => base.Describe() + "（猫）";
}

// sealed override: これ以上オーバーライドさせない
class GuideDog : Dog
{
    public GuideDog(string name) : base(name) { }

    // sealed override: GuideDog を継承してもこのメソッドはオーバーライド不可
    public sealed override string Speak() => "（静かに誘導します）";
}

// ============================================================
// 3. base キーワード
// ============================================================

class LoudCat : Cat
{
    public LoudCat(string name) : base(name) { }

    public override string Speak()
    {
        var parentSpeech = base.Speak(); // 親 Cat.Speak() の結果を再利用
        return parentSpeech + parentSpeech + "!!";
    }
}

// ============================================================
// 4. is / as によるダウンキャスト
// ============================================================

class PetClinic
{
    // is パターンマッチング（推奨）
    public static void ExamineWithIs(Animal animal)
    {
        Console.Write($"  {animal.Name}: ");

        if (animal is GuideDog gd)
            Console.WriteLine($"盲導犬です → {gd.Speak()}");
        else if (animal is Dog dog)
            Console.WriteLine($"犬です → {dog.Speak()}");
        else if (animal is Cat cat)
            Console.WriteLine($"猫です → {cat.Speak()}");
        else
            Console.WriteLine("不明な動物です");
    }

    // as によるキャスト（失敗時は null）
    public static void ExamineWithAs(Animal animal)
    {
        var dog = animal as Dog;
        if (dog is not null)
            Console.WriteLine($"  {animal.Name} は Dog 型にキャスト成功: {dog.Speak()}");
        else
            Console.WriteLine($"  {animal.Name} は Dog 型ではありません");
    }
}

// ============================================================
// エントリーポイント
// ============================================================
class Program
{
    static void Main()
    {
        Console.WriteLine("=== 1. インターフェースの実装 ===");

        var circle   = new Circle(5.0);
        var triangle = new RightTriangle(3.0, 4.0);

        circle.Print(); // IPrintable

        // IShape として統一的に扱う（ポリモーフィズム）
        IShape[] shapes = { circle, triangle };
        foreach (var shape in shapes)
            Console.WriteLine($"  {shape.Describe()}");

        Console.WriteLine();
        Console.WriteLine("=== 2. 抽象クラスと継承 ===");

        // Animal 型のコレクションに Dog / Cat / GuideDog を混在
        var animals = new List<Animal>
        {
            new Dog("ポチ"),
            new Cat("タマ"),
            new GuideDog("ガイド"),
            new LoudCat("おさわがせ"),
        };

        // ポリモーフィズム: 実行時の型に応じたメソッドが呼ばれる
        foreach (var animal in animals)
            Console.WriteLine($"  {animal} → {animal.Speak()}");

        Console.WriteLine();
        Console.WriteLine("=== 3. is / as によるダウンキャスト ===");

        Console.WriteLine("[is パターンマッチング]");
        foreach (var animal in animals)
            PetClinic.ExamineWithIs(animal);

        Console.WriteLine("[as によるキャスト]");
        foreach (var animal in animals)
            PetClinic.ExamineWithAs(animal);

        Console.WriteLine();
        Console.WriteLine("=== 4. 直接キャストとの比較 ===");

        Animal a = new Dog("タロウ");

        // 安全: is パターン
        if (a is Dog d)
            Console.WriteLine($"  is パターン: {d.Speak()}");

        // 安全: as + null チェック
        var c = a as Cat;
        Console.WriteLine($"  as Cat: {(c is null ? "null（Dog なので失敗）" : c.Speak())}");

        // 危険: 直接キャスト（型が合わないと InvalidCastException）
        try
        {
            var cat = (Cat)a; // Dog を Cat にキャスト → 例外
        }
        catch (InvalidCastException)
        {
            Console.WriteLine("  直接キャスト: InvalidCastException 発生（as を使うべき）");
        }
    }
}
