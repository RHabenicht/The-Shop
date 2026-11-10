Imports System.Text.Json.Serialization

Public NotInheritable Class ModificationSelection
    <JsonRequired>
    Public Property Id As String
    <JsonRequired>
    Public Property Quantity As Integer
End Class

Public NotInheritable Class ModificationRule
    Public ReadOnly Property Id As String
    Public ReadOnly Property Name As String
    Public ReadOnly Property QuantityLabel As String
    Public ReadOnly Property Maximum As Integer

    Public Sub New(id As String, name As String, quantityLabel As String, maximum As Integer)
        Me.Id = id
        Me.Name = name
        Me.QuantityLabel = quantityLabel
        Me.Maximum = maximum
    End Sub
End Class

Public NotInheritable Class ModifiedRatings
    Public Property Armor As Integer?
    Public Property Bulwark As Integer?
    Public Property Handling As Integer?
    Public Property OnRoadHandling As Integer?
    Public Property OffRoadHandling As Integer?
    Public Property Sensor As Integer?
    Public Property TowedArraySonar As Integer?
End Class

Public Module ModificationCatalog
    Private ReadOnly sensorPoints As Integer() = {0, 50, 75, 100, 125, 150, 200, 300, 500, 1000, 5000}
    Private ReadOnly sensorCargo As Integer() = {0, 0, 1, 2, 2, 3, 4, 6, 10, 12, 16}
    Private ReadOnly sensorLoad As Integer() = {0, 12, 20, 25, 35, 50, 75, 110, 150, 200, 250}

    Public ReadOnly Property Rules As IReadOnlyList(Of ModificationRule) = Array.AsReadOnly({
        New ModificationRule("airlock", "Airlock", "people per lock", Integer.MaxValue),
        New ModificationRule("crash-cage", "Crash cage", "", 1),
        New ModificationRule("datajack-port", "Datajack port", "", 1),
        New ModificationRule("electronics-port", "Electronics port", "ports", Integer.MaxValue),
        New ModificationRule("nitrous", "Nitrous oxide injectors", "rating", 6),
        New ModificationRule("rigger-adaptation", "Rigger adaptation", "", 1),
        New ModificationRule("spotlight", "Spotlight", "lights", Integer.MaxValue),
        New ModificationRule("performance-tires", "Performance tires", "tires", Integer.MaxValue),
        New ModificationRule("off-road-tires", "Off-road tires", "tires", Integer.MaxValue),
        New ModificationRule("dual-purpose-tires", "Dual-purpose tires", "tires", Integer.MaxValue),
        New ModificationRule("vehicle-armor", "Vehicle armor", "added points", Integer.MaxValue),
        New ModificationRule("concealed-armor", "Concealed armor", "added points", Integer.MaxValue),
        New ModificationRule("bulwark", "Bulwark", "added points", Integer.MaxValue),
        New ModificationRule("sensors", "Sensor system", "rating", 10),
        New ModificationRule("towed-array-sonar", "Towed-array sonar", "rating", 6),
        New ModificationRule("hovercraft-water-seal", "Hovercraft water seal", "", 1)
    })

    Public Function IsCompatible(rule As ModificationRule, chassis As StockChassis, plant As StockPowerPlant) As Boolean
        Select Case rule.Id
            Case "airlock"
                Return chassis.Category = "Submarine"
            Case "crash-cage"
                Return Not chassis.IsDrone AndAlso chassis.Category <> "Bike"
            Case "datajack-port", "rigger-adaptation"
                Return Not chassis.IsDrone AndAlso chassis.Id <> "semiballistic" AndAlso chassis.Id <> "suborbital"
            Case "vehicle-armor"
                Return chassis.Body.GetValueOrDefault() > 0
            Case "concealed-armor"
                Return chassis.Body.GetValueOrDefault() > 0 AndAlso chassis.Armor.GetValueOrDefault() = 0
            Case "bulwark"
                Return chassis.Hull.HasValue
            Case "sensors"
                Return chassis.Sensor.GetValueOrDefault() < rule.Maximum
            Case "towed-array-sonar"
                Return chassis.Category = "Ship" OrElse chassis.Category = "Submarine"
            Case "hovercraft-water-seal"
                Return chassis.Category = "Hovercraft"
            Case "nitrous"
                Return plant.Id = "gasoline" OrElse plant.Id = "diesel"
            Case "performance-tires", "off-road-tires", "dual-purpose-tires"
                Return chassis.Body.GetValueOrDefault() > 0 AndAlso Not chassis.IsDrone AndAlso
                    (chassis.Category = "Bike" OrElse
                    (chassis.Category = "Car" AndAlso chassis.Id <> "tracked-apc" AndAlso Not chassis.Id.EndsWith("-caterpillar", StringComparison.Ordinal)))
            Case Else
                Return True
        End Select
    End Function

    Friend Function Apply(selections As IReadOnlyList(Of ModificationSelection), chassis As StockChassis, plant As StockPowerPlant,
                          design As VehicleDesign, upgrades As UpgradedRatings,
                          ByRef finalRatings As ModifiedRatings) As IReadOnlyList(Of DesignAddition)
        If selections Is Nothing OrElse selections.Count = 0 Then Return Nothing
        If design.Additions Is Nothing Then Throw New ArgumentException("Additions must be an array.")
        Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim result As New List(Of DesignAddition)

        For Each choice In selections
            If choice Is Nothing Then Throw New ArgumentException("A modification selection cannot be null.")
            Dim rule = Rules.SingleOrDefault(Function(item) String.Equals(item.Id, choice.Id, StringComparison.OrdinalIgnoreCase))
            If rule Is Nothing Then Throw New ArgumentException($"Unknown catalog modification '{choice.Id}'.")
            If Not seen.Add(rule.Id) Then Throw New ArgumentException($"'{rule.Name}' was selected more than once.")
            If choice.Quantity < 1 OrElse choice.Quantity > rule.Maximum Then
                Throw New ArgumentException($"'{rule.Name}' requires a quantity from 1 to {rule.Maximum}.")
            End If
            If Not IsCompatible(rule, chassis, plant) Then
                Throw New ArgumentException($"'{rule.Name}' is unavailable for this chassis or power plant.")
            End If
            If rule.Id = "sensors" AndAlso choice.Quantity <= chassis.Sensor.GetValueOrDefault() Then
                Throw New ArgumentException($"Choose a sensor rating above the factory rating of {chassis.Sensor}.")
            End If
            result.Add(Price(rule.Id, rule.Name, choice.Quantity, chassis))
            Select Case rule.Id
                Case "vehicle-armor", "concealed-armor", "bulwark", "sensors", "towed-array-sonar"
                    If finalRatings Is Nothing Then finalRatings = New ModifiedRatings()
                    ApplyRating(finalRatings, rule.Id, choice.Quantity, chassis, upgrades)
            End Select
        Next
        If seen.Contains("datajack-port") AndAlso seen.Contains("rigger-adaptation") Then
            Throw New ArgumentException("Rigger adaptation already includes a datajack port.")
        End If
        If seen.Where(Function(id) id.EndsWith("-tires", StringComparison.Ordinal)).Count() > 1 Then
            Throw New ArgumentException("Select one tire type for the design.")
        End If
        If seen.Contains("vehicle-armor") AndAlso seen.Contains("concealed-armor") Then
            Throw New ArgumentException("Vehicle armor and concealed armor cannot be combined.")
        End If
        design.Additions = design.Additions.Concat(result).ToList()
        Return result
    End Function

    Private Sub ApplyRating(ratings As ModifiedRatings, id As String, quantity As Integer, chassis As StockChassis,
                            upgrades As UpgradedRatings)
        Select Case id
            Case "sensors"
                ratings.Sensor = quantity
                Return
            Case "towed-array-sonar"
                ratings.TowedArraySonar = quantity
                Return
            Case "bulwark"
                ratings.Bulwark = chassis.Bulwark.GetValueOrDefault() + quantity
            Case Else
                ratings.Armor = chassis.Armor.GetValueOrDefault() + quantity
        End Select
        Dim penalty = quantity \ 6
        ratings.Handling = If(upgrades?.Handling, chassis.Handling) + penalty
        ratings.OnRoadHandling = If(upgrades?.OnRoadHandling, chassis.OnRoadHandling) + penalty
        ratings.OffRoadHandling = If(upgrades?.OffRoadHandling, chassis.OffRoadHandling) + penalty
    End Sub

    Private Function Price(id As String, name As String, quantity As Integer, chassis As StockChassis) As DesignAddition
        Dim body = CDec(chassis.Body.GetValueOrDefault())
        Dim item As New DesignAddition With {.Name = name}
        Select Case id
            Case "airlock"
                item.DesignPoints = 500D
                item.CargoConsumed = 40D + 40D * quantity
                item.LoadConsumedKg = 1000D
            Case "crash-cage"
                item.DesignPoints = 40D
                item.LoadConsumedKg = 10D
            Case "datajack-port"
                item.DesignPoints = 25D
            Case "electronics-port"
                item.DesignPoints = 10D * quantity
            Case "nitrous"
                item.DesignPoints = 55D * quantity
                item.CargoConsumed = 1.5D
                item.LoadConsumedKg = 15D
            Case "rigger-adaptation"
                item.DesignPoints = 35D
            Case "spotlight"
                item.DesignPoints = 6D * quantity
            Case "performance-tires"
                item.DirectCostNuyen = 75D * body * quantity
            Case "off-road-tires"
                item.DirectCostNuyen = 125D * body * quantity
            Case "dual-purpose-tires"
                item.DirectCostNuyen = 250D * body * quantity
            Case "vehicle-armor", "concealed-armor"
                item.DesignPoints = 50D * quantity
                item.LoadConsumedKg = 5D * body * body * quantity
                If id = "concealed-armor" Then item.CargoConsumed = 2D * quantity
            Case "bulwark"
                Dim hull = CDec(chassis.Hull.GetValueOrDefault())
                Dim hullFactor = 100D * hull * hull
                item.DesignPoints = hullFactor * 100D * quantity
                item.LoadConsumedKg = hullFactor * 500D * quantity
            Case "sensors"
                item.DesignPoints = sensorPoints(quantity)
                item.CargoConsumed = sensorCargo(quantity)
                item.LoadConsumedKg = sensorLoad(quantity)
            Case "towed-array-sonar"
                item.DesignPoints = 1000D * quantity
                item.CargoConsumed = 125D
                item.LoadConsumedKg = 1500D
            Case "hovercraft-water-seal"
                item.DesignPoints = body * 5D
        End Select
        Return item
    End Function
End Module
