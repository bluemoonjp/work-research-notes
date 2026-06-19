using System;
using System.Collections.Generic;

// ========================================
// ジェネリクス サンプルコード
// ========================================

class GenericsSamples
{
    static void Main()
    {
        GenericClassDemo();
        GenericMethodDemo();
        ConstraintDemo();
        RepositoryPatternDemo();
    }

    // ----------------------------------------
    // 1. ジェネリッククラス
    // ----------------------------------------
    static void GenericClassDemo()
    {
        Console.WriteLine("=== ジェネリッククラス ===");

        // Box<T>: 任意の型を格納
        var intBox    = new Box<int>(42);
        var stringBox = new Box<string>("hello");

        Console.WriteLine($"intBox.Value    = {intBox.Value}");
        Console.WriteLine($"stringBox.Value = {stringBox.Value}");

        // GenericStack<T>: スタックの実装例
        var stack = new GenericStack<string>();
        stack.Push("first");
        stack.Push("second");
        stack.Push("third");

        Console.WriteLine($"Peek: {stack.Peek()}");
        while (!stack.IsEmpty)
            Console.WriteLine($"Pop: {stack.Pop()}");
    }

    // ----------------------------------------
    // 2. ジェネリックメソッド
    // ----------------------------------------
    static void GenericMethodDemo()
    {
        Console.WriteLine("\n=== ジェネリックメソッド ===");

        // Swap: 型引数は推論される
        int a = 1, b = 2;
        Swap(ref a, ref b);
        Console.WriteLine($"Swap(int): a={a}, b={b}");

        string x = "foo", y = "bar";
        Swap(ref x, ref y);
        Console.WriteLine($"Swap(string): x={x}, y={y}");

        // Max: IComparable<T> 制約により比較できる
        Console.WriteLine($"Max(3, 7)     = {Max(3, 7)}");
        Console.WriteLine($"Max(\"abc\", \"xyz\") = {Max("abc", "xyz")}");

        // Clamp
        Console.WriteLine($"Clamp(15, 0, 10) = {Clamp(15, 0, 10)}");
        Console.WriteLine($"Clamp(-5, 0, 10) = {Clamp(-5, 0, 10)}");
    }

    // 2 つの変数を入れ替える汎用メソッド
    static void Swap<T>(ref T a, ref T b)
    {
        T temp = a;
        a = b;
        b = temp;
    }

    // IComparable<T> 制約: T.CompareTo を呼べる
    static T Max<T>(T a, T b) where T : IComparable<T>
        => a.CompareTo(b) >= 0 ? a : b;

    // 値を min〜max の範囲に収める
    static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0) return min;
        if (value.CompareTo(max) > 0) return max;
        return value;
    }

    // ----------------------------------------
    // 3. 型制約のサンプル
    // ----------------------------------------
    static void ConstraintDemo()
    {
        Console.WriteLine("\n=== 型制約 ===");

        // new() 制約: 内部でインスタンスを生成できる
        var p = CreateInstance<SamplePoint>();
        Console.WriteLine($"CreateInstance<SamplePoint>: X={p.X}, Y={p.Y}");

        // class 制約: null を許容する参照型
        string? nullableStr = null;
        Console.WriteLine($"IsNull(null)    = {IsNull(nullableStr)}");
        Console.WriteLine($"IsNull(\"hello\") = {IsNull("hello")}");

        // struct 制約: 値型のみ
        Console.WriteLine($"DefaultOf<int>    = {DefaultOf<int>()}");
        Console.WriteLine($"DefaultOf<double> = {DefaultOf<double>()}");
    }

    // new() 制約: パラメータなしコンストラクタを持つ型のみ
    static T CreateInstance<T>() where T : new() => new T();

    // class 制約: 参照型のみ（null チェックが可能）
    static bool IsNull<T>(T? value) where T : class => value is null;

    // struct 制約: 値型のみ（default は 0 や false など）
    static T DefaultOf<T>() where T : struct => default;

    // ----------------------------------------
    // 4. リポジトリパターン（ジェネリクスの実用例）
    // ----------------------------------------
    static void RepositoryPatternDemo()
    {
        Console.WriteLine("\n=== リポジトリパターン ===");

        var repo = new InMemoryRepository<SampleEntity>();

        repo.Add(new SampleEntity { Id = 1, Name = "Alice" });
        repo.Add(new SampleEntity { Id = 2, Name = "Bob" });
        repo.Add(new SampleEntity { Id = 3, Name = "Carol" });

        Console.WriteLine($"Count: {repo.Count}");

        var found = repo.GetById(2);
        Console.WriteLine($"GetById(2): {found?.Name ?? "not found"}");

        repo.Delete(1);
        Console.WriteLine($"Count after Delete(1): {repo.Count}");

        Console.WriteLine("All:");
        foreach (var e in repo.GetAll())
            Console.WriteLine($"  Id={e.Id}, Name={e.Name}");
    }
}

// ---- ジェネリッククラスの定義 ----

// シンプルな汎用コンテナ
class Box<T>
{
    public T Value { get; }
    public Box(T value) => Value = value;
}

// ジェネリックスタック（List<T> を内部で使用）
class GenericStack<T>
{
    private readonly List<T> _items = new();

    public void Push(T item) => _items.Add(item);

    public T Pop()
    {
        if (IsEmpty) throw new InvalidOperationException("スタックが空です。");
        var top = _items[^1]; // ^1 = 末尾インデックス
        _items.RemoveAt(_items.Count - 1);
        return top;
    }

    public T Peek()
    {
        if (IsEmpty) throw new InvalidOperationException("スタックが空です。");
        return _items[^1];
    }

    public bool IsEmpty => _items.Count == 0;
}

// ---- リポジトリパターン ----

// エンティティの基底インターフェイス（Id を持つことを保証）
interface IEntity
{
    int Id { get; }
}

// 汎用インメモリリポジトリ
// T : IEntity, new() の制約で、Id を持ち、生成可能な型のみ受け付ける
class InMemoryRepository<T> where T : IEntity
{
    private readonly Dictionary<int, T> _store = new();

    public void Add(T entity)    => _store[entity.Id] = entity;
    public void Delete(int id)   => _store.Remove(id);
    public T? GetById(int id)    => _store.TryGetValue(id, out var e) ? e : default;
    public IEnumerable<T> GetAll() => _store.Values;
    public int Count             => _store.Count;
}

// ---- ダミーデータクラス ----

class SamplePoint
{
    public int X { get; set; }
    public int Y { get; set; }
}

class SampleEntity : IEntity
{
    public int    Id   { get; set; }
    public string Name { get; set; } = string.Empty;
}
