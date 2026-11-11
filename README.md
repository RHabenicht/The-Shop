# The Shop

The Shop is a vehicle builder for Shadowrun. This project updates James Ojaste’s original Rigger 2 program for Shadowrun Third Edition using **Rigger 3 Revised** and modern Visual Basic .NET.

The port can read legacy `.shp` vehicle saves and calculate Rigger 3 Revised stock vehicle costs and remaining cargo and load from entered design values. The component catalog covers 117 chassis and 379 chassis and power plant combinations. Included chassis equipment is displayed and participates in dependencies and mounting without charging its price, space or weight again. Factory arm Strength and torpedo tube counts retain their printed specifications; unspecified torpedo loading methods are not invented. Coverage includes ground vehicles, boats, hovercraft, crewed aircraft, aerial drones, ships, submarines, sea sleds, ground drones, rail vehicles, trailers, and rocket craft. The Windows desktop preview can edit and price catalog-based designs. The Windows workflow verifies the desktop build. Final installer packaging is a separate manual workflow step.

## Windows desktop

The editor lets you select a chassis and compatible power plant, enter options or modifications, and calculate the design cost and remaining capacity. Open and save use the same stock design JSON format as the command-line calculator. The editor imports legacy `.shp` vehicles and converts their supported selections to revised rules.

On Windows, use the .NET 10 SDK to run the editor from source:

```sh
dotnet run --project src/TheShop.Desktop
```

The [Windows desktop workflow](../../actions/workflows/windows.yml) publishes the Windows executable and runs editor, calculation, conversion and file round-trip tests on pushes and pull requests. Installer packaging is deferred. When the implementation is ready to package, run the workflow manually with `package_installer` enabled; that final step builds the installer, tests installation/startup/uninstall and uploads `The-Shop-Windows-x64`.

## Apply design upgrades

In the Windows editor, use **Design upgrades** to enter the number of steps. The calculator applies costs and limits from *Rigger 3 Revised*, pp. 115–116. Supported upgrades cover speed, acceleration, single or split Handling, cargo, load, Signature, and Economy. Economy steps add five percent of the starting rating each, up to the power plant maximum. Signature has a gamemaster-set limit. The dialog also supports p. 115 fuel expansion for compatible fuel units and depth enhancement on chassis with a depth rating, including Hull-rated submarines.

Stock design JSON can contain the same selections. For example, `"Upgrades": {"OffRoadHandling": 1, "Acceleration": 2, "CargoSteps": 10, "LoadSteps": 3}` applies the functional upgrades from the Light Strike Vehicle example. Cargo steps add 0.2 CF each. Load steps add 10 kg, or 1 kg on Body 0 vehicles. Clear the corresponding manual capacity override before using automatic cargo or load steps. Existing designs without `Upgrades` still use their original entries.

Hull-rated load upgrades are available in the same dialog, using the nonzero-size vehicle increment. Where a power plant lists two operating speeds, the editor prices increases to each rating separately and enforces its listed maximum. Fuel expansion uses the table’s electrical load costs. Where an engine table uses bars or liters but the fuel enlargement table specifies another unit, the added storage and original capacity are reported separately. No conversion between incompatible printed units is assumed. Fixed-wing speed upgrades apply to flight speed and preserve stall speed.

## Add catalog modifications

Choose **Catalog mods…** in the editor. The catalog prices convertible tops, airlocks for submarines and vehicles with sealed cabins, crash cages, datajack ports, electronics ports, nitrous oxide injectors, rigger adaptation, spotlights, and three types of special tires. It uses the design costs and design capacity requirements from *Rigger 3 Revised*, pp. 116, 126, 128, 130, 132, 145–146, and 154–155. Payload entries hold the actual weights and purchase prices of carried equipment. The sheet identifies effects and decisions that are resolved during play. Use one tire type and enter the number of tires. Rigger adaptation already includes a datajack port.

In stock design JSON, use `"CatalogModifications": [{"Id": "nitrous", "Quantity": 2}]` for a rating-2 injector. Catalog and manual entries are both included in the capacity and price totals. Keep each modification in only one of those lists.

