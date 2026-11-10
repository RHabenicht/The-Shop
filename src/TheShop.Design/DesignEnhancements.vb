Public Module DesignEnhancements
    Public Function Has(selections As IReadOnlyList(Of ModificationSelection), id As String) As Boolean
        Return selections IsNot Nothing AndAlso selections.Any(Function(item) item IsNot Nothing AndAlso
            String.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
    End Function

    Public Function PreparePlant(chassis As StockChassis, original As StockPowerPlant,
                                 selections As IReadOnlyList(Of ModificationSelection)) As StockPowerPlant
        If Not (Has(selections, "hybrid-engine") OrElse Has(selections, "smart-materials") OrElse Has(selections, "fin-drive")) Then Return original
        Dim plant = original.Copy()
        If Has(selections, "hybrid-engine") Then
            plant.Signature += 1
            plant.StartingAcceleration -= 2
            plant.MaximumAcceleration -= 2
            plant.StartingEconomyKmPerLiter *= 2D
            plant.MaximumEconomyKmPerLiter *= 2D
            plant.StartingEconomyKmPerBar *= 2D
            plant.MaximumEconomyKmPerBar *= 2D
        End If
        If Has(selections, "smart-materials") Then
            plant.MaximumSpeed = Scale(plant.MaximumSpeed, 1.15D)
            plant.MaximumAcceleration = Scale(plant.MaximumAcceleration, 1.15D)
            plant.MaximumLoadKg *= 1.15D
            If chassis.Category = "Rotor Craft" Then plant.Signature += 1
        End If
        If Has(selections, "fin-drive") Then
            plant.StartingSpeed = Scale(plant.StartingSpeed, 1.25D)
            plant.MaximumSpeed = Scale(plant.MaximumSpeed, 1.25D)
            plant.StartingAcceleration = Scale(plant.StartingAcceleration, 1.25D)
            plant.MaximumAcceleration = Scale(plant.MaximumAcceleration, 1.25D)
        End If
        Return plant
    End Function

    Private Function Scale(value As Integer?, multiplier As Decimal) As Integer?
        If Not value.HasValue Then Return Nothing
        Return CInt(Math.Floor(value.Value * multiplier))
    End Function
End Module
