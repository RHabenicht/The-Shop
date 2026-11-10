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

    Friend ReadOnly Property CargoPerUnit As Decimal

    Public Sub New(id As String, name As String, quantityLabel As String, maximum As Integer, Optional cargoPerUnit As Decimal = 0D)
        Me.Id = id
        Me.Name = name
        Me.QuantityLabel = quantityLabel
        Me.Maximum = maximum
        Me.CargoPerUnit = cargoPerUnit
    End Sub
End Class

Public NotInheritable Class ModifiedRatings
    Public Property Armor As Integer?
    Public Property Bulwark As Integer?
    Public Property Handling As Integer?
    Public Property OnRoadHandling As Integer?
    Public Property OffRoadHandling As Integer?
    Public Property Sensor As Integer?
    Public Property Sonar As Integer?
    Public Property TowedArraySonar As Integer?
    Public Property WaterSpeed As Integer?
    Public Property WaterSpeedMetersPerTurn As Integer?
    Public Property WaterHandlingModifier As Integer?
End Class

Public Module ModificationCatalog
    Private ReadOnly sensorPoints As Integer() = {0, 50, 75, 100, 125, 150, 200, 300, 500, 1000, 5000}
    Private ReadOnly sensorCargo As Integer() = {0, 0, 1, 2, 2, 3, 4, 6, 10, 12, 16}
    Private ReadOnly sensorLoad As Integer() = {0, 12, 20, 25, 35, 50, 75, 110, 150, 200, 250}

    Private ReadOnly sonarPoints As Integer() = {0, 50, 500, 2500, 5000, 10000, 25000, 50000, 75000, 125000, 250000}
    Private ReadOnly sonarCargo As Integer() = {0, 4, 6, 10, 16, 50, 75, 100, 200, 300, 500}
    Private ReadOnly sonarLoad As Integer() = {0, 120, 250, 350, 500, 750, 1000, 1250, 1500, 2000, 2500}

    Private ReadOnly craneCapacityKg As Integer() = {0, 25, 200, 750, 2000, 5000, 20000, 30000, 45000, 60000}

    Public ReadOnly Property Rules As IReadOnlyList(Of ModificationRule) = Array.AsReadOnly({
        New ModificationRule("airlock", "Airlock", "people per lock", Integer.MaxValue),
        New ModificationRule("crash-cage", "Crash cage", "", 1),
        New ModificationRule("datajack-port", "Datajack port", "", 1),
        New ModificationRule("electronics-port", "Electronics port", "ports", Integer.MaxValue),
        New ModificationRule("remote-control-encryption", "Remote-control encryption module", "rating", Integer.MaxValue),
        New ModificationRule("retransmission-unit", "Retransmission unit", "units", Integer.MaxValue),
        New ModificationRule("retransmission-power-amplifier", "Retransmission power amplifier", "Flux per unit", 10),
        New ModificationRule("magnetic-anomaly-detector", "Magnetic-anomaly detector", "", 1),
        New ModificationRule("over-the-horizon-sensors", "Over-the-horizon sensors", "", 1),
        New ModificationRule("trideo-display", "Video/trideo display", "20 cm screen units", Integer.MaxValue, 0.15D),
        New ModificationRule("simsense-player", "Simsense player", "units", Integer.MaxValue, 0.1D),
        New ModificationRule("cellular-phone", "Cellular phone", "units", Integer.MaxValue, 0.05D),
        New ModificationRule("tabletop-computer", "Tabletop computer, excluding monitor", "units", Integer.MaxValue, 0.5D),
        New ModificationRule("computer-printer", "Computer printer", "units", Integer.MaxValue, 0.25D),
        New ModificationRule("radio", "Radio", "total Flux", Integer.MaxValue, 0.3D),
        New ModificationRule("av-recorder", "Audio/video/trideo recorder", "units", Integer.MaxValue, 0.25D),
        New ModificationRule("cyberdeck", "Cyberdeck", "units", Integer.MaxValue, 0.15D),
        New ModificationRule("remote-control-deck", "Remote-control deck", "units", Integer.MaxValue, 0.25D),
        New ModificationRule("battletac-master", "BattleTac master component", "units", Integer.MaxValue, 0.6D),
        New ModificationRule("tactical-comms-master", "Tactical communications master unit", "units", Integer.MaxValue, 2D),
        New ModificationRule("tactical-comms-personal", "Tactical communications personal unit", "total Flux", Integer.MaxValue, 0.3D),
        New ModificationRule("microwave-laser-link", "Microwave/laser link", "units", Integer.MaxValue, 1D),
        New ModificationRule("satellite-dish-portable", "Standard portable satellite dish", "units", Integer.MaxValue, 2D),
        New ModificationRule("satellite-dish-large", "Large portable satellite dish", "units", Integer.MaxValue, 6D),
        New ModificationRule("satellite-dish-fixed", "Fixed-base satellite dish", "units", Integer.MaxValue, 20D),
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
        New ModificationRule("sonar", "Sonar system", "rating", 10),
        New ModificationRule("towed-array-sonar", "Towed-array sonar", "rating", 6),
        New ModificationRule("improved-anti-theft-security", "Improved anti-theft security", "rating (3–10)", 10),
        New ModificationRule("anti-theft-electric-system", "Anti-theft electric system", "", 1),
        New ModificationRule("proximity-alert", "Proximity alert", "", 1),
        New ModificationRule("crane", "Crane", "kg lifting capacity", Integer.MaxValue),
        New ModificationRule("amphibious-operation", "Amphibious operation package", "level", 3),
        New ModificationRule("hovercraft-water-seal", "Hovercraft water seal", "", 1)
    })

    Public Function MaximumQuantity(rule As ModificationRule, chassis As StockChassis) As Integer
        If rule.Id <> "crane" Then Return rule.Maximum
        Dim body = chassis.Body.GetValueOrDefault()
        If body < craneCapacityKg.Length Then Return craneCapacityKg(body)
        Return CInt(Math.Min(Integer.MaxValue, 750D * body * body))
    End Function

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
            Case "magnetic-anomaly-detector"
                Return chassis.Category = "Fixed-Wing Aircraft" OrElse chassis.Category = "Rotor Craft" OrElse
                    chassis.Category = "Vectored-Thrust Aircraft"
            Case "over-the-horizon-sensors"
                Return chassis.Category = "Ship"
            Case "sensors"
                Return chassis.Sensor.GetValueOrDefault() < rule.Maximum
            Case "sonar"
                Return (chassis.Category = "Ship" OrElse chassis.Category = "Submarine") AndAlso
                    chassis.Sonar.GetValueOrDefault() < rule.Maximum
            Case "towed-array-sonar"
                Return chassis.Category = "Ship" OrElse chassis.Category = "Submarine"
            Case "crane"
                Return chassis.Body.GetValueOrDefault() > 0
            Case "amphibious-operation"
                Return chassis.Category = "Bike" OrElse chassis.Category = "Car"
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
        Dim validated As New List(Of (Rule As ModificationRule, Quantity As Integer))

        For Each choice In selections
            If choice Is Nothing Then Throw New ArgumentException("A modification selection cannot be null.")
            Dim rule = Rules.SingleOrDefault(Function(item) String.Equals(item.Id, choice.Id, StringComparison.OrdinalIgnoreCase))
            If rule Is Nothing Then Throw New ArgumentException($"Unknown catalog modification '{choice.Id}'.")
            If Not seen.Add(rule.Id) Then Throw New ArgumentException($"'{rule.Name}' was selected more than once.")
            If Not IsCompatible(rule, chassis, plant) Then
                Throw New ArgumentException($"'{rule.Name}' is unavailable for this chassis or power plant.")
            End If
            Dim maximum = MaximumQuantity(rule, chassis)
            If choice.Quantity < 1 OrElse choice.Quantity > maximum Then
                Throw New ArgumentException($"'{rule.Name}' requires a quantity from 1 to {maximum}.")
            End If
            If rule.Id = "sensors" AndAlso choice.Quantity <= chassis.Sensor.GetValueOrDefault() Then
                Throw New ArgumentException($"Choose a sensor rating above the factory rating of {chassis.Sensor}.")
            End If
            If rule.Id = "sonar" AndAlso choice.Quantity <= chassis.Sonar.GetValueOrDefault() Then
                Throw New ArgumentException($"Choose a sonar rating above the factory rating of {chassis.Sonar}.")
            End If
            If rule.Id = "amphibious-operation" AndAlso choice.Quantity = 1 AndAlso
                (chassis.Id = "tracked-apc" OrElse chassis.Id = "micro-crawler" OrElse
                 chassis.Id.EndsWith("-tracked-crawler", StringComparison.Ordinal) OrElse
                 chassis.Id.EndsWith("-caterpillar", StringComparison.Ordinal)) Then
                Throw New ArgumentException("A level 1 amphibious operation package requires wheels.")
            End If
            If rule.Id = "improved-anti-theft-security" AndAlso choice.Quantity < 3 Then
                Throw New ArgumentException("Improved anti-theft security requires a rating from 3 to 10.")
            End If
            validated.Add((rule, choice.Quantity))
            Select Case rule.Id
                Case "vehicle-armor", "concealed-armor", "bulwark", "sensors", "sonar", "towed-array-sonar", "amphibious-operation"
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
        Dim retransmissionUnits = validated.Where(Function(choice) choice.Rule.Id = "retransmission-unit").
            Select(Function(choice) choice.Quantity).SingleOrDefault()
        If seen.Contains("retransmission-power-amplifier") AndAlso retransmissionUnits = 0 Then
            Throw New ArgumentException("A retransmission power amplifier requires a retransmission unit.")
        End If
        Dim sensorRating = If(finalRatings?.Sensor, chassis.Sensor.GetValueOrDefault())
        Dim result = validated.Select(Function(choice) Price(choice.Rule, choice.Quantity, chassis, sensorRating, retransmissionUnits)).ToList()
        design.Additions = design.Additions.Concat(result).ToList()
        Return result
    End Function

    Private Sub ApplyRating(ratings As ModifiedRatings, id As String, quantity As Integer, chassis As StockChassis,
                            upgrades As UpgradedRatings)
        Select Case id
            Case "sensors"
                ratings.Sensor = quantity
                Return
            Case "sonar"
                ratings.Sonar = quantity
                Return
            Case "towed-array-sonar"
                ratings.TowedArraySonar = quantity
                Return
            Case "amphibious-operation"
                ratings.WaterHandlingModifier = If(quantity = 1, 2, 0)
                If quantity = 1 Then
                    ratings.WaterSpeedMetersPerTurn = 15
                Else
                    ratings.WaterSpeed = If(quantity = 2, 30, 45)
                End If
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

    Private Function Price(rule As ModificationRule, quantity As Integer, chassis As StockChassis,
                           sensorRating As Integer, retransmissionUnits As Integer) As DesignAddition
        Dim body = CDec(chassis.Body.GetValueOrDefault())
        Dim item As New DesignAddition With {.Name = rule.Name, .CargoConsumed = rule.CargoPerUnit * quantity}
        Select Case rule.Id
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
            Case "remote-control-encryption"
                item.DesignPoints = 50D * quantity
            Case "retransmission-unit"
                item.DesignPoints = 250D * quantity
                item.CargoConsumed = quantity
                item.LoadConsumedKg = 5D * quantity
            Case "retransmission-power-amplifier"
                item.Name = $"{rule.Name} (Flux {quantity} per unit)"
                item.DesignPoints = 5D * quantity * retransmissionUnits
                item.CargoConsumed = Math.Floor(0.25D * quantity) * retransmissionUnits
                item.LoadConsumedKg = CDec(quantity) * retransmissionUnits
            Case "magnetic-anomaly-detector", "over-the-horizon-sensors"
                If sensorRating = 0 Then Throw New ArgumentException($"'{rule.Name}' requires a sensor system.")
                If rule.Id = "magnetic-anomaly-detector" Then
                    item.DesignPoints = 250D * sensorRating
                    item.CargoConsumed = 12D
                    item.LoadConsumedKg = 50D
                Else
                    item.DesignPoints = 1000D * sensorRating
                    item.CargoConsumed = 216D
                    item.LoadConsumedKg = 2500D
                End If
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
                If rule.Id = "concealed-armor" Then item.CargoConsumed = 2D * quantity
            Case "bulwark"
                Dim hull = CDec(chassis.Hull.GetValueOrDefault())
                Dim hullFactor = 100D * hull * hull
                item.DesignPoints = hullFactor * 100D * quantity
                item.LoadConsumedKg = hullFactor * 500D * quantity
            Case "sensors"
                item.DesignPoints = sensorPoints(quantity)
                item.CargoConsumed = sensorCargo(quantity)
                item.LoadConsumedKg = sensorLoad(quantity)
            Case "sonar"
                item.DesignPoints = sonarPoints(quantity)
                item.CargoConsumed = sonarCargo(quantity)
                item.LoadConsumedKg = sonarLoad(quantity)
            Case "towed-array-sonar"
                item.DesignPoints = 1000D * quantity
                item.CargoConsumed = 125D
                item.LoadConsumedKg = 1500D
            Case "improved-anti-theft-security"
                Dim pointsPerRating = If(quantity <= 6, 4D, If(quantity <= 9, 10D, 50D))
                item.DesignPoints = pointsPerRating * quantity
            Case "anti-theft-electric-system"
                item.DesignPoints = 20D
            Case "proximity-alert"
                If sensorRating = 0 Then Throw New ArgumentException("A proximity alert requires a sensor system.")
                item.DesignPoints = 2D
            Case "crane"
                item.DesignPoints = quantity / 50D
                item.CargoConsumed = 15D
                item.LoadConsumedKg = body * 80D
            Case "amphibious-operation"
                Select Case quantity
                    Case 1
                        item.DesignPoints = 25D
                    Case 2
                        item.DesignPoints = 80D
                    Case 3
                        item.DesignPoints = 200D
                        item.CargoConsumed = 2D
                End Select
            Case "hovercraft-water-seal"
                item.DesignPoints = body * 5D
        End Select
        Return item
    End Function
End Module
