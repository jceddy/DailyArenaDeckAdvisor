dotnet publish -f netcoreapp2.2 -r win-x86 --self-contained false
dotnet publish -f netcoreapp2.2 -r win-x64 --self-contained false
dotnet publish -f netcoreapp2.2 -r osx-x64 --self-contained false
"C:\Program Files (x86)\Windows Kits\8.1\bin\x86\signtool.exe"  sign  /t http://timestamp.comodoca.com /sha1 9CFAD3D9DCDE70DC56909D3927A0DD5CB7DA1A83 ".\bin\Debug\netcoreapp2.2\win-x86\DailyArena.DeckAdvisor.Console.exe"
"C:\Program Files (x86)\Windows Kits\8.1\bin\x86\signtool.exe"  sign  /t http://timestamp.comodoca.com /sha1 9CFAD3D9DCDE70DC56909D3927A0DD5CB7DA1A83 ".\bin\Debug\netcoreapp2.2\win-x86\publish\DailyArena.DeckAdvisor.Console.exe"
"C:\Program Files (x86)\Windows Kits\8.1\bin\x64\signtool.exe"  sign  /t http://timestamp.comodoca.com /sha1 9CFAD3D9DCDE70DC56909D3927A0DD5CB7DA1A83 ".\bin\Debug\netcoreapp2.2\win-x64\DailyArena.DeckAdvisor.Console.exe"
"C:\Program Files (x86)\Windows Kits\8.1\bin\x64\signtool.exe"  sign  /t http://timestamp.comodoca.com /sha1 9CFAD3D9DCDE70DC56909D3927A0DD5CB7DA1A83 ".\bin\Debug\netcoreapp2.2\win-x64\publish\DailyArena.DeckAdvisor.Console.exe"