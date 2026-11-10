Public Module ElectronicRetrofits
    Public Function Contains(id As String) As Boolean
        Return {"sensors", "sonar", "ecm", "eccm", "electronic-deception", "electronic-counter-deception"}.Contains(id)
    End Function

    Public Function Quote(id As String, rating As Integer) As RetrofitQuote
        Dim prices As Decimal()
        Dim cargo As Integer()
        Dim load As Integer()
        Dim indices As Decimal?()
        Dim availability As String()
        Dim sensorCargo = {0, 1, 2, 3, 3, 6, 8, 9, 12, 16, 20}
        Dim sensorAvailability = {"", "4 / 7 days", "4 / 10 days", "5 / 14 days", "6 / 21 days",
            "8 / 30 days", "10 / 45 days", "12 / 60 days", "14 / 3 months", "16 / 6 months", "18 / 1 year"}
        Dim sensorIndices As Decimal?() = {Nothing, 2D, 2.5D, 3D, 3.5D, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing}
        Select Case id
            Case "sensors"
                prices = {0D, 5000D, 7500D, 10000D, 12500D, 15000D, 20000D, 30000D, 50000D, 100000D, 500000D}
                cargo = sensorCargo
                load = {0, 12, 20, 25, 35, 50, 75, 110, 150, 200, 250}
                indices = sensorIndices
                availability = sensorAvailability
            Case "sonar"
                prices = {0D, 5000D, 25000D, 125000D, 625000D, 2000000D, 5000000D, 15000000D, 20000000D, 30000000D, 50000000D}
                cargo = {0, 6, 8, 14, 25, 75, 100, 150, 300, 500, 750}
                load = {0, 120, 250, 350, 500, 750, 1000, 1250, 1500, 2000, 2500}
                indices = sensorIndices
                availability = sensorAvailability
            Case "ecm"
                prices = {0D, 10000D, 20000D, 30000D, 50000D, 75000D, 100000D, 200000D, 300000D, 500000D, 1000000D}
                cargo = sensorCargo
                load = {0, 5, 10, 15, 20, 25, 50, 75, 100, 150, 250}
                indices = New Decimal?() {Nothing, 2.5D, 3D, 3.5D, 4D, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing}
                availability = {"", "5 / 7 days", "6 / 10 days", "7 / 14 days", "8 / 21 days",
                    "10 / 30 days", "12 / 45 days", "14 / 60 days", "16 / 3 months", "18 / 6 months", "20 / 1 year"}
            Case "eccm"
                prices = {0D, 10000D, 20000D, 30000D, 40000D, 50000D, 75000D, 100000D, 250000D, 300000D, 500000D}
                cargo = sensorCargo
                load = {0, 3, 5, 8, 12, 18, 25, 50, 75, 100, 150}
                indices = sensorIndices
                availability = sensorAvailability
            Case "electronic-deception"
                prices = {0D, 15000D, 30000D, 50000D, 75000D, 100000D, 250000D}
                cargo = {0, 2, 3, 5, 6, 7, 9}
                load = {0, 10, 20, 30, 45, 50, 75}
                indices = New Decimal?() {Nothing, 3D, 3.5D, 4D, 4.5D, 5D, Nothing}
                availability = {"", "8 / 30 days", "8 / 45 days", "8 / 60 days", "10 / 3 months", "12 / 6 months", "16 / 1 year"}
            Case "electronic-counter-deception"
                prices = {0D, 10000D, 30000D, 50000D, 75000D, 150000D, 300000D}
                cargo = {0, 2, 3, 4, 5, 6, 8}
                load = {0, 25, 30, 35, 45, 60, 75}
                indices = New Decimal?() {Nothing, 3D, 3.5D, 4D, 4.5D, 5D, Nothing}
                availability = {"", "8 / 30 days", "8 / 45 days", "8 / 60 days", "10 / 3 months", "12 / 6 months", "16 / 1 year"}
            Case Else
                Throw New ArgumentException("Unknown electronic retrofit system.")
        End Select
        If rating < 1 OrElse rating >= prices.Length Then Throw New ArgumentException("The electronic system rating is outside its table.")
        Return New RetrofitQuote With {
            .PartsCostNuyen = prices(rating), .StreetIndex = indices(rating), .Availability = availability(rating),
            .CargoConsumed = cargo(rating), .LoadConsumedKg = load(rating)
        }
    End Function
End Module
