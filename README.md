# The Shop

The Shop is a vehicle builder for Shadowrun. This project updates James Ojaste’s original Rigger 2 program for Shadowrun Third Edition using **Rigger 3 Revised** and modern Visual Basic .NET.

The repository is at the planning stage. The legacy installer is included for reference. The revised build and installer are pending.

## Port plan

1. Use [The Shop 1.4.2](https://ojaste.ca/TheShop/) as the reference for the vehicle editor, saved designs, and HTML sheets. The original download contains a classic Visual Basic project that depends on `COMDLG32.OCX`.
2. Target Visual Basic 17.13 and .NET 10 for the Windows desktop port. Use native .NET file dialogs.
3. Implement and verify the Rigger 3 Revised rules for chassis, power plants, modifications, costs, and final vehicle stats. Keep the rules separate from the editor so calculations can be tested against worked examples.
4. Check which legacy vehicle files can be imported and identify any values that need manual conversion under the revised rules.

## Original program

James Ojaste created The Shop. His site hosts the original Visual Basic source and credits David Taylor for entering most of the chassis and engine data and Nullsheen for arranging the later VB6 migration. 

- [ShopWindows10.zip](legacy/ShopWindows10.zip) contains the Windows installer for the VB6 build. Its setup manifest identifies `Shop6.exe`, the VB6 runtime, and the Rigger 2 data files.
- [Shop142s.zip](legacy/Shop142s.zip) is the original source download. Use `Shop.vbp` for the project structure, `Evaluate.bas` for calculations, and `FileOps.bas` for file formats and HTML output.

The archives are preserved as supplied. The source archive contains no license file.
