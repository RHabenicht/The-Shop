# The Shop

The Shop is a vehicle builder for Shadowrun. This project updates James Ojaste’s original Rigger 2 program for Shadowrun Third Edition using **Rigger 3 Revised** and modern Visual Basic .NET.

The port can read legacy `.shp` vehicle saves and calculate Rigger 3 Revised stock vehicle costs and remaining cargo and load from entered design values. The component catalog covers 117 chassis and 370 chassis and power plant combinations. Coverage includes ground vehicles, boats, hovercraft, crewed aircraft, aerial drones, ships, submarines, sea sleds, ground drones, rail vehicles, trailers, and rocket craft. The Windows desktop preview can edit and price catalog-based designs. The Windows workflow builds a self-contained x64 installer.

## Windows desktop preview

The editor lets you select a chassis and compatible power plant, enter options or modifications, and calculate the design cost and remaining capacity. Open and save use the same stock design JSON format as the command-line calculator. Legacy `.shp` import still runs separately.

On Windows, use the .NET 10 SDK to run the editor from source:

```sh
dotnet run --project src/TheShop.Desktop
```

The [Windows desktop workflow](../../actions/workflows/windows.yml) builds a self-contained x64 installer, runs the editor tests, and checks installation, startup, and uninstall. Download the `The-Shop-Windows-x64` artifact from a passing run and extract `The-Shop-Setup-x64.exe`.

## Apply design upgrades

In the Windows editor, use **Design upgrades** to enter the number of steps. The calculator applies costs and limits from *Rigger 3 Revised*, pp. 115–116. Supported upgrades cover speed, acceleration, single or split Handling, cargo, load, Signature, and Economy. Economy steps add five percent of the starting rating each, up to the power plant maximum. Signature has a gamemaster-set limit. The dialog also supports p. 115 fuel expansion for compatible fuel units and depth enhancement on Body-rated chassis with a depth rating.

Stock design JSON can contain the same selections. For example, `"Upgrades": {"OffRoadHandling": 1, "Acceleration": 2, "CargoSteps": 10, "LoadSteps": 3}` applies the functional upgrades from the Light Strike Vehicle example. Cargo steps add 0.2 CF each. Load steps add 10 kg, or 1 kg on Body 0 vehicles. Clear the corresponding manual capacity override before using automatic cargo or load steps. Existing designs without `Upgrades` still use their original entries.

Use manual entries for Hull-rated options, speed upgrades on vehicles with two operating speeds, and options outside this set. Fuel expansion uses the table’s electrical load costs. Jet entries measured in bars and fuel cells measured in liters require manual entries. Fixed-wing speed upgrades apply to flight speed and preserve stall speed.

## Add catalog modifications

Choose **Catalog mods…** in the editor. The catalog prices airlocks for submarines, crash cages, datajack ports, electronics ports, nitrous oxide injectors, rigger adaptation, spotlights, and three types of special tires. It uses the design costs and design capacity requirements from *Rigger 3 Revised*, pp. 116, 126, 128, 130, 132, 145–146, and 154–155. Special installations outside this catalog and in-play effects still require manual handling. Use one tire type and enter the number of tires. Rigger adaptation already includes a datajack port.

In stock design JSON, use `"CatalogModifications": [{"Id": "nitrous", "Quantity": 2}]` for a rating-2 injector. Catalog and manual entries are both included in the capacity and price totals. Keep each modification in only one of those lists.

The protection and sensor entries add vehicle armor, concealed armor, Bulwark, sensor systems, standard sonar, towed-array sonar, and hovercraft water seals (*Rigger 3 Revised*, pp. 131–132, 147–148, 152). Enter additional points for armor and Bulwark. The editor reports final protection, Handling, and sensor ratings. Enter the final sensor rating. To replace factory sensors, choose a higher rating. The calculator charges the full design points, cargo and load listed for the new system and gives no credit for the original equipment. Standard sonar is available on ships and submarines. Enter the final rating and choose a higher rating when replacing factory sonar. The same full-cost convention applies. Towed-array sonar keeps its own rating.

The electronics catalog includes the common equipment listed on p. 146, plus remote-control encryption and retransmission units on p. 147. For displays, enter the total screen size in 20 cm units. For radios and personal tactical communications units, enter total Flux before any electronics port bonus. For other attached equipment, enter the number of units. The p. 146 table supplies cargo requirements. Use a manual entry for the equipment’s separate purchase price and any known weight. Price electronics ports separately.

Magnetic-anomaly detectors are available on fixed-wing aircraft, rotor craft, and vectored-thrust aircraft. Over-the-horizon sensors are available on ships. Both require a sensor system and use the vehicle’s final Sensor rating to calculate Design Points (*Rigger 3 Revised*, p. 146).

Select **Retransmission power amplifier** to set Flux for the installed retransmission units. The catalog applies the same rating to each unit and rounds cargo down per amplifier (*Rigger 3 Revised*, p. 147). Amplifiers for other devices and mixed retransmission ratings still use manual entries.

## Availability

Select an Availability class in the editor to apply the p. 113 formula. The calculator rounds base price divided by ¥20,000 up, applies a minimum of two, then adds the class modifier to the rating and lead time. This follows the written formula. The worked security vehicle example omits the modifier. Street Index stays an independent input.

## Import a legacy vehicle

To bring a `Shop v1.40` save into the editor, choose the revised chassis and power plant and select **Import .shp…**. Review the source record before starting the new design. Original options and markups stay in **Legacy reference** and are saved with the new design. Reapply them using Rigger 3 Revised values where appropriate.

For command-line extraction:

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

The catalog uses the printed component tables in *Rigger 3 Revised*, pp. 194–205. Ship and submarine entries preserve Hull and Bulwark where the book uses those ratings, along with paired speeds and sonar data. Jet-turbine entries use the units printed on their source page. Page 199 uses liters and p. 200 uses bars. Aircraft with paired speed ratings retain their stall and flight speeds separately. Battery entries use Power Factors, methane uses bars, and liquid fuel uses liters. The aircraft, hovercraft, and submarine fuel-cell table on p. 197 labels capacity in Power Factors. Earlier fuel-cell entries follow the p. 196 table, which labels capacity in liters. Sail, nuclear, and unpowered entries omit fuel and economy where the tables give no ratings. Unpowered chassis use an Unpowered selection that carries the chassis load rating and costs zero power plant Design Points. Chemical rockets report capacity in Power Factors. Dedicated People Space is listed separately where specified. The gasoline sports car has 60 L in the table, while the worked race-car example lists 40 L.

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

## Original program

James Ojaste created The Shop. His site hosts the original Visual Basic source and credits David Taylor for entering most of the chassis and engine data and Nullsheen for arranging the later VB6 migration. 

- [ShopWindows10.zip](legacy/ShopWindows10.zip) contains the Windows installer for the VB6 build. Its setup manifest identifies `Shop6.exe`, the VB6 runtime, and the Rigger 2 data files.
- [Shop142s.zip](legacy/Shop142s.zip) is the original source download. Use `Shop.vbp` for the project structure, `Evaluate.bas` for calculations, and `FileOps.bas` for file formats and HTML output.

The archives are preserved as supplied. The source archive contains no license file.

## Export a vehicle sheet

Enter a vehicle name and choose **Export HTML…**. Open the exported file in a browser to print it or use the browser’s Save as PDF option. The sheet includes calculated ratings, capacity, price, Availability when selected, manual entries, catalog modifications, and any imported legacy reference.
