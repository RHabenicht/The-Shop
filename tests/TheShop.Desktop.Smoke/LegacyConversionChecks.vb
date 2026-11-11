Imports System.IO
Imports System.Text.Json
Imports TheShop.Design

Friend Module LegacyConversionChecks
    Public Sub Run(folder As String, catalog As IReadOnlyList(Of StockChassis))
        Dim flags = Convert(folder, catalog, "Van", "diesel", "Rigger2,0,Roll Bars", "Rigger2,0,Rigger Adaptation", "Rigger2,0,Datajack port")
        Check(flags.CatalogModifications.Count = 2 AndAlso flags.CatalogModifications.Any(Function(item) item.Id = "rigger-adaptation") AndAlso
              Not flags.CatalogModifications.Any(Function(item) item.Id = "datajack-port"), "Zero-level saved switches must convert, without a duplicate rigger datajack.")
        Dim performance = Convert(folder, catalog, "Van", "diesel", "Rigger2,3,Speed Increase", "Rigger2,1,Acceleration Increase", "Rigger2,2,Increased Cargo Space", "Rigger2,1,Improved Economy")
        Check(performance.Upgrades.Speed = 3 AndAlso performance.Upgrades.Acceleration = 1 AndAlso performance.Upgrades.CargoSteps = 10 AndAlso
              performance.Upgrades.EconomySteps = 1 AndAlso performance.Calculate(catalog).Totals.CargoRemaining = 50D,
              "Whole CF must become five revised 0.2 CF steps and performance must be recalculated.")
        Dim controls = Convert(folder, catalog, "Van", "diesel", "Rigger2,2,Remote Pilot Advanced Programming", "Rigger2,0,Remote-Control Interfaces", "Rigger2,2,Sensors")
        Check(controls.Calculate(catalog).ModifiedRatings.Pilot.GetValueOrDefault() = 2 AndAlso controls.Calculate(catalog).ModifiedRatings.Sensor.GetValueOrDefault() = 2,
              "Pilot/interface dependencies must convert together even when their source records precede sensors.")
        Dim sealed = Convert(folder, catalog, "Van", "diesel", "Rigger2,21,Life Support Systems", "Rigger2,0,EnviroSeal (gas)")
        Check(sealed.CatalogModifications.Single(Function(item) item.Id = "life-support").Quantity = 3,
              "Person-hours must round up to complete ten-person-hour life support blocks.")
        Dim crane = Convert(folder, catalog, "Van", "diesel", "Rigger2,1,Crane", "Rigger2,1,Winches")
        Check(crane.CatalogModifications.Single(Function(item) item.Id = "crane").Quantity = 25 AndAlso
              crane.CatalogModifications.Single(Function(item) item.Id = "winch").Quantity = 25,
              "Old lifting capacity indices must become kilograms, not ratings of one kilogram.")
        Dim drive = Convert(folder, catalog, "Van", "diesel", "Rigger2,10,Drive-by-Wire: Speed", "Rigger2,1,Structural Agility", "Rigger2,1,Drive-by-Wire")
        Check(drive.CatalogModifications.Count = 3 AndAlso drive.Calculate(catalog).ModifiedRatings.StructuralAgility.GetValueOrDefault() = 1,
              "Drive-by-wire and its allocation must be validated as a complete selection.")
        Dim engine = Convert(folder, catalog, "Van", "diesel", "Rigger2,2,Active Thermal Masking", "Rigger2,2,Engine Customization", "Rigger2,1,Engine Customization: Speed", "Rigger2,1,Engine Customization: Acceleration")
        Dim engineResult = engine.Calculate(catalog)
        Dim engineCost = engineResult.AppliedModifications.Where(Function(item) item.Name.StartsWith("Engine customization", StringComparison.Ordinal)).Sum(Function(item) item.DesignPoints)
        Check(engineResult.AppliedModifications.Single(Function(item) item.Name = "Active thermal masking").DesignPoints = engineCost * 2.25D AndAlso
              engineResult.OperatingModes.Single(Function(item) item.Name = "Thermal masking venting").Signature.GetValueOrDefault() = 2 AndAlso
              engineResult.OperatingModes.Single(Function(item) item.Name = "Active thermal masking level 2").Notes.Contains("50 minutes"),
              "Thermal masking must use revised customization cost and report active and venting modes.")
        Dim turret = Convert(folder, catalog, "Van", "diesel", "Rigger2,0,Turrets: Pop-Up (micro)", "Rigger2,1,""Turrets, Remote (micro)""", "Rigger2,0,Roll Bars")
        Check(turret.CatalogModifications.Any(Function(item) item.Id = "remote-micro-turret-pop-up") AndAlso
              Not turret.CatalogModifications.Any(Function(item) item.Id = "remote-micro-turret"),
              "Turret variants must replace the base turret instead of charging for both.")
        Dim recoil = Convert(folder, catalog, "Van", "diesel", "Rigger2,4,Gunnery Recoil Adjusters (fixed firm)", "Rigger2,2,""Fixed mount, ext Firmpoint""")
        Check(recoil.Additions.Single().DesignPoints = 80D, "Targeted recoil adjusters must retain mount count and revised per-level pricing.")
        Dim arms = Convert(folder, catalog, "Van", "diesel", "Rigger2,1,Mechanical Arm #1", "Rigger2,0,Mechanical Arm #2")
        Check(arms.CatalogModifications.Single(Function(item) item.Id = "mechanical-arm").Quantity = 2 AndAlso arms.Additions.Count = 1 AndAlso
              arms.Additions.Single().Name.Contains("Strength 17"), "Separate arms must retain their different enhancement levels.")
        Dim mixed = Convert(folder, catalog, "Van", "diesel", "Rigger2,999,Bench Seats", "Rigger2,2,Nitrous Oxide Injectors", "Quality,1,Lemon")
        Check(mixed.CatalogModifications.Count = 1 AndAlso mixed.CatalogModifications.Single().Id = "nitrous" AndAlso
              mixed.LegacyImportNotes.Count(Function(note) note.StartsWith("Review", StringComparison.Ordinal)) = 2,
              "Invalid revised equipment and homebrew must be reported without losing independent valid options.")
        Dim ambiguous = Convert(folder, catalog, "Van", "diesel", "Rigger2,2,Medical-Treatment Gear", "Rigger2,8,Drone Rack", "Rigger2,4,Power Amplifiers")
        Check(ambiguous.CatalogModifications Is Nothing AndAlso ambiguous.LegacyImportNotes.Count = 3,
              "Missing source specifications must never be replaced by invented clinic, drone or amplifier ratings.")
        Dim componentReference = JsonSerializer.SerializeToElement(New With {
            .Chassis = New With {.Name = "Commuter/Subcomp."}, .Engine = New With {.Name = "Electric"}})
        Dim matched = LegacyDesignConversion.FindComponents(componentReference, catalog)
        Check(matched.Chassis.Id = "subcompact" AndAlso matched.Plant.Id = "electric-battery", "Old component aliases must resolve to revised components instead of the editor's current selection.")
        Dim destination = Path.Combine(folder, "converted-round-trip.json")
        File.WriteAllText(destination, JsonSerializer.Serialize(engine))
        Dim reopened = JsonSerializer.Deserialize(Of StockDesign)(File.ReadAllText(destination))
        Check(reopened.LegacyReference.Value.GetProperty("Modifications").GetArrayLength() = 4 AndAlso
              reopened.Calculate(catalog).Totals.BaseCostNuyen = engineResult.Totals.BaseCostNuyen,
              "Revised conversions and original evidence must survive save/reopen.")
        Dim sheet = Path.Combine(folder, "converted-sheet.html")
        VehicleSheet.Write(sheet, reopened, catalog)
        Check(File.ReadAllText(sheet).Contains("Active thermal masking level 2") AndAlso File.ReadAllText(sheet).Contains("Repriced using Rigger 3 Revised"),
              "Revised operating modes and conversion results must appear on the exported sheet.")
    End Sub

    Private Function Convert(folder As String, catalog As IReadOnlyList(Of StockChassis), chassis As String, plant As String, ParamArray rows As String()) As StockDesign
        Dim path = IO.Path.Combine(folder, "legacy-conversion-case.shp")
        Dim lines As New List(Of String) From {"Shop v1.40", "Conversion regression", "Rigger2," & chassis, "Rigger2," & plant, "Bench", "Open", "0,0,0,0,0"}
        lines.AddRange(rows)
        File.WriteAllLines(path, lines)
        Dim match = catalog.Single(Function(item) String.Equals(item.Name, chassis, StringComparison.OrdinalIgnoreCase))
        Return LegacyDesignConversion.CreateDesign(LegacyVehicleReader.Read(path), match.Id, plant, catalog)
    End Function

    Private Sub Check(condition As Boolean, message As String)
        If Not condition Then Throw New InvalidOperationException(message)
    End Sub
End Module