The protection and sensor entries add vehicle armor, concealed armor, Bulwark, sensor systems, standard sonar, towed-array sonar, and hovercraft water seals (*Rigger 3 Revised*, pp. 131–132, 147–148, 152). Enter additional points for armor and Bulwark. The editor reports final protection, Handling, and sensor ratings. Enter the final sensor rating. To replace factory sensors, choose a higher rating. The calculator charges the full design points, cargo and load listed for the new system and gives no credit for the original equipment. Standard sonar is available on ships and submarines. Enter the final rating and choose a higher rating when replacing factory sonar. The same full-cost convention applies. Towed-array sonar keeps its own rating.

The electronics catalog includes the common equipment listed on p. 146, plus remote-control encryption and retransmission units on p. 147. For displays, enter the total screen size in 20 cm units. For radios and personal tactical communications units, enter total Flux before any electronics port bonus. For other attached equipment, enter the number of units. The p. 146 table supplies cargo requirements. Use **Payload / equipment…** for the actual device’s separate purchase price and weight. Leave its payload cargo at zero when the catalog entry already reserves its space. Price electronics ports separately.

Magnetic-anomaly detectors are available on fixed-wing aircraft, rotor craft, and vectored-thrust aircraft. Over-the-horizon sensors are available on ships. Both require a sensor system and use the vehicle’s final Sensor rating to calculate Design Points (*Rigger 3 Revised*, p. 146).

Select **Retransmission power amplifier** to set Flux for the installed retransmission units. The catalog applies the same rating to each unit and rounds cargo down per amplifier (*Rigger 3 Revised*, p. 147). Generic power amplifiers have their own catalog entry. The **Individual equipment** tab supports different amplifier ratings and identifies the device each serves, with design and retrofit pricing.

Amphibious operation packages add surface-water operation to bike and car chassis (*Rigger 3 Revised*, p. 149). Level 1 requires wheels. The editor and vehicle sheet list the water speed and Handling modifier separately from land ratings. The package describes surface-water performance. Assess underwater operating limits separately.

The anti-theft catalog includes improved security at ratings 3–10, the electric system, and proximity alerts (*Rigger 3 Revised*, pp. 149–150). Enter the final security rating. Proximity alerts require either factory sensors or a sensor system selected in the current design.

For a crane, enter its lifting capacity in kilograms. The catalog limits the rating by chassis Body, charges the rating divided by 50 in Design Points, and deducts 15 CF and 80 kg per point of Body (*Rigger 3 Revised*, pp. 150–151). Hull-only chassis have no Body-based crane rating in the catalog.

The equipment catalog includes winches, generators for trailers, barges and rail cars, medical clinics, Valkyrie modules, and added standard, reinforced, folding, and ejection seats (*Rigger 3 Revised*, pp. 151–155). Enter winch pulling capacity in kilograms and seat counts as additional seats. Existing seating and clinic People Space are determined separately.

Aircraft floats apply their speed and economy adjustments after design upgrades. The drone rack entries are sized for Body 0–3 assembled aerial drones. Rack weight is charged at design time, and carried drone weight is payload. A mechanical arm starts at Body squared in Strength. An enhancement applies the selected increase to every installed arm (*Rigger 3 Revised*, pp. 62, 151–152). Use the **Individual equipment** tab for different enhancements on numbered arms. The builder applies the revised per-arm costs, weight and Strength limit.

Cabin and engine seals, cabin over-pressurization, and life support are available as separate options. Enter life support in blocks of ten person-hours. Check that the design has a suitable sealed cabin and that its entry points, engine air supply, and depth limits suit the intended operation. Roll bars and SunCell solar power are also available (*Rigger 3 Revised*, pp. 126, 132–133).

Control options include GridLink, adjusted and secondary controls, contingency maneuver controls, advanced passenger protection, motorbike gyro-stabilization, Autonav, remote-control interfaces, and advanced Pilot ratings (*Rigger 3 Revised*, pp. 126–131). Autonav and Pilot selections set final ratings. Remote-control interfaces require sensors and give a base Pilot of 1. Motorbikes require gyro-stabilization for remote control or Autonav 2 and above.

Additional entry points are free within the Body or Hull limit. Ballast tanks require an engine water seal and provide depth control. Horizontal underwater propulsion and practical operating depth depend on the rest of the design (*Rigger 3 Revised*, pp. 115–116).

