Imports TheShop.Design

Friend Module PrintedTables
    ' Independent transcription of the printed tables, R3 Revised pp. 140–141, 144–145, 147–148.
    Private NotInheritable Class ElectronicTable
        Public Id As String
        Public Points As Decimal()
        Public Parts As Decimal()
        Public DesignCargo As Decimal()
        Public RetrofitCargo As Decimal()
        Public Mass As Decimal()
        Public Indices As Decimal?()
        Public Availability As String()
    End Class

    Public Sub Run(test As Action(Of String, Action))
        Dim standardAvailability = {"4 / 7 days", "4 / 10 days", "5 / 14 days", "6 / 21 days", "8 / 30 days", "10 / 45 days", "12 / 60 days", "14 / 3 months", "16 / 6 months", "18 / 1 year"}
        Dim standardIndices As Decimal?() = {2D, 2.5D, 3D, 3.5D, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing}
        Dim commonDesignCargo = {0D, 1D, 2D, 2D, 3D, 4D, 6D, 10D, 12D, 16D}
        Dim commonRetrofitCargo = {1D, 2D, 3D, 3D, 6D, 8D, 9D, 12D, 16D, 20D}
        Dim deceptionAvailability = {"8 / 30 days", "8 / 45 days", "8 / 60 days", "10 / 3 months", "12 / 6 months", "16 / 1 year"}
        Dim deceptionIndices As Decimal?() = {3D, 3.5D, 4D, 4.5D, 5D, Nothing}
        Dim tables = {
            New ElectronicTable With {.Id = "sensors", .Points = {50D, 75D, 100D, 125D, 150D, 200D, 300D, 500D, 1000D, 5000D},
                .Parts = {5000D, 7500D, 10000D, 12500D, 15000D, 20000D, 30000D, 50000D, 100000D, 500000D}, .DesignCargo = commonDesignCargo, .RetrofitCargo = commonRetrofitCargo,
                .Mass = {12D, 20D, 25D, 35D, 50D, 75D, 110D, 150D, 200D, 250D}, .Indices = standardIndices, .Availability = standardAvailability},
            New ElectronicTable With {.Id = "sonar", .Points = {50D, 500D, 2500D, 5000D, 10000D, 25000D, 50000D, 75000D, 125000D, 250000D},
                .Parts = {5000D, 25000D, 125000D, 625000D, 2000000D, 5000000D, 15000000D, 20000000D, 30000000D, 50000000D},
                .DesignCargo = {4D, 6D, 10D, 16D, 50D, 75D, 100D, 200D, 300D, 500D}, .RetrofitCargo = {6D, 8D, 14D, 25D, 75D, 100D, 150D, 300D, 500D, 750D},
                .Mass = {120D, 250D, 350D, 500D, 750D, 1000D, 1250D, 1500D, 2000D, 2500D}, .Indices = standardIndices, .Availability = standardAvailability},
            New ElectronicTable With {.Id = "ecm", .Points = {100D, 200D, 300D, 500D, 750D, 1000D, 2000D, 3000D, 5000D, 10000D},
                .Parts = {10000D, 20000D, 30000D, 50000D, 75000D, 100000D, 200000D, 300000D, 500000D, 1000000D}, .DesignCargo = commonDesignCargo, .RetrofitCargo = commonRetrofitCargo,
                .Mass = {5D, 10D, 15D, 20D, 25D, 50D, 75D, 100D, 150D, 250D}, .Indices = {2.5D, 3D, 3.5D, 4D, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing},
                .Availability = {"5 / 7 days", "6 / 10 days", "7 / 14 days", "8 / 21 days", "10 / 30 days", "12 / 45 days", "14 / 60 days", "16 / 3 months", "18 / 6 months", "20 / 1 year"}},
            New ElectronicTable With {.Id = "eccm", .Points = {100D, 200D, 300D, 400D, 500D, 750D, 1000D, 2500D, 3000D, 5000D},
                .Parts = {10000D, 20000D, 30000D, 40000D, 50000D, 75000D, 100000D, 250000D, 300000D, 500000D}, .DesignCargo = commonDesignCargo, .RetrofitCargo = commonRetrofitCargo,
                .Mass = {3D, 5D, 8D, 12D, 18D, 25D, 50D, 75D, 100D, 150D}, .Indices = standardIndices, .Availability = standardAvailability},
            New ElectronicTable With {.Id = "electronic-deception", .Points = {150D, 300D, 500D, 750D, 1000D, 2500D}, .Parts = {15000D, 30000D, 50000D, 75000D, 100000D, 250000D},
                .DesignCargo = {1D, 2D, 4D, 5D, 6D, 8D}, .RetrofitCargo = {2D, 3D, 5D, 6D, 7D, 9D}, .Mass = {10D, 20D, 30D, 45D, 50D, 75D}, .Indices = deceptionIndices, .Availability = deceptionAvailability},
            New ElectronicTable With {.Id = "electronic-counter-deception", .Points = {100D, 300D, 500D, 750D, 1500D, 3000D}, .Parts = {10000D, 30000D, 50000D, 75000D, 150000D, 300000D},
                .DesignCargo = {1D, 2D, 3D, 4D, 6D, 8D}, .RetrofitCargo = {2D, 3D, 4D, 5D, 6D, 8D}, .Mass = {25D, 30D, 35D, 45D, 60D, 75D}, .Indices = deceptionIndices, .Availability = deceptionAvailability}}
        Dim fixture = Chassis()
        Dim catalog As IReadOnlyList(Of StockChassis) = {fixture}
        For Each table In tables
            For row = 0 To table.Points.Length - 1
                Dim i = row
                test(table.Id & " rating " & (i + 1), Sub()
                    Dim selection As New ModificationSelection With {.Id = table.Id, .Quantity = i + 1}
                    Dim d As New StockDesign With {.ChassisId = fixture.Id, .PowerPlantId = "diesel", .CatalogModifications = New List(Of ModificationSelection) From {selection}}
                    Dim made = d.Calculate(catalog)
                    Number(made.AppliedModifications.Single().DesignPoints, table.Points(i), "Design points")
                    Number(made.Totals.CargoRemaining, 1000D - table.DesignCargo(i), "Manufacture CF")
                    Number(made.Totals.LoadRemainingKg, 100000D - table.Mass(i), "Manufacture Load")
                    d.CatalogModifications = Nothing
                    d.InstalledRetrofits = New List(Of ModificationSelection) From {selection}
                    Dim installed = d.Calculate(catalog)
                    Dim quote = installed.InstalledRetrofits.Single().Quote
                    Number(quote.PartsCostNuyen, table.Parts(i), "Parts")
                    Number(installed.Totals.CargoRemaining, 1000D - table.RetrofitCargo(i), "Customization CF")
                    Number(installed.Totals.LoadRemainingKg, 100000D - table.Mass(i), "Customization Load")
                    If quote.StreetIndex <> table.Indices(i) OrElse quote.StreetIndex.HasValue <> table.Indices(i).HasValue Then Throw New InvalidOperationException("Street Index differs from printed table.")
                    If quote.Availability <> table.Availability(i) Then Throw New InvalidOperationException("Availability differs from printed table.")
                    If installed.RetrofitPartsCostNuyen.HasValue <> table.Indices(i).HasValue Then Throw New InvalidOperationException("Unlisted street prices must remain unknown.")
                    selection.StreetIndexOverride = 1.25D
                    Number(d.Calculate(catalog).RetrofitPartsCostNuyen.Value, table.Parts(i) * 1.25D, "Explicit Street Index")
                End Sub)
            Next
        Next
        Dim turrets = {
            (Id:="crewed-mini-turret", Points:=125D, DesignCf:=6D, Parts:=5000D, RetrofitCf:=7D, Mass:=25D, Capacity:=2D),
            (Id:="crewed-small-turret", Points:=250D, DesignCf:=7D, Parts:=7500D, RetrofitCf:=8D, Mass:=100D, Capacity:=3D),
            (Id:="crewed-medium-turret", Points:=500D, DesignCf:=16D, Parts:=15000D, RetrofitCf:=24D, Mass:=1000D, Capacity:=6D),
            (Id:="crewed-large-turret", Points:=1500D, DesignCf:=60D, Parts:=300000D, RetrofitCf:=72D, Mass:=6000D, Capacity:=8D),
            (Id:="crewed-extra-large-turret", Points:=3000D, DesignCf:=120D, Parts:=1000000D, RetrofitCf:=144D, Mass:=30000D, Capacity:=10D),
            (Id:="remote-micro-turret", Points:=100D, DesignCf:=0D, Parts:=2500D, RetrofitCf:=1D, Mass:=10D, Capacity:=1D),
            (Id:="remote-mini-turret", Points:=175D, DesignCf:=3D, Parts:=6000D, RetrofitCf:=4D, Mass:=25D, Capacity:=2D),
            (Id:="remote-small-turret", Points:=350D, DesignCf:=4D, Parts:=9000D, RetrofitCf:=5D, Mass:=100D, Capacity:=3D),
            (Id:="remote-medium-turret", Points:=600D, DesignCf:=4D, Parts:=17500D, RetrofitCf:=8D, Mass:=1000D, Capacity:=6D),
            (Id:="remote-large-turret", Points:=2500D, DesignCf:=32D, Parts:=600000D, RetrofitCf:=36D, Mass:=8000D, Capacity:=8D),
            (Id:="remote-extra-large-turret", Points:=5000D, DesignCf:=64D, Parts:=2000000D, RetrofitCf:=72D, Mass:=35000D, Capacity:=10D)}
        For Each turret In turrets
            For Each suffix In {"", "-anti-aircraft", "-pop-up", "-anti-aircraft-pop-up"}
                If suffix.Contains("pop-up") AndAlso turret.Capacity >= 8D Then Continue For
                Dim id = turret.Id & suffix
                Dim aa = suffix.Contains("anti-aircraft"), popup = suffix.Contains("pop-up"), micro = turret.Capacity = 1D
                test(id & " table values", Sub()
                    Dim d As New StockDesign With {.ChassisId = fixture.Id, .PowerPlantId = "diesel", .CatalogModifications = New List(Of ModificationSelection) From {New ModificationSelection With {.Id = id, .Quantity = 1}}}
                    Dim made = d.Calculate(catalog)
                    Dim cf = (turret.DesignCf + If(aa, 1D, 0D)) * If(popup, 2D, 1D)
                    If micro AndAlso popup AndAlso Not aa Then cf = 1D
                    Number(made.AppliedModifications.Single().DesignPoints, turret.Points * If(aa, 1.5D, 1D), "Turret DP")
                    Number(made.Totals.CargoRemaining, 1000D - cf, "Turret manufacture CF")
                    Number(made.Totals.LoadRemainingKg, 100000D - turret.Mass, "Turret manufacture mass")
                    Number(VehiclePayload.WeaponCapacity(id), turret.Capacity - If(popup AndAlso Not micro, 1D, 0D), "Turret Weapon Value")
                    d.InstalledRetrofits = d.CatalogModifications
                    d.CatalogModifications = Nothing
                    Dim installed = d.Calculate(catalog)
                    Dim retrofitCf = (turret.RetrofitCf + If(aa, 1D, 0D)) * If(popup, 2D, 1D)
                    If micro AndAlso popup AndAlso Not aa Then retrofitCf = 1D
                    Number(installed.RetrofitListPartsCostNuyen, turret.Parts * If(aa, 1.5D, 1D), "Turret parts")
                    Number(installed.Totals.CargoRemaining, 1000D - retrofitCf, "Turret customization CF")
                    Number(installed.Totals.LoadRemainingKg, 100000D - turret.Mass, "Turret customization mass")
                End Sub)
            Next
        Next
    End Sub

    Private Function Chassis() As StockChassis
        Return New StockChassis With {.Id = "audit-fixture", .Name = "Table audit fixture", .Category = "Ship", .Hull = 1, .Handling = 4, .Markup = 1D,
            .StartingCargo = 1000D, .MaximumCargo = 1000D, .PowerPlants = {New StockPowerPlant With {.Id = "diesel", .Name = "Fixture", .StartingLoadKg = 100000D, .MaximumLoadKg = 100000D, .Signature = 4}}}
    End Function
    Private Sub Number(actual As Decimal, expected As Decimal, label As String)
        If actual <> expected Then Throw New InvalidOperationException($"{label}: expected {expected}; got {actual}.")
    End Sub
End Module
