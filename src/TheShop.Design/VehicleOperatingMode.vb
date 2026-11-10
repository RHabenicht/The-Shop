Public NotInheritable Class VehicleOperatingMode
    Public Property Name As String
    Public Property Speed As Integer?
    Public Property Acceleration As Integer?
    Public Property Handling As Integer?
    Public Property Signature As Integer?
    Public Property SonarSignature As Integer?
    Public Property Notes As String
End Class

Public Module VehicleOperatingModes
    Public Function Build(selections As IReadOnlyList(Of ModificationSelection), plant As StockPowerPlant, chassis As StockChassis,
                          upgrades As UpgradedRatings, modified As ModifiedRatings, Optional externalCargo As Decimal = 0D) As IReadOnlyList(Of VehicleOperatingMode)
        Dim modes As New List(Of VehicleOperatingMode)
        Dim speed = If(modified?.Speed, If(upgrades?.Speed, plant.StartingSpeed))
        Dim accel = If(modified?.Acceleration, If(upgrades?.Acceleration, plant.StartingAcceleration))
        Dim signature = If(modified?.Signature, If(upgrades?.Signature, plant.Signature))
        Dim sonarSignature = If(modified?.SonarSignature, plant.SonarSignature)
        If DesignEnhancements.Has(selections, "hydrofoil") Then
            modes.Add(New VehicleOperatingMode With {
                .Name = "Hydrofoil engaged", .Speed = Scale(speed, 1.25D), .Acceleration = Scale(accel, 1.25D),
                .Handling = If(modified?.Handling, If(upgrades?.Handling, chassis.Handling)) + 2,
                .Signature = signature - 1
            })
        End If
        If DesignEnhancements.Has(selections, "ducted-waterjet") Then
            modes.Add(New VehicleOperatingMode With {
                .Name = "Ducted waterjet engaged", .Speed = 20, .SonarSignature = sonarSignature + 2,
                .Notes = "Cavitation benefits depend on the operating speed."
            })
        End If
        If DesignEnhancements.Has(selections, "emd-drive") Then
            modes.Add(New VehicleOperatingMode With {
                .Name = "Electromagnetic ducted waterjet engaged", .Speed = speed, .SonarSignature = sonarSignature + 2,
                .Notes = "No cavitation. Normal Signature is reduced by 3 against magnetic-anomaly detection."
            })
        End If
        If DesignEnhancements.Has(selections, "weapon-gyro-stabilization") Then
            Dim rating = selections.Single(Function(item) String.Equals(item.Id, "weapon-gyro-stabilization", StringComparison.OrdinalIgnoreCase)).Quantity
            Dim penalty = Math.Max(0, rating - chassis.Body.GetValueOrDefault())
            modes.Add(New VehicleOperatingMode With {
                .Name = "Weapon gyro-stabilization active", .Speed = speed,
                .Handling = If(modified?.Handling, If(upgrades?.Handling, chassis.Handling)) + penalty,
                .Notes = $"Handling test modifier +{penalty} while active."
            })
        End If
        If DesignEnhancements.Has(selections, "aircraft-drop-tanks") Then
            Dim pairs = selections.Single(Function(item) String.Equals(item.Id, "aircraft-drop-tanks", StringComparison.OrdinalIgnoreCase)).Quantity
            modes.Add(New VehicleOperatingMode With {
                .Name = "Drop tanks jettisoned", .Speed = speed + 15 * (pairs - 1),
                .Handling = If(modified?.Handling, If(upgrades?.Handling, chassis.Handling)) - pairs \ 2,
                .Signature = signature + pairs,
                .Notes = $"Fuel capacity drops by {2000D * pairs} L and fuel remaining in the tanks is lost."
            })
        End If
        If externalCargo > 0D Then
            modes.Add(New VehicleOperatingMode With {
                .Name = "External cargo mounts loaded", .Speed = speed,
                .Handling = If(modified?.Handling, If(upgrades?.Handling, chassis.Handling)) + 1,
                .Notes = "Apply +1 to Handling when external cargo is loaded. Payload weight counts against Load."
            })
        End If
        Return If(modes.Count = 0, Nothing, modes)
    End Function

    Private Function Scale(value As Integer?, multiplier As Decimal) As Integer?
        If Not value.HasValue Then Return Nothing
        Return CInt(Math.Floor(value.Value * multiplier))
    End Function
End Module
