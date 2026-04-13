/********************************************************************************
 * LICENSE: zlib/libpng
 *
 * Copyright (c) 2026 Juan Diez Liste
 *
 * This software is provided ‘as-is’, without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software
 * in a product, an acknowledgment in the product documentation would be
 * appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 * distribution.
 *
 *******************************************************************************/
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using System.Collections.Generic;

namespace Csheader;
class Program
{
    static void Main(string[] args) {
        if(args.Length == 0) {
            Console.Error.WriteLine("""
            USAGE: 
                csheader <path to csproj>  [<output path>]

                If an output path is not specified, then the result will be on the standard output.
            """);
        }
        string path = args[0];
        Console.WriteLine($"loading {path}");
        if (!File.Exists(path)) {
            Console.Error.WriteLine("file not found");
            Environment.Exit(1);
        }

        Stream outSink;
        if(args.Length > 1) {
            outSink = File.Open(args[1], FileMode.Create);
        } else {
            outSink = Console.OpenStandardOutput();
        }
        //TODO: use defer for disposal
        using var outStream = outSink;

        var sourceFiles = Project.ListSources(path);
        foreach(var f in sourceFiles) { 
            Console.WriteLine(f);
        }
        Console.WriteLine(sourceFiles.Count);

        List<SyntaxTree> trees = new(capacity: sourceFiles.Count);
        foreach(var f in sourceFiles) {
            var text = File.ReadAllText(f);
            SyntaxTree ftree = CSharpSyntaxTree.ParseText(text);
            trees.Add(ftree);
        }

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create(
                "Analysis",
                syntaxTrees: trees,
                references: references);

        var globalNs = compilation.Assembly.GlobalNamespace;
        using (StreamWriter sink = new StreamWriter(outStream))
        {   
            ExploreNameSpace(globalNs, sink, 0);
        }

    }

    static void MainRef(string[] args)
    {
        // string path = "/Users/jojor/Projects/csharp/csharp_header_doc/SemanticQuickStart.csproj";
        string path = args[0];

        // Required for MSBuildWorkspace to work
        MSBuildLocator.RegisterDefaults();

        var workspace = MSBuildWorkspace.Create();
        var project = workspace.OpenProjectAsync(path).GetAwaiter().GetResult();

        var compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
        Debug.Assert(compilation is not null, "Compilation is null, that means it was called multiple times and it shouldn't");

        var globalNs = compilation.Assembly.GlobalNamespace;
        using (var stdout = Console.OpenStandardOutput())
        using (StreamWriter sink = new StreamWriter(stdout))
        {   
            ExploreNameSpace(globalNs, sink, 0);
        }
    }

    private static void ExploreNameSpace(INamespaceSymbol ns, StreamWriter sink, int level) {
        if(!ns.IsGlobalNamespace) {
            AddIdentation(sink, level);
            sink.WriteLine($"namespace {ns.Name} {{");
        }
        int nextLevel = ns.IsGlobalNamespace ? level : level+1;
        foreach(var member in ns.GetMembers()) {
            switch(member) {
                case INamespaceSymbol n: ExploreNameSpace(n, sink, nextLevel); break;
                case INamedTypeSymbol t: ExploreType(t, sink, nextLevel); break;
            }
        }

        if(!ns.IsGlobalNamespace) {
            AddIdentation(sink, level);
            sink.WriteLine("}");
        }
    }

    private static void ExploreType(INamedTypeSymbol ts, StreamWriter sink, int level) {
        if(ts.DeclaredAccessibility == Accessibility.Private) return;
        if(ts.DeclaredAccessibility == Accessibility.Protected) return;

        Debug.Assert(ts.DeclaredAccessibility == Accessibility.Public || ts.DeclaredAccessibility == Accessibility.Internal, "not public or internal");

        AddIdentation(sink, level);
        var docs = ts.GetDocumentationCommentXml();
        if(!string.IsNullOrWhiteSpace(docs)){
            sink.Write(docs);
            if(docs.Last() != '\n') {
                sink.Write('\n');
            }
            AddIdentation(sink, level);
        }


        string access = AccessibilityString(ts.DeclaredAccessibility);
        sink.Write(access);
        sink.Write(' ');
        if(ts.IsStatic) {
            sink.Write("static ");
        }

        if(ts.IsRecord) {
            sink.Write("record ");
        }

        var kind = ts.TypeKind;
        sink.Write(kind.ToString().ToLower());
        sink.Write(' ');

        string name = ts.Name;
        sink.Write(name);

        if( ts.IsGenericType) {
            sink.Write('<');
            for(int t = 0; t < ts.TypeParameters.Length; t++) {
                if(t > 0) sink.Write(", ");
                sink.Write(ts.TypeParameters[t]);
            }
            sink.Write('>');
        }

        sink.WriteLine(" {");

        if(ts.TypeKind == TypeKind.Enum) {
            foreach(var member in ts.GetMembers()) {
                if(member is not IFieldSymbol) continue;
                AddIdentation(sink, level+1);
                sink.WriteLine($"{member.Name},");
            }
        } else {
            foreach(var member in ts.GetMembers()) {
                switch(member) {
                    case {DeclaredAccessibility: Accessibility.Private}: break;
                    case {DeclaredAccessibility: Accessibility.Protected}: break;
                    case INamedTypeSymbol n: ExploreType(n, sink, level+1); break;
                    case IPropertySymbol p: ExploreProperty(p, sink, level+1); break;
                    case IFieldSymbol f: ExploreField(f, sink, level+1); break;
                    case IMethodSymbol f: ExploreMethod(f, sink, level+1); break;
                }
            }
        }
        AddIdentation(sink, level);
        sink.WriteLine('}');
    }

