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
                        reopened.StreetIndex = 2D AndAlso reopened.Additions.Single().LoadConsumedKg = 25D AndAlso
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
                    Require(dialog.ReadSelection().Single(Function(item) item.Id = "performance-tires").Quantity = 4,
                            "The catalog dialog did not restore the selected quantity.")
                End Using
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
