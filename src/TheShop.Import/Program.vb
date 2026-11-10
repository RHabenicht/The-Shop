Imports System.IO
Imports System.Text.Json
Imports Microsoft.VisualBasic.FileIO
Imports TheShop.Design

Module Program
    Function Main(args As String()) As Integer
        If args.Length <> 1 Then
            Console.Error.WriteLine("Usage: TheShop.Import <legacy vehicle.shp>")
            Return 2
        End If

        Try
            Console.WriteLine(JsonSerializer.Serialize(LegacyVehicleReader.Read(args(0)), New JsonSerializerOptions With {
                .WriteIndented = True
            }))
            Return 0
        Catch ex As Exception When TypeOf ex Is IOException OrElse
                                   TypeOf ex Is UnauthorizedAccessException OrElse
                                   TypeOf ex Is FormatException OrElse
                                   TypeOf ex Is MalformedLineException
            Console.Error.WriteLine(ex.Message)
            Return 1
        End Try
    End Function

End Module
