Imports System.Globalization
Imports System.IO
Imports System.Text.Json
Imports System.Windows.Forms
Imports TheShop.Design
Imports TheShop.Desktop

Module Program
    <STAThread>
    Sub Main()
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US")
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        Dim folder = Directory.CreateTempSubdirectory("the-shop-smoke-")
        Try
            Dim destination = Path.Combine(folder.FullName, "design.json")
            Using editor As New DesignForm()
                editor.Show()
                Application.DoEvents()

                Dim chassis = FindControl(Of ComboBox)(editor, "chassisBox")
                Dim powerPlant = FindControl(Of ComboBox)(editor, "powerPlantBox")
                chassis.SelectedIndex = chassis.Items.Cast(Of StockChassis)().ToList().
                    FindIndex(Function(item) item.Id = "sand-buggy")
                Require(powerPlant.Items.Count = 4, "Expected the four catalog power plants for the sand buggy.")
                powerPlant.SelectedIndex = powerPlant.Items.Cast(Of StockPowerPlant)().ToList().
                    FindIndex(Function(item) item.Id = "gasoline")
                FindControl(Of TextBox)(editor, "streetIndex").Text = "2"
                Dim grid = FindControl(Of DataGridView)(editor, "additions")
                grid.Rows.Add("Ring mount", "10", "0", "1", "25")

                Dim layout = editor.Controls.OfType(Of TableLayoutPanel)().Single()
                Dim actions = layout.Controls.OfType(Of FlowLayoutPanel)().Single()
                actions.Controls.OfType(Of Button)().Single(Function(item) item.Text = "Calculate").PerformClick()
                Dim totals = FindControl(Of Label)(editor, "totals")
                Require(totals.Text.Contains("Base ¥2,800.00") AndAlso totals.Text.Contains("Street ¥5,600.00") AndAlso
                        totals.Text.Contains("3 CF") AndAlso totals.Text.Contains("15 kg"), totals.Text)

                editor.WriteDesignFile(destination, editor.ReadDesign())
                editor.WriteDesignFile(destination, editor.ReadDesign())
                Dim saved = JsonSerializer.Deserialize(Of StockDesign)(File.ReadAllText(destination))
                Require(saved.ChassisId = "sand-buggy" AndAlso saved.PowerPlantId = "gasoline" AndAlso
                        saved.Additions.Single().CargoConsumed = 1D, "Saved design values did not match the editor.")

                chassis.SelectedIndex = 0
                grid.Rows.Clear()
                editor.LoadDesignFile(destination)
                Dim reopened = editor.ReadDesign()
                Require(reopened.ChassisId = "sand-buggy" AndAlso reopened.PowerPlantId = "gasoline" AndAlso
                        reopened.StreetIndex.GetValueOrDefault() = 2D AndAlso reopened.Additions.Single().LoadConsumedKg = 25D AndAlso
                        totals.Text.Contains("Street ¥5,600.00"), "Reopening the file did not restore the editor and its totals.")
                File.WriteAllText(destination, "{""ChassisId"":""skiff"",""PowerPlantId"":""sail"",""StreetIndex"":1}")
                editor.LoadDesignFile(destination)
                Require(editor.ReadDesign().PowerPlantId = "sail" AndAlso totals.Text.Contains("Base ¥9,375.00") AndAlso
                        totals.Text.Contains("150 kg"), "The sail-powered skiff did not load and calculate.")
                File.WriteAllText(destination, "{""ChassisId"":""airliner"",""PowerPlantId"":""jet-propeller"",""StreetIndex"":1}")
                editor.LoadDesignFile(destination)
                Dim aircraft = DirectCast(powerPlant.SelectedItem, StockPowerPlant)
                Require(aircraft.StartingStallSpeed.GetValueOrDefault() = 135 AndAlso aircraft.StartingSpeed.GetValueOrDefault() = 320 AndAlso
                        totals.Text.Contains("Base ¥625,000.00"), "The airliner did not retain both speed ratings or its price.")
                File.WriteAllText(destination, "{""ChassisId"":""attack-submarine"",""PowerPlantId"":""nuclear"",""StreetIndex"":1}")
                editor.LoadDesignFile(destination)
                Dim submarine = DirectCast(chassis.SelectedItem, StockChassis)
                Dim reactor = DirectCast(powerPlant.SelectedItem, StockPowerPlant)
                Require(submarine.Hull.GetValueOrDefault() = 5 AndAlso Not submarine.Body.HasValue AndAlso
                        reactor.StartingSecondarySpeed.GetValueOrDefault() = 35 AndAlso
                        totals.Text.Contains("Base ¥300,000,000.00"), "The submarine did not retain Hull, secondary speed, or price.")
                File.WriteAllText(destination, "{""ChassisId"":""light-axle-trailer"",""PowerPlantId"":""unpowered"",""StreetIndex"":1}")
                editor.LoadDesignFile(destination)
                Dim trailer = DirectCast(powerPlant.SelectedItem, StockPowerPlant)
                Require(Not trailer.StartingSpeed.HasValue AndAlso Not trailer.StartingAcceleration.HasValue AndAlso
                        totals.Text.Contains("Base ¥1,500.00") AndAlso totals.Text.Contains("150 kg"),
                        "The trailer should have its chassis load rating and no propulsion ratings.")
                File.WriteAllText(destination, "{""ChassisId"":""sand-buggy"",""PowerPlantId"":""gasoline"",""StreetIndex"":2," &
                                  """Upgrades"":{""OffRoadHandling"":1,""Acceleration"":2,""CargoSteps"":10,""LoadSteps"":3}}")
                editor.LoadDesignFile(destination)
                Dim upgraded = editor.ReadDesign()
                editor.WriteDesignFile(destination, upgraded)
                editor.LoadDesignFile(destination)
                Require(upgraded.Upgrades.OffRoadHandling = 1 AndAlso totals.Text.Contains("Upgraded handling 4/3") AndAlso
                        totals.Text.Contains("Accel 8") AndAlso totals.Text.Contains("Street ¥11,840.00"),
                        "Catalog upgrades should calculate and survive saving and reopening.")
                File.WriteAllText(destination, "{""ChassisId"":""sand-buggy"",""PowerPlantId"":""gasoline"",""StreetIndex"":1," &
                                  """AvailabilityClass"":""Security"",""Upgrades"":{""EconomySteps"":2}}")
                editor.LoadDesignFile(destination)
                Require(totals.Text.Contains("Availability 8 / 8 days") AndAlso totals.Text.Contains("Economy 8.80 km/L"),
                        "Availability and Economy did not display the calculated ratings.")

                Dim legacyPath = Path.Combine(folder.FullName, "legacy.shp")
                File.WriteAllLines(legacyPath, {"Shop v1.40", """Test, Buggy""", "Rigger2,Sand Buggy", "Rigger2,Gasoline",
                                   "Bench", "Open", "0,0,0,0,0", "Rigger2,2,Test modification"})
                editor.StartLegacyDesign(LegacyVehicleReader.Read(legacyPath))
                Dim migrated = editor.ReadDesign()
                editor.WriteDesignFile(destination, migrated)
                editor.LoadDesignFile(destination)
                migrated = editor.ReadDesign()
                Dim original = migrated.LegacyReference.Value
                Require(migrated.ChassisId = "sand-buggy" AndAlso migrated.Upgrades Is Nothing AndAlso
                        Not migrated.AvailabilityClass.HasValue AndAlso migrated.Additions.Count = 0 AndAlso
                        original.GetProperty("Name").GetString() = "Test, Buggy" AndAlso
                        original.GetProperty("Modifications")(0).GetProperty("Level").GetInt32() = 2,
                        "Legacy reference and the new-design reset did not survive saving and reopening.")
                File.WriteAllText(destination, "{""ChassisId"":""light-minisub"",""PowerPlantId"":""gasoline"",""StreetIndex"":1," &
                                  """Upgrades"":{""DepthSteps"":2}}")
                editor.LoadDesignFile(destination)
                Require(totals.Text.Contains("Depth 70 m") AndAlso totals.Text.Contains("184 kg"),
                        "Depth did not apply its rating and load costs.")
                File.WriteAllText(destination, "{""ChassisId"":""sand-buggy"",""PowerPlantId"":""gasoline"",""StreetIndex"":2," &
                                  """CatalogModifications"":[{""Id"":""nitrous"",""Quantity"":2}," &
                                  "{""Id"":""crash-cage"",""Quantity"":1},{""Id"":""electronics-port"",""Quantity"":1}," &
                                  "{""Id"":""performance-tires"",""Quantity"":4}]}")
                editor.LoadDesignFile(destination)
                Dim modified = editor.ReadDesign()
                editor.WriteDesignFile(destination, modified)
                editor.LoadDesignFile(destination)
                Require(editor.ReadDesign().CatalogModifications.Count = 4 AndAlso
                        totals.Text.Contains("Street ¥18,800.00") AndAlso totals.Text.Contains("2.5 CF") AndAlso totals.Text.Contains("15 kg"),
                        "Catalog modifications did not price and round trip correctly.")
                Using dialog As New ModificationDialog(modified.CatalogModifications, DirectCast(chassis.SelectedItem, StockChassis),
                                                       DirectCast(powerPlant.SelectedItem, StockPowerPlant))
                    FindControl(Of TextBox)(dialog, "modificationSearch").Text = "sensor"
                    Require(dialog.ReadSelection().Single(Function(item) item.Id = "performance-tires").Quantity = 4,
                            "The catalog dialog did not restore the selected quantity.")
                End Using
                File.WriteAllText(destination, "{""ChassisId"":""van"",""PowerPlantId"":""diesel"",""StreetIndex"":1," &
                                  """Upgrades"":{""OnRoadHandling"":1},""CatalogModifications"":" &
                                  "[{""Id"":""vehicle-armor"",""Quantity"":6},{""Id"":""sensors"",""Quantity"":2}]}")
                editor.LoadDesignFile(destination)
                Dim armored = editor.ReadDesign()
                editor.WriteDesignFile(destination, armored)
                editor.LoadDesignFile(destination)
                Require(editor.ReadDesign().CatalogModifications.Count = 2 AndAlso totals.Text.Contains("Armor 6") AndAlso
                        totals.Text.Contains("Final Handling 4/11") AndAlso totals.Text.Contains("Sensor 2") AndAlso
                        totals.Text.Contains("500 kg") AndAlso totals.Text.Contains("Base ¥49,500.00"),
                        "Armor, Handling, sensors, and their costs should combine and round trip.")
                Dim named = editor.ReadDesign()
                named.Name = "Courier <One> & Co."
                editor.WriteDesignFile(destination, named)
                editor.LoadDesignFile(destination)
                Require(editor.ReadDesign().Name = named.Name, "The vehicle name did not round trip.")
                Dim sheetPath = Path.Combine(folder.FullName, "vehicle.html")
                Dim sheetCatalog = StockCatalog.Read(New JsonSerializerOptions())
                VehicleSheet.Write(sheetPath, editor.ReadDesign(), sheetCatalog)
                VehicleSheet.Write(sheetPath, editor.ReadDesign(), sheetCatalog)
                Dim sheet = File.ReadAllText(sheetPath)
                Require(sheet.Contains("Courier &lt;One&gt; &amp; Co.") AndAlso sheet.Contains("49,500") AndAlso
                        sheet.Contains("Vehicle armor") AndAlso sheet.Contains("Sensor system") AndAlso
                        Not sheet.Contains("<One>"), "The HTML sheet did not retain and escape the vehicle values.")
                File.WriteAllText(destination, "{""ChassisId"":""airliner"",""PowerPlantId"":""jet-propeller"",""StreetIndex"":1," &
                                  """CatalogModifications"":[{""Id"":""sensors"",""Quantity"":2}]}")
                editor.LoadDesignFile(destination)
                editor.WriteDesignFile(destination, editor.ReadDesign())
                editor.LoadDesignFile(destination)
                Require(editor.ReadDesign().CatalogModifications.Single().Quantity = 2 AndAlso
                        totals.Text.Contains("Sensor 2") AndAlso totals.Text.Contains("499 CF") AndAlso
                        totals.Text.Contains("4980 kg") AndAlso totals.Text.Contains("Base ¥643,750.00"),
                        "Replacing factory sensors should charge for the selected system and report its final rating.")
                File.WriteAllText(destination, "{""ChassisId"":""harbor-tug"",""PowerPlantId"":""diesel"",""StreetIndex"":1," &
                                  """CatalogModifications"":[{""Id"":""sonar"",""Quantity"":2},{""Id"":""towed-array-sonar"",""Quantity"":1}]}")
                editor.LoadDesignFile(destination)
                editor.WriteDesignFile(destination, editor.ReadDesign())
                editor.LoadDesignFile(destination)
                Require(totals.Text.Contains("Sonar 2") AndAlso totals.Text.Contains("Towed-array sonar 1") AndAlso
                        totals.Text.Contains("69 CF") AndAlso totals.Text.Contains("8250 kg") AndAlso
                        totals.Text.Contains("Base ¥825,000.00"),
                        "Standard sonar and towed-array sonar should price and round trip independently.")
                File.WriteAllText(destination, "{""ChassisId"":""airliner"",""PowerPlantId"":""jet-propeller"",""StreetIndex"":1," &
                                  """CatalogModifications"":[{""Id"":""electronics-port"",""Quantity"":1}," &
                                  "{""Id"":""remote-control-encryption"",""Quantity"":3},{""Id"":""retransmission-unit"",""Quantity"":1}," &
                                  "{""Id"":""trideo-display"",""Quantity"":2},{""Id"":""radio"",""Quantity"":4}," &
                                  "{""Id"":""remote-control-deck"",""Quantity"":1}]}")
                editor.LoadDesignFile(destination)
                editor.WriteDesignFile(destination, editor.ReadDesign())
                editor.LoadDesignFile(destination)
                Require(editor.ReadDesign().CatalogModifications.Count = 6 AndAlso
                        totals.Text.Contains("497.25 CF") AndAlso totals.Text.Contains("4995 kg") AndAlso
                        totals.Text.Contains("Base ¥727,500.00"),
                        "Electronics should round trip with their design points, cargo and load.")
                File.WriteAllText(destination, "{""ChassisId"":""airliner"",""PowerPlantId"":""jet-propeller"",""StreetIndex"":1," &
                                  """CatalogModifications"":[{""Id"":""magnetic-anomaly-detector"",""Quantity"":1}," &
                                  "{""Id"":""sensors"",""Quantity"":2}]}")
                editor.LoadDesignFile(destination)
                editor.WriteDesignFile(destination, editor.ReadDesign())
                editor.LoadDesignFile(destination)
                Require(editor.ReadDesign().CatalogModifications.Count = 2 AndAlso
                        totals.Text.Contains("Sensor 2") AndAlso totals.Text.Contains("487 CF") AndAlso
                        totals.Text.Contains("4930 kg") AndAlso totals.Text.Contains("Base ¥768,750.00"),
                        "A magnetic-anomaly detector should price from the final sensor rating after reopening.")
                File.WriteAllText(destination, "{""ChassisId"":""corvette"",""PowerPlantId"":""diesel"",""StreetIndex"":1," &
                                  """CatalogModifications"":[{""Id"":""over-the-horizon-sensors"",""Quantity"":1}]}")
                editor.LoadDesignFile(destination)
                Require(totals.Text.Contains("159 CF") AndAlso totals.Text.Contains("17500 kg"),
                        "Over-the-horizon sensors should work with factory sensors and consume their listed space and load.")
                File.WriteAllText(destination, "{""ChassisId"":""airliner"",""PowerPlantId"":""jet-propeller"",""StreetIndex"":1," &
                                  """CatalogModifications"":[{""Id"":""retransmission-power-amplifier"",""Quantity"":7}," &
                                  "{""Id"":""retransmission-unit"",""Quantity"":2}]}")
                editor.LoadDesignFile(destination)
                editor.WriteDesignFile(destination, editor.ReadDesign())
                editor.LoadDesignFile(destination)
                Require(editor.ReadDesign().CatalogModifications.Count = 2 AndAlso
                        totals.Text.Contains("496 CF") AndAlso totals.Text.Contains("4976 kg") AndAlso
                        totals.Text.Contains("Base ¥767,500.00"),
                        "Retransmission amplifier costs should scale and round cargo separately for each unit.")
                VehicleSheet.Write(sheetPath, editor.ReadDesign(), sheetCatalog)
                Require(File.ReadAllText(sheetPath).Contains("7 Flux per unit"),
                        "The vehicle sheet should report amplified retransmission Flux.")
                Dim waterSpeeds = {"Water speed 15 m/turn", "Water Speed rating 30", "Water Speed rating 45"}
                Dim amphibiousCosts = {"Base ¥3,400.00", "Base ¥5,600.00", "Base ¥10,400.00"}
                For level As Integer = 1 To 3
                    File.WriteAllText(destination, "{""ChassisId"":""sand-buggy"",""PowerPlantId"":""gasoline"",""StreetIndex"":1," &
                                      $"""CatalogModifications"":[{{""Id"":""amphibious-operation"",""Quantity"":{level}}}]}}")
                    editor.LoadDesignFile(destination)
                    editor.WriteDesignFile(destination, editor.ReadDesign())
                    editor.LoadDesignFile(destination)
                    Require(totals.Text.Contains(waterSpeeds(level - 1)) AndAlso totals.Text.Contains(amphibiousCosts(level - 1)) AndAlso
                            totals.Text.Contains(If(level = 3, "2 CF", "4 CF")) AndAlso
                            totals.Text.Contains($"Water Handling modifier +{If(level = 1, 2, 0)}"),
                            $"Amphibious level {level} did not apply the listed water ratings and costs.")
                Next
                VehicleSheet.Write(sheetPath, editor.ReadDesign(), sheetCatalog)
                Require(File.ReadAllText(sheetPath).Contains("<td>Water Speed rating</td><td>45</td>"),
                        "The vehicle sheet should report amphibious water speed separately.")
                Dim securityDesign As New StockDesign With {
                    .ChassisId = "sand-buggy", .PowerPlantId = "gasoline", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "proximity-alert", .Quantity = 1},
                        New ModificationSelection With {.Id = "anti-theft-electric-system", .Quantity = 1},
                        New ModificationSelection With {.Id = "improved-anti-theft-security", .Quantity = 6}
                    }
                }
                Dim proximityRejected = False
                Try
                    securityDesign.Calculate(sheetCatalog)
                Catch ex As ArgumentException When ex.Message.Contains("requires a sensor system")
                    proximityRejected = True
                End Try
                Require(proximityRejected, "Proximity alerts should require sensors.")
                securityDesign.CatalogModifications.Add(New ModificationSelection With {.Id = "sensors", .Quantity = 1})
                For Each test In {(Rating:=6, Price:="6,240"), (Rating:=7, Price:="8,080"), (Rating:=10, Price:="25,280")}
                    securityDesign.CatalogModifications(2).Quantity = test.Rating
                    editor.WriteDesignFile(destination, securityDesign)
                    editor.LoadDesignFile(destination)
                    Require(totals.Text.Contains($"Base ¥{test.Price}.00") AndAlso
                            editor.ReadDesign().CatalogModifications.Count = 4,
                            $"Anti-theft rating {test.Rating} did not apply the expected design cost.")
                Next
                Dim craneDesign As New StockDesign With {
                    .ChassisId = "van", .PowerPlantId = "diesel", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "crane", .Quantity = 1275}
                    }
                }
                editor.WriteDesignFile(destination, craneDesign)
                editor.LoadDesignFile(destination)
                Require(editor.ReadDesign().CatalogModifications.Single().Quantity = 1275 AndAlso
                        totals.Text.Contains("Base ¥12,050.00") AndAlso
                        totals.Text.Contains("33 CF") AndAlso totals.Text.Contains("680 kg"),
                        "The crane did not apply fractional design points or the listed cargo and load.")
                VehicleSheet.Write(sheetPath, editor.ReadDesign(), sheetCatalog)
                Require(File.ReadAllText(sheetPath).Contains("1275 kg lifting capacity"),
                        "The vehicle sheet should report the crane's lifting capacity.")
                craneDesign.CatalogModifications(0).Quantity = 2001
                Dim craneRejected = False
                Try
                    craneDesign.Calculate(sheetCatalog)
                Catch ex As ArgumentException When ex.Message.Contains("from 1 to 2000")
                    craneRejected = True
                End Try
                Require(craneRejected, "The van should reject a crane above its Body-based limit.")
                Dim craneRule = ModificationCatalog.Rules.Single(Function(rule) rule.Id = "crane")
                Require(ModificationCatalog.MaximumQuantity(craneRule, StockCatalog.FindById(sheetCatalog, "heavy-caterpillar")) = 75000,
                        "The crane should use the quadratic limit at Body 10.")
                Dim equipmentDesign As New StockDesign With {
                    .ChassisId = "airliner", .PowerPlantId = "jet-propeller", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "medical-clinic", .Quantity = 2},
                        New ModificationSelection With {.Id = "valkyrie-module", .Quantity = 1},
                        New ModificationSelection With {.Id = "reinforced-ejection-bucket-seat", .Quantity = 1}
                    }
                }
                editor.WriteDesignFile(destination, equipmentDesign)
                editor.LoadDesignFile(destination)
                Require(editor.ReadDesign().CatalogModifications.Count = 3 AndAlso
                        totals.Text.Contains("432 CF") AndAlso totals.Text.Contains("4100 kg") AndAlso
                        totals.Text.Contains("Base ¥1,554,500.00"),
                        "Medical equipment and reinforced ejection seating should price and reopen together.")
                Dim winchDesign As New StockDesign With {
                    .ChassisId = "van", .PowerPlantId = "diesel", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "winch", .Quantity = 1250}
                    }
                }
                Dim winch = winchDesign.Calculate(sheetCatalog).AppliedModifications.Single()
                Require(winch.DesignPoints = 12.5D AndAlso winch.LoadConsumedKg = 10D AndAlso winch.CargoConsumed = 0D,
                        "The winch should use the design-time cost and capacity rules.")
                Dim trailerDesign As New StockDesign With {
                    .ChassisId = "heavy-axle-trailer", .PowerPlantId = "unpowered", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "generator", .Quantity = 1}
                    }
                }
                Dim generator = trailerDesign.Calculate(sheetCatalog).AppliedModifications.Single()
                Require(generator.DesignPoints = 18.75D AndAlso generator.CargoConsumed = 4D AndAlso generator.LoadConsumedKg = 400D,
                        "A generator should use the trailer's starting chassis cost and capacities.")
                Dim flotationDesign As New StockDesign With {
                    .ChassisId = "airliner", .PowerPlantId = "jet-propeller", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "aircraft-flotation", .Quantity = 1},
                        New ModificationSelection With {.Id = "drone-rack-body-2", .Quantity = 1}
                    }
                }
                editor.WriteDesignFile(destination, flotationDesign)
                editor.LoadDesignFile(destination)
                Require(totals.Text.Contains("Final Speed 160") AndAlso flotationDesign.Calculate(sheetCatalog).ModifiedRatings.Economy.GetValueOrDefault() = 0.9D AndAlso
                        totals.Text.Contains("493 CF") AndAlso totals.Text.Contains("4955 kg") AndAlso
                        totals.Text.Contains("Base ¥752,000.00"),
                        "Aircraft floats and drone racks should apply their design costs, capacity, and ratings.")
                Dim armDesign As New StockDesign With {
                    .ChassisId = "van", .PowerPlantId = "diesel", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "mechanical-arm-strength", .Quantity = 3},
                        New ModificationSelection With {.Id = "mechanical-arm", .Quantity = 2}
                    }
                }
                Dim armResult = armDesign.Calculate(sheetCatalog)
                Require(armResult.AppliedModifications(0).DesignPoints = 1440D AndAlso
                        armResult.AppliedModifications(0).Name.Contains("Strength 19 per arm") AndAlso
                        armResult.Totals.LoadRemainingKg = 620D AndAlso armResult.Totals.CargoRemaining = 44D,
                        "Arm enhancements should be calculated for each installed arm.")
                Dim sealedVan As New StockDesign With {
                    .ChassisId = "van", .PowerPlantId = "diesel", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "cabin-overpressure", .Quantity = 1},
                        New ModificationSelection With {.Id = "cabin-water-seal", .Quantity = 1},
                        New ModificationSelection With {.Id = "engine-water-seal", .Quantity = 1},
                        New ModificationSelection With {.Id = "life-support", .Quantity = 2}
                    }
                }
                editor.WriteDesignFile(destination, sealedVan)
                editor.LoadDesignFile(destination)
                Require(totals.Text.Contains("Base ¥52,000.00") AndAlso totals.Text.Contains("45 CF") AndAlso
                        totals.Text.Contains("940 kg"), "Cabin equipment did not apply the listed costs and capacities. " & totals.Text)
                Dim remoteVan As New StockDesign With {
                    .ChassisId = "van", .PowerPlantId = "diesel", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "advanced-drone-pilot", .Quantity = 3},
                        New ModificationSelection With {.Id = "remote-control-interface", .Quantity = 1},
                        New ModificationSelection With {.Id = "autonav", .Quantity = 2},
                        New ModificationSelection With {.Id = "contingency-maneuver-controls", .Quantity = 3},
                        New ModificationSelection With {.Id = "sensors", .Quantity = 1}
                    }
                }
                editor.WriteDesignFile(destination, remoteVan)
                editor.LoadDesignFile(destination)
                Require(totals.Text.Contains("Pilot 3") AndAlso totals.Text.Contains("Autonav 2") AndAlso
                        totals.Text.Contains("Base ¥61,000.00") AndAlso totals.Text.Contains("963 kg"),
                        "Remote control, Autonav, and Pilot should use final ratings regardless of selection order.")
                sealedVan.CatalogModifications.Add(New ModificationSelection With {.Id = "ballast-tanks", .Quantity = 1})
                sealedVan.CatalogModifications.Add(New ModificationSelection With {.Id = "extra-entry-points", .Quantity = 2})
                Dim ballastDesign = sealedVan.Calculate(sheetCatalog)
                Require(ballastDesign.Totals.CargoRemaining = 13D AndAlso ballastDesign.Totals.LoadRemainingKg = 540D AndAlso
                        ballastDesign.Totals.BaseCostNuyen = 68000D,
                        "Ballast tanks should use Body squared and entry points should carry no design cost.")
                Dim sidecarDesign As New StockDesign With {
                    .ChassisId = "chopper", .PowerPlantId = "gasoline", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "sidecar", .Quantity = 3},
                        New ModificationSelection With {.Id = "runflat-upgrade", .Quantity = 3}
                    }
                }
                editor.WriteDesignFile(destination, sidecarDesign)
                editor.LoadDesignFile(destination)
                Dim sidecarResult = editor.ReadDesign().Calculate(sheetCatalog)
                Require(sidecarResult.Totals.CargoRemaining = 14D AndAlso sidecarResult.Totals.DirectCostNuyen = 600D AndAlso
                        sidecarResult.ModifiedRatings.Economy.GetValueOrDefault() = 9D AndAlso
                        totals.Text.Contains("Final Speed 45") AndAlso totals.Text.Contains("Final Handling 6/8"),
                        "Sidecars and runflat tires should apply the listed ratings and costs.")
                Dim deepSub As New StockDesign With {
                    .ChassisId = "attack-submarine", .PowerPlantId = "diesel", .StreetIndex = 1,
                    .Upgrades = New DesignUpgrades With {.DepthSteps = 2, .Acceleration = 1, .Speed = 2, .SecondarySpeed = 1}
                }
                editor.WriteDesignFile(destination, deepSub)
                editor.LoadDesignFile(destination)
                Dim deepResult = editor.ReadDesign().Calculate(sheetCatalog)
                Require(deepResult.Totals.DesignPoints = 370035D AndAlso
                        deepResult.Totals.LoadRemainingKg = 700000D AndAlso totals.Text.Contains("Depth 420 m") AndAlso
                        deepResult.UpgradedRatings.Speed.GetValueOrDefault() = 32 AndAlso
                        deepResult.UpgradedRatings.SecondarySpeed.GetValueOrDefault() = 26,
                        "Hull-rated depth and paired speed upgrades should apply their listed costs and limits.")
                editor.StartLegacyDesign(LegacyVehicleReader.Read(legacyPath))
                Require(editor.ReadDesign().ChassisId = "sand-buggy" AndAlso editor.ReadDesign().PowerPlantId = "gasoline",
                        "A uniquely named legacy chassis and compatible engine should replace the current components.")
                Dim quoteChassis = StockCatalog.FindById(sheetCatalog, "van")
                Dim quotePlant = quoteChassis.PowerPlants.Single(Function(candidate) candidate.Id = "diesel")
                Using quoteDialog As New RetrofitQuoteDialog(quoteChassis, quotePlant, New DesignTotals With {
                    .CargoRemaining = 48D, .LoadRemainingKg = 1000D
                })
                    Dim quoteOptions = FindControl(Of ComboBox)(quoteDialog, "retrofitOptions")
                    quoteDialog.BindingContext = New BindingContext()
                    quoteOptions.SelectedIndex = quoteOptions.Items.Cast(Of ModificationRule)().ToList().
                        FindIndex(Function(rule) rule.Id = "crane")
                    FindControl(Of NumericUpDown)(quoteDialog, "retrofitQuantity").Value = 1000D
                    Dim quoteText = FindControl(Of Label)(quoteDialog, "retrofitOutput").Text
                    Require(quoteText.Contains("Street parts cost ¥4,000.00") AndAlso quoteText.Contains("6 / 14 days") AndAlso
                            quoteText.Contains("13 CF") AndAlso quoteText.Contains("680 kg"),
                            "The retrofit dialog should use customization costs and remaining capacity.")
                End Using
                Dim retrofittedVan As New StockDesign With {
                    .ChassisId = "van", .PowerPlantId = "diesel", .StreetIndex = 1,
                    .InstalledRetrofits = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "crane", .Quantity = 1000},
                        New ModificationSelection With {.Id = "electronics-port", .Quantity = 1}
                    }
                }
                editor.WriteDesignFile(destination, retrofittedVan)
                editor.LoadDesignFile(destination)
                editor.WriteDesignFile(destination, editor.ReadDesign())
                editor.LoadDesignFile(destination)
                Dim installedResult = editor.ReadDesign().Calculate(sheetCatalog)
                Require(editor.ReadDesign().InstalledRetrofits.Count = 2 AndAlso
                        installedResult.RetrofitPartsCostNuyen.GetValueOrDefault() = 5000D AndAlso
                        installedResult.Totals.BaseCostNuyen = 9500D AndAlso
                        installedResult.Totals.CargoRemaining = 13D AndAlso installedResult.Totals.LoadRemainingKg = 680D,
                        "Installed retrofits should preserve their parts cost and use customization capacity after reopening.")
                VehicleSheet.Write(sheetPath, editor.ReadDesign(), sheetCatalog)
                Require(File.ReadAllText(sheetPath).Contains("Crane (installed retrofit)") AndAlso
                        File.ReadAllText(sheetPath).Contains("Installed retrofit street parts total"),
                        "Installed retrofit equipment and costs should appear on the vehicle sheet.")
                retrofittedVan.InstalledRetrofits.Add(New ModificationSelection With {.Id = "medical-clinic", .Quantity = 1})
                Dim excessRetrofitRejected = False
                Try
                    retrofittedVan.Calculate(sheetCatalog)
                Catch ex As ArgumentException When ex.Message.Contains("exceed")
                    excessRetrofitRejected = True
                End Try
                Require(excessRetrofitRejected, "Installed retrofits should reject capacity overflows.")
                Dim turboVan As New StockDesign With {
                    .ChassisId = "van", .PowerPlantId = "diesel", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "turbocharging", .Quantity = 2}
                    }
                }
                Dim turboResult = turboVan.Calculate(sheetCatalog)
                Dim turboPlant = StockCatalog.FindById(sheetCatalog, "van").PowerPlants.Single(Function(item) item.Id = "diesel")
                Require(turboResult.ModifiedRatings.Speed.GetValueOrDefault() = turboPlant.StartingSpeed.GetValueOrDefault() + 30 AndAlso
                        turboResult.ModifiedRatings.Economy.GetValueOrDefault() = turboPlant.StartingEconomyKmPerLiter.GetValueOrDefault() * 0.9D AndAlso
                        turboResult.AppliedModifications.Single().DesignPoints = 52.5D,
                        "Turbocharging should apply the listed power plant cost and final rating changes.")
                Dim electronicAircraft As New StockDesign With {
                    .ChassisId = "airliner", .PowerPlantId = "jet-propeller", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "ecm", .Quantity = 10},
                        New ModificationSelection With {.Id = "eccm", .Quantity = 10},
                        New ModificationSelection With {.Id = "electronic-deception", .Quantity = 6},
                        New ModificationSelection With {.Id = "electronic-counter-deception", .Quantity = 6}
                    }
                }
                editor.WriteDesignFile(destination, electronicAircraft)
                editor.LoadDesignFile(destination)
                Require(totals.Text.Contains("ECM 10") AndAlso totals.Text.Contains("ECCM 10") AndAlso
                        totals.Text.Contains("ED 6") AndAlso totals.Text.Contains("ECD 6") AndAlso
                        totals.Text.Contains("452 CF") AndAlso totals.Text.Contains("4450 kg") AndAlso
                        totals.Text.Contains("Base ¥5,750,000.00"),
                        "Electronic systems should apply the complete rating tables and reopen correctly.")
                electronicAircraft.CatalogModifications = New List(Of ModificationSelection) From {
                    New ModificationSelection With {.Id = "radar-absorbent-materials", .Quantity = 2},
                    New ModificationSelection With {.Id = "thermal-baffles", .Quantity = 1},
                    New ModificationSelection With {.Id = "smart-armor", .Quantity = 1},
                    New ModificationSelection With {.Id = "vehicle-armor", .Quantity = 1}
                }
                Dim signatureResult = electronicAircraft.Calculate(sheetCatalog)
                Require(signatureResult.ModifiedRatings.RadarSignature.GetValueOrDefault() = 5 AndAlso
                        signatureResult.ModifiedRatings.InfraredSignature.GetValueOrDefault() = 4,
                        "Radar and infrared improvements should retain their separate final signatures.")
                Dim suspensionDesign As New StockDesign With {
                    .ChassisId = "sand-buggy", .PowerPlantId = "gasoline", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "improved-suspension", .Quantity = 1},
                        New ModificationSelection With {.Id = "off-road-suspension", .Quantity = 1},
                        New ModificationSelection With {.Id = "dual-purpose-tires", .Quantity = 4}
                    }
                }
                Dim suspensionResult = suspensionDesign.Calculate(sheetCatalog)
                Dim suspensionChassis = StockCatalog.FindById(sheetCatalog, "sand-buggy")
                Require(suspensionResult.ModifiedRatings.OnRoadHandling.GetValueOrDefault() = suspensionChassis.OnRoadHandling.GetValueOrDefault() AndAlso
                        suspensionResult.ModifiedRatings.OffRoadHandling.GetValueOrDefault() = suspensionChassis.OffRoadHandling.GetValueOrDefault() - 1 AndAlso
                        suspensionResult.AppliedModifications(0).DesignPoints = 10D AndAlso
                        suspensionResult.AppliedModifications(1).DesignPoints = 20D,
                        "Both suspension types should compose with dual-purpose tires and charge their listed chassis costs.")
                electronicAircraft.CatalogModifications = New List(Of ModificationSelection) From {
                    New ModificationSelection With {.Id = "external-fixed-hardpoint", .Quantity = 1},
                    New ModificationSelection With {.Id = "remote-small-turret", .Quantity = 1},
                    New ModificationSelection With {.Id = "internal-fixed-firmpoint", .Quantity = 1},
                    New ModificationSelection With {.Id = "smartlink-integration-2", .Quantity = 2}
                }
                editor.WriteDesignFile(destination, electronicAircraft)
                editor.LoadDesignFile(destination)
                Dim mountResult = editor.ReadDesign().Calculate(sheetCatalog)
                Require(mountResult.Totals.BaseCostNuyen = 741250D AndAlso mountResult.Totals.CargoRemaining = 492D AndAlso
                        mountResult.Totals.LoadRemainingKg = 4880D AndAlso mountResult.ModifiedRatings.Signature.GetValueOrDefault() = 1,
                        "Weapon mounts should apply their design costs, capacity, and Signature penalties.")
                electronicAircraft.CatalogModifications.Add(New ModificationSelection With {.Id = "remote-medium-turret", .Quantity = 1})
                Dim mountLimitRejected = False
                Try
                    electronicAircraft.Calculate(sheetCatalog)
                Catch ex As ArgumentException When ex.Message.Contains("hardpoint")
                    mountLimitRejected = True
                End Try
                Require(mountLimitRejected, "Weapon mounts should share the vehicle's Body-based mounting budget.")
                Dim engineVan As New StockDesign With {
                    .ChassisId = "van", .PowerPlantId = "diesel", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "engine-customization-speed", .Quantity = 2},
                        New ModificationSelection With {.Id = "engine-customization-acceleration", .Quantity = 1},
                        New ModificationSelection With {.Id = "engine-customization-load", .Quantity = 1},
                        New ModificationSelection With {.Id = "drive-by-wire", .Quantity = 2},
                        New ModificationSelection With {.Id = "drive-by-wire-speed-allocation", .Quantity = 10}
                    }
                }
                editor.WriteDesignFile(destination, engineVan)
                editor.LoadDesignFile(destination)
                Dim engineResult = editor.ReadDesign().Calculate(sheetCatalog)
                Require(engineResult.ModifiedRatings.Speed.GetValueOrDefault() = 154 AndAlso
                        engineResult.ModifiedRatings.Acceleration.GetValueOrDefault() = 6 AndAlso
                        engineResult.ModifiedRatings.OnRoadHandling.GetValueOrDefault() = 2 AndAlso
                        engineResult.ModifiedRatings.OffRoadHandling.GetValueOrDefault() = 8 AndAlso
                        engineResult.Totals.LoadRemainingKg = 1200D AndAlso engineResult.Totals.BaseCostNuyen = 22875D,
                        "Engine customization and drive-by-wire should apply limits, tiered costs, and the allocated bonus.")
                retrofittedVan.InstalledRetrofits = New List(Of ModificationSelection) From {
                    New ModificationSelection With {.Id = "external-fixed-firmpoint", .Quantity = 2},
                    New ModificationSelection With {.Id = "smartlink-integration-2", .Quantity = 1}
                }
                editor.WriteDesignFile(destination, retrofittedVan)
                editor.LoadDesignFile(destination)
                Dim retrofitMountResult = editor.ReadDesign().Calculate(sheetCatalog)
                Require(retrofitMountResult.ModifiedRatings.Signature.GetValueOrDefault() = 1 AndAlso
                        retrofitMountResult.RetrofitPartsCostNuyen.GetValueOrDefault() = 4800D AndAlso
                        retrofitMountResult.Totals.CargoRemaining = 45D AndAlso retrofitMountResult.Totals.LoadRemainingKg = 980D,
                        "Retrofitted mounts should update final ratings and use customization pricing and capacity.")
                electronicAircraft.CatalogModifications = New List(Of ModificationSelection) From {
                    New ModificationSelection With {.Id = "remote-small-turret-pop-up", .Quantity = 1}
                }
                Dim concealedMount = electronicAircraft.Calculate(sheetCatalog)
                Require(concealedMount.Totals.CargoRemaining = 492D AndAlso
                        concealedMount.Totals.BaseCostNuyen = 712500D AndAlso
                        If(concealedMount.ModifiedRatings?.Signature, 3) = 3,
                        "A pop-up turret should double its standard cargo space and retain the stowed Signature.")
                Dim torpedoSub As New StockDesign With {
                    .ChassisId = "attack-submarine", .PowerPlantId = "diesel", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "torpedo-tube-autoloader", .Quantity = 1}
                    }
                }
                Dim torpedoResult = torpedoSub.Calculate(sheetCatalog)
                Require(torpedoResult.Totals.CargoRemaining = 112D AndAlso
                        torpedoResult.Totals.LoadRemainingKg = 699500D AndAlso
                        torpedoResult.Totals.DesignPoints = 370500D,
                        "Autoloaded torpedo tubes should include their loading machinery and design capacity.")
                Dim auxiliaryVan As New StockDesign With {
                    .ChassisId = "van", .PowerPlantId = "diesel", .AuxiliaryPowerPlantId = "electric-battery", .StreetIndex = 1
                }
                editor.WriteDesignFile(destination, auxiliaryVan)
                editor.LoadDesignFile(destination)
                editor.WriteDesignFile(destination, editor.ReadDesign())
                editor.LoadDesignFile(destination)
                Dim auxiliaryResult = editor.ReadDesign().Calculate(sheetCatalog)
                Require(editor.ReadDesign().AuxiliaryPowerPlantId = "electric-battery" AndAlso
                        auxiliaryResult.AuxiliaryEngine.Speed.GetValueOrDefault() = 30 AndAlso
                        auxiliaryResult.Totals.CargoRemaining = 38D AndAlso auxiliaryResult.Totals.LoadRemainingKg = 20D AndAlso
                        auxiliaryResult.Totals.BaseCostNuyen = 12000D,
                        "Auxiliary engines should use their listed cost, half-speed ratings, and the lower engine Load limit.")
                VehicleSheet.Write(sheetPath, editor.ReadDesign(), sheetCatalog)
                Require(File.ReadAllText(sheetPath).Contains("Auxiliary engine"), "The vehicle sheet should include the alternate engine.")
                remoteVan.CatalogModifications = New List(Of ModificationSelection) From {
                    New ModificationSelection With {.Id = "robotic-pilot", .Quantity = 3},
                    New ModificationSelection With {.Id = "remote-control-interface", .Quantity = 1},
                    New ModificationSelection With {.Id = "sensors", .Quantity = 1},
                    New ModificationSelection With {.Id = "improved-neural-network", .Quantity = 2},
                    New ModificationSelection With {.Id = "fuzzy-logic", .Quantity = 1},
                    New ModificationSelection With {.Id = "multi-object-manipulation", .Quantity = 2},
                    New ModificationSelection With {.Id = "battletac-ivis", .Quantity = 1},
                    New ModificationSelection With {.Id = "robotic-reflexes", .Quantity = 2}
                }
                editor.WriteDesignFile(destination, remoteVan)
                editor.LoadDesignFile(destination)
                Dim robotResult = editor.ReadDesign().Calculate(sheetCatalog)
                Require(robotResult.ModifiedRatings.RoboticPilot.GetValueOrDefault() = 3 AndAlso
                        robotResult.ModifiedRatings.AdaptationPool.GetValueOrDefault() = 5 AndAlso
                        robotResult.ModifiedRatings.RoboticReflexes.GetValueOrDefault() = 2 AndAlso
                        robotResult.Totals.BaseCostNuyen = 399500D,
                        "Robot programming should calculate Pilot, Adaptation Pool, prerequisites, and dependent costs.")
                electronicAircraft.CatalogModifications = New List(Of ModificationSelection) From {
                    New ModificationSelection With {.Id = "living-amenities-high", .Quantity = 2},
                    New ModificationSelection With {.Id = "living-amenities-people-space", .Quantity = 2}
                }
                Dim amenities = electronicAircraft.Calculate(sheetCatalog)
                Require(amenities.Totals.BaseCostNuyen = 670000D AndAlso amenities.Totals.CargoRemaining = 314D AndAlso
                        amenities.Totals.LoadRemainingKg = 4500D,
                        "Living amenities and reserved People Space should be counted separately.")
                Dim hybridVan As New StockDesign With {
                    .ChassisId = "van", .PowerPlantId = "diesel", .StreetIndex = 1,
                    .Upgrades = New DesignUpgrades With {.EconomySteps = 2},
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "hybrid-engine", .Quantity = 1}
                    }
                }
                Dim hybridResult = hybridVan.Calculate(sheetCatalog)
                Require(hybridResult.ModifiedRatings.Acceleration.GetValueOrDefault() = 2 AndAlso
                        hybridResult.ModifiedRatings.Signature.GetValueOrDefault() = 3 AndAlso
                        hybridResult.ModifiedRatings.Economy.GetValueOrDefault() = 8.8D AndAlso
                        hybridResult.Totals.CargoRemaining = 42D AndAlso hybridResult.Totals.LoadRemainingKg = 500D AndAlso
                        hybridResult.Totals.BaseCostNuyen = 11375D,
                        "Hybrid engines should change their starting ratings before functional improvements.")
                hybridVan.CatalogModifications = New List(Of ModificationSelection) From {
                    New ModificationSelection With {.Id = "smart-materials", .Quantity = 1}
                }
                hybridVan.Upgrades = New DesignUpgrades With {.Speed = 120}
                Dim smartMaterialResult = hybridVan.Calculate(sheetCatalog)
                Require(smartMaterialResult.ModifiedRatings.Speed.GetValueOrDefault() = 200 AndAlso
                        smartMaterialResult.Totals.BaseCostNuyen = 43500D AndAlso
                        StockCatalog.FindById(sheetCatalog, "van").PowerPlants.Single(Function(item) item.Id = "diesel").MaximumSpeed.GetValueOrDefault() = 175,
                        "Smart materials should raise limits without mutating the shared catalog.")
                Dim hydrofoilBoat As New StockDesign With {
                    .ChassisId = "sport-cruiser", .PowerPlantId = "diesel", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "hydrofoil", .Quantity = 1}
                    }
                }
                editor.WriteDesignFile(destination, hydrofoilBoat)
                editor.LoadDesignFile(destination)
                Dim hydrofoilResult = editor.ReadDesign().Calculate(sheetCatalog)
                Require(hydrofoilResult.OperatingModes.Single().Speed.GetValueOrDefault() = 37 AndAlso
                        hydrofoilResult.OperatingModes.Single().Acceleration.GetValueOrDefault() = 5 AndAlso
                        hydrofoilResult.OperatingModes.Single().Handling.GetValueOrDefault() = 6 AndAlso
                        hydrofoilResult.Totals.CargoRemaining = 40D,
                        "Hydrofoil performance should be shown as an operating mode with its design capacity cost.")
                Dim deckVan As New StockDesign With {
                    .ChassisId = "van", .PowerPlantId = "diesel", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "flight-deck", .Quantity = 10},
                        New ModificationSelection With {.Id = "flight-deck-catapult", .Quantity = 1},
                        New ModificationSelection With {.Id = "flight-deck-angled", .Quantity = 1}
                    }
                }
                Dim deckResult = deckVan.Calculate(sheetCatalog)
                Require(deckResult.Totals.BaseCostNuyen = 14700D AndAlso deckResult.Totals.CargoRemaining = 40D AndAlso
                        deckResult.Totals.LoadRemainingKg = 760D, "Flight decks should include their length, catapult, and angled-deck costs.")
                suspensionDesign.CatalogModifications.Add(New ModificationSelection With {.Id = "spare-standard-tires", .Quantity = 2})
                Dim spareResult = suspensionDesign.Calculate(sheetCatalog)
                Require(spareResult.Totals.CargoRemaining = 2D AndAlso spareResult.Totals.DirectCostNuyen = 2200D,
                        "Spare tires should consume storage and purchase cost while retaining the fitted tire choice.")
                electronicAircraft.CatalogModifications = New List(Of ModificationSelection) From {
                    New ModificationSelection With {.Id = "external-fixed-firmpoint", .Quantity = 1},
                    New ModificationSelection With {.Id = "remote-small-turret", .Quantity = 1},
                    New ModificationSelection With {.Id = "gunnery-recoil-adjusters", .Quantity = 4}
                }
                Dim recoilResult = electronicAircraft.Calculate(sheetCatalog)
                Require(recoilResult.AppliedModifications.Single(Function(item) item.Name = "Gunnery recoil adjusters").DesignPoints = 80D,
                        "Uniform recoil adjustment should be priced for each eligible fixed mount and turret.")
                Dim finBoat As New StockDesign With {
                    .ChassisId = "skiff", .PowerPlantId = "diesel", .StreetIndex = 1,
                    .AvailabilityClass = VehicleAvailabilityClass.Standard,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "fin-drive", .Quantity = 1}
                    }
                }
                Dim finResult = finBoat.Calculate(sheetCatalog)
                Require(finResult.ModifiedRatings.Speed.GetValueOrDefault() = 56 AndAlso
                        finResult.Totals.CargoRemaining = 4D AndAlso finResult.Totals.LoadRemainingKg = 150D AndAlso
                        finResult.Availability.Rating = 11D AndAlso finResult.Availability.BaseDays = 11D,
                        "Fin drives should adjust starting performance, capacities, and Availability.")
                editor.ClientSize = New System.Drawing.Size(780, 550)
                Application.DoEvents()
                Require(grid.Height > 30 AndAlso totals.Parent.ClientRectangle.Height >= 100,
                        "The editor should retain an editable grid and scrollable results at its minimum size.")
                File.WriteAllLines(legacyPath, {"Shop v1.40", "Converted Buggy", "Rigger2,Sand Buggy", "Rigger2,Gasoline",
                                   "Bench", "Open", "0,0,0,0,0", "Rigger2,1,Crash cage", "Rigger2,2,Nitrous oxide injectors",
                                   "Rigger2,1,Unknown option"})
                editor.StartLegacyDesign(LegacyVehicleReader.Read(legacyPath))
                editor.WriteDesignFile(destination, editor.ReadDesign())
                editor.LoadDesignFile(destination)
                Dim converted = editor.ReadDesign()
                Require(converted.CatalogModifications.Count = 2 AndAlso converted.LegacyImportNotes.Count = 3 AndAlso
                        converted.Calculate(sheetCatalog).Totals.BaseCostNuyen = 8400D AndAlso
                        converted.LegacyReference.Value.GetProperty("Modifications").GetArrayLength() = 3,
                        "Recognized legacy options should be repriced while unsupported source entries remain available for review.")
                VehicleSheet.Write(sheetPath, converted, sheetCatalog)
                Require(File.ReadAllText(sheetPath).Contains("Review manually: Unknown option"),
                        "Legacy conversion review notes should appear on the vehicle sheet.")
                LegacyConversionChecks.Run(folder.FullName, sheetCatalog)
                CompletionChecks.Run(folder.FullName, sheetCatalog)
                Dim sensorRefit As New StockDesign With {
                    .ChassisId = "van", .PowerPlantId = "diesel", .StreetIndex = 1,
                    .CatalogModifications = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "sensors", .Quantity = 2}
                    },
                    .InstalledRetrofits = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "sensors", .Quantity = 5, .StreetIndexOverride = 4D}
                    }
                }
                editor.WriteDesignFile(destination, sensorRefit)
                editor.LoadDesignFile(destination)
                Dim sensorRefitResult = editor.ReadDesign().Calculate(sheetCatalog)
                Require(sensorRefitResult.ModifiedRatings.Sensor.GetValueOrDefault() = 5 AndAlso
                        sensorRefitResult.RetrofitPartsCostNuyen.GetValueOrDefault() = 60000D AndAlso sensorRefitResult.Totals.CargoRemaining = 41D AndAlso
                        sensorRefitResult.Totals.LoadRemainingKg = 930D AndAlso sensorRefitResult.Totals.BaseCostNuyen = 17000D,
                        "Replacement sensors should use the final rating, saved Street Index, and customization capacity.")
                Using editRetrofits As New ModificationDialog(sensorRefit.InstalledRetrofits, quoteChassis, quotePlant, RetrofitCatalog.Rules)
                    Require(editRetrofits.ReadSelection().Single().StreetIndexOverride.GetValueOrDefault() = 4D,
                            "Editing installed quantities should retain the assigned parts Street Index.")
                End Using
                Using restrictedQuote As New RetrofitQuoteDialog(quoteChassis, quotePlant, New DesignTotals With {
                    .CargoRemaining = 48D, .LoadRemainingKg = 1000D
                })
                    restrictedQuote.BindingContext = New BindingContext()
                    Dim choices = FindControl(Of ComboBox)(restrictedQuote, "retrofitOptions")
                    choices.SelectedIndex = choices.Items.Cast(Of ModificationRule)().ToList().FindIndex(Function(rule) rule.Id = "sensors")
                    FindControl(Of NumericUpDown)(restrictedQuote, "retrofitQuantity").Value = 5D
                    Require(FindControl(Of Button)(restrictedQuote, "installRetrofit").Enabled AndAlso
                            FindControl(Of Label)(restrictedQuote, "retrofitOutput").Text.Contains("pending GM review"),
                            "A restricted part should be available on a proposed sheet without an invented Street Index.")
                    FindControl(Of NumericUpDown)(restrictedQuote, "retrofitStreetIndex").Value = 4D
                    FindControl(Of CheckBox)(restrictedQuote, "customStreetIndex").Checked = True
                    Require(FindControl(Of Button)(restrictedQuote, "installRetrofit").Enabled AndAlso
                            FindControl(Of Label)(restrictedQuote, "retrofitOutput").Text.Contains("¥60,000.00"),
                            "The assigned Street Index should price restricted equipment and enable installation.")
                End Using
                sensorRefit.InstalledRetrofits.Single().StreetIndexOverride = Nothing
                editor.WriteDesignFile(destination, sensorRefit)
                editor.LoadDesignFile(destination)
                Dim pendingRefit = editor.ReadDesign().Calculate(sheetCatalog)
                Require(Not pendingRefit.RetrofitPartsCostNuyen.HasValue AndAlso pendingRefit.RetrofitListPartsCostNuyen = 15000D AndAlso
                        pendingRefit.ModifiedRatings.Sensor.GetValueOrDefault() = 5 AndAlso pendingRefit.Totals.CargoRemaining = 41D,
                        "Unpriced street equipment should survive saving with its list cost, rating and capacity intact.")
                VehicleSheet.Write(sheetPath, editor.ReadDesign(), sheetCatalog)
                Require(File.ReadAllText(sheetPath).Contains("street price pending GM review") AndAlso File.ReadAllText(sheetPath).Contains("GM review"),
                        "The exported sheet must identify unresolved street prices for the GM.")
                Dim bookPriced As New StockDesign With {.ChassisId = "van", .PowerPlantId = "diesel"}
                For Each classification In [Enum].GetValues(Of VehicleAvailabilityClass)()
                    bookPriced.AvailabilityClass = classification
                    Dim expected = If(classification = VehicleAvailabilityClass.Standard, 1D, If(classification = VehicleAvailabilityClass.Military, 3D, 2D))
                    Require(bookPriced.Calculate(sheetCatalog).EffectiveStreetIndex = expected,
                            "Vehicle class should supply the p. 113 base Street Index when no override is supplied.")
                Next
                bookPriced.AvailabilityClass = VehicleAvailabilityClass.Standard
                bookPriced.CatalogModifications = New List(Of ModificationSelection) From {New ModificationSelection With {.Id = "smart-materials", .Quantity = 1}}
                Require(bookPriced.Calculate(sheetCatalog).EffectiveStreetIndex = 2D, "Smart materials should add the p. 118 Street Index modifier.")
                bookPriced.SpecialtyClass = VehicleSpecialtyClass.Medical
                Require(bookPriced.Calculate(sheetCatalog).EffectiveStreetIndex = 2.5D, "Medical specialty should add 0.5 to the base index.")
                bookPriced.SpecialtyClass = VehicleSpecialtyClass.NonCombat
                Require(bookPriced.Calculate(sheetCatalog).EffectiveStreetIndex = 3D, "Noncombat specialty should add 1 to the base index.")
                editor.WriteDesignFile(destination, bookPriced)
                editor.LoadDesignFile(destination)
                Require(Not editor.ReadDesign().StreetIndex.HasValue AndAlso editor.ReadDesign().SpecialtyClass = VehicleSpecialtyClass.NonCombat AndAlso
                        editor.ReadDesign().Calculate(sheetCatalog).EffectiveStreetIndex = 3D, "Book pricing and specialty selection should survive an editor round trip.")
                bookPriced.StreetIndex = 4D
                Require(bookPriced.Calculate(sheetCatalog).EffectiveStreetIndex = 4D, "A saved final Street Index override must remain authoritative.")
                electronicAircraft.CatalogModifications = New List(Of ModificationSelection) From {
                    New ModificationSelection With {.Id = "aircraft-drop-tanks", .Quantity = 2}
                }
                Dim dropTankResult = electronicAircraft.Calculate(sheetCatalog)
                Require(dropTankResult.ModifiedRatings.FuelCapacity.GetValueOrDefault() = 9000D AndAlso
                        dropTankResult.ModifiedRatings.Speed.GetValueOrDefault() = 305 AndAlso
                        dropTankResult.ModifiedRatings.Handling.GetValueOrDefault() = 7 AndAlso
                        dropTankResult.OperatingModes.Single().Speed.GetValueOrDefault() = 320 AndAlso
                        dropTankResult.Totals.DirectCostNuyen = 16000D,
                        "Drop tanks should add fuel, mounting penalties, cost, and a jettisoned operating mode.")
                electronicAircraft.CatalogModifications = New List(Of ModificationSelection) From {
                    New ModificationSelection With {.Id = "external-fixed-firmpoint", .Quantity = 1},
                    New ModificationSelection With {.Id = "remote-micro-turret", .Quantity = 1},
                    New ModificationSelection With {.Id = "ammo-bin-small-arms", .Quantity = 10}
                }
                Dim ammoResult = electronicAircraft.Calculate(sheetCatalog)
                Require(ammoResult.Totals.CargoRemaining = 497.5D AndAlso ammoResult.Totals.LoadRemainingKg = 4980D,
                        "Design-time ammunition bins should use their cargo blocks without the customization-only installation weight.")
                Dim externalCargoVan As New StockDesign With {
                    .ChassisId = "van", .PowerPlantId = "diesel", .StreetIndex = 1,
                    .InstalledRetrofits = New List(Of ModificationSelection) From {
                        New ModificationSelection With {.Id = "external-cargo-mounts", .Quantity = 12}
                    }
                }
                editor.WriteDesignFile(destination, externalCargoVan)
                editor.LoadDesignFile(destination)
                Dim externalCargoResult = editor.ReadDesign().Calculate(sheetCatalog)
                Require(externalCargoResult.ExternalCargoCapacity = 12D AndAlso externalCargoResult.RetrofitPartsCostNuyen.GetValueOrDefault() = 3000D AndAlso
                        externalCargoResult.Totals.CargoRemaining = 48D AndAlso externalCargoResult.OperatingModes.Single().Name = "External cargo mounts loaded",
                        "External mounts should store separate cargo capacity and report the loaded Handling modifier.")
                Require(JsonSerializer.Serialize(externalCargoResult).Contains("""ExternalCargoCapacity"":12"),
                        "Stock calculator results should serialize new value-type fields for the command-line entry point.")
                Console.WriteLine("Windows editor passed component selection, calculation, save, overwrite, and reopen for ground, marine, aircraft, submarine, and unpowered designs.")
            End Using
        Finally
            folder.Delete(recursive:=True)
        End Try
    End Sub

    Private Function FindControl(Of T As Control)(editor As Form, name As String) As T
        Return DirectCast(editor.Controls.Find(name, searchAllChildren:=True).Single(), T)
    End Function

    Private Sub Require(condition As Boolean, failure As String)
        If Not condition Then Throw New InvalidOperationException(failure)
    End Sub
End Module
