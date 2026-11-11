Public Module InstallationRules
    Public Sub Describe(result As RetrofitQuote, rule As ModificationRule, quantity As Integer, chassis As StockChassis)
        result.EquipmentRequired = "Appropriate vehicle facility"
        result.InstallationTest = "Appropriate Vehicle B/R; see source for target number"
        Dim body = VehicleScale.RuleBody(chassis)
        Dim q = CDec(quantity)
        Select Case rule.Id
            Case "engine-customization-speed", "engine-customization-acceleration", "engine-customization-load"
                result.SourcePage = 125 : result.InstallationBaseHours = 40D * q
                result.InstallationTest = $"Appropriate Vehicle B/R ({quantity + If(chassis.Category.Contains("Aircraft") OrElse chassis.Category = "Rotor Craft", 5, If(chassis.IsDrone OrElse chassis.Category = "Hovercraft", 4, 3))}); GM resolves the separate secret reliability test"
            Case "gridlink"
                result.SourcePage = 126
            Case "nitrous"
                result.SourcePage = 126 : result.InstallationBaseHours = q + 47D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "suncell"
                result.SourcePage = 126 : result.EquipmentRequired = "Vehicle shop"
            Case "turbocharging"
                result.SourcePage = 126 : result.InstallationBaseHours = 8D * q
            Case "adjusted-controls"
                result.SourcePage = 127 : result.InstallationBaseHours = 40D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "advanced-drone-pilot"
                result.SourcePage = 127
            Case "autonav"
                result.SourcePage = 128 : result.InstallationBaseHours = If(quantity = 1, 16D, If(quantity = 2, 32D, If(quantity = 3, 40D, 48D))) : result.InstallationTest = "Appropriate Vehicle B/R (8 − Handling; on-road for ground vehicles)"
            Case "contingency-maneuver-controls"
                result.SourcePage = 128 : result.InstallationBaseHours = 80D : result.InstallationTest = "Appropriate Vehicle B/R (10 − Handling; on-road for ground vehicles)"
            Case "datajack-port"
                result.SourcePage = 128
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
                result.SourcePage = 130 : result.InstallationBaseHours = 16D
            Case "rigger-adaptation", "secondary-controls"
                result.SourcePage = 130 : result.InstallationBaseHours = 40D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "ablative-armor"
                result.SourcePage = 131 : result.InstallationBaseHours = 6D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "passenger-protection"
                result.SourcePage = 131 : result.InstallationBaseHours = 40D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "vehicle-armor", "personal-armor"
                result.SourcePage = 131 : result.InstallationBaseHours = q * If(rule.Id = "personal-armor", 6D, 8D) : result.InstallationTest = "Appropriate Vehicle B/R (desired Armor + 3)"
            Case "bulwark"
                result.SourcePage = 132 : result.EquipmentRequired = "Ship facility" : result.InstallationBaseHours = q * 16D * 24D : result.InstallationTest = "Appropriate Vehicle B/R (desired Bulwark + 3)"
            Case "concealed-armor", "crash-cage", "cabin-gas-seal", "cabin-water-seal", "engine-water-seal", "cabin-overpressure"
                result.SourcePage = 132
            Case "life-support"
                result.SourcePage = 133 : result.InstallationBaseHours = 8D : result.EquipmentRequired = "Vehicle shop" : result.InstallationTest = "Appropriate Vehicle B/R (3)"
            Case "roll-bar"
                result.SourcePage = 133 : result.InstallationBaseHours = 24D : result.EquipmentRequired = "Vehicle shop" : result.InstallationTest = "Appropriate Vehicle B/R (3)"
            Case "smart-armor", "active-thermal-masking", "noisemaker-dispenser"
                result.SourcePage = 134
                If rule.Id = "active-thermal-masking" Then result.InstallationBaseHours = q * 8D
            Case "radar-absorbent-materials", "thermal-baffles"
                result.SourcePage = 135 : result.InstallationBaseHours = If(rule.Id = "radar-absorbent-materials", 12D, q * 8D)
            Case "ammo-bin-small-arms", "ammo-bin-machine-gun", "ammo-bin-heavy", "pistol-mount-conversion", "smg-mount-conversion", "rifle-mount-conversion", "heavy-mount-conversion"
                result.SourcePage = 136 : result.InstallationBaseHours = 12D : result.InstallationTest = If(rule.Id.StartsWith("ammo", StringComparison.Ordinal), "Appropriate Vehicle B/R (4)", "Appropriate Weapon B/R (4)")
            Case "external-fixed-firmpoint", "external-fixed-hardpoint", "internal-fixed-firmpoint", "internal-fixed-hardpoint", "gunnery-recoil-adjusters", "medium-launch-control", "heavy-launch-control"
                result.SourcePage = 137 : result.InstallationBaseHours = 24D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "external-missile-mounts", "external-missile-mounts-reinforced", "internal-missile-storage", "internal-heavy-missile-storage"
                result.SourcePage = 138 : result.InstallationBaseHours = 8D : result.InstallationTest = "Appropriate Vehicle B/R (4)"
            Case "naval-weapons-control"
                result.SourcePage = 138 : result.InstallationBaseHours = 480D : result.EquipmentRequired = "Ship facility" : result.InstallationTest = "Electronics B/R (8)"
            Case "pintle-mount"
                result.SourcePage = 138 : result.InstallationBaseHours = 12D : result.EquipmentRequired = "Vehicle shop" : result.InstallationTest = "Appropriate Vehicle B/R (2)"
            Case "ring-mount", "ring-mount-door-gun", "smartlink-integration-1", "smartlink-integration-2", "torpedo-tube-manual", "torpedo-tube-autoloader", "torpedo-storage-rack"
                result.SourcePage = 139
                If rule.Id.StartsWith("torpedo", StringComparison.Ordinal) Then result.InstallationBaseHours = 300D * q
            Case "weapon-gyro-stabilization", "autosoft-interpretation", "battletac-fddm"
                result.SourcePage = 142
            Case "battletac-ivis", "ccss-integration", "dipping-sonar"
                result.SourcePage = 143
            Case "ecm", "eccm", "electronic-deception", "electronic-counter-deception"
                result.SourcePage = 145 : result.InstallationBaseHours = 16D * q : result.InstallationTest = "Electronics B/R (4)"
            Case "electronics-port"
                result.SourcePage = 146
            Case "power-amplifier", "retransmission-power-amplifier", "remote-control-encryption", "retransmission-unit"
                result.SourcePage = 147
                If rule.Id = "power-amplifier" OrElse rule.Id = "retransmission-power-amplifier" Then result.InstallationBaseHours = 8D
            Case "sensors", "sonar", "towed-array-sonar"
                result.SourcePage = 148
            Case "aircraft-drop-tanks"
                result.SourcePage = 149 : result.InstallationBaseHours = q / 6D : result.InstallationTest = "Ten minutes to mount each pair"
            Case "amphibious-operation"
                result.SourcePage = 149
            Case "improved-anti-theft-security", "anti-theft-electric-system", "proximity-alert"
                result.SourcePage = 150
            Case "aircraft-flotation", "crane", "drone-rack-body-0", "drone-rack-body-1", "drone-rack-body-2", "drone-rack-body-3", "ejection-bucket-seat", "reinforced-ejection-bucket-seat", "external-cargo-mounts"
                result.SourcePage = 151
            Case "generator", "hovercraft-water-seal", "mechanical-arm", "mechanical-arm-strength"
                result.SourcePage = 152
            Case "medical-clinic", "chameleon-paint"
                result.SourcePage = 153
            Case "bucket-seat", "bench-seat", "folding-bench-seat", "reinforced-bucket-seat", "reinforced-bench-seat", "seat-armor", "sidecar", "spotlight"
                result.SourcePage = 154
                If rule.QuantityLabel = "seats" Then result.InstallationBaseHours = 4D * q
                If rule.Id = "sidecar" Then result.InstallationBaseHours = 0.25D
            Case Else
                result.SourcePage = If(rule.Id.Contains("turret"), If(rule.Id.StartsWith("remote", StringComparison.Ordinal), 141, 140), 155)
                If rule.Id.Contains("turret") Then result.InstallationBaseHours = 72D
        End Select
        If result.InstallationBaseHours.HasValue AndAlso {"units", "mounts", "ports", "lights", "racks"}.Contains(rule.QuantityLabel) Then result.InstallationBaseHours *= q
    End Sub
End Module
