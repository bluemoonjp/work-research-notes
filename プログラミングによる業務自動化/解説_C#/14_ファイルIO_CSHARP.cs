// 14_ファイルIO_CSHARP.cs
// ファイル IO に関するサンプルコード
// コンソールアプリ（.NET 8 以降を想定）
//
// 注意: このサンプルは一時ディレクトリにファイルを書き出します。
//       実行するとカレントディレクトリ配下の temp/ フォルダにファイルが作られます。

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// ───────────────────────────────────────────────
// サンプル 1: File クラスの静的メソッド
// ───────────────────────────────────────────────
class FileClassSamples
{
    public static void Run(string workDir)
    {
        Console.WriteLine("=== File クラスの静的メソッド ===");

        string path = Path.Combine(workDir, "sample.txt");

        // 書き込み（上書き）
        File.WriteAllText(path, "1行目\n2行目\n3行目\n");
        Console.WriteLine($"書き込み完了: {path}");

        // 読み取り（全テキスト）
        string content = File.ReadAllText(path);
        Console.WriteLine($"全テキスト:\n{content}");

        // 行単位で読み取り
        string[] lines = File.ReadAllLines(path);
        Console.WriteLine($"行数: {lines.Length}");
        foreach (string line in lines)
            Console.WriteLine($"  '{line}'");

        // 追記
        File.AppendAllText(path, "追記行\n");
        Console.WriteLine($"追記後の行数: {File.ReadAllLines(path).Length}");

        // 存在確認・コピー・削除
        string copy = Path.Combine(workDir, "sample_copy.txt");
        if (File.Exists(path))
        {
            File.Copy(path, copy, overwrite: true);
            Console.WriteLine($"コピー完了: {copy}");
        }
        File.Delete(copy);
        Console.WriteLine($"コピー削除後 存在: {File.Exists(copy)}");

        // 文字コードを指定して読み書き（例: UTF-8 BOM 付き）
        string bomPath = Path.Combine(workDir, "bom.txt");
        File.WriteAllText(bomPath, "UTF-8 BOM\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        Console.WriteLine($"BOM 付き UTF-8 書き込み完了");
    }
}

// ───────────────────────────────────────────────
// サンプル 2: StreamReader / StreamWriter
// ───────────────────────────────────────────────
class StreamReaderWriterSamples
{
    public static void Run(string workDir)
    {
        Console.WriteLine("\n=== StreamReader / StreamWriter ===");

        string path = Path.Combine(workDir, "stream.txt");

        // StreamWriter で書き込み（using 宣言 C# 8+）
        using (var writer = new StreamWriter(path, append: false, encoding: Encoding.UTF8))
        {
            writer.WriteLine("StreamWriter で書いた 1 行目");
            writer.WriteLine("StreamWriter で書いた 2 行目");
            writer.WriteLine("StreamWriter で書いた 3 行目");
        } // ここで Flush & Close される

        // StreamReader で逐次読み取り
        using var reader = new StreamReader(path, Encoding.UTF8);
        int lineNumber = 0;
        while (!reader.EndOfStream)
        {
            string? line = reader.ReadLine();
            lineNumber++;
            Console.WriteLine($"  行 {lineNumber}: {line}");
        }
        // reader は using 宣言のスコープ（メソッド末尾）で Dispose される
    }
}

// ───────────────────────────────────────────────
// サンプル 3: FileStream（バイナリ）
// ───────────────────────────────────────────────
class FileStreamSamples
{
    public static void Run(string workDir)
    {
        Console.WriteLine("\n=== FileStream（バイナリ）===");

        string path = Path.Combine(workDir, "binary.bin");

        // バイト列を書き込む
        byte[] data = { 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03 };
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            fs.Write(data, 0, data.Length);
        }

        // 読み取って確認
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            byte[] buf = new byte[fs.Length];
            int read = fs.Read(buf, 0, buf.Length);
            Console.Write($"読み取り {read} バイト: ");
            foreach (byte b in buf)
                Console.Write($"{b:X2} "); // 16 進数表示
            Console.WriteLine();
        }
    }
}

