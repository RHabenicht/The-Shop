Imports System.IO
Imports System.Text.Json
Imports System.Text.Json.Serialization

Module Program
    Function Main(args As String()) As Integer
        If args.Length <> 1 Then
            Console.Error.WriteLine("Usage: TheShop.Design <vehicle.json>")
            Return 2
        End If

        Try
            Dim options As New JsonSerializerOptions With {
                .UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                .WriteIndented = True
            }
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
