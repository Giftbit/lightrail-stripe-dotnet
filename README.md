# lightrail-stripe-dotnet
Lightrail integration with Stripe

## Development

### Requirements
- [.NET Core SDK](https://dotnet.github.io/)
- [Visual Studio Code](https://code.visualstudio.com/) (or some other editor)

### Compiling
`dotnet build`

### Unit Testing
`dotnet test`

### Building Test Packages
- (once) install [nuget](https://docs.microsoft.com/en-us/nuget/install-nuget-client-tools#cli-tools)
- (once) create a local dir to store packages: `mkdir ~/nuget-packages`
- bump the version number in `Lightrail.Stripe/Lightrail.Stripe.csproj`
- create a test nuget package: `dotnet pack -c Debug`
- add the package to a local dir `nuget add Lightrail.Stripe/bin/Debug/Lightrail-Stripe.<version>.nupkg -source ~/nuget-packages`
- (optional) if replacing a package with the same version number clear the local cache `nuget locals all -clear`
- use the package in the child project `dotnet add package Lightrail-Stripe --version <version> -s ~/nuget-packages`

### Releasing
- bump the PackageVersion appropriately in `Lightrail.Stripe/Lightrail.Stripe.csproj`
- create the nuget package with `dotnet pack -c Release`
- upload to nuget.org as per https://docs.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package
