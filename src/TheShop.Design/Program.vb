Imports System.IO
Imports System.Text.Json
Imports System.Text.Json.Serialization

Module Program
    Function Main(args As String()) As Integer
        Dim listCatalog = args.Length = 1 AndAlso args(0) = "--catalog"
        Dim stockDesign = args.Length = 2 AndAlso args(0) = "--stock"
        If args.Length <> 1 AndAlso Not stockDesign Then
            Console.Error.WriteLine("Usage: TheShop.Design <vehicle.json> | --catalog | --stock <vehicle.json>")
            Return 2
        End If

        Try
            Dim options As New JsonSerializerOptions With {
                .UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                .WriteIndented = True
            }
            If listCatalog Then
                Console.WriteLine(JsonSerializer.Serialize(StockCatalog.Read(options), options))
                Return 0
            End If

            If stockDesign Then
                Dim stock = JsonSerializer.Deserialize(Of StockDesign)(File.ReadAllText(args(1)), options)
                If stock Is Nothing Then
                    Throw New JsonException("Expected a stock vehicle design object.")
                End If
                Console.WriteLine(JsonSerializer.Serialize(stock.Calculate(StockCatalog.Read(options)), options))
                Return 0
            End If

            Dim design = JsonSerializer.Deserialize(Of VehicleDesign)(File.ReadAllText(args(0)), options)
            If design Is Nothing Then
                Throw New JsonException("Expected a vehicle design object.")
            End If
            Console.WriteLine(JsonSerializer.Serialize(design.Calculate(), options))
            Return 0
        Catch ex As Exception When TypeOf ex Is IOException OrElse
                                   TypeOf ex Is UnauthorizedAccessException OrElse
                                   TypeOf ex Is JsonException OrElse
                                   TypeOf ex Is ArgumentException OrElse
                                   TypeOf ex Is OverflowException
            Console.Error.WriteLine(ex.Message)
            Return 1
        End Try
    End Function
End Module
