# VEXOR independent rebuild (v0.1)

This application is new .NET 8 WinForms source code, not a forked binary or a patch applied to Roblox Account Manager. The existing working legacy build remains on the backup-working-vexor-legacy branch and main.

## Implemented
- Native VEXOR-branded dashboard
- Add, edit, delete, search profiles
- JSON import/export of VEXOR profile names
- Own local profile storage at %LOCALAPPDATA%\\VEXOR\\profiles.json
- No shared mutex, cookie lock, or upstream application runtime
- Opens Roblox's official website

## Not implemented
- Roblox authentication, account switching, direct account launches, running session detection, VirtualBox support, TinyTask automation
- Import of the original manager's encrypted account database

Profiles currently store only username and display alias. **Do not enter passwords or session cookies.** Existing account data is untouched. This early independent build is a foundation, not yet a replacement for the working legacy build.

Build with `dotnet publish independent/Vexor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true`.
