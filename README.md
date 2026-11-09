# The Shop

The Shop is a vehicle builder for Shadowrun. This project updates James Ojaste’s original Rigger 2 program for Shadowrun Third Edition using **Rigger 3 Revised** and modern Visual Basic .NET.

The port can read legacy `.shp` vehicle saves and calculate Rigger 3 Revised stock vehicle costs and remaining cargo and load from entered design values. A starter component catalog supplies chassis and power plant values for six ground vehicle chassis. The full catalog, vehicle editor, and Windows installer are still in development.

## Import a legacy vehicle

With the .NET 10 SDK installed, run:

```sh
dotnet run --project src/TheShop.Import -- path/to/vehicle.shp
```

The importer accepts the `Shop v1.40` save header emitted by the original source archive. It writes JSON containing the vehicle name, chassis, engine, seating, entry points, markups, and modifications. Chassis, engine, and modification entries retain their source book names. The JSON identifies the input as Rigger 2 data for later conversion. Older save headers are reported as unsupported.

## Select catalog components

```sh
dotnet run --project src/TheShop.Design -- --catalog
dotnet run --project src/TheShop.Design -- --stock path/to/stock.json
```

A minimal stock design file looks like this:

```json
{
  "ChassisId": "suv",
  "PowerPlantId": "diesel",
  "StreetIndex": 1
}
```

The catalog lists compatible power plants under each chassis along with their starting values and limits. The calculator looks up component Design Points, chassis markup, and starting cargo and load. You can supply `CargoCapacity` and `LoadCapacityKg` within the catalog limits. Enter the costs of any capacity upgrades in `Additions`. Use the manual design format below for designs that change those limits or use components outside the starter catalog.

The starter catalog uses the printed component tables in *Rigger 3 Revised*, pp. 194, 198–199 and 202. The gasoline sports car has 60 L in the table, while the worked race-car example lists 40 L.

## Calculate a stock vehicle design from manual inputs

```sh
dotnet run --project src/TheShop.Design -- path/to/design.json
```

The calculator takes Rigger 3 Revised values entered in a JSON file. Enter cargo and load capacity after any capacity upgrades and before subtracting the space and weight used by additions. Include the Design Point costs of those upgrades in the additions. The Street Index is the final value assigned by the gamemaster.

```json
{
  "ChassisDesignPoints": 40,
  "PowerPlantDesignPoints": 20,
  "ChassisMarkup": 0.4,
  "CargoCapacity": 6,
  "LoadCapacityKg": 70,
  "StreetIndex": 2,
  "Additions": [
    { "Name": "Example mount", "DesignPoints": 10, "CargoConsumed": 1, "LoadConsumedKg": 25 }
  ]
}
```

`MarkupAdjustment` is optional and is added to the chassis markup. Use `DirectCostNuyen` on an addition only where the rules specify a separate nuyen charge. The calculator multiplies total Design Points by the effective markup and 100, adds any separate nuyen costs, and applies Street Index to produce the street price. The tool checks remaining cargo and load. Component compatibility, upgrade limits, and Availability still require manual review. Legacy importer output uses a separate format.

The Lockheed-Chenowth Light Strike Vehicle example reproduces the book's 169 Design Points, ¥6,760 base price, ¥13,520 street price, 5 CF and 45 kg remaining. For the Brickyard Beauty, the book gives ¥210,300 before tires and ¥900 for tires. The calculator gives ¥211,200 base and ¥633,600 at Street Index 3 from those inputs. The printed totals on p. 114 differ. See *Rigger 3 Revised*, pp. 111–114.

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
