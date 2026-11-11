# Rigger 3 Revised coverage

Rigger 3 Revised supplies the rules and prices. Rigger 2 source records are conversion inputs only.

| Area | Implemented behavior | Printed reference |
| --- | --- | --- |
| Components | 117 chassis and 370 power plant combinations; original units retained | 194–205 |
| Functional upgrades | Speed, acceleration, Handling, cargo, Load, depth, Economy, fuel storage and Signature; chassis and plant limits | 114–116 |
| Underwater vehicles | Sealed non-submarine starting depth, drone multiplier, depth maximum and reinforcement weight | 115 |
| Hull installations | General Hull Factor substitution; Hull load upgrades, Bulwark, naval stations and mount budgets | 114–116, 132, 138 |
| Design enhancements | Auxiliary and hybrid engines, fin drives, smart materials, special storage, setup, flight and water profiles | 116–119 |
| Facilities and robots | Amenities, People Space, work spaces, robot Pilot, reflexes, neural networks and Adaptation Pool options | 119–121 |
| Engine and controls | Design/customization costs and limits; drive-by-wire allocation; suspension, Autonav and remote control | 125–130 |
| Protection | Vehicle, personal ballistic, concealed, ablative and smart armor; seals, life support, roll bars | 131–134 |
| Signature systems | Active thermal masking and venting, baffles, radar materials, noisemakers | 134–135 |
| Weapons | Mount budgets, turret Weapon Values, arcs, pop-up/anti-aircraft combinations, recoil, aircraft door guns | 135–142 |
| Naval equipment | Launch stations, tubes/loaders, storage racks and external/internal ordnance mounts | 137–141 |
| Payload | Actual item count, weight, list price and cargo; weapons/ordnance assigned to numbered mounts | 135–141 |
| Turret storage | Per-turret CF and seating; stored cargo does not consume vehicle CF twice | 140 |
| Electronics | Sensors, sonar, ECM/ECCM/ED/ECD, ports, amplifiers, encryption and relay units | 142–148 |
| Accessories | Drop tanks, amphibious packages, security, crane/winch, drone racks, arms, clinics, seating, tires | 149–155 |
| Retrofit quotes | 161 catalog entries; printed parts prices, Availability, SI and customization capacity; source and installation references | 122–155 |
| Individual equipment | Different arm strengths, mount-specific recoil and amplifiers; design and retrofit pricing | 137, 147, 152 |
| Old saves | All 128 standard source names are classified for revised conversion, removal or missing replacement specifications; original evidence retained | Conversion uses the revised entries above |
| Sheet export | Ratings, price, operating modes, payload, mounting, retrofit totals, references, import report and GM review | 113, 121 |

## Source limits and player data

The book requires final GM approval. Signature limits, secret engine reliability tests, unusual designs and campaign adjustments are recorded for that review. They do not prevent preparing a proposed vehicle sheet. Permanent engine Stress can be recorded after the GM resolves the secret test.

A dash in a parts Street Index table remains unpriced on the street until a GM override is supplied. The printed list cost and physical equipment effects remain available. Unknown street prices are never zeroed.

Some engine tables and the fuel-enlargement table use incompatible units. Additional storage is retained in the enlargement table's units beside the original tank capacity. The program does not invent a liters/bars/PF conversion.

A named weapon, ammunition load, passenger, tool or electronic device requires its actual weight and purchase price. These are payload inputs; selecting a generic vehicle component does not identify a particular model. Physical mount/space costs already reserved by a catalog entry must not be entered a second time as payload cargo.

The old medical-treatment record contains patient count but no revised clinic rating. The old drone-rack record contains storage space but no carried drone Body. Such incomplete records retain their original data and a conversion explanation. Revised components can be selected in the catalog once those actual specifications are known. Obsolete aggregate engine-budget checks, cosmetic flags and price modifiers are discarded. Homebrew or unknown options are removed from the active design. Arms, recoil, amplifiers and tires use the same current calculators as new designs; no copied pricing formula is retained in the importer. Source history never supplies executable rules or prices.

## Verification before packaging

Windows CI publishes the self-contained application and runs the full desktop regression executable. Checks include all stock chassis/plant combinations and all compatible retrofit quotes, plus actual file round trips, conversion, payload/mount rejection, armor modes, individual equipment, Hull upgrades and HTML output.

Installer creation, installer startup/uninstall verification and installer upload are gated behind the manual `package_installer` workflow input. Packaging is the final remaining delivery operation after the source checks pass.
