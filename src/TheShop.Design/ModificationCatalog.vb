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

    Friend ReadOnly Property PointsPerUnit As Decimal
    Friend ReadOnly Property LoadPerUnit As Decimal
    Friend ReadOnly Property MountPoints As Integer
    Friend ReadOnly Property CargoPerUnit As Decimal

    Public Sub New(id As String, name As String, quantityLabel As String, maximum As Integer, Optional cargoPerUnit As Decimal = 0D,
                   Optional pointsPerUnit As Decimal = 0D, Optional loadPerUnit As Decimal = 0D, Optional mountPoints As Integer = 0)
        Me.Id = id
        Me.Name = name
        Me.QuantityLabel = quantityLabel
        Me.Maximum = maximum
        Me.CargoPerUnit = cargoPerUnit
        Me.PointsPerUnit = pointsPerUnit
        Me.LoadPerUnit = loadPerUnit
        Me.MountPoints = mountPoints
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
    Public Property ECM As Integer?
    Public Property ECCM As Integer?
    Public Property ElectronicDeception As Integer?
    Public Property ElectronicCounterDeception As Integer?
    Public Property DippingSonar As Integer?
    Public Property RadarSignature As Integer?
    Public Property InfraredSignature As Integer?
    Public Property Acceleration As Integer?
    Public Property Signature As Integer?
    Public Property Speed As Integer?
    Public Property Economy As Decimal?
    Public Property Pilot As Integer?
    Public Property Autonav As Integer?
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

    Private ReadOnly ecmPoints As Integer() = {0, 100, 200, 300, 500, 750, 1000, 2000, 3000, 5000, 10000}
    Private ReadOnly ecmLoad As Integer() = {0, 5, 10, 15, 20, 25, 50, 75, 100, 150, 250}
    Private ReadOnly eccmPoints As Integer() = {0, 100, 200, 300, 400, 500, 750, 1000, 2500, 3000, 5000}
    Private ReadOnly eccmLoad As Integer() = {0, 3, 5, 8, 12, 18, 25, 50, 75, 100, 150}
    Private ReadOnly edPoints As Integer() = {0, 150, 300, 500, 750, 1000, 2500}
    Private ReadOnly edCargo As Integer() = {0, 1, 2, 4, 5, 6, 8}
    Private ReadOnly edLoad As Integer() = {0, 10, 20, 30, 45, 50, 75}
    Private ReadOnly ecdPoints As Integer() = {0, 100, 300, 500, 750, 1500, 3000}
    Private ReadOnly ecdCargo As Integer() = {0, 1, 2, 3, 4, 5, 8}
    Private ReadOnly ecdLoad As Integer() = {0, 25, 30, 35, 45, 60, 75}
    Private ReadOnly pilotPoints As Integer() = {0, 0, 50, 250, 1250, 5000}
    Private ReadOnly autonavPoints As Integer() = {0, 5, 10, 50, 150}

    Private ReadOnly liftingCapacityKg As Integer() = {0, 25, 200, 750, 2000, 5000, 20000, 30000, 45000, 60000}

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
        New ModificationRule("winch", "Winch", "kg pulling capacity", Integer.MaxValue),
        New ModificationRule("generator", "Generator", "", 1),
        New ModificationRule("medical-clinic", "Medical clinic", "rating", 6),
        New ModificationRule("valkyrie-module", "Valkyrie module", "units", Integer.MaxValue),
        New ModificationRule("bucket-seat", "Bucket seat", "seats", Integer.MaxValue, 6D),
        New ModificationRule("bench-seat", "Bench seat", "seats", Integer.MaxValue, 6D),
        New ModificationRule("folding-bench-seat", "Folding bench seat", "seats", Integer.MaxValue, 6D),
        New ModificationRule("reinforced-bucket-seat", "Reinforced bucket seat", "seats", Integer.MaxValue, 8D),
        New ModificationRule("reinforced-bench-seat", "Reinforced bench seat", "seats", Integer.MaxValue, 8D),
        New ModificationRule("ejection-bucket-seat", "Ejection bucket seat", "seats", Integer.MaxValue, 6D),
        New ModificationRule("reinforced-ejection-bucket-seat", "Reinforced ejection bucket seat", "seats", Integer.MaxValue, 6D),
        New ModificationRule("aircraft-flotation", "Aircraft flotation package", "", 1),
        New ModificationRule("mechanical-arm", "Mechanical arm", "arms", Integer.MaxValue),
        New ModificationRule("mechanical-arm-strength", "Mechanical arm strength enhancement", "extra Strength per arm", Integer.MaxValue),
        New ModificationRule("drone-rack-body-0", "Body 0 drone rack", "racks", Integer.MaxValue),
        New ModificationRule("drone-rack-body-1", "Body 1 drone rack", "racks", Integer.MaxValue, 2D),
        New ModificationRule("drone-rack-body-2", "Body 2 drone rack", "racks", Integer.MaxValue, 7D),
        New ModificationRule("drone-rack-body-3", "Body 3 drone rack", "racks", Integer.MaxValue, 13D),
        New ModificationRule("cabin-gas-seal", "Cabin gas seal", "", 1),
        New ModificationRule("cabin-water-seal", "Cabin water seal", "", 1),
        New ModificationRule("engine-water-seal", "Engine water seal", "", 1),
        New ModificationRule("cabin-overpressure", "Cabin over-pressurization", "", 1),
        New ModificationRule("life-support", "Life support", "10 person-hour blocks", Integer.MaxValue),
        New ModificationRule("roll-bar", "Roll bar", "", 1),
        New ModificationRule("suncell", "SunCell", "", 1),
        New ModificationRule("gridlink", "GridLink", "", 1),
        New ModificationRule("adjusted-controls", "Adjusted controls", "1 dwarf, 2 troll, 3 other", 3),
        New ModificationRule("secondary-controls", "Secondary controls", "", 1, 1D),
        New ModificationRule("contingency-maneuver-controls", "Contingency maneuver controls", "rating", 9),
        New ModificationRule("passenger-protection", "Advanced passenger protection", "sets", Integer.MaxValue),
        New ModificationRule("motorbike-gyro", "Motorbike gyro-stabilization", "", 1),
        New ModificationRule("autonav", "Autonav", "rating", 4),
        New ModificationRule("remote-control-interface", "Remote-control interface", "", 1),
        New ModificationRule("advanced-drone-pilot", "Advanced drone pilot", "rating (2–5)", 5),
        New ModificationRule("extra-entry-points", "Additional entry points", "entry points", Integer.MaxValue),
        New ModificationRule("ballast-tanks", "Ballast tanks", "", 1),
        New ModificationRule("sidecar", "Motorcycle sidecar", "1 small, 2 medium, 3 large", 3),
        New ModificationRule("runflat-upgrade", "Runflat tire upgrade", "tires", Integer.MaxValue),
        New ModificationRule("turbocharging", "Turbocharging / superconductive drive", "levels", Integer.MaxValue),
        New ModificationRule("ecm", "Electronic countermeasures", "rating", 10),
        New ModificationRule("eccm", "Electronic counter-countermeasures", "rating", 10),
        New ModificationRule("electronic-deception", "Electronic deception", "rating", 6),
        New ModificationRule("electronic-counter-deception", "Electronic counter-deception", "rating", 6),
        New ModificationRule("radar-absorbent-materials", "Radar-absorbent materials", "levels", 3),
        New ModificationRule("thermal-baffles", "Thermal baffles", "levels", 2),
        New ModificationRule("noisemaker-dispenser", "Noisemaker dispenser", "noisemakers", Integer.MaxValue),
        New ModificationRule("smart-armor", "Smart armor system", "", 1),
        New ModificationRule("chameleon-paint", "Photovoltaic chameleon paint", "", 1),
        New ModificationRule("improved-suspension", "Improved suspension", "levels", 2),
        New ModificationRule("off-road-suspension", "Off-road suspension", "levels", 2),
        New ModificationRule("improved-control-surfaces", "Improved control surfaces", "levels", 2),
        New ModificationRule("external-fixed-firmpoint", "External fixed firmpoint", "mounts", Integer.MaxValue, 0.5D, 10D, 10D, 1),
        New ModificationRule("external-fixed-hardpoint", "External fixed hardpoint", "mounts", Integer.MaxValue, 1D, 25D, 10D, 2),
        New ModificationRule("internal-fixed-firmpoint", "Internal fixed firmpoint", "mounts", Integer.MaxValue, 3D, 20D, 10D, 1),
        New ModificationRule("internal-fixed-hardpoint", "Internal fixed hardpoint", "mounts", Integer.MaxValue, 4D, 35D, 10D, 2),
        New ModificationRule("pintle-mount", "Pintle mount", "mounts", Integer.MaxValue, 0D, 1D, 0D, 1),
        New ModificationRule("ring-mount", "Ring mount", "mounts", Integer.MaxValue, 1D, 10D, 25D, 2),
        New ModificationRule("remote-micro-turret", "Remote micro-turret", "mounts", Integer.MaxValue, 0D, 100D, 10D, 1),
        New ModificationRule("remote-mini-turret", "Remote mini-turret", "mounts", Integer.MaxValue, 3D, 175D, 25D, 2),
        New ModificationRule("remote-small-turret", "Remote small turret", "mounts", Integer.MaxValue, 4D, 350D, 100D, 4),
        New ModificationRule("remote-medium-turret", "Remote medium turret", "mounts", Integer.MaxValue, 4D, 600D, 1000D, 6),
        New ModificationRule("remote-large-turret", "Remote large turret", "mounts", Integer.MaxValue, 32D, 2500D, 8000D, 8),
        New ModificationRule("remote-extra-large-turret", "Remote extra-large turret", "mounts", Integer.MaxValue, 64D, 5000D, 35000D, 10),
        New ModificationRule("crewed-mini-turret", "Crewed mini-turret", "mounts", Integer.MaxValue, 6D, 125D, 25D, 2),
        New ModificationRule("crewed-small-turret", "Crewed small turret", "mounts", Integer.MaxValue, 7D, 250D, 100D, 4),
        New ModificationRule("crewed-medium-turret", "Crewed medium turret", "mounts", Integer.MaxValue, 16D, 500D, 1000D, 6),
        New ModificationRule("crewed-large-turret", "Crewed large turret", "mounts", Integer.MaxValue, 60D, 1500D, 6000D, 8),
        New ModificationRule("crewed-extra-large-turret", "Crewed extra-large turret", "mounts", Integer.MaxValue, 120D, 3000D, 30000D, 10),
        New ModificationRule("medium-launch-control", "Medium launch control", "mounts", Integer.MaxValue, 0.5D, 10D, 10D, 1),
        New ModificationRule("heavy-launch-control", "Heavy launch control", "mounts", Integer.MaxValue, 1D, 25D, 10D, 2),
        New ModificationRule("autosoft-interpretation", "Autosoft interpretation system", "", 1),
        New ModificationRule("battletac-fddm", "BattleTac FDDM receiver", "", 1, 0D, 350D),
        New ModificationRule("battletac-ivis", "BattleTac IVIS receiver", "", 1, 0D, 250D),
        New ModificationRule("ccss-integration", "Closed-circuit simsense integration", "technical rating", 10),
        New ModificationRule("dipping-sonar", "Dipping sonar", "rating", 6, 12D),
        New ModificationRule("smartlink-integration-1", "Smartlink I mount integration", "mounts", Integer.MaxValue, 0D, 25D),
        New ModificationRule("smartlink-integration-2", "Smartlink II mount integration", "mounts", Integer.MaxValue, 0D, 35D),
        New ModificationRule("engine-customization-speed", "Engine customization — Speed", "levels", Integer.MaxValue),
        New ModificationRule("engine-customization-acceleration", "Engine customization — Acceleration", "levels", Integer.MaxValue),
        New ModificationRule("engine-customization-load", "Engine customization — Load", "levels", Integer.MaxValue),
        New ModificationRule("drive-by-wire", "Drive-by-wire system", "rating", 3),
        New ModificationRule("drive-by-wire-speed-allocation", "Drive-by-wire Speed allocation", "percent", 10),
        New ModificationRule("drive-by-wire-acceleration-allocation", "Drive-by-wire Acceleration allocation", "percent", 10),
        New ModificationRule("drive-by-wire-load-allocation", "Drive-by-wire Load allocation", "percent", 10),
        New ModificationRule("amphibious-operation", "Amphibious operation package", "level", 3),
        New ModificationRule("hovercraft-water-seal", "Hovercraft water seal", "", 1)
    })

    Public Function MaximumQuantity(rule As ModificationRule, chassis As StockChassis) As Integer
        Dim body = chassis.Body.GetValueOrDefault()
        If rule.Id = "extra-entry-points" Then
            If chassis.Hull.HasValue Then Return CInt(Math.Min(Integer.MaxValue, chassis.Hull.Value * 2D))
            Return CInt(Math.Ceiling(body / 2D))
        End If
        If rule.Id = "mechanical-arm-strength" Then Return If(body > 0 AndAlso body < 10, body * (10 - body), 0)
        If rule.Id <> "crane" AndAlso rule.Id <> "winch" Then Return rule.Maximum
        If rule.Id = "winch" AndAlso body = 0 Then Return 5
        If body < liftingCapacityKg.Length Then Return liftingCapacityKg(body)
        Return CInt(Math.Min(Integer.MaxValue, 750D * body * body))
    End Function

    Public Function IsCompatible(rule As ModificationRule, chassis As StockChassis, plant As StockPowerPlant) As Boolean
        If rule.MountPoints > 0 Then
            If chassis.Body.GetValueOrDefault() < rule.MountPoints OrElse chassis.Category = "Bike" Then Return False
            If rule.Id = "ring-mount" AndAlso (chassis.IsDrone OrElse chassis.Category = "Rotor Craft") Then Return False
            If rule.Id.StartsWith("crewed-", StringComparison.Ordinal) AndAlso chassis.IsDrone Then Return False
            Return True
        End If
        Select Case rule.Id
            Case "ccss-integration"
                Return chassis.Category = "Ship"
            Case "dipping-sonar"
                Return chassis.Category = "Rotor Craft" OrElse chassis.Category = "Vectored-Thrust Aircraft"
            Case "airlock"
                Return chassis.Category = "Submarine" OrElse
                    (chassis.Body.HasValue AndAlso chassis.Category <> "Bike" AndAlso Not chassis.IsDrone AndAlso chassis.Id <> "sand-buggy")
            Case "extra-entry-points"
                Return MaximumQuantity(rule, chassis) > 0
            Case "ballast-tanks"
                Return chassis.Body.HasValue AndAlso chassis.Category <> "Submarine" AndAlso chassis.Category <> "Ship" AndAlso
                    chassis.Category <> "Boat" AndAlso chassis.Category <> "Bike" AndAlso chassis.Id <> "sand-buggy" AndAlso plant.Id <> "unpowered"
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
            Case "winch"
                Return chassis.Body.HasValue AndAlso (chassis.Category = "Bike" OrElse chassis.Category = "Car")
            Case "generator"
                Return chassis.Id = "barge" OrElse chassis.Id.EndsWith("-trailer", StringComparison.Ordinal) OrElse
                    chassis.Id.EndsWith("-rail-car", StringComparison.Ordinal)
            Case "bench-seat", "folding-bench-seat", "reinforced-bench-seat"
                Return Not chassis.IsDrone AndAlso chassis.Category <> "Bike"
            Case "bucket-seat", "reinforced-bucket-seat", "ejection-bucket-seat", "reinforced-ejection-bucket-seat"
                Return Not chassis.IsDrone
            Case "crane"
                Return chassis.Body.GetValueOrDefault() > 0
            Case "aircraft-flotation"
                Return plant.StartingSpeed.HasValue AndAlso (chassis.Category = "Fixed-Wing Aircraft" OrElse
                    chassis.Category = "Rotor Craft" OrElse chassis.Category = "Vectored-Thrust Aircraft")
            Case "mechanical-arm"
                Return chassis.Body.GetValueOrDefault() > 0
            Case "mechanical-arm-strength"
                Return MaximumQuantity(rule, chassis) > 0
            Case "drone-rack-body-0", "drone-rack-body-1", "drone-rack-body-2", "drone-rack-body-3"
                Return chassis.Body.GetValueOrDefault() > 0
            Case "cabin-gas-seal", "cabin-water-seal", "cabin-overpressure"
                Return chassis.Body.HasValue AndAlso chassis.Category <> "Bike" AndAlso Not chassis.IsDrone AndAlso chassis.Id <> "sand-buggy"
            Case "engine-water-seal"
                Return chassis.Body.HasValue AndAlso chassis.Category <> "Bike" AndAlso
                    chassis.Id <> "sand-buggy" AndAlso plant.Id <> "unpowered"
            Case "life-support"
                Return Not chassis.IsDrone AndAlso chassis.Category <> "Bike"
            Case "roll-bar"
                Return Not chassis.IsDrone AndAlso chassis.Category = "Car"
            Case "suncell"
                Return chassis.Category <> "Bike" AndAlso plant.Id.StartsWith("electric-", StringComparison.Ordinal)
            Case "gridlink"
                Return plant.Id.StartsWith("electric-", StringComparison.Ordinal) AndAlso
                    (chassis.Category = "Bike" OrElse chassis.Category = "Car")
            Case "adjusted-controls"
                Return Not chassis.IsDrone AndAlso plant.Id <> "unpowered"
            Case "secondary-controls", "passenger-protection"
                Return Not chassis.IsDrone AndAlso chassis.Category <> "Bike" AndAlso plant.Id <> "unpowered"
            Case "motorbike-gyro"
                Return chassis.Category = "Bike"
            Case "autonav"
                Return chassis.Autonav.GetValueOrDefault() < rule.Maximum AndAlso plant.Id <> "unpowered"
            Case "remote-control-interface"
                Return Not chassis.IsDrone AndAlso chassis.Body.HasValue AndAlso plant.Id <> "unpowered"
            Case "advanced-drone-pilot"
                Return chassis.Pilot.GetValueOrDefault() < rule.Maximum AndAlso plant.Id <> "unpowered"
            Case "turbocharging"
                Return plant.StartingSpeed.HasValue AndAlso plant.MaximumSpeed.HasValue AndAlso
                    plant.StartingAcceleration.HasValue AndAlso
                    (plant.Id = "gasoline" OrElse plant.Id = "diesel" OrElse plant.Id = "methane" OrElse
                     plant.Id.StartsWith("electric-", StringComparison.Ordinal)) AndAlso
                    (Not chassis.Category.Contains("Aircraft") AndAlso chassis.Category <> "Rotor Craft" OrElse
                     plant.Id.StartsWith("electric-", StringComparison.Ordinal))
            Case "thermal-baffles"
                Return chassis.Body.HasValue AndAlso
                    (plant.Id = "gasoline" OrElse plant.Id = "methane" OrElse plant.Id = "diesel" OrElse plant.Id.StartsWith("jet-", StringComparison.Ordinal))
            Case "noisemaker-dispenser"
                Return chassis.Category = "Boat" OrElse chassis.Category = "Ship" OrElse chassis.Category = "Submarine"
            Case "smart-armor"
                Return chassis.Body.GetValueOrDefault() >= 4
            Case "engine-customization-speed"
                Return chassis.Body.HasValue AndAlso plant.StartingSpeed.HasValue AndAlso plant.MaximumSpeed.HasValue AndAlso Not plant.StartingSecondarySpeed.HasValue
            Case "engine-customization-acceleration"
                Return chassis.Body.HasValue AndAlso plant.StartingAcceleration.HasValue AndAlso plant.MaximumAcceleration.HasValue
            Case "engine-customization-load"
                Return chassis.Body.GetValueOrDefault() > 0 AndAlso plant.Id <> "unpowered"
            Case "drive-by-wire", "drive-by-wire-speed-allocation", "drive-by-wire-acceleration-allocation", "drive-by-wire-load-allocation"
                Return chassis.Body.HasValue AndAlso plant.StartingSpeed.HasValue AndAlso Not plant.StartingSecondarySpeed.HasValue AndAlso plant.Id <> "unpowered"
            Case "amphibious-operation"
                Return chassis.Category = "Bike" OrElse chassis.Category = "Car"
            Case "hovercraft-water-seal"
                Return chassis.Category = "Hovercraft"
            Case "nitrous"
                Return plant.Id = "gasoline" OrElse plant.Id = "diesel"
            Case "sidecar"
                Return chassis.Category = "Bike" AndAlso chassis.Id <> "atv"
            Case "improved-control-surfaces"
                Return chassis.Category = "Boat" AndAlso chassis.Handling.HasValue
            Case "performance-tires", "off-road-tires", "dual-purpose-tires", "runflat-upgrade", "improved-suspension", "off-road-suspension"
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
            If rule.Id = "autonav" AndAlso choice.Quantity <= chassis.Autonav.GetValueOrDefault() Then
                Throw New ArgumentException("Choose an Autonav rating above the factory rating.")
            End If
            If rule.Id = "advanced-drone-pilot" AndAlso choice.Quantity <= Math.Max(1, chassis.Pilot.GetValueOrDefault()) Then
                Throw New ArgumentException("Choose a Pilot rating above the starting rating.")
            End If
            validated.Add((rule, choice.Quantity))
            Select Case rule.Id
                Case "vehicle-armor", "concealed-armor", "bulwark", "sensors", "sonar", "towed-array-sonar", "amphibious-operation", "aircraft-flotation", "autonav", "advanced-drone-pilot", "ecm", "eccm", "electronic-deception", "electronic-counter-deception", "dipping-sonar"
                    If finalRatings Is Nothing Then finalRatings = New ModifiedRatings()
                    ApplyRating(finalRatings, rule.Id, choice.Quantity, chassis, plant, upgrades)
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
        Dim hasCabinSeal = seen.Contains("cabin-gas-seal") OrElse seen.Contains("cabin-water-seal")
        If seen.Contains("airlock") AndAlso chassis.Category <> "Submarine" AndAlso Not hasCabinSeal Then
            Throw New ArgumentException("An airlock on this chassis requires a cabin seal.")
        End If
        If seen.Contains("ballast-tanks") AndAlso Not seen.Contains("engine-water-seal") Then
            Throw New ArgumentException("Ballast tanks require an engine water seal.")
        End If
        If seen.Contains("cabin-gas-seal") AndAlso seen.Contains("cabin-water-seal") Then
            Throw New ArgumentException("Select one cabin seal.")
        End If
        If seen.Contains("cabin-overpressure") AndAlso Not hasCabinSeal Then
            Throw New ArgumentException("Cabin over-pressurization requires a cabin seal.")
        End If
        If seen.Contains("remote-control-interface") Then
            If sensorRating = 0 Then Throw New ArgumentException("A remote-control interface requires sensors.")
            If finalRatings Is Nothing Then finalRatings = New ModifiedRatings()
            If Not finalRatings.Pilot.HasValue Then finalRatings.Pilot = Math.Max(1, chassis.Pilot.GetValueOrDefault())
        ElseIf seen.Contains("advanced-drone-pilot") AndAlso Not chassis.IsDrone AndAlso chassis.Pilot.GetValueOrDefault() = 0 Then
            Throw New ArgumentException("An advanced drone pilot requires a remote-control interface.")
        End If
        If chassis.Category = "Bike" AndAlso Not seen.Contains("motorbike-gyro") AndAlso
            (seen.Contains("remote-control-interface") OrElse If(finalRatings?.Autonav, 0) >= 2) Then
            Throw New ArgumentException("This motorcycle control option requires gyro-stabilization.")
        End If
        Dim armCount = validated.Where(Function(choice) choice.Rule.Id = "mechanical-arm").
            Select(Function(choice) choice.Quantity).SingleOrDefault()
        If seen.Contains("mechanical-arm-strength") AndAlso armCount = 0 Then
            Throw New ArgumentException("Mechanical arm strength enhancement requires a mechanical arm.")
        End If
        If seen.Contains("sidecar") Then
            Dim size = validated.Single(Function(choice) choice.Rule.Id = "sidecar").Quantity
            Dim handlingPenalty = If(size = 3, 2, 1)
            If finalRatings Is Nothing Then finalRatings = New ModifiedRatings()
            finalRatings.Speed = Math.Max(0, If(upgrades?.Speed, plant.StartingSpeed).GetValueOrDefault() - 15)
            Dim economy = If(upgrades?.Economy, If(plant.StartingEconomyKmPerLiter,
                If(plant.StartingEconomyKmPerPowerFactor, plant.StartingEconomyKmPerBar)))
            finalRatings.Economy = economy * 0.9D
            finalRatings.Handling = If(finalRatings.Handling, If(upgrades?.Handling, chassis.Handling)) + handlingPenalty
            finalRatings.OnRoadHandling = If(finalRatings.OnRoadHandling, If(upgrades?.OnRoadHandling, chassis.OnRoadHandling)) + handlingPenalty
            finalRatings.OffRoadHandling = If(finalRatings.OffRoadHandling, If(upgrades?.OffRoadHandling, chassis.OffRoadHandling)) + handlingPenalty
            design.CargoCapacity += If(size = 1, 6D, If(size = 2, 8D, 12D))
        End If
        If seen.Contains("drive-by-wire") AndAlso (seen.Contains("off-road-suspension") OrElse seen.Contains("improved-control-surfaces")) Then
            Throw New ArgumentException("Drive-by-wire is incompatible with off-road suspension and improved control surfaces.")
        End If
        For Each choice In validated.Where(Function(item) item.Rule.Id.StartsWith("engine-customization-", StringComparison.Ordinal))
            If finalRatings Is Nothing Then finalRatings = New ModifiedRatings()
            Dim levels = CDec(choice.Quantity)
            Select Case choice.Rule.Id
                Case "engine-customization-speed"
                    Dim starting = If(upgrades?.Speed, plant.StartingSpeed).GetValueOrDefault()
                    Dim maximum = Math.Max(starting + 30D, Math.Ceiling(plant.MaximumSpeed.Value * 1.75D))
                    Dim value = starting + 30D * levels
                    If value > maximum Then Throw New ArgumentException("Engine customization exceeds the increased Speed maximum.")
                    finalRatings.Speed = CInt(value)
                Case "engine-customization-acceleration"
                    Dim starting = If(upgrades?.Acceleration, plant.StartingAcceleration).GetValueOrDefault()
                    Dim maximum = Math.Max(starting + 2D, Math.Ceiling(plant.MaximumAcceleration.Value * 1.75D))
                    Dim value = starting + 2D * levels
                    If value > maximum Then Throw New ArgumentException("Engine customization exceeds the increased Acceleration maximum.")
                    finalRatings.Acceleration = CInt(value)
                Case "engine-customization-load"
                    Dim increment = chassis.Body.GetValueOrDefault() * 50D
                    Dim maximum = Math.Max(design.LoadCapacityKg + increment, Math.Ceiling(plant.MaximumLoadKg * 1.75D))
                    Dim value = design.LoadCapacityKg + increment * levels
                    If value > maximum Then Throw New ArgumentException("Engine customization exceeds the increased Load maximum.")
                    design.LoadCapacityKg = value
            End Select
        Next
        Dim implicitDualPurpose = Not seen.Any(Function(id) id.EndsWith("-tires", StringComparison.Ordinal)) AndAlso
            (seen.Contains("improved-suspension") OrElse seen.Contains("drive-by-wire"))
        For Each choice In validated.Where(Function(item) item.Rule.Id = "improved-suspension" OrElse
                                           item.Rule.Id = "off-road-suspension" OrElse item.Rule.Id = "improved-control-surfaces")
            If finalRatings Is Nothing Then finalRatings = New ModifiedRatings()
            Dim levels = choice.Quantity
            If choice.Rule.Id = "improved-control-surfaces" Then
                finalRatings.Handling = If(finalRatings.Handling, If(upgrades?.Handling, chassis.Handling)) - levels
            Else
                Dim roadBenefit = levels
                Dim offRoadBenefit = levels
                If choice.Rule.Id = "improved-suspension" Then
                    If seen.Contains("off-road-tires") Then roadBenefit = 0
                    If Not (implicitDualPurpose OrElse seen.Contains("performance-tires") OrElse seen.Contains("dual-purpose-tires") OrElse seen.Contains("off-road-tires")) Then roadBenefit = levels \ 2
                    finalRatings.OnRoadHandling = If(finalRatings.OnRoadHandling, If(upgrades?.OnRoadHandling, chassis.OnRoadHandling)) - roadBenefit
                    If chassis.Category = "Bike" Then finalRatings.OffRoadHandling = If(finalRatings.OffRoadHandling, If(upgrades?.OffRoadHandling, chassis.OffRoadHandling)) - roadBenefit
                Else
                    If seen.Contains("performance-tires") Then offRoadBenefit = 0
                    If Not (implicitDualPurpose OrElse seen.Contains("performance-tires") OrElse seen.Contains("dual-purpose-tires") OrElse seen.Contains("off-road-tires")) Then offRoadBenefit = levels \ 2
                    finalRatings.OnRoadHandling = If(finalRatings.OnRoadHandling, If(upgrades?.OnRoadHandling, chassis.OnRoadHandling)) + offRoadBenefit
                    finalRatings.OffRoadHandling = If(finalRatings.OffRoadHandling, If(upgrades?.OffRoadHandling, chassis.OffRoadHandling)) - offRoadBenefit
                    Dim transport = chassis.Id.Contains("transport") OrElse chassis.Id.Contains("tractor")
                    Dim multiplier = If(chassis.Category = "Bike", 0.7D, If(transport, 0.6D, 0.85D))
                    finalRatings.Speed = CInt(Math.Floor(If(finalRatings.Speed, If(upgrades?.Speed, plant.StartingSpeed)).GetValueOrDefault() * multiplier))
                    Dim economy = If(finalRatings.Economy, If(upgrades?.Economy, If(plant.StartingEconomyKmPerLiter,
                        If(plant.StartingEconomyKmPerPowerFactor, plant.StartingEconomyKmPerBar))))
                    finalRatings.Economy = economy * multiplier
                End If
            End If
        Next
        If seen.Contains("turbocharging") Then
            Dim levels = validated.Single(Function(choice) choice.Rule.Id = "turbocharging").Quantity
            If finalRatings Is Nothing Then finalRatings = New ModifiedRatings()
            Dim baseSpeed = If(finalRatings.Speed, If(upgrades?.Speed, plant.StartingSpeed)).GetValueOrDefault()
            Dim maximum = Math.Max(baseSpeed + 15D, plant.MaximumSpeed.Value * 1.25D)
            Dim speed = baseSpeed + 15D * levels
            If speed > maximum Then Throw New ArgumentException("Turbocharging exceeds the increased maximum Speed.")
            finalRatings.Speed = CInt(speed)
            finalRatings.Acceleration = CInt(If(finalRatings.Acceleration, If(upgrades?.Acceleration, plant.StartingAcceleration)).GetValueOrDefault() + CLng(levels))
            finalRatings.Signature = CInt(If(upgrades?.Signature, plant.Signature).GetValueOrDefault() - CLng(levels))
            Dim economy = If(finalRatings.Economy, If(upgrades?.Economy, If(plant.StartingEconomyKmPerLiter,
                If(plant.StartingEconomyKmPerPowerFactor, plant.StartingEconomyKmPerBar))))
            If levels >= 20 Then Throw New ArgumentException("Turbocharging must leave positive Economy.")
            finalRatings.Economy = economy * (1D - 0.05D * levels)
        End If
        Dim allocations = validated.Where(Function(choice) choice.Rule.Id.StartsWith("drive-by-wire-", StringComparison.Ordinal)).ToArray()
        If allocations.Length > 0 AndAlso Not seen.Contains("drive-by-wire") Then Throw New ArgumentException("Bonus allocation requires a drive-by-wire system.")
        If seen.Contains("drive-by-wire") AndAlso allocations.Sum(Function(choice) choice.Quantity) <> 10 Then Throw New ArgumentException("Allocate exactly ten percent of drive-by-wire bonus across Speed, Acceleration, and Load.")
        If seen.Contains("drive-by-wire") Then
            Dim level = validated.Single(Function(choice) choice.Rule.Id = "drive-by-wire").Quantity
            If finalRatings Is Nothing Then finalRatings = New ModifiedRatings()
            finalRatings.Handling = If(finalRatings.Handling, If(upgrades?.Handling, chassis.Handling)) - level
            finalRatings.OnRoadHandling = If(finalRatings.OnRoadHandling, If(upgrades?.OnRoadHandling, chassis.OnRoadHandling)) - level
            finalRatings.OffRoadHandling = If(finalRatings.OffRoadHandling, If(upgrades?.OffRoadHandling, chassis.OffRoadHandling)) - level
            For Each allocation In allocations
                Dim multiplier = 1D + allocation.Quantity / 100D
                Select Case allocation.Rule.Id
                    Case "drive-by-wire-speed-allocation"
                        finalRatings.Speed = CInt(Math.Floor(If(finalRatings.Speed, If(upgrades?.Speed, plant.StartingSpeed)).GetValueOrDefault() * multiplier))
                    Case "drive-by-wire-acceleration-allocation"
                        finalRatings.Acceleration = CInt(Math.Floor(If(finalRatings.Acceleration, If(upgrades?.Acceleration, plant.StartingAcceleration)).GetValueOrDefault() * multiplier))
                    Case "drive-by-wire-load-allocation"
                        design.LoadCapacityKg *= multiplier
                End Select
            Next
        End If
        Dim mountPoints = validated.Sum(Function(choice) CLng(choice.Rule.MountPoints) * choice.Quantity)
        If mountPoints > chassis.Body.GetValueOrDefault() Then Throw New ArgumentException("Weapon mounts exceed the vehicle's Body-based hardpoint and firmpoint limit.")
        Dim fixedAndTurretCount = validated.Where(Function(choice) choice.Rule.Id.Contains("fixed-") OrElse choice.Rule.Id.Contains("turret")).
            Sum(Function(choice) CLng(choice.Quantity))
        Dim smartlinks = validated.Where(Function(choice) choice.Rule.Id.StartsWith("smartlink-integration-", StringComparison.Ordinal)).
            Sum(Function(choice) CLng(choice.Quantity))
        If smartlinks > fixedAndTurretCount Then Throw New ArgumentException("Smartlink integrations require a separate fixed mount or turret for each installation.")
        If chassis.Category = "Car" AndAlso Not chassis.IsDrone AndAlso
            validated.Any(Function(choice) choice.Rule.Id.Contains("turret")) AndAlso Not seen.Contains("roll-bar") Then
            Throw New ArgumentException("A crewed ground vehicle requires roll bars for turret installation.")
        End If
        Dim pilot = If(finalRatings?.Pilot, chassis.Pilot.GetValueOrDefault())
        If (seen.Contains("autosoft-interpretation") OrElse seen.Contains("battletac-fddm") OrElse seen.Contains("battletac-ivis")) AndAlso pilot < 1 Then
            Throw New ArgumentException("Drone software systems require a Pilot rating or remote-control interface.")
        End If
        If seen.Contains("smart-armor") AndAlso If(finalRatings?.Armor, chassis.Armor.GetValueOrDefault()) < 1 Then
            Throw New ArgumentException("Smart armor requires vehicle Armor of at least 1.")
        End If
        If seen.Contains("chameleon-paint") AndAlso (seen.Contains("smart-armor") OrElse seen.Contains("radar-absorbent-materials")) Then
            Throw New ArgumentException("Chameleon paint is incompatible with smart armor and radar-absorbent materials.")
        End If
        For Each choice In validated.Where(Function(item) item.Rule.Id = "radar-absorbent-materials" OrElse item.Rule.Id = "thermal-baffles")
            If finalRatings Is Nothing Then finalRatings = New ModifiedRatings()
            Dim signature = If(finalRatings.Signature, If(upgrades?.Signature, plant.Signature))
            If Not signature.HasValue Then Throw New ArgumentException("Signature enhancement requires a base Signature rating.")
            If choice.Rule.Id = "radar-absorbent-materials" Then
                finalRatings.RadarSignature = signature.Value + choice.Quantity
            Else
                finalRatings.InfraredSignature = signature.Value + choice.Quantity
            End If
        Next
        Dim externalMounts = validated.Any(Function(choice) choice.Rule.Id.StartsWith("external-fixed-", StringComparison.Ordinal))
        Dim turretPenalty = validated.Where(Function(choice) choice.Rule.Id.Contains("turret")).Sum(Function(choice) CLng(choice.Quantity))
        If externalMounts OrElse turretPenalty > 0 Then
            If finalRatings Is Nothing Then finalRatings = New ModifiedRatings()
            finalRatings.Signature = CInt(If(finalRatings.Signature, If(upgrades?.Signature, plant.Signature)).GetValueOrDefault() - turretPenalty - If(externalMounts, 1, 0))
            If finalRatings.RadarSignature.HasValue Then finalRatings.RadarSignature -= CInt(turretPenalty) + If(externalMounts, 1, 0)
            If finalRatings.InfraredSignature.HasValue Then finalRatings.InfraredSignature -= CInt(turretPenalty) + If(externalMounts, 1, 0)
        End If
        Dim result = validated.Select(Function(choice) Price(choice.Rule, choice.Quantity, chassis, plant, sensorRating, retransmissionUnits, armCount, pilot)).ToList()
        design.Additions = design.Additions.Concat(result).ToList()
        Return result
    End Function

    Private Sub ApplyRating(ratings As ModifiedRatings, id As String, quantity As Integer, chassis As StockChassis, plant As StockPowerPlant,
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
            Case "dipping-sonar"
                ratings.DippingSonar = quantity
                Return
            Case "ecm"
                ratings.ECM = quantity
                Return
            Case "eccm"
                ratings.ECCM = quantity
                Return
            Case "electronic-deception"
                ratings.ElectronicDeception = quantity
                Return
            Case "electronic-counter-deception"
                ratings.ElectronicCounterDeception = quantity
                Return
            Case "autonav"
                ratings.Autonav = quantity
                Return
            Case "advanced-drone-pilot"
                ratings.Pilot = quantity
                Return
            Case "aircraft-flotation"
                Dim isRotorCraft = chassis.Category = "Rotor Craft"
                Dim speedFactor = If(isRotorCraft, 0.9D, 0.5D)
                ratings.Speed = CInt(Math.Floor(If(upgrades?.Speed, plant.StartingSpeed).Value * speedFactor))
                Dim economy = If(upgrades?.Economy, If(plant.StartingEconomyKmPerLiter,
                    If(plant.StartingEconomyKmPerPowerFactor, plant.StartingEconomyKmPerBar)))
                ratings.Economy = If(isRotorCraft, economy, economy * 1.2D)
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

    Private Function Price(rule As ModificationRule, quantity As Integer, chassis As StockChassis, plant As StockPowerPlant,
                           sensorRating As Integer, retransmissionUnits As Integer, armCount As Integer, pilot As Integer) As DesignAddition
        Dim body = CDec(chassis.Body.GetValueOrDefault())
        Dim item As New DesignAddition With {.Name = rule.Name, .CargoConsumed = rule.CargoPerUnit * quantity,
            .DesignPoints = rule.PointsPerUnit * quantity, .LoadConsumedKg = rule.LoadPerUnit * quantity}
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
            Case "winch"
                item.DesignPoints = quantity / 100D
                item.LoadConsumedKg = body * 2.5D
            Case "generator"
                item.DesignPoints = chassis.DesignPoints / 4D
                item.CargoConsumed = chassis.StartingCargo / 3D
                item.LoadConsumedKg = plant.StartingLoadKg / 2D
            Case "medical-clinic"
                Dim factor = quantity + 4D
                item.DesignPoints = factor * factor * factor * 3D
                item.CargoConsumed = 52D
                item.LoadConsumedKg = 250D
            Case "valkyrie-module"
                item.DesignPoints = 3000D * quantity
                item.CargoConsumed = 10D * quantity
                item.LoadConsumedKg = 400D * quantity
            Case "bucket-seat"
                item.LoadConsumedKg = 100D * quantity
            Case "bench-seat", "folding-bench-seat"
                item.LoadConsumedKg = 150D * quantity
            Case "reinforced-bucket-seat"
                item.DesignPoints = 5D * quantity
                item.LoadConsumedKg = 250D * quantity
            Case "reinforced-bench-seat"
                item.DesignPoints = 5D * quantity
                item.LoadConsumedKg = 300D * quantity
            Case "ejection-bucket-seat"
                item.DesignPoints = 35D * quantity
                item.LoadConsumedKg = 100D * quantity
            Case "reinforced-ejection-bucket-seat"
                item.DesignPoints = 70D * quantity
                item.LoadConsumedKg = 250D * quantity
            Case "crane"
                item.DesignPoints = quantity / 50D
                item.CargoConsumed = 15D
                item.LoadConsumedKg = body * 80D
            Case "aircraft-flotation"
                item.DesignPoints = chassis.DesignPoints * 0.2D
            Case "mechanical-arm"
                item.DesignPoints = body * 100D * quantity
                item.CargoConsumed = 2D * quantity
                item.LoadConsumedKg = body * body * 10D * quantity
            Case "mechanical-arm-strength"
                item.DesignPoints = If(quantity <= 3, 60D, 75D) * body * quantity * armCount
                item.LoadConsumedKg = 10D * quantity * armCount
                item.Name = $"Mechanical arm enhancement (Strength {body * body + quantity} per arm)"
            Case "drone-rack-body-0", "drone-rack-body-1", "drone-rack-body-2", "drone-rack-body-3"
                item.DesignPoints = body * 12D * quantity
                item.LoadConsumedKg = 45D * quantity
            Case "cabin-gas-seal"
                item.DesignPoints = body * 3D
            Case "cabin-water-seal"
                item.DesignPoints = body * 10D
            Case "engine-water-seal"
                item.DesignPoints = body * 15D
            Case "cabin-overpressure"
                item.DesignPoints = body * 75D
                item.CargoConsumed = 1D
                item.LoadConsumedKg = 10D
            Case "life-support"
                item.DesignPoints = 5D + 10D * quantity
                item.CargoConsumed = quantity
                item.LoadConsumedKg = 25D * quantity
            Case "suncell"
                item.DesignPoints = 5D
            Case "gridlink"
                item.DesignPoints = 2D
            Case "adjusted-controls"
                item.DesignPoints = If(quantity = 1, 25D, If(quantity = 2, 35D, 30D))
            Case "secondary-controls"
                item.DesignPoints = 5D
            Case "contingency-maneuver-controls"
                item.DesignPoints = If(quantity <= 3, 35D, If(quantity <= 6, 75D, 150D)) * quantity
                item.LoadConsumedKg = 25D
            Case "passenger-protection"
                item.DesignPoints = 30D * quantity
            Case "motorbike-gyro"
                item.DesignPoints = chassis.DesignPoints * 0.25D
                item.LoadConsumedKg = 5D
            Case "autonav"
                item.DesignPoints = autonavPoints(quantity)
            Case "remote-control-interface"
                item.DesignPoints = 25D * body
            Case "advanced-drone-pilot"
                item.DesignPoints = pilotPoints(quantity)
            Case "ballast-tanks"
                Dim squaredBody = body * body
                item.DesignPoints = squaredBody * 10D
                item.CargoConsumed = squaredBody * 2D
                item.LoadConsumedKg = squaredBody * 25D
            Case "sidecar"
                item.DesignPoints = If(quantity = 1, 10D, If(quantity = 2, 20D, 35D))
            Case "runflat-upgrade"
                item.DirectCostNuyen = 200D * quantity
            Case "turbocharging"
                item.DesignPoints = plant.DesignPoints * 0.75D * quantity
            Case "ecm"
                item.DesignPoints = ecmPoints(quantity)
                item.CargoConsumed = sensorCargo(quantity)
                item.LoadConsumedKg = ecmLoad(quantity)
            Case "eccm"
                item.DesignPoints = eccmPoints(quantity)
                item.CargoConsumed = sensorCargo(quantity)
                item.LoadConsumedKg = eccmLoad(quantity)
            Case "electronic-deception"
                item.DesignPoints = edPoints(quantity)
                item.CargoConsumed = edCargo(quantity)
                item.LoadConsumedKg = edLoad(quantity)
            Case "electronic-counter-deception"
                item.DesignPoints = ecdPoints(quantity)
                item.CargoConsumed = ecdCargo(quantity)
                item.LoadConsumedKg = ecdLoad(quantity)
            Case "radar-absorbent-materials"
                Dim levels = CDec(quantity)
                item.DesignPoints = levels * levels * levels * 50D
            Case "thermal-baffles"
                item.DesignPoints = 75D * quantity
                item.LoadConsumedKg = body * 50D * quantity
            Case "noisemaker-dispenser"
                item.DesignPoints = 350D
                item.CargoConsumed = 16D + 8D * quantity
                item.LoadConsumedKg = 150D + 25D * quantity
            Case "smart-armor"
                item.DesignPoints = 250D
                item.CargoConsumed = 2D
                item.LoadConsumedKg = body * 50D
            Case "chameleon-paint"
                item.DesignPoints = 50D
            Case "improved-suspension"
                item.DesignPoints = chassis.DesignPoints * 0.25D * quantity
            Case "off-road-suspension"
                item.DesignPoints = chassis.DesignPoints * 0.5D * quantity
            Case "improved-control-surfaces"
                item.DesignPoints = chassis.DesignPoints * 0.4D * quantity
            Case "autosoft-interpretation"
                item.DesignPoints = pilot * 50D
                item.CargoConsumed = 1D
            Case "ccss-integration"
                item.DesignPoints = 5000D * quantity
            Case "dipping-sonar"
                item.DesignPoints = 250D * quantity
                item.LoadConsumedKg = 50D
            Case "engine-customization-speed", "engine-customization-acceleration", "engine-customization-load"
                item.DesignPoints = plant.DesignPoints * (0.25D + 0.5D * (quantity - 1D))
            Case "drive-by-wire"
                item.DesignPoints = chassis.DesignPoints * 0.75D * quantity
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