Motorcycle sidecars add cargo and apply the size’s Handling penalty, a 15-point Speed reduction, and a ten-percent Economy reduction. Enter a runflat upgrade separately for each tire that receives one (*Rigger 3 Revised*, pp. 154–155).

Turbocharging and superconductive drive increase Speed and Acceleration, reduce Signature, and apply the Economy penalty after other design options. The catalog prices each level at 75 percent of the power plant’s Design Points and enforces the increased Speed ceiling (*Rigger 3 Revised*, p. 126).

The electronics catalog also covers ECM and ECCM ratings 1–10 and ED and ECD ratings 1–6. Radar-absorbent materials and thermal baffles report separate radar and infrared Signatures. Noisemaker dispensers, smart armor, and chameleon paint use the printed design specifications and compatibility rules (*Rigger 3 Revised*, pp. 134–135, 144–145, 153). Smart armor and radar-absorbent materials require the appropriate vehicle Availability class.

Improved suspension, off-road suspension, and improved control surfaces use their chassis-based design costs. Ground suspension benefits account for the selected tire type. Off-road suspension applies the printed Speed and Economy multiplier (*Rigger 3 Revised*, pp. 129–130).

Vehicles can select fixed mounts, pintles, rings, aircraft door-gun rings, crewed and remote turrets, and launch controls. Hull-rated installations use the book’s general Hull Factor substitution. These entries share the vehicle’s hardpoint and firmpoint budget. The payload editor assigns weapons and ordnance to numbered mount instances and validates Weapon Value or ordnance weight limits. It accounts for carried weapon, ammunition and crew weight and purchase cost. The sheet reports firing arcs, turret storage/seating and operating modes. Gear placed within a turret uses its internal CF without charging the same cargo space twice. Autosoft interpretation, BattleTac FDDM and IVIS receivers, ship CCSS integration, dipping sonar, and mount smartlink integration are also available (*Rigger 3 Revised*, pp. 136–143).

Active thermal masking uses the revised customization-cost multiplier, consumes 3 CF and 100 kg, and requires matching engine customization levels. The sheet shows active speed and Signature, operating duration and the ten-minute venting mode (*Rigger 3 Revised*, p. 134).

Engine customization has separate Speed, Acceleration, and Load entries with the first-level and subsequent-level prices. The sheet states the book’s secret reliability test. Enter the GM’s resulting permanent engine Stress in the editor after review; leaving it blank does not block the proposed design. Drive-by-wire requires a total ten-percent bonus allocation across Speed, Acceleration, and Load. It applies after other design changes and rounds whole-number ratings down. For suspension effects, a vehicle with improved suspension or drive-by-wire and no selected tire type uses the book’s assumed dual-purpose tires. Tire purchase prices still require the actual count (*Rigger 3 Revised*, pp. 125, 129, 155).

Turret variants include anti-aircraft and pop-up configurations. Pop-up turrets use their stowed Signature and doubled cargo requirement. Torpedo tubes include a manual-loading or autoloading option. Torpedo racks, internal missile storage, external missile mounts, and naval weapon control stations are available separately. Account for carried ordnance weight and review naval hardpoint allocation separately (*Rigger 3 Revised*, pp. 137–141).

Choose **Auxiliary engine…** to select an alternate engine from the chassis’s compatible power plants. Its speed and acceleration ratings are half the normal ratings. The design uses the lower starting and maximum Load values of the two engines and charges the auxiliary engine’s listed Design Points, space, and installation weight (*Rigger 3 Revised*, p. 116).

Robot options include robotic-pilot programming, robotic reflexes, improved neural networks, fuzzy logic, and multi-object manipulation. Costs and limits use the final Pilot rating and Adaptation Pool. Robotic programming replaces drone-pilot programming. Amenities, workshop and facility equipment space, and reserved People Space have separate entries. Add tool and facility purchase prices as manual costs (*Rigger 3 Revised*, pp. 119–121).

