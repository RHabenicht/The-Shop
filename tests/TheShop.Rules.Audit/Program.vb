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
        Finally
            Directory.Delete(folder, True)
        End Try
        Console.WriteLine($"Rules audit: {checks} cases; {failures.Count} failed.")
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