    private static void ExploreProperty(IPropertySymbol p, StreamWriter sink, int level) {
        AddIdentation(sink, level);
        sink.Write(AccessibilityString(p.DeclaredAccessibility));
        sink.Write($" {p.Type} ");
        if(p.IsIndexer) {
            sink.Write("this[");

            for(int i = 0; i < p.Parameters.Length; i++){
                if(i > 0) sink.Write(", ");
                var par = p.Parameters[i];
                sink.Write(par);
            }
            sink.Write(']');
            Console.WriteLine(p);
        } else {
            sink.Write(p.Name);
        }
        sink.WriteLine(";");
    }
    private static void ExploreField(IFieldSymbol f, StreamWriter sink, int level) {
        AddIdentation(sink, level);
        var docs = f.GetDocumentationCommentXml();
        if(!string.IsNullOrWhiteSpace(docs)){
            sink.Write(docs);
            if(docs.Last() != '\n') {
                sink.Write('\n');
            }
            AddIdentation(sink, level);
        }

        string access = AccessibilityString(f.DeclaredAccessibility);
        sink.Write(access);
        sink.Write(' ');
        if(f.IsStatic) {
            sink.Write("static ");
        }
        //TODO: readonly, required...

        var returnType = f.Type;
        sink.Write($"{returnType} ");

        string name = f.Name;
        sink.Write(name);

        sink.WriteLine(";");
    }
    private static void ExploreMethod(IMethodSymbol ts, StreamWriter sink, int level) {
        Debug.Assert(ts.DeclaredAccessibility == Accessibility.Public || ts.DeclaredAccessibility == Accessibility.Internal, "not public or internal");
        switch (ts.MethodKind) {
            case MethodKind.PropertyGet or MethodKind.PropertySet: 
                return;
            case MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise:
                return;
            case MethodKind.BuiltinOperator:
                return;
            case MethodKind.UserDefinedOperator when ts.DeclaringSyntaxReferences.Length == 0:
                return;
            case MethodKind.Destructor when ts.DeclaringSyntaxReferences.Length == 0:
                return;
        }
        if( ts.DeclaringSyntaxReferences.Length == 0) {
            // Console.WriteLine($"method '{ts.Name}' has no declaring references");
            return;
        }
        if( ts.IsImplicitlyDeclared) {
            //swich for exceptions
            switch(ts.Name) {
                case "Deconstruct":
                    break;
                default:
                    // Console.WriteLine($"method '{ts.Name}' is implicit");
                    return;
            }
        }
        if(ts.MethodKind == MethodKind.UserDefinedOperator || ts.MethodKind == MethodKind.Conversion) {
            ExploreOperator(ts, sink, level);
            return;
        }

        AddIdentation(sink, level);
        var docs = ts.GetDocumentationCommentXml();
        if(!string.IsNullOrWhiteSpace(docs)){
            sink.Write(docs);
            if(docs.Last() != '\n') {
                sink.Write('\n');
            }
            AddIdentation(sink, level);
        }

        string access = AccessibilityString(ts.DeclaredAccessibility);
        sink.Write(access);
        sink.Write(' ');
        if(ts.IsStatic) {
            sink.Write("static ");
        }

        /* var kind = ts.MethodKind;
        sink.Write(kind.ToString().ToLower());
        sink.Write(' '); */

        if(ts.MethodKind == MethodKind.Constructor) {
            sink.Write($"{ts.ContainingType.Name}");
        } else {
            var returnType = ts.ReturnType;
            sink.Write($"{returnType} ");

            string name = ts.Name;
            sink.Write(name);
        }


        if( ts.IsGenericMethod) {
            sink.Write('<');
            for(int t = 0; t < ts.TypeParameters.Length; t++) {
                if(t > 0) sink.Write(", ");
                sink.Write(ts.TypeParameters[t]);
            }
            sink.Write('>');
        }

        sink.Write('(');
        int i = 0;
        foreach(var param in ts.Parameters) {
            if(i > 0) sink.Write(", ");

            sink.Write(param);
            if(param.HasExplicitDefaultValue) {
                sink.Write($" = {RenderConstant(param.ExplicitDefaultValue)}");
            }

            i++;
        }
        sink.WriteLine(");");
    }