Design enhancements include hybrid engines, smart materials, fin drives, removed manual controls, setup/breakdown capability, special storage, Structural Agility, tailhooks, and STOL/VSTOL profiles. Hybrid engines and fin drives adjust starting ratings before functional upgrades. Smart materials raise maximum ratings. Smart materials automatically add 1 to the book Street Index. Fin drives apply the same +1 Street Index increase as smart materials. They add four to the Availability target number when an Availability class is selected; the acquisition time retains its ordinary value. Enter other GM-assigned chassis markup adjustments separately. Special storage entries charge supporting machinery space; the designated storage volume remains cargo space (*Rigger 3 Revised*, pp. 117–119).

Hydrofoils, ducted waterjets, and nuclear-powered electromagnetic ducted drives show their engaged ratings as operating modes. Flight decks use a length in metres, with catapult/arrestor wire and angled-deck options. Check aircraft size, runway profile, and deck length for takeoff and landing (*Rigger 3 Revised*, pp. 116–119).

Partial amenities serve groups of up to 50 people. Special machinery consumes its designated space and requires a gamemaster-set markup. Bench-seat armor is limited to two points per bench; the book does not impose that limit on bucket-seat armor. Spare tires have their own storage and purchase costs. Firearm mount conversions, uniform gunnery recoil adjusters, and weapon gyro-stabilization use their design specifications. Enter weapon weights and mixed recoil-adjustment configurations separately (*Rigger 3 Revised*, pp. 118, 120, 136–137, 142, 154–155).

Ammunition bins use 0.2 CF blocks at design time. Payload rows account for carried ammunition weight. Aircraft drop tanks come in pairs, consume external mounting positions, increase fuel capacity, and apply mounted performance penalties. The exported sheet also shows the jettisoned operating mode (*Rigger 3 Revised*, pp. 136, 149).

## Availability

Select an Availability class in the editor to apply the p. 113 formula. The calculator rounds base price divided by ¥20,000 up, applies a minimum of two, then adds the class modifier to the rating and lead time. This follows the written formula. The worked security vehicle example omits the modifier. With Street Index override blank, the editor uses the p. 113 base index: Standard 1, Luxury 2, Security 2, Military 3 (the start of the printed 3+ range). Medical specialty adds 0.5 and noncombat specialty adds 1. Smart materials add 1 (p. 118). A numeric override is a final Street Index and replaces these defaults. Existing saved numeric values remain final overrides. The printed military minimum is applied for radar materials and manufactured smart armor; ablative armor applies the printed security classification. Final approval and any final Street Index override belong to the gamemaster (pp. 113, 121).

## Quote a retrofit

Choose **Retrofit quote…** to calculate list and street parts cost, Availability, and remaining capacity for one item on the current design. The retrofit catalog covers 164 entries, including engine and suspension work, controls, armor and ablative protection, Signature systems, electronic systems, mounts/turrets, naval installations and accessories. Parts cost, Availability, Street Index and customization space/weight use the printed specifications. Percentage prices use the vehicle’s list price before Street Index. Quotes and sheets identify source pages and installation requirements. Choose **Install** to add the quoted equipment to the vehicle, then save the design. **Installed retrofits…** changes quantities or removes entries with a zero quantity. Installed parts use customization capacity requirements and appear on the HTML sheet with a separate street parts total. Sensor, sonar, ECM, ECCM, ED, ECD, Autonav, Pilot, and anti-theft retrofits are also supported. Printed parts Street Index values are used automatically. When the book specifies the vehicle’s own Street Index or Availability, the quote inherits those values. Invalid dependencies disable Install with an explanation. Where a table lists a dash, the equipment can still be added, saved and exported on a proposed vehicle for GM review. Its list cost, Availability, ratings and capacity remain calculated. The street parts total stays unresolved until a GM Street Index override is supplied; it is never shown as zero. An optional override is saved with the equipment. Replacement ratings must exceed the existing rating. Existing equipment receives no parts or capacity credit. External cargo mounts are available as retrofits and retain their own capacity. Their loaded Handling modifier appears as an operating mode. Installation base time, equipment and skill tests follow the printed specifications. Tires have no listed installation time; torpedo storage is included in the torpedo installation. Actual work time depends on Installation Test successes. Labor, mechanic contact rates and the secret engine reliability test are resolved during play.

## Import a legacy vehicle

