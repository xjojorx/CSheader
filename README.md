# CSheader

Header-like documentation generation for c# projects

## Description

Generate header-like documentation/api reference for C# projects. CSheader produces an equivalent to a c-like header file including all public types and members of a c# project to allow for easy to use api exploration.

Most documentation generation tools are focused on the production of heavy docs, which are hard to set up, and usually require extra infraestructure to host the results.
While there is nothing wrong with that approach, it is too much work for individual developers or small teams that just want better tooling to explore a project, or want a quick reference for the surface usage.

In the C world, header files serve a double purpose of helping the compiler to know the definition of functions and types of a library, while at the same time being a form of compact documentation. For a lot of libraries it is possible to explore an API by the heather file, since it includes the types and signatures, along with comments that describe what they are and how to use them.
In C#, we have all the tools to generate this kind of lightweight documentation, and is easy to access programmatically via the Roslyn platform and IDEs; but still most of the times we are missing a way to quickly explore what is really exposed by a project

## Usage

```bash
csheader <path to csproj> [<output file>]
```
If no output file is specified, it will print on the standard output

## Installation

CSheader is available as a [nuget package](https://www.nuget.org/packages/CSheader):

```bash
dotnet tool install --global CSheader
```

Until then, compile from source.


## Future work

- Explore/include related projects
- Parse csproj to look for excluded cs files
- Subcommand to generate the documentation for nuget packages (this would allow to get a header for a project that is being used, instead of requiring local source)
- Namespace filtering

## License

CSheader is licensed under an unmodified zlib/libpng license, which is an OSI-certified, BSD-like license that allows the usage in close-source and commercial projects. Attribution is appreciated but not required. Check [LICENSE](LICENSE) for more details.
