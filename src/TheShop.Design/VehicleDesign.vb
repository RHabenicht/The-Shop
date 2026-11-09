Imports System.Text.Json.Serialization

Public NotInheritable Class VehicleDesign
    <JsonRequired>
    Public Property ChassisDesignPoints As Decimal
    <JsonRequired>
    Public Property PowerPlantDesignPoints As Decimal
    <JsonRequired>
    Public Property ChassisMarkup As Decimal
    <JsonRequired>
    Public Property CargoCapacity As Decimal
    <JsonRequired>
    Public Property LoadCapacityKg As Decimal
    <JsonRequired>
    Public Property StreetIndex As Decimal

    Public Property MarkupAdjustment As Decimal
    Public Property Additions As New List(Of DesignAddition)

    Public Function Calculate() As DesignTotals
        Dim effectiveMarkup = ChassisMarkup + MarkupAdjustment
        If ChassisDesignPoints < 0D OrElse PowerPlantDesignPoints < 0D OrElse
           CargoCapacity < 0D OrElse LoadCapacityKg < 0D Then
            Throw New ArgumentException("Starting design points and capacities must be nonnegative.")
        End If
        If ChassisMarkup <= 0D OrElse effectiveMarkup <= 0D OrElse StreetIndex <= 0D Then
            Throw New ArgumentException("Chassis markup, effective markup, and Street Index must be positive.")
        End If
        If Additions Is Nothing Then
            Throw New ArgumentException("Additions must be an array.")
        End If

        Dim points = ChassisDesignPoints + PowerPlantDesignPoints
        Dim directCost As Decimal = 0D
        Dim cargoUsed As Decimal = 0D
        Dim loadUsed As Decimal = 0D
        For Each addition In Additions
            If addition Is Nothing OrElse String.IsNullOrWhiteSpace(addition.Name) Then
                Throw New ArgumentException("Each addition needs a name.")
            End If
            If addition.DesignPoints < 0D OrElse addition.DirectCostNuyen < 0D OrElse
               addition.CargoConsumed < 0D OrElse addition.LoadConsumedKg < 0D Then
                Throw New ArgumentException($"Addition '{addition.Name}' contains a negative cost or capacity use.")
            End If

            points += addition.DesignPoints
            directCost += addition.DirectCostNuyen
            cargoUsed += addition.CargoConsumed
            loadUsed += addition.LoadConsumedKg
        Next

        If cargoUsed > CargoCapacity Then
            Throw New ArgumentException($"Additions consume {cargoUsed} CF, exceeding the available {CargoCapacity} CF.")
        End If
        If loadUsed > LoadCapacityKg Then
            Throw New ArgumentException($"Additions consume {loadUsed} kg, exceeding the available {LoadCapacityKg} kg.")
        End If

        Dim markedUpCost = points * effectiveMarkup * 100D
        Dim baseCost = markedUpCost + directCost
        Return New DesignTotals With {
            .DesignPoints = points,
            .EffectiveMarkup = effectiveMarkup,
            .DesignPointCostNuyen = markedUpCost,
            .DirectCostNuyen = directCost,
            .BaseCostNuyen = baseCost,
            .StreetCostNuyen = baseCost * StreetIndex,
            .CargoRemaining = CargoCapacity - cargoUsed,
            .LoadRemainingKg = LoadCapacityKg - loadUsed
        }
    End Function
End Class

Public NotInheritable Class DesignAddition
    Public Property Name As String
    Public Property DesignPoints As Decimal
    Public Property DirectCostNuyen As Decimal
    Public Property CargoConsumed As Decimal
    Public Property LoadConsumedKg As Decimal
End Class

Public NotInheritable Class DesignTotals
    Public Property DesignPoints As Decimal
    Public Property EffectiveMarkup As Decimal
    Public Property DesignPointCostNuyen As Decimal
    Public Property DirectCostNuyen As Decimal
    Public Property BaseCostNuyen As Decimal
    Public Property StreetCostNuyen As Decimal
    Public Property CargoRemaining As Decimal
    Public Property LoadRemainingKg As Decimal
End Class