To bring a `Shop v1.40` save into the editor, select **Import .shp…**. The editor suggests the revised chassis and compatible power plant when both names match uniquely. For other saves it uses the editor’s current selection. Review the source record before starting the new design. Original options and markups stay in **Legacy reference** and are saved with the new design. The importer accounts for all 128 standard option names in the supplied Rigger 2 archive. Supported choices map to revised rules; obsolete entries are removed from the active design. It converts old units, zero-level switches, sensor increases, control allocations, turret variants, seats, arms and mount-specific recoil adjusters. Revised costs and limits replace the old formulas. Dependencies are processed together, independent of save order. Invalid groups are reported while independent valid groups still convert. Old patient-count medical gear and drone rack space lack the clinic rating or carried drone Body required by the revised rules and stay in the source reference with an explanation. Amplifiers and tires use the active revised catalog. Mixed arm enhancements and recoil settings use the same current equipment calculators as new designs. The obsolete aggregate engine budget is discarded. Convertible tops map to the revised p. 150 option, including its zero manufacture cost, retrofit price and roll-bar requirement for a ring mount. Homebrew and unknown entries are removed from the active design and reported as removed. Their source records remain only as import history. Luxury and medical/noncombat classifications use revised pricing; old unusual/special price modifiers are retained only as reference. The notes are saved and included on the exported sheet.

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

The catalog uses the printed component tables in *Rigger 3 Revised*, pp. 194–205. The p. 195 battery entry for Cars: All Others applies to the remaining car chassis as well. Its literal starting Acceleration formula, 6 − Body, can yield a nonpositive template rating on heavy chassis. Those entries retain the printed result and a source note; select Acceleration improvements or another engine before using the design for movement. Ship and submarine entries preserve Hull and Bulwark where the book uses those ratings, along with paired speeds and sonar data. Jet-turbine entries use the units printed on their source page. Page 199 uses liters and p. 200 uses bars. Aircraft with paired speed ratings retain their stall and flight speeds separately. Battery entries use Power Factors, methane uses bars, and liquid fuel uses liters. The aircraft, hovercraft, and submarine fuel-cell table on p. 197 labels capacity in Power Factors. Earlier fuel-cell entries follow the p. 196 table, which labels capacity in liters. Sail, nuclear, and unpowered entries omit fuel and economy where the tables give no ratings. Unpowered chassis use an Unpowered selection that carries the chassis load rating and costs zero power plant Design Points. Chemical rockets report capacity in Power Factors. Dedicated People Space is listed separately where specified. The gasoline sports car has 60 L in the table, while the worked race-car example lists 40 L.

## Calculate a stock vehicle design from manual inputs

```sh
dotnet run --project src/TheShop.Design -- path/to/design.json
```

The calculator takes Rigger 3 Revised values entered in a JSON file. Enter cargo and load capacity after any capacity upgrades and before subtracting the space and weight used by additions. Include the Design Point costs of those upgrades in the additions. For this manual-input format, Street Index is the final value to apply. Stock catalog designs can omit it to use the printed class and specialty defaults.

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

Enter a vehicle name and choose **Export HTML…**. Open the exported file in a browser to print it or use the browser’s Save as PDF option. The sheet includes calculated ratings, capacity, price, Availability, payload and weapon assignments, individual equipment settings, turret space and arcs, retrofit prices and installation references, operating modes and any imported legacy reference.

## Verification and remaining packaging

The catalog contains 225 rule entries, 117 chassis and 379 compatible chassis/power plant combinations. The independent rules audit checks every electronics rating and turret variant against transcribed printed values, targeted dependency and pricing regressions, and calculation/JSON round trips for all stock combinations. The stock and quote sweeps check execution and consistency; they are not numerical proof of every formula. Windows tests exercise the editor, invalid numeric edits, missing quote prerequisites, overwrite/reopen, conversion and HTML export. The final installer is intentionally not rebuilt during development checks.

[Rigger 3 Revised coverage](docs/RIGGER_3_REVISED_COVERAGE.md) records the implementation scope and source references. GM approval and actual player choices remain part of the vehicle sheet workflow. Missing specifications for a revised replacement are identified as source-data gaps. Obsolete rules never remain as active or manual fallback options.