    private static void ExploreOperator(IMethodSymbol ts, StreamWriter sink, int level)
    {
        AddIdentation(sink, level);
        if(ts.Name ==  "op_Implicit") {
            sink.WriteLine($"implicit operator {ts.ReturnType}({ts.Parameters[0].Type} {ts.Parameters[0].Name});");
            return;
        }
        if(ts.Name ==  "op_Explicit") {
            sink.WriteLine($"explicit operator {ts.ReturnType}({ts.Parameters[0].Type} {ts.Parameters[0].Name});");
            return;
        }
        sink.Write($"{ts.ReturnType} operator {OperatorFromName(ts.Name)}(");
        //for each param, add param type and name
        int i = 0;
        foreach(var param in ts.Parameters) {
            if(i > 0) sink.Write(", ");

            sink.Write(param);

            i++;
        }
        sink.WriteLine(");");
    }
    private static string OperatorFromName(string name) =>  name switch {
        //unary
        "op_UnaryPlus"          => "+",
        "op_UnaryNegation"      => "-",
        "op_LogicalNot"         => "!",
        "op_OnesComplement"     => "~",
        "op_True"               => "true",
        "op_False"              => "false",
        //binary
        "op_Addition"           => "+",
        "op_Subtraction"        => "-",
        "op_Multiply"           => "*",
        "op_Division"           => "/",
        "op_Modulus"            => "%",
        //bitwise
        "op_BitwiseAnd"         => "&",
        "op_BitwiseOr"          => "|",
        "op_ExclusiveOr"        => "^",
        "op_LeftShift"          => "<<",
        "op_RightShift"         => ">>",
        //logical
        "op_Equality"           => "==",
        "op_Inequality"         => "!=",
        "op_LessThan"           => "<",
        "op_GreaterThan"        => ">",
        "op_LessThanOrEqual"    => "<=",
        "op_GreaterThanOrEqual" => ">=",
        //inc/dec
        "op_Increment"          =>  "++",
        "op_Decrement"          =>  "--",
        _ => throw new Exception($"Unexpected operator '{name}'"),
    };


    private static string RenderConstant(object? value)
    {
        switch (value) {
            case null: return "null";
            case string s: return $"\"{EscapeString(s)}\"";
            case char c: return $"'{EscapeChar(c)}'";
            case bool b: return b ? "true" : "false";
            case Enum e: return $".{e}";
            default: return value.ToString()!;
        }
        
    }
    private static string EscapeString(string str) {
        return str
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
    private static string EscapeChar(char c)
    {
        return c switch
        {
            '\\' => "\\\\",
            '\'' => "\\'",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
        _ => c.ToString()
    };
}


    private static void AddIdentation(StreamWriter sink, int level) {
        const string lvl = "    ";
        for(int i = 0; i < level; i++) {
            sink.Write(lvl);
        }
    }

    private static string AccessibilityString(Accessibility acc) => acc switch {
        Accessibility.Public => "public",
        Accessibility.Private => "private",
        Accessibility.Internal => "internal",
        Accessibility.Protected => "internal",

        _ => throw new Exception("unexpected accessibility"),
    };
}


public static class Project {

    public static List<string> ListSources(string csproj) {
        Debug.Assert(File.Exists(csproj));
        Debug.Assert(Path.GetExtension(csproj) == ".csproj", "Not a csproj file");

        var dir = Path.GetDirectoryName(Path.GetFullPath(csproj));
        Console.WriteLine($"dir: {dir}");

        List<string> exclusions = new();
        exclusions.Add(Path.Join(dir, "bin"));
        exclusions.Add(Path.Join(dir, "obj"));
        //TODO: parse csproj and include the entries that are <Compile Remove="..."/>

        // foreach(var e in exclusions) Console.WriteLine(e);

        List<string> files = new(capacity: 256);
        foreach(var filePath in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)) {
            if(exclusions.Any(ep => filePath.Contains(ep))){
                //excluded
                continue;
            }
            files.Add(filePath);
        }


        return files;
    }
}
