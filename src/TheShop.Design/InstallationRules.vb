Public Module InstallationRules
    Public Sub Describe(result As RetrofitQuote, rule As ModificationRule, quantity As Integer, chassis As StockChassis, Optional context As IReadOnlyList(Of ModificationSelection) = Nothing)
        result.EquipmentRequired = "Appropriate vehicle facility"
        result.InstallationTest = "Appropriate Vehicle B/R; see source for target number"
        Dim body = VehicleScale.RuleBody(chassis)
        Dim q = CDec(quantity)
        Dim choices = If(context, Array.Empty(Of ModificationSelection)())
        Dim pilot = Math.Max(chassis.Pilot.GetValueOrDefault(), choices.Where(Function(item) item.Id = "advanced-drone-pilot" OrElse item.Id = "robotic-pilot").Select(Function(item) item.Quantity).DefaultIfEmpty(0).Max())
        If DesignEnhancements.Has(choices, "remote-control-interface") Then pilot = Math.Max(1, pilot)
        Select Case rule.Id
            Case "engine-customization-speed", "engine-customization-secondary-speed", "engine-customization-acceleration", "engine-customization-load"
                result.SourcePage = 125 : result.InstallationBaseHours = 40D * q
                result.InstallationTest = $"Appropriate Vehicle B/R ({quantity + If(chassis.Category.Contains("Aircraft") OrElse chassis.Category = "Rotor Craft", 5, If(chassis.IsDrone OrElse chassis.Category = "Hovercraft", 4, 3))}); GM resolves the separate secret reliability test"
            Case "gridlink"
                result.SourcePage = 126 : result.InstallationBaseHours = 16D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "nitrous"
                result.SourcePage = 126 : result.InstallationBaseHours = q + 47D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "suncell"
                result.SourcePage = 126 : result.EquipmentRequired = "Vehicle shop" : result.InstallationBaseHours = 8D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "turbocharging"
                result.SourcePage = 126 : result.InstallationBaseHours = 8D * q
                result.InstallationTest = $"Appropriate Vehicle B/R ({quantity + If(chassis.Category.Contains("Aircraft") OrElse chassis.Category = "Rotor Craft", 4, If(chassis.IsDrone OrElse chassis.Category = "Hovercraft", 3, 2))})"
            Case "adjusted-controls"
                result.SourcePage = 127 : result.InstallationBaseHours = 40D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "advanced-drone-pilot"
                result.SourcePage = 127 : result.InstallationBaseHours = 64D : result.InstallationTest = "Computer B/R (8 − Handling; on-road for ground vehicles)"
            Case "autonav"
                result.SourcePage = 128 : result.InstallationBaseHours = If(quantity = 1, 16D, If(quantity = 2, 32D, If(quantity = 3, 40D, 48D))) : result.InstallationTest = "Appropriate Vehicle B/R (8 − Handling; on-road for ground vehicles)"
            Case "contingency-maneuver-controls"
                result.SourcePage = 128 : result.InstallationBaseHours = 80D : result.InstallationTest = "Appropriate Vehicle B/R (10 − Handling; on-road for ground vehicles)"
            Case "datajack-port"
                result.SourcePage = 128 : result.InstallationBaseHours = If(chassis.Category = "Bike", 112D, 56D) : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "drive-by-wire", "drive-by-wire-speed-allocation", "drive-by-wire-acceleration-allocation", "drive-by-wire-load-allocation"
                result.SourcePage = 129
                If rule.Id = "drive-by-wire" Then
                    result.InstallationBaseHours = body * 160D
                    result.InstallationTest = "Appropriate Vehicle B/R (10 − on-road Handling), Computer (4), Electronics (4); all three tests required"
                Else
                    result.InstallationTest = "Included in drive-by-wire installation"
                End If
            Case "improved-control-surfaces", "improved-suspension"
                result.SourcePage = 129 : result.InstallationBaseHours = 40D * q : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "motorbike-gyro"
                result.SourcePage = 129 : result.InstallationBaseHours = 48D : result.InstallationTest = "Bike B/R (5)"
            Case "off-road-suspension"
                result.SourcePage = 130 : result.InstallationBaseHours = 40D * q : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "remote-control-interface"
                result.SourcePage = 130 : result.InstallationBaseHours = 16D : result.InstallationTest = "Appropriate Vehicle B/R (4), Electronics B/R (4)"
            Case "rigger-adaptation", "secondary-controls"
                result.SourcePage = 130 : result.InstallationBaseHours = 40D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "ablative-armor"
                result.SourcePage = 131 : result.InstallationBaseHours = 6D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "passenger-protection"
                result.SourcePage = 131 : result.InstallationBaseHours = 40D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "vehicle-armor", "personal-armor"
                result.SourcePage = 131 : result.InstallationBaseHours = q * If(rule.Id = "personal-armor", 6D, 8D) : result.InstallationTest = "Appropriate Vehicle B/R (desired Armor ÷ 3" & If(rule.Id = "personal-armor", ", rounded up)", ")")
            Case "bulwark"
                result.SourcePage = 132 : result.EquipmentRequired = "Ship facility" : result.InstallationBaseHours = q * 16D * 24D : result.InstallationTest = "Appropriate Vehicle B/R (desired Bulwark ÷ 3)"
            Case "concealed-armor"
                result.SourcePage = 132 : result.InstallationBaseHours = 8D * q : result.InstallationTest = "Appropriate Vehicle B/R (desired Armor ÷ 3, rounded up)"
            Case "crash-cage"
                result.SourcePage = 132 : result.EquipmentRequired = "Vehicle shop" : result.InstallationBaseHours = 16D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "cabin-gas-seal", "cabin-water-seal", "engine-water-seal", "cabin-overpressure"
                result.SourcePage = 133 : result.InstallationBaseHours = 12D : result.InstallationTest = "Appropriate Vehicle B/R (3)"
            Case "life-support"
                result.SourcePage = 133 : result.InstallationBaseHours = 8D : result.EquipmentRequired = "Vehicle shop" : result.InstallationTest = "Appropriate Vehicle B/R (3)"
            Case "roll-bar"
                result.SourcePage = 133 : result.InstallationBaseHours = 24D : result.EquipmentRequired = "Vehicle shop" : result.InstallationTest = "Appropriate Vehicle B/R (3)"
            Case "smart-armor"
                result.SourcePage = 134 : result.InstallationBaseHours = body * 40D : result.InstallationTest = "Demolitions B/R (4), Electronics (4), Computer (4); all three tests required"
            Case "active-thermal-masking"
                result.SourcePage = 134 : result.InstallationBaseHours = q * 8D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "noisemaker-dispenser"
                result.SourcePage = 134 : result.InstallationBaseHours = 48D : result.EquipmentRequired = "Ship facility" : result.InstallationTest = "Submarine B/R (4)"
            Case "radar-absorbent-materials", "thermal-baffles"
                result.SourcePage = 135 : result.InstallationBaseHours = If(rule.Id = "radar-absorbent-materials", 12D, q * 8D)
                result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "ammo-bin-small-arms", "ammo-bin-machine-gun", "ammo-bin-heavy"
                result.SourcePage = 136 : result.InstallationBaseHours = 12D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "pistol-mount-conversion", "smg-mount-conversion", "rifle-mount-conversion", "heavy-mount-conversion"
                result.SourcePage = 136 : result.InstallationBaseHours = 12D * q : result.EquipmentRequired = "Appropriate Weapons kit" : result.InstallationTest = "Appropriate Weapon B/R (4)"
            Case "external-fixed-firmpoint", "external-fixed-hardpoint", "internal-fixed-firmpoint", "internal-fixed-hardpoint", "medium-launch-control", "heavy-launch-control"
                result.SourcePage = 137 : result.InstallationBaseHours = 24D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "gunnery-recoil-adjusters"
                Dim count = choices.Where(Function(item) item.Id.Contains("fixed-") OrElse item.Id.Contains("turret")).Sum(Function(item) CLng(item.Quantity))
                result.SourcePage = 137 : result.InstallationBaseHours = 24D * count : result.EquipmentRequired = "Vehicle kit" : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "external-missile-mounts", "external-missile-mounts-reinforced", "internal-missile-storage", "internal-heavy-missile-storage"
                result.SourcePage = 138 : result.InstallationBaseHours = 8D : result.EquipmentRequired = "Aircraft facility" : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "naval-weapons-control"
                result.SourcePage = 138 : result.InstallationBaseHours = 480D : result.EquipmentRequired = "Ship facility" : result.InstallationTest = "Electronics B/R (8)"
            Case "pintle-mount"
                result.SourcePage = 138 : result.InstallationBaseHours = 12D : result.EquipmentRequired = "Vehicle shop" : result.InstallationTest = "Appropriate Vehicle B/R (2)"
            Case "ring-mount", "ring-mount-door-gun"
                result.SourcePage = 139 : result.InstallationBaseHours = 8D : result.EquipmentRequired = "Vehicle shop" : result.InstallationTest = "Appropriate Vehicle B/R (3)"
            Case "smartlink-integration-1", "smartlink-integration-2"
                result.SourcePage = 139 : result.InstallationBaseHours = 24D : result.EquipmentRequired = "Vehicle kit" : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "torpedo-tube-manual", "torpedo-tube-autoloader"
                result.SourcePage = 139 : result.InstallationBaseHours = 300D : result.EquipmentRequired = "Ship facility" : result.InstallationTest = "Submarine B/R (6)"
            Case "torpedo-storage-rack"
                result.SourcePage = 139 : result.EquipmentRequired = "Ship facility" : result.InstallationTest = "Included in torpedo installation"
            Case "weapon-gyro-stabilization"
                result.SourcePage = 142 : result.InstallationBaseHours = 24D : result.EquipmentRequired = "Vehicle kit" : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "autosoft-interpretation"
                result.SourcePage = 142 : result.InstallationBaseHours = 72D : result.EquipmentRequired = "Electronics shop" : result.InstallationTest = "Electronics B/R (4)"
            Case "battletac-fddm", "battletac-ivis"
                result.SourcePage = If(rule.Id = "battletac-fddm", 142, 143) : result.InstallationBaseHours = 64D * pilot : result.EquipmentRequired = "Microtronics shop" : result.InstallationTest = "Computer B/R (4)"
            Case "ccss-integration"
                result.SourcePage = 143 : result.InstallationBaseHours = 120D : result.EquipmentRequired = "Ship facility" : result.InstallationTest = "Computer B/R (6), Electronics B/R (6)"
            Case "dipping-sonar"
                result.SourcePage = 143 : result.InstallationBaseHours = 16D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "ecm", "eccm", "electronic-deception", "electronic-counter-deception"
                result.SourcePage = 145 : result.InstallationBaseHours = 16D * q : result.InstallationTest = "Electronics B/R (4)"
            Case "electronics-port"
                result.SourcePage = 146 : result.InstallationBaseHours = 8D : result.EquipmentRequired = "Vehicle shop" : result.InstallationTest = "Electronics B/R (3)"
            Case "magnetic-anomaly-detector", "over-the-horizon-sensors"
                Dim mad = rule.Id = "magnetic-anomaly-detector"
                result.SourcePage = 146 : result.InstallationBaseHours = If(mad, 16D, 40D) : result.EquipmentRequired = If(mad, "Vehicle facility", "Ship facility") : result.InstallationTest = If(mad, "Electronics B/R (8)", "Electronics B/R (6)")
            Case "power-amplifier", "retransmission-power-amplifier"
                result.SourcePage = 147 : result.InstallationBaseHours = 8D : result.EquipmentRequired = "Vehicle shop" : result.InstallationTest = "Electronics B/R (4)"
                If rule.Id = "retransmission-power-amplifier" Then result.InstallationBaseHours *= choices.Where(Function(item) item.Id = "retransmission-unit").Sum(Function(item) CLng(item.Quantity))
            Case "remote-control-encryption"
                result.SourcePage = 147 : result.InstallationBaseHours = 1D : result.EquipmentRequired = "Electronics kit" : result.InstallationTest = "Electronics B/R (4)"
            Case "retransmission-unit"
                result.SourcePage = 147 : result.InstallationBaseHours = 64D : result.EquipmentRequired = "Microtronics shop" : result.InstallationTest = "Electronics B/R (4)"
            Case "sensors", "sonar"
                result.SourcePage = 148 : result.InstallationBaseHours = If(rule.Id = "sensors", 16D, 40D) * q : result.InstallationTest = If(rule.Id = "sensors", "Electronics B/R (4)", "Electronics B/R (6)")
            Case "towed-array-sonar"
                result.SourcePage = 148 : result.InstallationBaseHours = 16D : result.EquipmentRequired = "Ship facility" : result.InstallationTest = "Ship or Submarine B/R (6)"
            Case "aircraft-drop-tanks"
                result.SourcePage = 149 : result.InstallationBaseHours = q / 6D : result.InstallationTest = "Ten minutes to mount each pair" : result.EquipmentRequired = "Not specified"
            Case "amphibious-operation"
                result.SourcePage = 149 : result.InstallationBaseHours = If(quantity = 1, 32D, If(quantity = 2, 40D, 80D)) : result.InstallationTest = $"Appropriate Vehicle B/R ({quantity + 3})"
            Case "improved-anti-theft-security", "anti-theft-electric-system", "proximity-alert"
                result.SourcePage = 150 : result.InstallationBaseHours = 40D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "convertible-top"
                result.SourcePage = 150 : result.InstallationBaseHours = 24D : result.EquipmentRequired = "Vehicle shop" : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "aircraft-flotation", "hovercraft-water-seal"
                result.SourcePage = 152 : result.InstallationBaseHours = 32D : result.InstallationTest = "Appropriate Vehicle B/R (3)"
            Case "crane", "winch"
                result.SourcePage = If(rule.Id = "crane", 151, 155) : result.InstallationBaseHours = body * 16D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "drone-rack-body-0", "drone-rack-body-1", "drone-rack-body-2", "drone-rack-body-3"
                result.SourcePage = 151 : result.InstallationBaseHours = body * 8D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "ejection-bucket-seat", "reinforced-ejection-bucket-seat"
                result.SourcePage = 151 : result.InstallationBaseHours = 16D * q : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "external-cargo-mounts"
                result.SourcePage = 151 : result.InstallationBaseHours = 8D : result.EquipmentRequired = "Vehicle shop" : result.InstallationTest = "Appropriate Vehicle B/R (3)"
            Case "generator"
                result.SourcePage = 152 : result.InstallationBaseHours = 16D : result.EquipmentRequired = "Vehicle shop" : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "mechanical-arm", "mechanical-arm-strength"
                result.SourcePage = 152 : result.InstallationBaseHours = 40D : result.InstallationTest = "Cybertechnology B/R (6)"
                If rule.Id = "mechanical-arm-strength" Then result.InstallationBaseHours *= choices.Where(Function(item) item.Id = "mechanical-arm").Sum(Function(item) CLng(item.Quantity))
            Case "medical-clinic"
                result.SourcePage = 153 : result.InstallationBaseHours = 24D * q : result.EquipmentRequired = "Vehicle shop, Electronics shop" : result.InstallationTest = $"Appropriate Vehicle B/R ({quantity}), Electronics B/R ({quantity})"
            Case "chameleon-paint"
                result.SourcePage = 153 : result.InstallationBaseHours = 72D : result.EquipmentRequired = "Vehicle shop" : result.InstallationTest = "Electronics (6)"
            Case "bucket-seat", "bench-seat", "folding-bench-seat", "reinforced-bucket-seat", "reinforced-bench-seat", "seat-armor", "sidecar", "spotlight"
                result.SourcePage = 154
                If rule.QuantityLabel = "seats" Then result.InstallationBaseHours = 4D * q
                result.EquipmentRequired = "Vehicle shop" : result.InstallationTest = "Appropriate Vehicle B/R (4)"
                If rule.Id = "sidecar" Then
                    result.InstallationBaseHours = 0.25D : result.EquipmentRequired = "Not specified" : result.InstallationTest = "15 minutes to install or remove"
                ElseIf rule.Id = "spotlight" Then
                    result.InstallationBaseHours = 24D
                ElseIf rule.Id = "seat-armor" Then
                    result.InstallationBaseHours = 4D
                End If
            Case "valkyrie-module"
                result.SourcePage = 155 : result.InstallationBaseHours = 120D : result.EquipmentRequired = "Vehicle shop, Electronics shop" : result.InstallationTest = "Appropriate Vehicle B/R (5), Electronics B/R (5)"
            Case "standard-tires", "performance-tires", "off-road-tires", "dual-purpose-tires", "runflat-upgrade", "spare-standard-tires", "spare-performance-tires", "spare-off-road-tires", "spare-dual-purpose-tires"
                result.SourcePage = 155 : result.EquipmentRequired = "Standard tool kit" : result.InstallationTest = "No installation time or test specified"
            Case Else
                result.SourcePage = If(rule.Id.Contains("turret"), If(rule.Id.StartsWith("remote", StringComparison.Ordinal), 141, 140), 155)
                If rule.Id.Contains("turret") Then
                    result.SourcePage = 141 : result.InstallationBaseHours = 72D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
                End If
        End Select
        If result.InstallationBaseHours.HasValue AndAlso {"units", "mounts", "ports", "lights", "racks"}.Contains(rule.QuantityLabel) Then result.InstallationBaseHours *= q
    End Sub
End Module
