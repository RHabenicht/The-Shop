Imports System.IO
Imports System.Text.Json
Imports TheShop.Design

Module Program
    Private catalog As IReadOnlyList(Of StockChassis)
    Private failures As New List(Of String)
    Private checks As Integer

    Sub Main()
        catalog = StockCatalog.Read(New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True})
        Dim folder = Path.Combine(Path.GetTempPath(), "shop-rules-audit-" & Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(folder)
        Try
            Test("legacy conversion and save/export", Sub() LegacyConversionChecks.Run(folder, catalog))
            Test("off-road speed and economy, p.129–130", Sub()
                Dim d = Van(("off-road-suspension", 2), ("off-road-tires", 4))
                Dim r = d.Calculate(catalog)
                Equal(r.ModifiedRatings.Speed, 65, "Speed")
                Equal(r.ModifiedRatings.Economy, 4.25D, "Economy")
                Equal(r.ModifiedRatings.OnRoadHandling, 6, "On-road Handling")
                Equal(r.ModifiedRatings.OffRoadHandling, 8, "Off-road Handling")
            End Sub)
            Test("hydrofoil operating ratings, p.117", Sub()
                Dim d As New StockDesign With {.ChassisId = "speedboat", .PowerPlantId = "diesel", .Upgrades = New DesignUpgrades With {.CargoSteps = 40}, .CatalogModifications = Choices(("hydrofoil", 1))}
                Dim p = Plant(d)
                Dim mode = d.Calculate(catalog).OperatingModes.Single()
                Equal(mode.Speed, CInt(Math.Floor(p.StartingSpeed.Value * 1.25D)), "Speed")
                Equal(mode.Signature, p.Signature.Value + 1, "Signature")
            End Sub)
            Dim conversionIds = {"pistol-mount-conversion", "smg-mount-conversion", "rifle-mount-conversion", "heavy-mount-conversion"}
            Dim conversionCosts = {150D, 350D, 750D, 1000D}
            For i = 0 To conversionIds.Length - 1
                Dim id = conversionIds(i), expected = conversionCosts(i)
                Test(id & " printed parts price, p.136", Sub() Equal(Quote(id, 2).PartsCostNuyen, expected * 2D, "Parts price"))
            Next
            Test("heavy conversion Availability, p.136", Sub() Equal(Quote("heavy-mount-conversion", 1).Availability, "16 / 14 days", "Availability"))
            Test("Smartlink I Street Index, p.139", Sub() Equal(Quote("smartlink-integration-1", 1).StreetIndex, 1D, "Street Index"))
            Test("uniform retrofit recoil pays for each mount, p.137", Sub()
                Dim d = Van(("external-fixed-firmpoint", 2))
                d.InstalledRetrofits = Choices(("gunnery-recoil-adjusters", 3))
                Dim r = d.Calculate(catalog)
                Equal(r.RetrofitListPartsCostNuyen, 3000D, "Parts price")
                Equal(r.Totals.CargoRemaining, 45D, "Cargo")
                Equal(r.Totals.LoadRemainingKg, 276D, "Load")
            End Sub)
            Test("factory and retrofit arm settings retain their origin", Sub()
                Dim d = Van(("mechanical-arm", 1))
                d.InstalledRetrofits = Choices(("mechanical-arm", 1))
                d.EquipmentSettings = New List(Of EquipmentSetting) From {
                    New EquipmentSetting With {.Component = "Arm strength", .Rating = 1, .Instance = 1},
                    New EquipmentSetting With {.Component = "Arm strength", .Rating = 1, .Instance = 2}}
                d.PowerPlantId = "diesel"
                Dim r = d.Calculate(catalog)
                Equal(r.ConfiguredRetrofitListCost, 20000D, "Only second arm is a retrofit")
                Equal(r.Totals.DesignPoints, 735D, "Factory arm enhancement")
            End Sub)
            Test("fixed hardpoint holds one weapon, pp.135–136", Sub()
                Dim d = Van(("external-fixed-hardpoint", 1))
                d.Payload = New List(Of VehiclePayloadItem) From {Weapon("Pistol", "external-fixed-hardpoint", 1D, 2)}
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            Test("manufactured uniform recoil does not cover future retrofit mounts", Sub()
                Dim d = Van(("external-fixed-firmpoint", 1), ("gunnery-recoil-adjusters", 3))
                d.InstalledRetrofits = Choices(("external-fixed-firmpoint", 1))
                d.EquipmentSettings = New List(Of EquipmentSetting) From {New EquipmentSetting With {.Component = "Recoil", .Target = "external-fixed-firmpoint", .Instance = 2, .Rating = 2}}
                Dim r = d.Calculate(catalog)
                Equal(r.ConfiguredRetrofitListCost, 1000D, "New mount's own adjusters")
                Equal(r.Totals.DesignPoints, 140D, "Original manufacture price")
                Equal(r.Totals.LoadRemainingKg, 304D, "Both mounts and new adjusters")
            End Sub)
            Test("a fixed gun mount cannot store missiles", Sub()
                Dim d = Van(("internal-fixed-firmpoint", 1))
                d.Payload = New List(Of VehiclePayloadItem) From {Weapon("Missile", "internal-fixed-firmpoint", 1D)}
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            Test("normal missile store cannot store heavy missiles", Sub()
                Dim d = Van(("internal-missile-storage", 1))
                d.Payload = New List(Of VehiclePayloadItem) From {Weapon("Heavy Missile", "internal-missile-storage", 1D)}
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            Test("aircraft mount accepts multiple missiles up to combined mass, p.137", Sub()
                Dim d As New StockDesign With {.ChassisId = "cargo-helicopter", .PowerPlantId = "jet-turbine", .CatalogModifications = Choices(("external-missile-mounts", 1))}
                d.Payload = New List(Of VehiclePayloadItem) From {Weapon("Missile", "external-missile-mounts", 100D, 3)}
                d.Calculate(catalog)
                d.Payload(0).Quantity = 4
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            Test("rockets and aircraft bombs retain their own ordnance classes", Sub()
                Dim d = Van(("internal-missile-storage", 1))
                d.Payload = New List(Of VehiclePayloadItem) From {Weapon("Rocket", "internal-missile-storage", 2D)}
                d.Calculate(catalog)
                d.Payload(0).WeaponClass = "Bomb"
                Reject(Sub() d.Calculate(catalog))
                d.ChassisId = "cargo-helicopter" : d.PowerPlantId = "jet-turbine"
                d.LoadCapacityKg = 2000D
                d.CatalogModifications = Choices(("external-missile-mounts-reinforced", 1))
                d.Payload(0).MountId = "external-missile-mounts-reinforced"
                d.Payload(0).Quantity = 3 : d.Payload(0).UnitWeightKg = 500D
                d.Calculate(catalog)
                d.Payload(0).Quantity = 4
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            Test("uniform enhancement quote requires its target", Sub()
                Reject(Sub() Quote("mechanical-arm-strength", 1))
                Reject(Sub() Quote("retransmission-power-amplifier", 1))
                Reject(Sub() Quote("autosoft-interpretation", 1))
                Reject(Sub() Quote("gunnery-recoil-adjusters", 1))
            End Sub)
            Test("upper-case mount identifiers behave consistently", Sub()
                Dim d = Van(("EXTERNAL-FIXED-FIRMPOINT", 1))
                d.EquipmentSettings = New List(Of EquipmentSetting) From {New EquipmentSetting With {.Component = "Recoil", .Target = "external-fixed-firmpoint", .Rating = 4}}
                d.Calculate(catalog)
            End Sub)
            Test("null selection rejects with a validation error", Sub()
                Dim d = Van()
                d.CatalogModifications = New List(Of ModificationSelection) From {Nothing}
                d.Payload = New List(Of VehiclePayloadItem) From {Weapon("Pistol", "external-fixed-firmpoint", 1D)}
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            Test("invalid specialty rejects even with a final Street Index override", Sub()
                Dim d = Van()
                d.StreetIndex = 2D
                d.SpecialtyClass = CType(99, VehicleSpecialtyClass)
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            Test("explicit null additions cannot silently discard data", Sub()
                Dim d = Van()
                d.Additions = Nothing
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            PrintedTables.Run(AddressOf Test)
            Test("fin drive pricing and Availability, p.117", Sub()
                Dim d As New StockDesign With {.ChassisId = "skiff", .PowerPlantId = "diesel", .AvailabilityClass = VehicleAvailabilityClass.Standard, .CatalogModifications = Choices(("fin-drive", 1))}
                Dim r = d.Calculate(catalog)
                Equal(r.EffectiveStreetIndex, 2D, "Smart-materials Street Index increase")
                Dim days = Math.Max(2D, Math.Ceiling(r.Totals.BaseCostNuyen / 20000D))
                Equal(r.Availability.Rating, days + 4D, "Availability target")
                Equal(r.Availability.BaseDays, days, "Acquisition time")
            End Sub)
            Test("vehicle-dependent retrofit prices inherit the vehicle SI, p.124", Sub()
                Dim d = Van()
                d.StreetIndex = 4D
                d.AvailabilityClass = VehicleAvailabilityClass.Security
                d.InstalledRetrofits = Choices(("standard-tires", 4))
                Dim r = d.Calculate(catalog)
                Equal(r.RetrofitListPartsCostNuyen, 800D, "Four replacement tires")
                Equal(r.RetrofitPartsCostNuyen.Value, 3200D, "Inherited Street Index")
                Equal(r.InstalledRetrofits.Single().Quote.Availability, $"{r.Availability.Rating} / {r.Availability.BaseDays} days", "Inherited Availability")
            End Sub)
            Test("security override preserves inherited Availability", Sub()
                Dim d = Van(("sensors", 1), ("improved-anti-theft-security", 7))
                d.InstalledRetrofits = Choices(("proximity-alert", 1))
                d.InstalledRetrofits(0).StreetIndexOverride = 4D
                Dim q = d.Calculate(catalog).InstalledRetrofits.Single().Quote
                Equal(q.StreetIndex.Value, 4D, "Override")
                Equal(q.Availability, "5 / 10 days", "Security system Availability")
            End Sub)
            Test("standard factory tires are included, p.155", Sub()
                Equal(Van(("standard-tires", 4)).Calculate(catalog).Totals.BaseCostNuyen, Van().Calculate(catalog).Totals.BaseCostNuyen, "Included tires")
            End Sub)
            Test("explicit standard tires halve Handling benefits, p.155", Sub()
                Dim d = Van(("standard-tires", 4), ("drive-by-wire", 1), ("drive-by-wire-load-allocation", 10))
                d.Upgrades = New DesignUpgrades With {.OnRoadHandling = 2}
                Equal(d.Calculate(catalog).ModifiedRatings.OnRoadHandling.Value, 3, "Halved functional improvement and rounded-down wire benefit")
            End Sub)
            Test("convertible top uses revised pricing and mount dependency, p.150", Sub()
                Dim d = Van(("convertible-top", 1))
                Equal(d.Calculate(catalog).Totals.BaseCostNuyen, 10000D, "Manufacture price")
                d.CatalogModifications.Add(New ModificationSelection With {.Id = "ring-mount", .Quantity = 1})
                Reject(Sub() d.Calculate(catalog))
                d.CatalogModifications.Add(New ModificationSelection With {.Id = "roll-bar", .Quantity = 1})
                d.Calculate(catalog)
                Dim q = Quote("convertible-top", 1)
                Equal(q.PartsCostNuyen, 3500D, "Customization price")
                Equal(q.InstallationBaseHours.Value, 24D, "Installation time")
            End Sub)
            Test("motorcycle sidecar hardpoint, p.135", Sub()
                Dim d As New StockDesign With {.ChassisId = "chopper", .PowerPlantId = "gasoline", .CatalogModifications = Choices(("sidecar", 2), ("external-fixed-hardpoint", 1))}
                d.Payload = New List(Of VehiclePayloadItem) From {Weapon("HMG", "external-fixed-hardpoint", 10D)}
                d.Calculate(catalog)
                d.Payload(0).Direction = "Rear"
                Reject(Sub() d.Calculate(catalog))
                d.Payload(0).Direction = "Forward"
                d.CatalogModifications(0).Quantity = 1
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            Test("fixed hardpoints are not subject to turret Weapon Values, p.135", Sub()
                Dim d = Van(("internal-fixed-hardpoint", 1))
                d.Payload = New List(Of VehiclePayloadItem) From {Weapon("Light Railgun", "internal-fixed-hardpoint", 20D)}
                d.Calculate(catalog)
            End Sub)
            Test("ring mount requires a man-portable gun, p.139", Sub()
                Dim d = Van(("ring-mount", 1))
                d.Payload = New List(Of VehiclePayloadItem) From {Weapon("Vehicle Laser", "ring-mount", 20D)}
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            Test("internal ordnance batches reserve distinct slots, p.138", Sub()
                Dim d = Van(("internal-missile-storage", 3))
                d.Payload = New List(Of VehiclePayloadItem) From {Weapon("Missile", "internal-missile-storage", 2D, 3)}
                Equal(d.Calculate(catalog).Totals.CargoRemaining, 37D, "Preallocated missile CF")
                d.Payload.Add(Weapon("Missile", "internal-missile-storage", 2D))
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            Test("two torpedo tubes share one loading installation, p.139", Sub()
                Dim chassis = StockCatalog.FindById(catalog, "attack-submarine")
                For Each id In {"torpedo-tube-manual", "torpedo-tube-autoloader"}
                    Dim d As New StockDesign With {.ChassisId = chassis.Id, .PowerPlantId = "diesel", .CargoCapacity = 2000D, .CatalogModifications = Choices((id, 2))}
                    Dim r = d.Calculate(catalog)
                    Equal(r.Totals.CargoRemaining, If(id = "torpedo-tube-manual", 1024D, 1384D), "Shared loading CF")
                    Equal(r.Totals.LoadRemainingKg, If(id = "torpedo-tube-manual", 749000D, 699000D), "Shared loading mass")
                Next
            End Sub)
            Test("cabin seal and overpressure share specified CF, p.133", Sub()
                Dim d = Van()
                d.InstalledRetrofits = Choices(("cabin-gas-seal", 1), ("cabin-overpressure", 1))
                Equal(d.Calculate(catalog).Totals.CargoRemaining, 46D, "Seal with overpressure")
            End Sub)
            Test("external mounts use manufactured cargo capacity, p.151", Sub()
                Dim d = Van(("bucket-seat", 1))
                d.Upgrades = New DesignUpgrades With {.CargoSteps = 260}
                d.Payload = New List(Of VehiclePayloadItem) From {New VehiclePayloadItem With {.Name = "Carried supplies", .UnitCargoCf = 30D}}
                d.InstalledRetrofits = Choices(("external-cargo-mounts", 94))
                Dim r = d.Calculate(catalog)
                Equal(r.OriginalCargoCapacity, 94D, "Manufactured rated cargo")
                Equal(r.ExternalCargoCapacity, 94D, "Maximum exterior capacity")
                Equal(r.Totals.CargoRemaining, 64D, "Cargo after supplies")
                d.InstalledRetrofits(0).Quantity = 95
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            Test("a zero-starting-cargo chassis can add mounts after cargo upgrades", Sub()
                Dim d As New StockDesign With {.ChassisId = "small-walker", .PowerPlantId = "electric-battery", .Upgrades = New DesignUpgrades With {.CargoSteps = 5}, .InstalledRetrofits = Choices(("external-cargo-mounts", 1))}
                Equal(d.Calculate(catalog).ExternalCargoCapacity, 1D, "Upgraded chassis exterior mount")
            End Sub)
            Test("bench and bucket armor limits differ, p.153", Sub()
                Dim d = Van(("bucket-seat", 1), ("seat-armor", 4))
                d.Calculate(catalog)
                d.CatalogModifications(0).Id = "bench-seat"
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            Test("all supported retrofit entries have installation requirements", Sub()
                Dim required = Choices(("mechanical-arm", 2), ("retransmission-unit", 2), ("external-fixed-firmpoint", 2), ("advanced-drone-pilot", 3), ("engine-customization-speed", 2), ("sensors", 3))
                For Each rule In RetrofitCatalog.Rules
                    Dim chassis = catalog.FirstOrDefault(Function(c) c.PowerPlants.Any(Function(p) RetrofitCatalog.AvailableRules(c, p).Any(Function(r) r.Id = rule.Id)))
                    If chassis Is Nothing Then Throw New InvalidOperationException("No compatible fixture for " & rule.Id)
                    Dim plant = chassis.PowerPlants.First(Function(p) RetrofitCatalog.AvailableRules(chassis, p).Any(Function(r) r.Id = rule.Id))
                    Dim quantity = If(rule.Id = "improved-anti-theft-security", 3, If(rule.Id = "advanced-drone-pilot", 2, 1))
                    Dim q = RetrofitCatalog.Quote(rule, quantity, chassis, plant, context:=required)
                    If q.SourcePage < 125 OrElse q.SourcePage > 155 OrElse String.IsNullOrWhiteSpace(q.EquipmentRequired) OrElse q.InstallationTest.Contains("see source") Then Throw New InvalidOperationException("Incomplete installation reference: " & rule.Id)
                    Dim unlistedTime = rule.Id.Contains("tires") OrElse rule.Id = "runflat-upgrade" OrElse rule.Id = "torpedo-storage-rack" OrElse rule.Id.Contains("-allocation")
                    If Not unlistedTime AndAlso (Not q.InstallationBaseHours.HasValue OrElse q.InstallationBaseHours <= 0D) Then Throw New InvalidOperationException("Missing time: " & rule.Id)
                Next
            End Sub)
            For Each chassis In catalog
                For Each engine In chassis.PowerPlants
                    Dim c = chassis, p = engine
                    Test(c.Id & "/" & p.Id & " stock calculation and JSON round trip", Sub()
                        Dim d As New StockDesign With {.ChassisId = c.Id, .PowerPlantId = p.Id}
                        Dim result = d.Calculate(catalog)
                        Equal(result.Totals.DesignPoints, c.DesignPoints + p.DesignPoints, "Stock points include factory equipment")
                        Equal(result.Totals.BaseCostNuyen, (c.DesignPoints + p.DesignPoints) * c.Markup * 100D, "Stock price")
                        Equal(result.Totals.CargoRemaining, c.StartingCargo, "Stock cargo")
                        Equal(result.Totals.LoadRemainingKg, p.StartingLoadKg, "Stock Load")
                        Dim reopened = JsonSerializer.Deserialize(Of StockDesign)(JsonSerializer.Serialize(d)).Calculate(catalog)
                        Equal(reopened.Totals.BaseCostNuyen, result.Totals.BaseCostNuyen, "Round-trip price")
                        Equal(reopened.Totals.CargoRemaining, result.Totals.CargoRemaining, "Round-trip cargo")
                    End Sub)
                Next
            Next
            Test("factory mechanical arms support individual enhancement, p.204", Sub()
                Dim d As New StockDesign With {.ChassisId = "medium-anthroform", .PowerPlantId = "diesel",
                    .EquipmentSettings = New List(Of EquipmentSetting) From {New EquipmentSetting With {.Component = "Arm strength", .Rating = 1}}}
                Dim result = d.Calculate(catalog)
                Equal(result.Totals.DesignPoints, 820D, "Only enhancement is priced")
                Equal(result.Totals.LoadRemainingKg, 65D, "Only added Strength consumes Load")
                d.EquipmentSettings(0).Rating = 15
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            Test("factory torpedo tubes accept payload without buying tubes again, p.205", Sub()
                Dim d As New StockDesign With {.ChassisId = "attack-submarine", .PowerPlantId = "diesel",
                    .Payload = New List(Of VehiclePayloadItem) From {Weapon("Torpedo", "factory-torpedo-tube", 2000D)}}
                Dim result = d.Calculate(catalog)
                Equal(result.Totals.BaseCostNuyen, 222000000D, "Factory price")
                Equal(result.Totals.LoadRemainingKg, 748000D, "Ordnance mass")
                d.Payload(0).MountNumber = 5
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            Test("factory cabin seal satisfies overpressure dependency, p.202", Sub()
                Dim d As New StockDesign With {.ChassisId = "jet-fighter", .PowerPlantId = "jet-turbine", .CatalogModifications = Choices(("cabin-overpressure", 1))}
                Equal(d.Calculate(catalog).Totals.CargoRemaining, 5D, "Factory gas seal consumes no additional cargo")
            End Sub)
            Test("included equipment appears on the proposed sheet", Sub()
                Dim d As New StockDesign With {.ChassisId = "medium-anthroform", .PowerPlantId = "diesel"}
                Dim html = VehicleSheet.Render(d, catalog)
                If Not html.Contains("Mechanical arm (factory included)") OrElse Not html.Contains("Factory arm Strength") Then Throw New InvalidOperationException("Factory equipment is absent from export.")
            End Sub)
            Test("MAD retrofit uses actual sensor rating, p.146", Sub()
                Dim d As New StockDesign With {.ChassisId = "airliner", .PowerPlantId = "jet-propeller", .InstalledRetrofits = Choices(("magnetic-anomaly-detector", 1))}
                Dim result = d.Calculate(catalog)
                Equal(result.RetrofitListPartsCostNuyen, 20000D, "MAD parts")
                Equal(result.Totals.CargoRemaining, 484D, "MAD cargo")
                Equal(result.Totals.LoadRemainingKg, 4950D, "MAD mass")
            End Sub)
            For Each id In {"rv", "tractor", "medium-transport", "heavy-transport", "light-caterpillar", "medium-caterpillar", "heavy-caterpillar", "tracked-apc", "wheeled-apc"}
                Dim chassis = StockCatalog.FindById(catalog, id)
                Test(id & " battery All Others formula, p.195", Sub()
                    Dim p = chassis.PowerPlants.Single(Function(item) item.Id = "electric-battery")
                    Dim body = chassis.Body.Value
                    Equal(p.StartingLoadKg, 15D * body, "Starting Load")
                    Equal(p.MaximumLoadKg, 100D * body, "Maximum Load")
                    Equal(p.StartingAcceleration.Value, 6 - body, "Starting Acceleration")
                    Equal(p.MaximumAcceleration.Value, 10 - body, "Maximum Acceleration")
                    Equal(p.BatteryPowerFactors.Value, 30D * body, "Battery PF")
                    Equal(p.DesignPoints, 5D + 5D * body, "Design points")
                    If p.StartingAcceleration <= 0 AndAlso String.IsNullOrWhiteSpace(p.SourceNote) Then Throw New InvalidOperationException("The literal nonpositive formula result needs a source note.")
                End Sub)
            Next
            Test("over-the-horizon retrofit uses ship sensors, p.146", Sub()
                Dim d As New StockDesign With {.ChassisId = "corvette", .PowerPlantId = "diesel", .InstalledRetrofits = Choices(("over-the-horizon-sensors", 1))}
                Dim result = d.Calculate(catalog)
                Equal(result.RetrofitListPartsCostNuyen, 300000D, "OTHS parts")
                Equal(result.Totals.CargoRemaining, 159D, "OTHS cargo")
                Equal(result.Totals.LoadRemainingKg, 17500D, "OTHS mass")
            End Sub)
            Test("Hull-rated engine acceleration customization, pp.114,125", Sub()
                Dim d As New StockDesign With {.ChassisId = "attack-submarine", .PowerPlantId = "diesel", .CatalogModifications = Choices(("engine-customization-acceleration", 1))}
                Dim r = d.Calculate(catalog)
                Equal(r.ModifiedRatings.Acceleration.Value, 5, "Acceleration")
                Equal(r.Totals.DesignPoints, 400000D, "First-level plant cost")
                d.InstalledRetrofits = d.CatalogModifications : d.CatalogModifications = Nothing
                Equal(d.Calculate(catalog).RetrofitListPartsCostNuyen, 11100000D, "Retrofit list parts")
                d.InstalledRetrofits(0).Quantity = 2
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            Test("secondary speed customization preserves the other rating", Sub()
                Dim d As New StockDesign With {.ChassisId = "attack-submarine", .PowerPlantId = "diesel", .CatalogModifications = Choices(("engine-customization-secondary-speed", 1))}
                Equal(d.Calculate(catalog).ModifiedRatings.SecondarySpeed.Value, 55, "One full secondary level")
                If Not VehicleSheet.Render(d, catalog).Contains("30 / 55") Then Throw New InvalidOperationException("Secondary customization is absent from export.")
                d.CatalogModifications(0).Quantity = 2
                Reject(Sub() d.Calculate(catalog))
            End Sub)
            Test("Hull-rated thermal baffles, pp.114,135", Sub()
                Dim d As New StockDesign With {.ChassisId = "attack-submarine", .PowerPlantId = "diesel", .InstalledRetrofits = Choices(("thermal-baffles", 1))}
                Dim r = d.Calculate(catalog)
                Equal(r.RetrofitListPartsCostNuyen, 6250000D, "Hull Factor parts")
                Equal(r.Totals.LoadRemainingKg, 625000D, "Hull Factor baffle mass")
                Equal(r.ModifiedRatings.InfraredSignature.Value, 7, "Thermal Signature")
            End Sub)
            Test("Hull-rated weapon gyro uses the same Body scale as its maximum", Sub()
                Dim d As New StockDesign With {.ChassisId = "attack-submarine", .PowerPlantId = "diesel", .CatalogModifications = Choices(("external-fixed-hardpoint", 1), ("weapon-gyro-stabilization", 4))}
                Equal(d.Calculate(catalog).OperatingModes.Single().Handling.Value, 4, "No gyro penalty below Hull Factor")
            End Sub)
            Test("Hull-rated remote control interface", Sub()
                Dim d As New StockDesign With {.ChassisId = "attack-submarine", .PowerPlantId = "diesel", .CatalogModifications = Choices(("remote-control-interface", 1))}
                Dim r = d.Calculate(catalog)
                Equal(r.ModifiedRatings.Pilot.Value, 1, "Remote Pilot")
                Equal(r.Totals.DesignPoints, 432500D, "Hull Factor interface cost")
            End Sub)
            Test("paired speeds receive turbo and wire effects", Sub()
                Dim d As New StockDesign With {.ChassisId = "attack-submarine", .PowerPlantId = "diesel", .CatalogModifications = Choices(("turbocharging", 1))}
                Dim r = d.Calculate(catalog)
                Equal(r.ModifiedRatings.Speed.Value, 45, "Primary turbo Speed")
                Equal(r.ModifiedRatings.SecondarySpeed.Value, 40, "Secondary turbo Speed")
                d.CatalogModifications = Choices(("drive-by-wire", 1), ("drive-by-wire-speed-allocation", 10))
                r = d.Calculate(catalog)
                Equal(r.ModifiedRatings.Speed.Value, 33, "Primary wire Speed")
                Equal(r.ModifiedRatings.SecondarySpeed.Value, 27, "Secondary wire Speed")
            End Sub)
            Test("Body 0 engine customization remains available", Sub()
                Dim d As New StockDesign With {.ChassisId = "micro-rotary-wing-uav", .PowerPlantId = "electric-battery", .CatalogModifications = Choices(("engine-customization-speed", 1))}
                Equal(d.Calculate(catalog).ModifiedRatings.Speed.Value, 40, "Body 0 customized Speed")
            End Sub)
            Test("sidecar and flotation penalties survive engine changes", Sub()
                Dim d As New StockDesign With {.ChassisId = "chopper", .PowerPlantId = "gasoline", .CatalogModifications = Choices(("sidecar", 2), ("engine-customization-speed", 1))}
                Equal(d.Calculate(catalog).ModifiedRatings.Speed.Value, 75, "Engine plus sidecar")
                d.ChassisId = "airliner" : d.PowerPlantId = "jet-propeller"
                d.CatalogModifications = Choices(("aircraft-flotation", 1), ("engine-customization-speed", 1))
                Equal(d.Calculate(catalog).ModifiedRatings.Speed.Value, 175, "Engine plus floats")
            End Sub)
            Test("wire bonus follows drop-tank penalties, pp.129,149", Sub()
                Dim d As New StockDesign With {.ChassisId = "airliner", .PowerPlantId = "jet-propeller", .CatalogModifications = Choices(("aircraft-drop-tanks", 2), ("drive-by-wire", 1), ("drive-by-wire-speed-allocation", 10))}
                Dim r = d.Calculate(catalog)
                Equal(r.ModifiedRatings.Speed.Value, 335, "(320 − 15) × 1.1, rounded down")
                Equal(r.ModifiedRatings.Signature.Value, 2, "One Signature penalty for drop tanks")
                Equal(r.OperatingModes.Single().Speed.Value, 352, "320 × 1.1 after jettison")
                Equal(r.OperatingModes.Single().Signature.Value, 3, "Normal Signature after jettison")
            End Sub)
            Test("wire Load bonus follows permanent equipment without depending on cargo", Sub()
                Dim d = Van(("bucket-seat", 1), ("drive-by-wire", 1), ("drive-by-wire-load-allocation", 10))
                d.PowerPlantId = "diesel"
                Equal(d.Calculate(catalog).Totals.LoadRemainingKg, 990D, "(1000 − 100) × 1.1")
                d.Payload = New List(Of VehiclePayloadItem) From {New VehiclePayloadItem With {.Name = "Carried tools", .UnitWeightKg = 200D}}
                Equal(d.Calculate(catalog).Totals.LoadRemainingKg, 790D, "Cargo does not change the vehicle's Load improvement")
                d.CatalogModifications = Choices(("bucket-seat", 1))
                d.InstalledRetrofits = Choices(("drive-by-wire", 1), ("drive-by-wire-load-allocation", 10))
                Equal(d.Calculate(catalog).Totals.LoadRemainingKg, 790D, "Retrofit Load improvement")
            End Sub)
        Finally
            Directory.Delete(folder, True)
        End Try
        Console.WriteLine($"Rules audit: {checks} cases; {failures.Count} failed.")
        Console.WriteLine($"Catalog: {catalog.Count} chassis; {catalog.Sum(Function(item) item.PowerPlants.Count)} engines; {ModificationCatalog.Rules.Count} rule entries; {RetrofitCatalog.Rules.Count} retrofit entries.")
        For Each failure In failures
            Console.Error.WriteLine(failure)
        Next
        Environment.ExitCode = If(failures.Count = 0, 0, 1)
    End Sub

    Private Function Van(ParamArray modifications As (Id As String, Quantity As Integer)()) As StockDesign
        Return New StockDesign With {.ChassisId = "van", .PowerPlantId = "gasoline", .CatalogModifications = Choices(modifications)}
    End Function
    Private Function Choices(ParamArray values As (Id As String, Quantity As Integer)()) As List(Of ModificationSelection)
        Return values.Select(Function(value) New ModificationSelection With {.Id = value.Id, .Quantity = value.Quantity}).ToList()
    End Function
    Private Function Plant(d As StockDesign) As StockPowerPlant
        Return StockCatalog.FindById(catalog, d.ChassisId).PowerPlants.Single(Function(p) p.Id = d.PowerPlantId)
    End Function
    Private Function Quote(id As String, quantity As Integer, Optional context As IReadOnlyList(Of ModificationSelection) = Nothing) As RetrofitQuote
        Return RetrofitCatalog.Quote(ModificationCatalog.Rules.Single(Function(r) r.Id = id), quantity, StockCatalog.FindById(catalog, "van"), Plant(Van()), context:=context)
    End Function
    Private Function Weapon(kind As String, mount As String, weight As Decimal, Optional quantity As Integer = 1) As VehiclePayloadItem
        Return New VehiclePayloadItem With {.Name = "Audit " & kind, .WeaponClass = kind, .MountId = mount, .UnitWeightKg = weight, .Quantity = quantity}
    End Function
    Private Sub Test(name As String, action As Action)
        checks += 1
        Try
            action()
        Catch ex As Exception
            failures.Add(name & ": " & ex.GetType().Name & " — " & ex.Message)
        End Try
    End Sub
    Private Sub Equal(Of T)(actual As T, expected As T, name As String)
        If Not EqualityComparer(Of T).Default.Equals(actual, expected) Then Throw New InvalidOperationException($"{name}: expected {expected}; got {actual}.")
    End Sub
    Private Sub Reject(action As Action)
        Try
            action()
        Catch ex As ArgumentException
            Return
        End Try
        Throw New InvalidOperationException("Expected an ArgumentException.")
    End Sub
End Module
