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
        If plant.StartingSecondarySpeed.HasValue Then speedText &= " / " & Number(plant.StartingSecondarySpeed) & " (paired operating ratings)"
        Row(page, "Speed", speedText)
        If modified?.WaterSpeed.HasValue Then Row(page, "Water Speed rating", Number(modified.WaterSpeed))
        If modified?.WaterSpeedMetersPerTurn.HasValue Then Row(page, "Water speed", Number(modified.WaterSpeedMetersPerTurn) & " m/turn")
        If modified?.WaterHandlingModifier.HasValue Then Row(page, "Water Handling modifier", "+" & Number(modified.WaterHandlingModifier))
        Row(page, "Acceleration", Number(If(up?.Acceleration, plant.StartingAcceleration)))
        Row(page, "Signature / Sonar Signature", Pair(If(up?.Signature, plant.Signature), plant.SonarSignature))
        Row(page, "Sensor / Sonar", Pair(If(modified?.Sensor, chassis.Sensor), If(modified?.Sonar, chassis.Sonar)))
        Row(page, "Autonav / Pilot", Pair(If(modified?.Autonav, chassis.Autonav), If(modified?.Pilot, chassis.Pilot)))
        Row(page, "Available cargo (CF)", Number(totals.CargoRemaining))
        Row(page, "Available load (kg)", Number(totals.LoadRemainingKg))
        If chassis.StartingDepth.HasValue Then Row(page, "Depth (m)", Number(If(up?.Depth, chassis.StartingDepth)))
        If modified?.TowedArraySonar.HasValue Then Row(page, "Towed-array sonar", Number(modified.TowedArraySonar))
        Dim fuel = If(up?.FuelCapacity, If(plant.FuelLiters, If(plant.FuelBars, If(plant.BatteryPowerFactors, If(plant.FuelCellPowerFactors, plant.RocketPowerFactors)))))
        Dim fuelUnit = If(up?.FuelUnit, If(plant.FuelLiters.HasValue, "L", If(plant.FuelBars.HasValue, "bars", "PF")))
        If fuel.HasValue Then Row(page, "Fuel capacity", Number(fuel) & " " & fuelUnit)
        Dim economy = If(modified?.Economy, If(up?.Economy, If(plant.StartingEconomyKmPerLiter, If(plant.StartingEconomyKmPerPowerFactor, plant.StartingEconomyKmPerBar))))
        If economy.HasValue Then Row(page, "Economy", Number(economy) & " " & plant.EconomyUnit)
        If chassis.TakeoffLandingProfile IsNot Nothing Then Row(page, "Takeoff / landing", chassis.TakeoffLandingProfile)
        If up?.TakeoffLandingDistanceMultiplier.HasValue Then Row(page, "Takeoff / landing distance multiplier", Number(up.TakeoffLandingDistanceMultiplier))
        page.Append("</tbody></table><h2>Cost and availability</h2><table><tbody>")
        Row(page, "Design Points", Number(totals.DesignPoints))
        Row(page, "Effective chassis markup", Number(totals.EffectiveMarkup))
        Row(page, "Base price", "¥" & Number(totals.BaseCostNuyen))
        Row(page, "Street Index", Number(design.StreetIndex))
        Row(page, "Street price", "¥" & Number(totals.StreetCostNuyen))
        If result.Availability IsNot Nothing Then
            Row(page, "Availability", $"{Number(result.Availability.Rating)} / {Number(result.Availability.BaseDays)} days")
        End If
        page.Append("</tbody></table><h2>Options and modifications</h2><table><thead><tr><th>Item</th><th>Selection</th></tr></thead><tbody>")
        If design.Upgrades IsNot Nothing Then
            Dim selections As New Dictionary(Of String, Integer) From {
                {"Speed increase", design.Upgrades.Speed}, {"Acceleration increase", design.Upgrades.Acceleration},
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
        For Each item In design.Additions
            Row(page, item.Name, $"{Number(item.DesignPoints)} DP; ¥{Number(item.DirectCostNuyen)} direct; {Number(item.CargoConsumed)} CF; {Number(item.LoadConsumedKg)} kg")
        Next
        page.Append("</tbody></table>")
        If design.LegacyReference.HasValue Then
            page.Append("<h2>Imported legacy reference</h2><pre style='white-space:pre-wrap;overflow-wrap:anywhere'>")
            page.Append(E(System.Text.Json.JsonSerializer.Serialize(design.LegacyReference.Value, New System.Text.Json.JsonSerializerOptions With {.WriteIndented = True})))
            page.Append("</pre>")
        End If
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
