-- create migrations
dotnet ef migrations add IntialMigration --startup-project .\Backend\Backend.csproj --project .\Db\Db.csproj
dotnet ef database update --project .\Db\Db.csproj --startup-project .\Backend\Backend.csproj