// ───────────────────────────────────────────────
// サンプル 4: Path クラスの使用例
// ───────────────────────────────────────────────
class PathSamples
{
    public static void Run(string workDir)
    {
        Console.WriteLine("\n=== Path クラス ===");

        // パスの結合（OS の区切り文字を自動で使う）
        string filePath = Path.Combine(workDir, "sub", "report.csv");
        Console.WriteLine($"Combine: {filePath}");

        // パス構成要素の取得
        Console.WriteLine($"GetDirectoryName: {Path.GetDirectoryName(filePath)}");
        Console.WriteLine($"GetFileName:      {Path.GetFileName(filePath)}");
        Console.WriteLine($"GetFileNameWithoutExtension: {Path.GetFileNameWithoutExtension(filePath)}");
        Console.WriteLine($"GetExtension:     {Path.GetExtension(filePath)}");

        // フルパスへの変換
        string relative = "output.txt";
        Console.WriteLine($"GetFullPath: {Path.GetFullPath(relative)}");

        // 一時ファイル
        string temp = Path.GetTempFileName();
        Console.WriteLine($"GetTempFileName: {temp}");
        File.Delete(temp); // 使わないので削除

        // ランダムなファイル名（拡張子なし）
        Console.WriteLine($"GetRandomFileName: {Path.GetRandomFileName()}");

        // パスの区切り文字
        Console.WriteLine($"DirectorySeparatorChar: '{Path.DirectorySeparatorChar}'");
    }
}

// ───────────────────────────────────────────────
// サンプル 5: Directory クラス
// ───────────────────────────────────────────────
class DirectorySamples
{
    public static void Run(string workDir)
    {
        Console.WriteLine("\n=== Directory クラス ===");

        string subDir = Path.Combine(workDir, "subdir");

        // 作成（既存でもエラーにならない）
        Directory.CreateDirectory(subDir);
        Console.WriteLine($"作成: {subDir}");

        // ダミーファイルを作っておく
        File.WriteAllText(Path.Combine(subDir, "a.txt"), "a");
        File.WriteAllText(Path.Combine(subDir, "b.csv"), "b");

        // ファイル一覧
        string[] allFiles = Directory.GetFiles(subDir);
        Console.WriteLine($"ファイル数: {allFiles.Length}");

        // 拡張子フィルタ
        string[] txtFiles = Directory.GetFiles(subDir, "*.txt");
        Console.WriteLine($".txt ファイル数: {txtFiles.Length}");

        // 存在確認
        Console.WriteLine($"subdir 存在: {Directory.Exists(subDir)}");

        // 削除（recursive: true でディレクトリ内のファイルも削除）
        Directory.Delete(subDir, recursive: true);
        Console.WriteLine($"削除後 存在: {Directory.Exists(subDir)}");
    }
}

// ───────────────────────────────────────────────
// サンプル 6: CSV 読み書き
// ───────────────────────────────────────────────
class CsvSamples
{
    record Employee(string Name, string Department, int Salary);

    public static void Run(string workDir)
    {
        Console.WriteLine("\n=== CSV 読み書き ===");

        string csvPath = Path.Combine(workDir, "employees.csv");

        // CSV 書き込み
        var employees = new List<Employee>
        {
            new("Alice",   "Engineering", 600000),
            new("Bob",     "Sales",       500000),
            new("Carol",   "Engineering", 650000),
            new("Dave",    "HR",          480000),
        };

        using (var writer = new StreamWriter(csvPath, append: false,
               encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
        {
            writer.WriteLine("Name,Department,Salary"); // ヘッダ行
            foreach (var e in employees)
                writer.WriteLine($"{e.Name},{e.Department},{e.Salary}");
        }
        Console.WriteLine($"CSV 書き込み完了: {csvPath}");

        // CSV 読み取り
        var loaded = new List<Employee>();
        using (var reader = new StreamReader(csvPath, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
        {
            bool isHeader = true;
            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();
                if (line is null) continue;

                if (isHeader) { isHeader = false; continue; } // ヘッダをスキップ

                string[] cols = line.Split(',');
                if (cols.Length < 3) continue;

                loaded.Add(new Employee(cols[0], cols[1], int.Parse(cols[2])));
            }
        }

        Console.WriteLine($"CSV 読み込み: {loaded.Count} 件");
        foreach (var e in loaded)
            Console.WriteLine($"  {e.Name} / {e.Department} / {e.Salary:N0}円");
    }
}

// ───────────────────────────────────────────────
// エントリポイント
// ───────────────────────────────────────────────
class Program
{
    static void Main()
    {
        // 作業用一時ディレクトリを用意する
        string workDir = Path.Combine(Path.GetTempPath(), "csharp_fileio_sample");
        Directory.CreateDirectory(workDir);
        Console.WriteLine($"作業ディレクトリ: {workDir}\n");

        try
        {
            FileClassSamples.Run(workDir);
            StreamReaderWriterSamples.Run(workDir);
            FileStreamSamples.Run(workDir);
            PathSamples.Run(workDir);
            DirectorySamples.Run(workDir);
            CsvSamples.Run(workDir);
        }
        finally
        {
            // クリーンアップ
            Directory.Delete(workDir, recursive: true);
            Console.WriteLine($"\n作業ディレクトリを削除しました: {workDir}");
        }
    }
}
