# GraphQL Query Builder .NET

![logo](https://raw.githubusercontent.com/charlesdevandiere/graphql-query-builder-dotnet/master/logo.png)

A tool to build GraphQL query from a C# model.

## Publishing to GitHub Packages

To publish a new version of the NuGet package:

1. Update the version in `src/GraphQL.Query.Builder/GraphQL.Query.Builder.csproj`

2. Pack the project:
   ```shell
   dotnet pack src/GraphQL.Query.Builder -c Release
   ```

3. Push to GitHub Packages:
   ```shell
   dotnet nuget push src/GraphQL.Query.Builder/bin/Release/SRF.GraphQL.Query.Builder.{VERSION}.nupkg \
     --source "SRF Packages (GitHub)" \
     --api-key YOUR_GITHUB_PAT
   ```

   Replace `{VERSION}` with the version number and `YOUR_GITHUB_PAT` with a GitHub Personal Access Token that has `write:packages` scope.

## Install

```shell
dotnet add package GraphQL.Query.Builder
```

## Usage

```csharp
// Create the query
IQuery<Human> query = new Query<Human>("humans") // set the name of the query
    .AddArguments(new { id = "uE78f5hq" }) // add query arguments
    .AddField(h => h.FirstName) // add firstName field
    .AddField(h => h.LastName) // add lastName field
    .AddField( // add a sub-object field
        h => h.HomePlanet, // set the name of the field
        sq => sq /// build the sub-query
            .AddField(p => p.Name)
    )
    .AddField<human>( // add a sub-list field
        h => h.Friends,
        sq => sq
            .AddField(f => f.FirstName)
            .AddField(f => f.LastName)
    );
// This corresponds to:
// humans(id: "uE78f5hq") {
//   FirstName
//   LastName
//   HomePlanet {
//     Name
//   }
//   Friends {
//     FirstName
//     LastName
//   }
// }

Console.WriteLine("{" + query.Build() + "}");
// Output:
// {humans(id:"uE78f5hq"){FirstName LastName HomePlanet{Name}Friends FirstName LastName}}
```
