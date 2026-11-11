Imports System.Globalization
Imports System.IO
Imports System.Net
Imports System.Text

Public Module VehicleSheet
    Public Sub Write(destination As String, design As StockDesign, catalog As IReadOnlyList(Of StockChassis))
        Dim contents = Render(design, catalog)
        Dim fullPath = Path.GetFullPath(destination)
        Dim temporary = Path.Combine(Path.GetDirectoryName(fullPath), $".the-shop-{Guid.NewGuid():N}.tmp")
        Try
            File.WriteAllText(temporary, contents, New UTF8Encoding(False))
            File.Move(temporary, fullPath, overwrite:=True)
        Finally
            File.Delete(temporary)
        End Try
    End Sub

    Public Function Render(design As StockDesign, catalog As IReadOnlyList(Of StockChassis)) As String
        Dim result = design.Calculate(catalog)
        Dim chassis = StockCatalog.FindById(catalog, result.ChassisId)
        Dim plant = chassis.PowerPlants.Single(Function(item) item.Id = result.PowerPlantId)
        Dim up = result.UpgradedRatings
        Dim modified = result.ModifiedRatings
        Dim totals = result.Totals
        Dim name = If(String.IsNullOrWhiteSpace(design.Name), chassis.Name, design.Name)
        Dim page As New StringBuilder()
        page.Append("<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'>")
        page.Append("<title>").Append(E(name)).Append(" — The Shop</title><style>")
        page.Append("body{font:15px/1.45 system-ui,sans-serif;color:#17202b;background:#edf0f3;margin:0}main{max-width:960px;margin:28px auto;padding:32px;background:white}h1{font-size:30px;margin:0}h2{font-size:18px;border-bottom:2px solid #23384c;padding-bottom:6px;margin-top:28px}.subtitle{color:#4d5d6c;margin:4px 0 22px}table{border-collapse:collapse;width:100%;margin:10px 0}th,td{text-align:left;padding:8px;border-bottom:1px solid #d7dfe5;vertical-align:top}th{background:#f0f3f6;font-weight:600}tr{break-inside:avoid}td:first-child{width:40%}.footer{font-size:12px;color:#536170;margin-top:24px}@media print{body{background:white}main{margin:0;padding:0;max-width:none}h2{break-after:avoid}table{font-size:11px}th,td{padding:5px}@page{margin:15mm}}")
        page.Append("</style></head><body><main><h1>").Append(E(name)).Append("</h1><p class='subtitle'>")
        page.Append(E(chassis.Category)).Append(" · ").Append(E(chassis.Name)).Append(" · ").Append(E(plant.Name))
        page.Append(" · Shadowrun Third Edition / Rigger 3 Revised</p><h2>Vehicle ratings</h2><table><tbody>")

        If chassis.Hull.HasValue Then
            Row(page, "Hull / Bulwark", Pair(chassis.Hull, If(modified?.Bulwark, chassis.Bulwark)))
        Else
            Row(page, "Body / Armor", Pair(chassis.Body, If(modified?.Armor, chassis.Armor)))
        End If
        Dim handling = If(modified?.Handling, If(up?.Handling, chassis.Handling))
        If handling.HasValue Then
            Row(page, "Handling", Number(handling))
        Else
            Row(page, "Handling (on-road / off-road)", Pair(If(modified?.OnRoadHandling, If(up?.OnRoadHandling, chassis.OnRoadHandling)),
                                                           If(modified?.OffRoadHandling, If(up?.OffRoadHandling, chassis.OffRoadHandling))))
        End If
        Dim speed = If(modified?.Speed, If(up?.Speed, plant.StartingSpeed))
        Dim speedText = Number(speed)
        If plant.StartingStallSpeed.HasValue Then speedText = Number(plant.StartingStallSpeed) & " / " & speedText & " (stall / flight)"
        If plant.StartingSecondarySpeed.HasValue Then speedText &= " / " & Number(If(up?.SecondarySpeed, plant.StartingSecondarySpeed)) & " (paired operating ratings)"
        Row(page, "Speed", speedText)
        If modified?.WaterSpeed.HasValue Then Row(page, "Water Speed rating", Number(modified.WaterSpeed))
        If modified?.WaterSpeedMetersPerTurn.HasValue Then Row(page, "Water speed", Number(modified.WaterSpeedMetersPerTurn) & " m/turn")
        If modified?.WaterHandlingModifier.HasValue Then Row(page, "Water Handling modifier", "+" & Number(modified.WaterHandlingModifier))
        Row(page, "Acceleration", Number(If(modified?.Acceleration, If(up?.Acceleration, plant.StartingAcceleration))))
        Row(page, "Signature / Sonar Signature", Pair(If(modified?.Signature, If(up?.Signature, plant.Signature)), If(modified?.SonarSignature, plant.SonarSignature)))
        If modified?.DippingSonar.HasValue Then Row(page, "Dipping sonar", Number(modified.DippingSonar))
        If modified?.ECM.HasValue Then Row(page, "ECM", Number(modified.ECM))
        If modified?.ECCM.HasValue Then Row(page, "ECCM", Number(modified.ECCM))
        If modified?.ElectronicDeception.HasValue Then Row(page, "Electronic deception", Number(modified.ElectronicDeception))
        If modified?.ElectronicCounterDeception.HasValue Then Row(page, "Electronic counter-deception", Number(modified.ElectronicCounterDeception))
        If modified?.RadarSignature.HasValue Then Row(page, "Radar Signature", Number(modified.RadarSignature))
        If modified?.InfraredSignature.HasValue Then Row(page, "Infrared Signature", Number(modified.InfraredSignature))
        Row(page, "Sensor / Sonar", Pair(If(modified?.Sensor, chassis.Sensor), If(modified?.Sonar, chassis.Sonar)))
        If result.AuxiliaryEngine IsNot Nothing Then
            Dim engine = result.AuxiliaryEngine
            Row(page, "Auxiliary engine", engine.Name)
            Row(page, "Auxiliary Speed / Acceleration", Pair(engine.Speed, engine.Acceleration))
            If engine.SecondarySpeed.HasValue Then Row(page, "Auxiliary secondary Speed", Number(engine.SecondarySpeed))
            Row(page, "Auxiliary maximum Speed / Acceleration", Pair(engine.MaximumSpeed, engine.MaximumAcceleration))
            If engine.Signature.HasValue Then Row(page, "Auxiliary Signature", Number(engine.Signature))
            If engine.Economy.HasValue Then Row(page, "Auxiliary Economy", Number(engine.Economy) & " " & engine.EconomyUnit)
            If engine.FuelCapacity.HasValue Then Row(page, "Auxiliary fuel", Number(engine.FuelCapacity) & " " & engine.FuelUnit)
        End If
        If modified?.RoboticPilot.HasValue Then Row(page, "Robotic Pilot", Number(modified.RoboticPilot))
        If modified?.AdaptationPool.HasValue Then Row(page, "Adaptation Pool", Number(modified.AdaptationPool))
        If modified?.RoboticReflexes.HasValue Then Row(page, "Robotic reflexes", Number(modified.RoboticReflexes))
        Row(page, "Autonav / Pilot", Pair(If(modified?.Autonav, chassis.Autonav), If(modified?.Pilot, chassis.Pilot)))
        If result.ExternalCargoCapacity > 0D Then Row(page, "External cargo capacity (CF)", Number(CType(result.ExternalCargoCapacity, Decimal?)))
        Row(page, "Available cargo (CF)", Number(totals.CargoRemaining))
        Row(page, "Available load (kg)", Number(totals.LoadRemainingKg))
        If chassis.StartingDepth.HasValue Then Row(page, "Depth (m)", Number(If(up?.Depth, chassis.StartingDepth)))
        If modified?.TowedArraySonar.HasValue Then Row(page, "Towed-array sonar", Number(modified.TowedArraySonar))
        If result.OperatingModes IsNot Nothing Then
            For Each mode In result.OperatingModes
                Row(page, mode.Name, $"Speed {Number(mode.Speed)}; Accel {Number(mode.Acceleration)}; Handling {Number(mode.Handling)}; Signature {Number(mode.Signature)}; Sonar Signature {Number(mode.SonarSignature)}")
                If mode.Notes IsNot Nothing Then Row(page, mode.Name & " notes", mode.Notes)
            Next
        End If
        Dim fuel = If(modified?.FuelCapacity, If(up?.FuelCapacity, If(plant.FuelLiters, If(plant.FuelBars, If(plant.BatteryPowerFactors, If(plant.FuelCellPowerFactors, plant.RocketPowerFactors))))))
        Dim fuelUnit = If(modified?.FuelUnit, If(up?.FuelUnit, If(plant.FuelLiters.HasValue, "L", If(plant.FuelBars.HasValue, "bars", "PF"))))
        If fuel.HasValue Then Row(page, "Fuel capacity", Number(fuel) & " " & fuelUnit)
        If up?.AdditionalFuelCapacity.HasValue Then Row(page, "Additional fuel storage", $"{Number(up.AdditionalFuelCapacity)} {up.AdditionalFuelUnit}; source tables use different units, so capacities are retained separately without an assumed conversion.")
        Dim economy = If(modified?.Economy, If(up?.Economy, If(plant.StartingEconomyKmPerLiter, If(plant.StartingEconomyKmPerPowerFactor, plant.StartingEconomyKmPerBar))))
        If economy.HasValue Then Row(page, "Economy", Number(economy) & " " & plant.EconomyUnit)
        If If(modified?.TakeoffLandingProfile, chassis.TakeoffLandingProfile) IsNot Nothing Then Row(page, "Takeoff / landing", If(modified?.TakeoffLandingProfile, chassis.TakeoffLandingProfile))
        If modified?.SetupBreakdownMinutes.HasValue Then Row(page, "Setup/breakdown time (minutes)", Number(modified.SetupBreakdownMinutes))
        If modified?.StructuralAgility.HasValue Then Row(page, "Structural Agility", Number(modified.StructuralAgility))
        If modified?.PersonalArmor.HasValue Then Row(page, "Personal ballistic armor", Number(modified.PersonalArmor))
        If modified?.AblativeArmor.HasValue Then Row(page, "Ablative armor bonus (not hardened)", Number(modified.AblativeArmor))
        If up?.TakeoffLandingDistanceMultiplier.HasValue Then Row(page, "Takeoff / landing distance multiplier", Number(up.TakeoffLandingDistanceMultiplier))
        page.Append("</tbody></table><h2>Cost and availability</h2><table><tbody>")
        Row(page, "Design Points", Number(totals.DesignPoints))
        Row(page, "Effective chassis markup", Number(totals.EffectiveMarkup))
        Row(page, "Base price", "¥" & Number(totals.BaseCostNuyen))
        If design.EngineStress.HasValue Then Row(page, "Permanent engine Stress (GM result)", Number(design.EngineStress))
        Row(page, "Street Index", Number(result.EffectiveStreetIndex))
        If design.SpecialtyClass <> VehicleSpecialtyClass.None Then Row(page, "Specialty class", design.SpecialtyClass.ToString())
        Row(page, "Street price", "¥" & Number(totals.StreetCostNuyen))
        If result.EffectiveAvailabilityClass.HasValue Then Row(page, "Effective vehicle class", result.EffectiveAvailabilityClass.ToString())
        If result.Availability IsNot Nothing Then
            Row(page, "Availability", $"{Number(result.Availability.Rating)} / {Number(result.Availability.BaseDays)} days")
        End If
        page.Append("</tbody></table><h2>Options and modifications</h2><table><thead><tr><th>Item</th><th>Selection</th></tr></thead><tbody>")
        If design.Upgrades IsNot Nothing Then
            Dim selections As New Dictionary(Of String, Integer) From {
                {"Speed increase", design.Upgrades.Speed}, {"Secondary speed increase", design.Upgrades.SecondarySpeed}, {"Acceleration increase", design.Upgrades.Acceleration},
                {"Handling improvement", design.Upgrades.Handling}, {"On-road Handling improvement", design.Upgrades.OnRoadHandling},
                {"Off-road Handling improvement", design.Upgrades.OffRoadHandling}, {"Cargo increase (0.2 CF steps)", design.Upgrades.CargoSteps},
                {"Load increase (steps)", design.Upgrades.LoadSteps}, {"Signature increase", design.Upgrades.Signature},
                {"Economy increase (5% steps)", design.Upgrades.EconomySteps}, {"Fuel increase (steps)", design.Upgrades.FuelSteps},
                {"Depth increase (10 m steps)", design.Upgrades.DepthSteps}
            }
            For Each item In selections.Where(Function(entry) entry.Value > 0)
                Row(page, item.Key, item.Value.ToString(CultureInfo.InvariantCulture))
            Next
        End If
        If design.CatalogModifications IsNot Nothing Then
            For Each item In design.CatalogModifications
                Dim rule = ModificationCatalog.Rules.Single(Function(candidate) String.Equals(candidate.Id, item.Id, StringComparison.OrdinalIgnoreCase))
                Row(page, rule.Name, item.Quantity.ToString(CultureInfo.InvariantCulture) & " " & rule.QuantityLabel)
            Next
        End If
        If result.InstalledRetrofits IsNot Nothing Then
            For Each item In result.InstalledRetrofits
                Row(page, item.Name & " (installed retrofit)",
                    $"{item.Quantity}; list parts ¥{Number(item.Quote.PartsCostNuyen)}; " &
                    If(item.Quote.StreetPartsCostNuyen.HasValue, "street parts ¥" & Number(item.Quote.StreetPartsCostNuyen), "street price pending GM review (unlisted Street Index)") &
                    $"; {item.Quote.CargoConsumed} CF; {item.Quote.LoadConsumedKg} kg; Availability {item.Quote.Availability}")
                Row(page, item.Name & " installation", $"Rigger 3 Revised p. {item.Quote.SourcePage}; {item.Quote.EquipmentRequired}; {item.Quote.InstallationTest}" &
                    If(item.Quote.InstallationBaseHours.HasValue, $"; base {item.Quote.InstallationBaseHours:0.##} hours before Installation Test successes", ""))
            Next
            Row(page, "Installed retrofit list parts total", "¥" & Number(result.RetrofitListPartsCostNuyen))
            Row(page, "Installed retrofit street parts total", If(result.RetrofitPartsCostNuyen.HasValue,
                "¥" & Number(result.RetrofitPartsCostNuyen), "Pending GM review (unlisted Street Index)"))
        End If
        For Each mount In RetrofitCatalog.CombinedSelections(design.CatalogModifications, design.InstalledRetrofits).Where(Function(item) item.Id.Contains("turret"))
            Row(page, mount.Id & " internal space", $"Per turret: {VehiclePayload.TurretStorage(mount.Id)} CF; {VehiclePayload.TurretSeating(mount.Id)} seating positions; {VehiclePayload.Arc(mount.Id)}")
        Next
        If design.Payload IsNot Nothing Then
            For Each item In design.Payload
                Row(page, item.Name & " (payload)", $"{item.Quantity}; {item.UnitWeightKg * item.Quantity} kg; {item.UnitCargoCf * item.Quantity} CF; list ¥{item.UnitCostNuyen * item.Quantity:N2}")
                If Not String.IsNullOrWhiteSpace(item.WeaponClass) Then Row(page, $"{item.MountId} #{item.MountNumber}", $"{item.WeaponClass}; {item.Direction}; {VehiclePayload.Arc(item.MountId)}")
            Next
        End If
        If design.EquipmentSettings IsNot Nothing Then
            For Each setting In design.EquipmentSettings
                Row(page, setting.Component & " (individual)", $"{setting.Target} #{setting.Instance}; rating {setting.Rating}")
            Next
        End If
        If result.ConfiguredRetrofitListCost > 0D Then Row(page, "Individual equipment retrofit parts", $"List ¥{result.ConfiguredRetrofitListCost:N2}; street ¥{result.ConfiguredRetrofitStreetCost:N2}")
        For Each item In design.Additions
            Row(page, item.Name, $"{Number(item.DesignPoints)} DP; ¥{Number(item.DirectCostNuyen)} direct; {Number(item.CargoConsumed)} CF; {Number(item.LoadConsumedKg)} kg")
        Next
        page.Append("</tbody></table>")
        If design.LegacyImportNotes IsNot Nothing Then
            page.Append("<h2>Legacy import notes</h2><ul>")
            For Each note In design.LegacyImportNotes
                page.Append("<li>").Append(E(note)).Append("</li>")
            Next
            page.Append("</ul>")
        End If
        If design.LegacyReference.HasValue Then
            page.Append("<h2>Imported legacy reference</h2><pre style='white-space:pre-wrap;overflow-wrap:anywhere'>")
            page.Append(E(System.Text.Json.JsonSerializer.Serialize(design.LegacyReference.Value, New System.Text.Json.JsonSerializerOptions With {.WriteIndented = True})))
            page.Append("</pre>")
        End If
        If design.CatalogModifications IsNot Nothing AndAlso design.CatalogModifications.Any(Function(item) item.Id.StartsWith("engine-customization-", StringComparison.OrdinalIgnoreCase)) Then
            page.Append("<p>Manufactured engine customization: GM secretly rolls 6 dice against target 6. On failure, roll 1D6, divide by 2 and round up for permanent engine Stress (Rigger 3 Revised p. 125).</p>")
        End If
        If design.InstalledRetrofits IsNot Nothing AndAlso design.InstalledRetrofits.Any(Function(item) item.Id.StartsWith("engine-customization-", StringComparison.Ordinal)) Then
            page.Append("<p>Customized engine: GM secretly makes the appropriate Build/Repair Test at twice the installation target. Failure adds 1D6 divided by 2, rounded up, permanent engine Stress (Rigger 3 Revised p. 125).</p>")
        End If
        page.Append("<h2>GM review</h2><p>Proposed vehicle. Final approval, price, Street Index, Availability and any changes are determined by the gamemaster (Rigger 3 Revised, pp. 113, 121).</p>")
        page.Append("<p class='footer'>The Shop · Calculated from the current design. Manual entries are included in the totals.</p></main></body></html>")
        Return page.ToString()
    End Function

    Private Sub Row(page As StringBuilder, label As String, value As String)
        page.Append("<tr><td>").Append(E(label)).Append("</td><td>").Append(E(value)).Append("</td></tr>")
    End Sub

    Private Function Pair(first As Integer?, second As Integer?) As String
        Return Number(first) & " / " & Number(second)
    End Function

    Private Function Number(value As Integer?) As String
        Return If(value.HasValue, value.GetValueOrDefault().ToString("N0", CultureInfo.InvariantCulture), "—")
    End Function

    Private Function Number(value As Decimal?) As String
        Return If(value.HasValue, value.GetValueOrDefault().ToString("#,0.##", CultureInfo.InvariantCulture), "—")
    End Function

    Private Function E(value As String) As String
        Return WebUtility.HtmlEncode(value)
    End Function
End Module
