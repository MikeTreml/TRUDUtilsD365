# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

TRUDUtilsD365 is a **Visual Studio 2019 add-in DLL** that plugs into the Microsoft Dynamics 365 Finance & Operations (F&O) development environment. It adds right-click/menu commands (Create Extension Class, Enum Builder, Table Builder, Form Builder, RunBase/DataContract snippet generators, etc.) that either generate X++ source text or mutate the D365 metamodel via the Dynamics VS SDK.

The add-in is only useful from inside a D365 F&O developer VM. Most of the compile-time dependencies are VS SDK DLLs that are **not redistributable** — they come from `...\Visual Studio\2019\...\Extensions\<hash>\Microsoft.Dynamics.AX.*.dll`, referenced by absolute `HintPath` in `TRUDUtilsD365/TRUDUtilsD365.csproj`. Keep this in mind: you cannot `dotnet build` this repo on Linux or on any machine without a D365 F&O VS install.

## Solution layout

`TRUDUtilsD365.sln` contains three projects:

- **`TRUDUtilsD365/`** — the add-in DLL itself (`OutputType=Library`, TFM `v4.7.2`). This is the whole product. It references the Dynamics VS SDK (`Microsoft.Dynamics.AX.Metadata*`, `Microsoft.Dynamics.Framework.Tools.*`) and `System.Windows.Forms`. After build, `Target Name="AfterBuildAction"` in the csproj copies outputs to `$(DynamicsVSToolsHintPath)\AddinExtensions\` so VS picks them up on next launch.
- **`InstallToVS/`** — a small WinForms installer exe that copies `TRUDUtilsD365.dll` / `.pdb` into the VS `AddinExtensions` folder for end users who download a release.
- **`AddTableFindMethodTest/`** — a WinForms exe used as a manual test harness. Despite the name, it hosts test stubs for many tools, not just AddTableFindMethod. See "Testing" below.

## Architectural pattern: every tool is a (Menu, Parms, Dialog) trio

Each tool lives in its own folder under `TRUDUtilsD365/` and follows the same three-file pattern. When adding, modifying, or debugging a tool, these three files are almost always what you need:

- **`XxxMenu.cs`** — the VS add-in entry point. A class deriving from `DesignerMenuBase` with `[Export(typeof(IDesignerMenu))]` (MEF) and `[DesignerMenuExportMetadata(AutomationNodeType = typeof(...))]` telling VS which metamodel node types (e.g. `BaseField`, `AxTable`, `AxForm`) get this right-click entry. `OnClick(AddinDesignerEventArgs e)` is the entry point: it reads `e.SelectedElement`, instantiates `XxxParms`, calls `parms.InitFromSelectedElement(e)`, then shows `XxxDialog`. All exceptions are funneled through `CoreUtility.HandleExceptionWithErrorMessage(ex)`. Tools that run from the main VS menu (Dynamics 365 → Addins, e.g. `EnumCreator`) use `[Export(typeof(IMainMenu))]` / `MainMenuBase` instead.
- **`XxxParms.cs`** — the logic + state for the tool. Fields represent dialog inputs; the class exposes either `GenerateResult()` (returns X++ text — e.g. `AddTableFindMethodParms`, the snippet builders) or `Run()` (mutates the VS metamodel to create new artefacts — e.g. `FormBuilderParms`, `TableFieldsBuilderParms`, `SecurityPrivilegeBuilderParms`, `MenuItemBuilderParms`, `CreateTableRelationParms`). **This is where the interesting logic lives.** The Parms class also owns `InitFromSelectedElement(AddinDesignerEventArgs e)`, which reads the currently-selected metamodel node and pre-populates fields.
- **`XxxDialog.cs` / `XxxDialog.Designer.cs` / `XxxDialog.resx`** — WinForms UI. `SetParameters(parms)` binds the form to the Parms object via `BindingSource`. The Designer file is generated — edit it only when you actually change the layout. Some tools (`RunBaseBuilder`, `DataContractBuilder`) don't have their own dialog and instead reuse the shared `Kernel/SnippetsDialog` by populating a `SnippetsParms` via `InitDialogParms(snippetsParms)`.

Two tools don't follow this pattern exactly: `CopyExtensionMethod/` and `CreateExtension/` ship without a dialog at all — their `Params` class runs synchronously from `OnClick`, and `DesignerXxx.cs` is the Menu equivalent.

### Shared Kernel

`TRUDUtilsD365/Kernel/` holds the utilities every tool uses:

- **`AxHelper.cs`** — pure string helpers for X++ naming conventions (`PrettyName`, `GetVarNameFromType`, etc.). No VS SDK dependency, safe to call from tests.
- **`CodeGenerateHelper.cs`** — indentation-aware X++ code builder used by every `GenerateResult()`. Key methods: `Append`, `AppendLine`, `IndentIncrease/Decrease/SetValue/SetAsCurrentPos/RestorePrev`, column-aligned `AddColumnAlignInt`/`Append(text, columnName)`. Access generated text via `ResultString.ToString()`.
- **`SnippetsParms` / `SnippetsDialog`** — generic "N named snippets + one big result pane" dialog reused by `RunBaseBuilder` and `DataContractBuilder`.

### Settings

`TRUDUtilsD365/KernelSettings/` persists user configuration (custom naming-convention rules, project prefixes, etc.) to `TRUDUtilsD365Settings.xml` **next to the built DLL** (same directory as `Assembly.GetExecutingAssembly().Location`). Serialized with `DataContractSerializer` over `AxModelSettings`. `KernelSettingsManager` is the in-process accessor, `KernelSettingsStorage` is the file I/O. The settings UI is the `KernelSettings` form, launched from the main VS menu.

## Adding a new tool

Following the existing pattern is almost always the right move:

1. Create `TRUDUtilsD365/YourTool/` with `YourToolMenu.cs`, `YourToolParms.cs`, `YourToolDialog.cs` (+ `.Designer.cs`, `.resx`).
2. Register all new files in `TRUDUtilsD365/TRUDUtilsD365.csproj` under the single `<ItemGroup>` that lists `<Compile Include>` entries. WinForms files need `<SubType>Form</SubType>` on the main `.cs`, `<DependentUpon>YourToolDialog.cs</DependentUpon>` on the Designer `.cs`, and a matching `<EmbeddedResource>` entry for the `.resx`.
3. Export the menu via MEF (`[Export(typeof(IDesignerMenu))]` for context menus, `[Export(typeof(IMainMenu))]` for the main VS menu). The `AutomationNodeType` metadata decides which metamodel nodes the right-click appears on.
4. Put logic in `YourToolParms`, not in the dialog. Prefer a pure `GenerateResult()` → string when the output is X++ code — it makes the logic testable without a D365 VM.
5. Add a stub to `AddTableFindMethodTest/Program.cs` so the tool can be launched against a mock Parms without starting VS (see below).

## Build

- **Primary**: open `TRUDUtilsD365.sln` in Visual Studio 2019 **on a D365 F&O developer VM** and build. The `HintPath`s in the csproj resolve against a specific VS Extensions folder (the exact hash path differs per PU — see README "Installation" for the list). The `AfterBuildAction` target will copy the DLL into `$(DynamicsVSToolsHintPath)\AddinExtensions\`, and a VS restart will load it.
- **Debugging from VS**: attach to the running `devenv.exe` that has the D365 project open, or set `TRUDUtilsD365`'s debug start action to "start external program" pointing at `devenv.exe`. Most interesting breakpoints belong in the `*Parms` classes.
- **From CI / headless environments**: building is not currently supported — the Dynamics SDK DLLs are not on NuGet. Do not attempt to mock, vendor, or refactor around those references without checking first.

## Testing

There is no unit-test framework. The `AddTableFindMethodTest` project is a WinForms exe whose `Program.Main` has one call uncommented at a time; running the project opens the corresponding dialog so you can visually verify it. Pattern:

```csharp
static void Main()
{
    // Uncomment exactly ONE of these to run it:
    //TestAddTableFindMethod();
    //TestCreateExtensionClass();
    //TestEnumCreator();
    TestTableBuilder();
}
```

To exercise a new tool's dialog locally, add a `TestYourTool()` method that instantiates `YourToolParms`, pre-populates it with mock data (bypassing `InitFromSelectedElement`, which would require a real VS selection), calls `dialog.SetParameters(parms)`, and `Application.Run(dialog)`. The **real win** for an AI or CI: if the tool has a pure `GenerateResult()`, you can call it directly on the Parms instance, bypass the dialog entirely, and assert on the returned string — no VS, no VM, no WinForms.

Note: tools whose Parms class depends on the Dynamics SDK metamodel (anything touching `AxTable`, `AxForm`, `BaseField`, `Microsoft.Dynamics.Framework.Tools.MetaModel.Automation.*`) can only be driven from a real VS process. The pure-string tools (`AddTableFindMethod`, the snippet builders in `RunBaseBuilder`/`DataContractBuilder`, most of `AxHelper`/`CodeGenerateHelper`) are the only ones currently runnable outside VS.

## Installation (reference for end users)

Built artefacts for users: `InstallToVS.exe`, `TRUDUtilsD365.dll`, `TRUDUtilsD365.pdb`. Users run `InstallToVS.exe` as admin; it copies the DLL+PDB into the VS `AddinExtensions` folder. The folder path differs per D365 PU and is resolved through the `DynamicsVSTools` / `DynamicsVSToolsHintPath` environment variables. For VMs without admin access (PU25+), the `C:\Users\<you>\Documents\...\DynamicsDevConfig.xml` `<AddInPaths>` element provides an alternative install location. Full details are in `README.md`.
