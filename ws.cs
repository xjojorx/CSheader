internal static class DataCache {
    public static void Init();
    public static void Destroy();
    public static void Clear();
    public static bool ContainsKey(string key);
    public static bool TryGet(string key, out object data);
    public static void Set(string key, object value, bool preemptive = false);
    public static CacheMemory ComputeMemoryFootprint();
    public static long EstimateObjectBytes(object obj, HashSet<object>? visited = null);
}
internal class CacheEntry {
    public object Data;
    public DateTimeOffset LastAccess;
    public bool IsPreemptive;
    public CacheEntry(object data, bool preemptive);
}
internal struct CacheMemory {
    public long PreemptiveBytes;
    public long UsedEntriesBytes;
    public long TotalBytes;
}
public static class Sizes {
    public static long GB(int n);
    public static long MB(int n);
    public static long KB(int n);
    public static long B(int n);
}
public static class Configuration {
    public static string datamine_repo_path;
    public static bool Benchmark;
}
public enum Tables {
    Item,
    Action,
}
public enum Languages {
    En,
    Jp,
    Fr,
    De,
}
public record struct RowSpec {
    public RowSpec(int StartLine, List<Slice> Cells);
    public int StartLine;
    public List<Slice> Cells;
    public void Deconstruct(out int StartLine, out List<Slice> Cells);
}
public struct Table {
    public string Name;
    public string Content;
    public List<string> Headers;
    public List<RowSpec> Rows;
    public Table(string name, string fullContent);
    public string GetCell(int row, int col);
    public string this[int r, int c];
}
public record struct Slice {
    public Slice(int Start, int Length);
    public int Start;
    public int Length;
    public void Deconstruct(out int Start, out int Length);
}
public static class Extensions {
    public static Span<T> Slice<T>(Span<T> span, Slice slice);
    public static ReadOnlySpan<T> Slice<T>(ReadOnlySpan<T> span, Slice slice);
    public static string Substring(string str, Slice slice);
    public static ReadOnlySpan<char> Slice(string str, Slice slice);
}
public static class Data {
    public static string GetLanguageStr(Languages lang);
    public static Languages ParseLanguage(string lang);
    public static List<string> ListTables();
    public static Table LoadTable(string name, Languages lang);
    public static (List<string> headers, List<RowSpec> rows) ParseTable(string name, ReadOnlySpan<char> content);
    public record struct RowRef {
        public RowRef(string tableName, Languages lang, int fileRow);
        public string tableName;
        public Languages lang;
        public int fileRow;
        public void Deconstruct(out string tableName, out Languages lang, out int fileRow);
    }
    public static List<Data.RowRef> Grep(string needle);
    public static List<int> Grep(string needle, string filePath);
    public static List<int> ByteGrep(string needle, string filePath);
    public static void BenchIO();
    public static void BenchIOParallel();
    public static void BuildMegaFile(string destDir);
    public static void BenchIOMega(string megaDir);
}
public struct RentedArray<T> {
    public T[] Arr;
    public int Length;
    public Span<T> Span;
    public static RentedArray<T> Empty();
    public static RentedArray<T> Empty(System.Buffers.ArrayPool<T> pool);
    public RentedArray(T[] arr, System.Buffers.ArrayPool<T> pool);
    public static RentedArray<T> Rent(int size, System.Buffers.ArrayPool<T>? pool = null, bool clearArray = false);
    implicit operator Span<T>(RentedArray<T> m);
    public void Return();
    public void Dispose();
}
public class Program {
}
public static class Core {
}
public class WSWorker {
    public static WSWorker StartNew(System.Threading.Channels.ChannelReader<Msg> input, System.Threading.Channels.ChannelWriter<Msg> output, CancellationToken? parentCancellation = null);
    public void Stop();
    public Task StopAsync();
}
public enum Messages {
    End,
    Ping,
    Pong,
    Tables,
    TableList,
    TableRequest,
    TableData,
    Translate,
    Translation,
    Search,
    SearchResult,
}
public struct Msg {
    public Messages Type;
    public MsgEnd End;
    public MsgPing Ping;
    public MsgPong Pong;
    public MsgTables Tables;
    public MsgTableList TableList;
    public MsgTableRequest TableRequest;
    public MsgTableData TableData;
    public MsgTranslate Translate;
    public MsgTranslation Translation;
    public MsgSearch Search;
    public MsgSearchResult SearchResult;
    implicit operator Msg(MsgEnd m);
    explicit operator MsgEnd(Msg m);
    implicit operator Msg(MsgPing m);
    explicit operator MsgPing(Msg m);
    implicit operator Msg(MsgPong m);
    explicit operator MsgPong(Msg m);
    implicit operator Msg(MsgTables m);
    explicit operator MsgTables(Msg m);
    implicit operator Msg(MsgTableList m);
    explicit operator MsgTableList(Msg m);
    implicit operator Msg(MsgTableRequest m);
    explicit operator MsgTableRequest(Msg m);
    implicit operator Msg(MsgTableData m);
    explicit operator MsgTableData(Msg m);
    implicit operator Msg(MsgTranslate m);
    explicit operator MsgTranslate(Msg m);
    implicit operator Msg(MsgTranslation m);
    explicit operator MsgTranslation(Msg m);
    implicit operator Msg(MsgSearch m);
    explicit operator MsgSearch(Msg m);
    implicit operator Msg(MsgSearchResult m);
    explicit operator MsgSearchResult(Msg m);
}
public record struct MsgEnd {
    public MsgEnd();
}
public record struct MsgPing {
    public MsgPing();
}
public record struct MsgPong {
    public MsgPong();
}
public record struct MsgTables {
    public MsgTables();
}
public record struct MsgTableList {
    public MsgTableList(List<string> Names);
    public List<string> Names;
    public void Deconstruct(out List<string> Names);
}
public record struct MsgTableRequest {
    public MsgTableRequest(string Name, Languages Lang);
    public string Name;
    public Languages Lang;
    public void Deconstruct(out string Name, out Languages Lang);
}
public record struct MsgTableData {
    public MsgTableData(Table Table, Languages Lang);
    public Table Table;
    public Languages Lang;
    public void Deconstruct(out Table Table, out Languages Lang);
}
public record struct MsgTranslate {
    public MsgTranslate(string Table, int Row, int Col);
    public string Table;
    public int Row;
    public int Col;
    public void Deconstruct(out string Table, out int Row, out int Col);
}
public record struct MsgTranslation {
    public MsgTranslation(string Table, int Row, int Col, Dictionary<Languages, string> translation);
    public string Table;
    public int Row;
    public int Col;
    public Dictionary<Languages, string> translation;
    public void Deconstruct(out string Table, out int Row, out int Col, out Dictionary<Languages, string> translation);
}
public record struct MsgSearch {
    public MsgSearch(string Needle);
    public string Needle;
    public void Deconstruct(out string Needle);
}
public record struct SearchResult {
    public SearchResult(Table table, Languages lang, List<int> rows);
    public Table table;
    public Languages lang;
    public List<int> rows;
    public void Deconstruct(out Table table, out Languages lang, out List<int> rows);
}
public record struct MsgSearchResult {
    public MsgSearchResult(string Needle, List<SearchResult> results);
    public string Needle;
    public List<SearchResult> results;
    public void Deconstruct(out string Needle, out List<SearchResult> results);
}
namespace Jojor {
    namespace Foundation {
        public class BenchmarkBlock {
            public BenchmarkBlock(string prefix);
            public void Dispose();
        }
        public struct Defer {
            public Defer(Action fun);
            public Defer(Func<object> fun);
            public void Dispose();
        }
        public class ObjectPool<T> {
            public ObjectPool(Func<T> create, Action<T> reset, int capacity = 16);
            public T Rent();
            public void Return(T item);
        }
    }
}
