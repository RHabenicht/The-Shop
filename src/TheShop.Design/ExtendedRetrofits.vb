Public Module ExtendedRetrofits
    Private ReadOnly ids As New HashSet(Of String)(StringComparer.Ordinal) From {
        "personal-armor", "ablative-armor", "vehicle-armor", "concealed-armor", "bulwark", "rigger-adaptation", "nitrous", "suncell", "turbocharging", "convertible-top",
        "adjusted-controls", "secondary-controls", "contingency-maneuver-controls", "motorbike-gyro", "remote-control-interface", "drive-by-wire",
        "drive-by-wire-speed-allocation", "drive-by-wire-acceleration-allocation", "drive-by-wire-load-allocation", "improved-control-surfaces", "improved-suspension", "off-road-suspension",
        "passenger-protection", "cabin-overpressure", "smart-armor", "active-thermal-masking", "thermal-baffles", "radar-absorbent-materials", "noisemaker-dispenser", "chameleon-paint",
        "engine-customization-speed", "engine-customization-secondary-speed", "engine-customization-acceleration", "engine-customization-load", "reinforced-bench-seat", "mechanical-arm", "mechanical-arm-strength",
        "autosoft-interpretation", "towed-array-sonar", "power-amplifier", "remote-control-encryption", "retransmission-power-amplifier", "aircraft-flotation", "generator",
        "drone-rack-body-0", "drone-rack-body-1", "drone-rack-body-2", "drone-rack-body-3", "aircraft-drop-tanks", "amphibious-operation", "seat-armor", "magnetic-anomaly-detector", "over-the-horizon-sensors",
        "standard-tires", "performance-tires", "off-road-tires", "dual-purpose-tires", "runflat-upgrade", "spare-standard-tires", "spare-performance-tires", "spare-off-road-tires", "spare-dual-purpose-tires",
        "external-missile-mounts", "external-missile-mounts-reinforced", "internal-missile-storage", "internal-heavy-missile-storage", "naval-weapons-control", "torpedo-tube-manual", "torpedo-tube-autoloader", "torpedo-storage-rack",
        "ring-mount-door-gun", "gunnery-recoil-adjusters", "weapon-gyro-stabilization", "pistol-mount-conversion", "smg-mount-conversion", "rifle-mount-conversion", "heavy-mount-conversion", "sidecar", "ammo-bin-small-arms", "ammo-bin-machine-gun", "ammo-bin-heavy"
    }
    Public Function Contains(id As String) As Boolean
        Return ids.Contains(id) OrElse id.Contains("turret")
    End Function
    Public Function Quote(rule As ModificationRule, q As Integer, chassis As StockChassis, plant As StockPowerPlant,
                          listCost As Decimal, selections As IReadOnlyList(Of ModificationSelection)) As RetrofitQuote
        Dim body = VehicleScale.RuleBody(chassis)
        Dim context = If(selections, Array.Empty(Of ModificationSelection)())
        Dim pilot = Math.Max(chassis.Pilot.GetValueOrDefault(), context.Where(Function(item) {"advanced-drone-pilot", "robotic-pilot"}.Contains(item.Id)).Select(Function(item) item.Quantity).DefaultIfEmpty(0).Max())
        If DesignEnhancements.Has(context, "remote-control-interface") Then pilot = Math.Max(pilot, 1)
        Dim armor = chassis.Armor.GetValueOrDefault() + context.Where(Function(item) item.Id = "vehicle-armor" OrElse item.Id = "concealed-armor").Sum(Function(item) item.Quantity)
        Dim result As New RetrofitQuote With {.StreetIndex = 1D, .Availability = "Same as vehicle (R3 Revised p. 124)"}
        Select Case rule.Id
            Case "magnetic-anomaly-detector", "over-the-horizon-sensors"
                Dim sensor = Math.Max(chassis.Sensor.GetValueOrDefault(), context.Where(Function(item) item.Id = "sensors").Select(Function(item) item.Quantity).DefaultIfEmpty(0).Max())
                If sensor < 1 Then Throw New ArgumentException("This installation requires a sensor system.")
                Dim mad = rule.Id = "magnetic-anomaly-detector"
                result.PartsCostNuyen = sensor * If(mad, 20000D, 100000D) : result.CargoConsumed = If(mad, 16D, 216D) : result.LoadConsumedKg = If(mad, 50D, 2500D)
                result.StreetIndex = If(mad, 4.5D, 5D) : result.Availability = If(mad, "8 / 21 days", "10 / 60 days")
            Case "convertible-top"
                result.PartsCostNuyen = listCost * 0.1D + 2500D : result.Availability = "4 / 72 hours"
            Case "engine-customization-speed", "engine-customization-secondary-speed", "engine-customization-acceleration", "engine-customization-load"
                result.PartsCostNuyen = listCost * 0.05D * q : result.StreetIndex = 2D : result.Availability = "8 / 14 days"
            Case "nitrous"
                result.PartsCostNuyen = If(q <= 3, 3500D, 7000D) * q : result.CargoConsumed = 2D : result.LoadConsumedKg = 15D : result.Availability = "4 / 48 hours"
            Case "suncell"
                result.PartsCostNuyen = 500D : result.CargoConsumed = 1D : result.Availability = "3 / 72 hours"
            Case "turbocharging"
                result.PartsCostNuyen = listCost * 0.1D * q : result.StreetIndex = 1.5D : result.Availability = "6 / 12 days"
            Case "adjusted-controls"
                result.PartsCostNuyen = If(q = 1, 2500D, If(q = 2, 3500D, 3000D)) : result.Availability = "3 / 72 hours"
            Case "secondary-controls"
                result.PartsCostNuyen = 400D : result.CargoConsumed = 2D : result.Availability = "3 / 72 hours"
            Case "contingency-maneuver-controls"
                result.PartsCostNuyen = If(q <= 3, 2500D, If(q <= 6, 5000D, 10000D)) * q : result.CargoConsumed = 2D : result.LoadConsumedKg = 30D : result.StreetIndex = 2D : result.Availability = "6 / 14 days"
            Case "drive-by-wire"
                result.PartsCostNuyen = listCost * 0.25D * q : result.StreetIndex = 2.5D : result.Availability = "8 / 16 days"
            Case "drive-by-wire-speed-allocation", "drive-by-wire-acceleration-allocation", "drive-by-wire-load-allocation"
                result.PartsCostNuyen = 0D : result.Availability = "Included with drive-by-wire"
            Case "improved-control-surfaces", "improved-suspension", "off-road-suspension", "motorbike-gyro"
                Dim transport = chassis.Id.Contains("transport") OrElse chassis.Id = "tractor"
                Dim rate = If(rule.Id = "improved-control-surfaces", 0.15D, If(rule.Id = "improved-suspension", 0.1D, If(rule.Id = "motorbike-gyro", 0.25D, If(chassis.Category = "Bike", 0.2D, If(transport, 0.5D, 0.35D)))))
                result.PartsCostNuyen = listCost * rate * q : result.StreetIndex = If(rule.Id = "motorbike-gyro", 1.5D, 2D) : result.Availability = If(rule.Id = "motorbike-gyro", "6 / 10 days", "6 / 12 days")
                If rule.Id = "motorbike-gyro" Then result.LoadConsumedKg = 5D
            Case "remote-control-interface"
                result.PartsCostNuyen = body * 2500D : result.StreetIndex = 2D : result.Availability = "4 / 7 days"
            Case "rigger-adaptation"
                result.PartsCostNuyen = 2800D : result.StreetIndex = 2D : result.Availability = "4 / 7 days"
            Case "passenger-protection"
                result.PartsCostNuyen = 2500D * q : result.CargoConsumed = 1D : result.Availability = "3 / 6 days"
            Case "personal-armor"
                result.PartsCostNuyen = 400D * q : result.LoadConsumedKg = body * 3D * q : result.StreetIndex = 1.5D : result.Availability = "7 / 10 days"
            Case "ablative-armor"
                result.PartsCostNuyen = If(q = 1, 700D, If(q = 2, 1600D, 2500D)) : result.LoadConsumedKg = body * 100D : result.StreetIndex = 2D : result.Availability = If(q = 1, "8 / 14 days", If(q = 2, "12 / 14 days", "14 / 21 days"))
            Case "vehicle-armor", "concealed-armor"
                result.PartsCostNuyen = If(rule.Id = "vehicle-armor", 1250D, 2000D) * q : result.LoadConsumedKg = body * body * 5D * q
                result.CargoConsumed = If(rule.Id = "concealed-armor", 3D * q, 0D) : result.StreetIndex = If(rule.Id = "concealed-armor", 3.5D, 2.5D) : result.Availability = If(rule.Id = "concealed-armor", "8 / 21 days", "6 / 12 days")
            Case "bulwark"
                result.PartsCostNuyen = body * 12500D * q : result.CargoConsumed = 10D * q : result.LoadConsumedKg = body * 500D * q : result.StreetIndex = 2.5D : result.Availability = "6 / 24 days"
            Case "cabin-overpressure"
                result.PartsCostNuyen = body * 5000D : result.CargoConsumed = 2D : result.LoadConsumedKg = 15D : result.StreetIndex = 2.5D : result.Availability = "8 / 14 days"
            Case "smart-armor"
                result.PartsCostNuyen = 20000D : result.CargoConsumed = armor * 3D : result.LoadConsumedKg = body * 50D : result.StreetIndex = Nothing : result.Availability = "10 / 28 days"
            Case "active-thermal-masking"
                Dim levels = context.Where(Function(item) item.Id.StartsWith("engine-customization-", StringComparison.Ordinal)).Sum(Function(item) CLng(item.Quantity))
                If q > levels Then Throw New ArgumentException("Active thermal masking requires at least as many engine customization levels.")
                result.PartsCostNuyen = listCost * 0.05D * levels * (2D + 0.25D * (q - 1D)) : result.CargoConsumed = 3D : result.LoadConsumedKg = 100D : result.StreetIndex = 2D : result.Availability = "8 / 21 days"
            Case "thermal-baffles"
                Dim rate = If(chassis.Category = "Bike", 6000D, If(chassis.Id = "tractor" OrElse chassis.Category = "Rotor Craft", 10000D, If(chassis.Category = "Hovercraft", 3750D, If(chassis.Category = "Boat" OrElse chassis.Category = "Ship" OrElse chassis.Category = "Submarine" OrElse chassis.Id = "zeppelin", 2500D, If(chassis.Id.Contains("transport") OrElse chassis.Category = "Fixed-Wing Aircraft", 7500D, 5000D)))))
                result.PartsCostNuyen = body * rate * q : result.CargoConsumed = 3D * q : result.LoadConsumedKg = body * 50D * q : result.StreetIndex = 2D : result.Availability = "6 / 14 days"
            Case "radar-absorbent-materials"
                result.PartsCostNuyen = CDec(q) * q * q * 25000D : result.StreetIndex = Nothing : result.Availability = "18 / 30 days"
            Case "noisemaker-dispenser"
                result.PartsCostNuyen = 350000D : result.CargoConsumed = 16D + 8D * q : result.LoadConsumedKg = 150D + 25D * q : result.StreetIndex = 4.5D : result.Availability = "8 / 21 days"
            Case "chameleon-paint"
                result.PartsCostNuyen = 5000D : result.StreetIndex = 1.5D : result.Availability = "6 / 14 days"
            Case "reinforced-bench-seat"
                result.PartsCostNuyen = 1500D * q : result.CargoConsumed = 8D * q : result.LoadConsumedKg = 300D * q : result.Availability = "3 / 48 hours"
            Case "mechanical-arm", "mechanical-arm-strength"
                Dim count = context.Where(Function(item) item.Id = "mechanical-arm").Sum(Function(item) CLng(item.Quantity))
                If rule.Id = "mechanical-arm-strength" AndAlso count < 1 Then Throw New ArgumentException("Arm enhancement requires a mechanical arm.")
                result.PartsCostNuyen = If(rule.Id = "mechanical-arm", body * 10000D * q, If(q <= 3, 5000D, 6000D) * body * q * count)
                result.CargoConsumed = If(rule.Id = "mechanical-arm", 4D * q, 0D) : result.LoadConsumedKg = If(rule.Id = "mechanical-arm", body * body * 10D * q, 10D * q * count) : result.Availability = "4 / 4 days"
            Case "autosoft-interpretation"
                If pilot < 1 Then Throw New ArgumentException("An autosoft system requires a Pilot rating or remote-control interface.")
                result.PartsCostNuyen = pilot * 1000D : result.CargoConsumed = 1D : result.LoadConsumedKg = 2D : result.StreetIndex = 2D : result.Availability = "8 / 14 days"
            Case "towed-array-sonar"
                result.PartsCostNuyen = q * 100000D : result.CargoConsumed = 175D : result.LoadConsumedKg = 1500D : result.StreetIndex = 4.5D : result.Availability = "10 / 45 days"
            Case "power-amplifier", "retransmission-power-amplifier"
                Dim count = If(rule.Id = "power-amplifier", 1L, context.Where(Function(item) item.Id = "retransmission-unit").Sum(Function(item) CLng(item.Quantity)))
                If count < 1 Then Throw New ArgumentException("A retransmission amplifier requires a retransmission unit.")
                result.PartsCostNuyen = 250D * q * count : result.CargoConsumed = Math.Ceiling(q * 0.5D) * count : result.LoadConsumedKg = q * count : result.StreetIndex = 1.5D : result.Availability = $"{q} / {q * 12} hours"
            Case "remote-control-encryption"
                result.PartsCostNuyen = q * 5000D : result.StreetIndex = 3D : result.Availability = $"{q} / {q} days"
            Case "aircraft-flotation"
                result.PartsCostNuyen = listCost * 0.1D + 2500D : result.LoadConsumedKg = 100D : result.StreetIndex = 2D : result.Availability = "4 / 7 days"
            Case "generator"
                result.PartsCostNuyen = chassis.DesignPoints * chassis.Markup * 100D / 4D : result.CargoConsumed = chassis.StartingCargo / 3D : result.LoadConsumedKg = plant.StartingLoadKg / 2D : result.Availability = "5 / 7 days"
            Case "drone-rack-body-0", "drone-rack-body-1", "drone-rack-body-2", "drone-rack-body-3"
                result.PartsCostNuyen = (listCost * 0.1D + 2500D) * q : result.CargoConsumed = (Math.Floor((Integer.Parse(rule.Id.Substring(rule.Id.Length - 1), Globalization.CultureInfo.InvariantCulture) + 1.5D) * Integer.Parse(rule.Id.Substring(rule.Id.Length - 1), Globalization.CultureInfo.InvariantCulture)) + 2D) * q : result.LoadConsumedKg = 45D * q : result.StreetIndex = 2D : result.Availability = "4 / 96 hours"
            Case "aircraft-drop-tanks"
                result.PartsCostNuyen = 8000D * q
            Case "amphibious-operation"
                result.PartsCostNuyen = If(q = 1, 2500D, If(q = 2, 7500D, 15000D)) : result.CargoConsumed = If(q = 1, 0D, If(q = 2, 2D, 4D)) : result.StreetIndex = If(q = 1, 1D, If(q = 2, 1.25D, 1.5D)) : result.Availability = If(q = 1, "3 / 6 days", If(q = 2, "5 / 10 days", "6 / 12 days"))
            Case "ammo-bin-small-arms", "ammo-bin-machine-gun", "ammo-bin-heavy"
                Dim mounts = context.Where(Function(item) item.Id.Contains("turret") OrElse item.Id.Contains("fixed-")).Sum(Function(item) CLng(item.Quantity))
                If mounts < 1 Then Throw New ArgumentException("Ammunition bins require a fixed mount or turret.")
                result.PartsCostNuyen = 50D * q : result.CargoConsumed = 0.2D * q : result.LoadConsumedKg = mounts : result.StreetIndex = Nothing : result.Availability = "Same as associated weapon mount"
            Case "seat-armor"
                result.PartsCostNuyen = 1250D * q : result.Availability = "3 / 48 hours"
            Case "runflat-upgrade"
                result.PartsCostNuyen = 200D * q
            Case "standard-tires", "performance-tires", "off-road-tires", "dual-purpose-tires", "spare-standard-tires", "spare-performance-tires", "spare-off-road-tires", "spare-dual-purpose-tires"
                Dim rate = If(rule.Id.Contains("performance"), 75D, If(rule.Id.Contains("off-road"), 125D, If(rule.Id.Contains("dual-purpose"), 250D, 50D)))
                result.PartsCostNuyen = body * rate * q
                If rule.Id.StartsWith("spare-", StringComparison.Ordinal) Then result.CargoConsumed = Math.Floor(body / 2D) * q
            Case "external-missile-mounts", "external-missile-mounts-reinforced"
                result.PartsCostNuyen = If(rule.Id.EndsWith("reinforced", StringComparison.Ordinal), 5000D, 1500D) * q : result.StreetIndex = 5D : result.Availability = "10 / 60 days"
            Case "internal-missile-storage", "internal-heavy-missile-storage"
                Dim heavy = rule.Id = "internal-heavy-missile-storage"
                result.CargoConsumed = If(heavy, 8D + 50D * q, 2D + 3D * q) : result.LoadConsumedKg = If(heavy, 500D, 0D)
                result.PartsCostNuyen = result.CargoConsumed * If(heavy, 50000D, 5000D) : result.StreetIndex = 5D : result.Availability = "10 / 60 days"
            Case "naval-weapons-control"
                result.PartsCostNuyen = q * 500000D : result.CargoConsumed = q * 100D : result.LoadConsumedKg = q * 500D : result.StreetIndex = Nothing : result.Availability = $"{q * 10} / {q * 10} days"
            Case "ring-mount-door-gun"
                result.PartsCostNuyen = 3000D * q : result.CargoConsumed = 17D * q : result.LoadConsumedKg = 25D * q : result.StreetIndex = 2D : result.Availability = "8 / 14 days"
            Case "gunnery-recoil-adjusters", "weapon-gyro-stabilization"
                Dim count = context.Where(Function(item) item.Id.Contains("fixed-") OrElse item.Id.Contains("turret")).Sum(Function(item) CLng(item.Quantity))
                If count < 1 Then Throw New ArgumentException("Weapon stabilization requires a fixed mount or turret.")
                Dim installations = If(rule.Id = "gunnery-recoil-adjusters", count, 1L)
                result.PartsCostNuyen = If(rule.Id = "gunnery-recoil-adjusters", 500D, 1000D) * q * installations : result.CargoConsumed = installations : result.LoadConsumedKg = (q + 24D) * installations : result.StreetIndex = 1.5D : result.Availability = If(rule.Id = "gunnery-recoil-adjusters", "6 / 48 hours", "8 / 72 hours")
            Case "pistol-mount-conversion", "smg-mount-conversion", "rifle-mount-conversion", "heavy-mount-conversion"
                result.PartsCostNuyen = If(rule.Id.StartsWith("pistol"), 150D, If(rule.Id.StartsWith("smg"), 350D, If(rule.Id.StartsWith("rifle"), 750D, 1000D))) * q : result.LoadConsumedKg = If(rule.Id.StartsWith("pistol"), 0D, If(rule.Id.StartsWith("smg"), 0.25D, If(rule.Id.StartsWith("rifle"), 0.5D, 1D))) * q
                result.StreetIndex = If(rule.Id.StartsWith("rifle") OrElse rule.Id.StartsWith("heavy"), 2.5D, 2D) : result.Availability = If(rule.Id.StartsWith("pistol"), "4 / 36 hours", If(rule.Id.StartsWith("smg"), "5 / 48 hours", If(rule.Id.StartsWith("rifle"), "6 / 72 hours", "16 / 14 days")))
            Case "sidecar"
                result.PartsCostNuyen = If(q = 1, 1000D, If(q = 2, 2000D, 3500D))
            Case "torpedo-tube-manual", "torpedo-tube-autoloader"
                Dim automatic = rule.Id = "torpedo-tube-autoloader"
                result.PartsCostNuyen = If(automatic, 150000D, 100000D) * q : result.CargoConsumed = 50D + 150D * q + If(automatic, 400D, 720D)
                result.LoadConsumedKg = 750D * q + If(automatic, 60000D, 0D) : result.StreetIndex = Nothing : result.Availability = "25 / 6 months"
            Case "torpedo-storage-rack"
                result.CargoConsumed = 32D * q : result.StreetIndex = Nothing : result.Availability = "Included in torpedo installation; ordnance purchased separately"
            Case Else
                If Not rule.Id.Contains("turret") Then Throw New ArgumentException("Missing revised retrofit specification.")
                Dim remote = rule.Id.StartsWith("remote-", StringComparison.Ordinal)
                Dim size = rule.Id.Split("-"c)(1)
                Dim index = Array.IndexOf({"micro", "mini", "small", "medium", "large", "extra"}, size)
                Dim costs = If(remote, {2500D, 6000D, 9000D, 17500D, 600000D, 2000000D}, {0D, 5000D, 7500D, 15000D, 300000D, 1000000D})
                Dim cargo = If(remote, {1D, 4D, 5D, 8D, 36D, 72D}, {0D, 7D, 8D, 24D, 72D, 144D})
                result.PartsCostNuyen = costs(index) : result.CargoConsumed = cargo(index) : result.LoadConsumedKg = rule.LoadPerUnit
                If rule.Id.Contains("anti-aircraft") Then
                    result.PartsCostNuyen *= 1.5D : result.CargoConsumed += 1D
                End If
                If rule.Id.EndsWith("pop-up", StringComparison.Ordinal) Then result.CargoConsumed *= 2D
                If rule.Id = "remote-micro-turret-pop-up" Then result.CargoConsumed = 1D
                result.StreetIndex = If(index <= 1, CType(2D, Decimal?), If(index = 2, CType(3D, Decimal?), Nothing))
                result.Availability = If(index >= 3, "Military only", If(remote, If(index = 0, "Same as vehicle (R3 Revised p. 124)", $"{listCost / 25D / 6D:0.##} / {listCost / 25D * 14D:0.##} days"), $"{result.PartsCostNuyen / 400D:0.##} / {result.PartsCostNuyen / 180D:0.##} days"))
                result.PartsCostNuyen *= q : result.CargoConsumed *= q : result.LoadConsumedKg *= q
        End Select
        Return result
    End Function
End Module
