Imports System.IO
Imports System.Text.Json
Imports System.Windows.Forms
Imports TheShop.Design

Friend Module CompletionChecks
    Public Sub Run(folder As String, catalog As IReadOnlyList(Of StockChassis))
        Dim van = StockCatalog.FindById(catalog, "van")
        Dim diesel = van.PowerPlants.Single(Function(item) item.Id = "diesel")
        Dim baseline = MakeVan().Calculate(catalog)
        Dim armor As New StockDesign With {.ChassisId = "van", .PowerPlantId = "diesel", .CatalogModifications = Choices(("personal-armor", 2))}
        Dim armorResult = armor.Calculate(catalog)
        Check(armorResult.ModifiedRatings.PersonalArmor.GetValueOrDefault() = 2 AndAlso armorResult.Totals.LoadRemainingKg = 984D, "Personal armor must use its own ballistic rating and design weight.")
        armor.CatalogModifications = Nothing
        armor.InstalledRetrofits = Choices(("personal-armor", 2))
        Dim retrofitArmor = armor.Calculate(catalog)
        Check(retrofitArmor.Totals.LoadRemainingKg = 976D AndAlso retrofitArmor.RetrofitListPartsCostNuyen = 800D, "Personal armor retrofits must use the customization weight and price.")
        armor.InstalledRetrofits = Choices(("ablative-armor", 1))
        Dim ablative = armor.Calculate(catalog)
        Check(ablative.ModifiedRatings.AblativeArmor.GetValueOrDefault() = 2 AndAlso ablative.RetrofitListPartsCostNuyen = 700D AndAlso ablative.Totals.LoadRemainingKg = 600D, "Ablative armor must remain separate from hardened armor.")
        armor.CatalogModifications = armor.InstalledRetrofits
        armor.InstalledRetrofits = Nothing
        MustReject(Sub() armor.Calculate(catalog), "Ablative armor must be unavailable during manufacture.")
        Dim hull As New StockDesign With {.ChassisId = "attack-submarine", .PowerPlantId = "diesel", .Upgrades = New DesignUpgrades With {.LoadSteps = 2}}
        Dim unmodifiedHull As New StockDesign With {.ChassisId = "attack-submarine", .PowerPlantId = "diesel"}
        Check(hull.Calculate(catalog).Totals.LoadRemainingKg = unmodifiedHull.Calculate(catalog).Totals.LoadRemainingKg + 20D, "Hull-rated load must use the large-vehicle increment.")
        Dim sealed = MakeVan()
        sealed.CatalogModifications = Choices(("cabin-water-seal", 1), ("engine-water-seal", 1))
        sealed.Upgrades = New DesignUpgrades With {.DepthSteps = 1}
        Check(sealed.Calculate(catalog).UpgradedRatings.Depth.GetValueOrDefault() = 150, "Sealed ground vehicles must receive their printed starting depth and upgrades.")
        Dim mixedFuel = catalog.SelectMany(Function(chassis) chassis.PowerPlants.Select(Function(plant) (Chassis:=chassis, Plant:=plant))).First(Function(pair) pair.Plant.Id = "electric-fuel-cell" AndAlso pair.Plant.FuelLiters.HasValue)
        Dim fuel As New StockDesign With {.ChassisId = mixedFuel.Chassis.Id, .PowerPlantId = mixedFuel.Plant.Id, .Upgrades = New DesignUpgrades With {.FuelSteps = 1}}
        Dim fuelResult = fuel.Calculate(catalog)
        Check(fuelResult.UpgradedRatings.AdditionalFuelCapacity.GetValueOrDefault() = 5D AndAlso Not fuelResult.UpgradedRatings.FuelCapacity.HasValue, "Different printed fuel units must remain separate without an assumed conversion.")
        Dim mounted = MakeVan()
        mounted.CatalogModifications = Choices(("external-fixed-firmpoint", 1))
        mounted.Payload = New List(Of VehiclePayloadItem) From {New VehiclePayloadItem With {.Name = "Test LMG", .WeaponClass = "LMG", .UnitWeightKg = 8D, .UnitCostNuyen = 1000D, .MountId = "external-fixed-firmpoint"}}
        Dim weaponResult = mounted.Calculate(catalog)
        Check(weaponResult.Totals.LoadRemainingKg = 982D, "Carried weapons must consume their actual payload weight in addition to mount weight.")
        mounted.Payload(0).Quantity = 2
        MustReject(Sub() mounted.Calculate(catalog), "Multiple weapons cannot exceed one firmpoint's capacity.")
        mounted.Payload(0).Quantity = 1
        mounted.EquipmentSettings = New List(Of EquipmentSetting) From {New EquipmentSetting With {.Component = "Recoil", .Target = "external-fixed-firmpoint", .Rating = 4}}
        Check(mounted.Calculate(catalog).Totals.DesignPoints = weaponResult.Totals.DesignPoints + 40D, "Individual recoil must price only its selected mount.")
        Dim armed = MakeVan()
        armed.CatalogModifications = Choices(("roll-bar", 1), ("remote-micro-turret-pop-up", 1))
        Check(armed.Calculate(catalog).Totals.CargoRemaining = 47D, "A pop-up micro-turret must consume the printed one CF.")
        armed.Payload = New List(Of VehiclePayloadItem) From {New VehiclePayloadItem With {.Name = "Oversized LMG", .WeaponClass = "LMG", .UnitWeightKg = 8D, .MountId = "remote-micro-turret-pop-up"}}
        MustReject(Sub() armed.Calculate(catalog), "A micro-turret cannot exceed Weapon Value 1.")
        Dim arms = MakeVan()
        arms.CatalogModifications = Choices(("mechanical-arm", 2))
        arms.EquipmentSettings = New List(Of EquipmentSetting) From {New EquipmentSetting With {.Component = "Arm strength", .Rating = 1}, New EquipmentSetting With {.Component = "Arm strength", .Instance = 2, .Rating = 4}}
        Check(arms.Calculate(catalog).Totals.LoadRemainingKg = 630D, "Different arm enhancements must retain individual weight increases.")
        Dim turretStorage = MakeVan()
        turretStorage.CatalogModifications = Choices(("remote-small-turret", 1), ("roll-bar", 1))
        turretStorage.Payload = New List(Of VehiclePayloadItem) From {New VehiclePayloadItem With {.Name = "Turret supplies", .UnitCargoCf = 1D, .UnitWeightKg = 5D, .MountId = "remote-small-turret"}}
        Check(turretStorage.Calculate(catalog).Totals.CargoRemaining = 44D, "Turret internal storage must not be charged a second time against vehicle cargo.")
        turretStorage.Payload(0).Quantity = 3
        MustReject(Sub() turretStorage.Calculate(catalog), "Turret storage must not exceed the published internal capacity.")
        Dim naval As New StockDesign With {.ChassisId = "attack-submarine", .PowerPlantId = "diesel", .CatalogModifications = Choices(("remote-large-turret", 1)),
            .Payload = New List(Of VehiclePayloadItem) From {New VehiclePayloadItem With {.Name = "Railgun fixture", .WeaponClass = "Heavy Railgun", .UnitWeightKg = 10D, .MountId = "remote-large-turret"}}}
        MustReject(Sub() naval.Calculate(catalog), "The printed Heavy Railgun Weapon Value 10 must exceed a large turret's capacity of 8.")
        Dim amplifier = MakeVan()
        amplifier.EquipmentSettings = New List(Of EquipmentSetting) From {New EquipmentSetting With {.Component = "Amplifier", .Target = "Radio", .Rating = 3, .Retrofit = True}}
        Dim amplified = amplifier.Calculate(catalog)
        Check(amplified.RetrofitListPartsCostNuyen = 750D AndAlso amplified.RetrofitPartsCostNuyen.GetValueOrDefault() = 1125D AndAlso amplified.Totals.CargoRemaining = 46D,
              "Individual retrofit amplifiers must use the customization rounding and parts Street Index.")
        Dim quote = RetrofitCatalog.Quote(ModificationCatalog.Rules.Single(Function(rule) rule.Id = "improved-suspension"), 1, van, diesel, listCost:=20000D)
        Check(quote.PartsCostNuyen = 2000D AndAlso quote.StreetPartsCostNuyen.GetValueOrDefault() = 4000D, "Percentage retrofit prices must use list cost before Street Index.")
        MustReject(Sub() RetrofitCatalog.Quote(ModificationCatalog.Rules.Single(Function(rule) rule.Id = "nitrous"), 6, van, diesel), "Customization nitrous is limited to 5 even though manufacture allows 6.")
        Dim mountedRetro = MakeVan()
        mountedRetro.InstalledRetrofits = Choices(("mechanical-arm", 1))
        mountedRetro.EquipmentSettings = New List(Of EquipmentSetting) From {New EquipmentSetting With {.Component = "Arm strength", .Rating = 1}}
        Dim retroResult = mountedRetro.Calculate(catalog)
        Check(retroResult.ConfiguredRetrofitListCost = 20000D AndAlso retroResult.RetrofitListPartsCostNuyen = 60000D AndAlso retroResult.Totals.BaseCostNuyen = baseline.Totals.BaseCostNuyen, "Individual installed equipment must enter parts totals without inflating manufacture price.")
        Dim quotedCount = 0
        For Each chassis In catalog
            For Each plant In chassis.PowerPlants
                Dim stock As New StockDesign With {.ChassisId = chassis.Id, .PowerPlantId = plant.Id}
                stock.Calculate(catalog)
                For Each rule In RetrofitCatalog.AvailableRules(chassis, plant)
                    Dim quantity = If(rule.Id = "improved-anti-theft-security", 3, If(rule.Id = "advanced-drone-pilot", 2, 1))
                    RetrofitCatalog.Quote(rule, quantity, chassis, plant)
                    quotedCount += 1
                Next
            Next
        Next
        Console.WriteLine($"Completion coverage: {catalog.Count} chassis; {catalog.Sum(Function(item) item.PowerPlants.Count)} power plant combinations; {quotedCount} compatible retrofit quotes.")
        Check(quotedCount > 10000, "Every compatible catalog retrofit should have an executable quote.")
        mounted.EngineStress = 2
        Dim path = IO.Path.Combine(folder, "completion-design.json")
        File.WriteAllText(path, JsonSerializer.Serialize(mounted))
        Dim reopened = JsonSerializer.Deserialize(Of StockDesign)(File.ReadAllText(path))
        Check(reopened.EngineStress.GetValueOrDefault() = 2 AndAlso reopened.Calculate(catalog).Totals.LoadRemainingKg = mounted.Calculate(catalog).Totals.LoadRemainingKg, "Payload and individual settings must survive save/reopen.")
        Dim html = IO.Path.Combine(folder, "completion-sheet.html")
        VehicleSheet.Write(html, reopened, catalog)
        Check(File.ReadAllText(html).Contains("Test LMG") AndAlso File.ReadAllText(html).Contains("Recoil (individual)"), "The exported sheet must include payload, mounting and equipment settings.")
        Using dialog As New PayloadDialog(mounted.Payload, mounted.EquipmentSettings)
            dialog.Show()
            Application.DoEvents()
            Check(dialog.Payload.Single().Name = "Test LMG" AndAlso dialog.Settings.Single().Rating = 4, "The payload editor must bind existing entries without discarding configuration.")
            dialog.Close()
        End Using
    End Sub
    Private Function MakeVan() As StockDesign
        Return New StockDesign With {.ChassisId = "van", .PowerPlantId = "diesel"}
    End Function
    Private Function Choices(ParamArray values As (Id As String, Quantity As Integer)()) As List(Of ModificationSelection)
        Return values.Select(Function(value) New ModificationSelection With {.Id = value.Id, .Quantity = value.Quantity}).ToList()
    End Function
    Private Sub Check(condition As Boolean, message As String)
        If Not condition Then Throw New InvalidOperationException(message)
    End Sub
    Private Sub MustReject(action As Action, message As String)
        Try
            action()
        Catch ex As ArgumentException
            Return
        End Try
        Throw New InvalidOperationException(message)
    End Sub
End Module
