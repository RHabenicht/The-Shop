Imports System.Globalization
Imports System.Text
Imports System.Text.Json
Imports Microsoft.VisualBasic.FileIO

Public Module LegacyVehicleReader
    Public Function Read(path As String) As JsonElement
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
        Using parser As New TextFieldParser(path, Encoding.GetEncoding(1252), detectEncoding:=True)
            parser.SetDelimiters(",")
            parser.TrimWhiteSpace = False
            parser.HasFieldsEnclosedInQuotes = True

            Dim version = ReadRecord(parser, 1, "file header")(0)
            If version <> "Shop v1.40" Then
                Throw New FormatException($"Unsupported vehicle header '{version}'. Expected Shop v1.40.")
            End If

            Dim name = ReadRecord(parser, 1, "vehicle name")(0)
            Dim chassis = ReadRecord(parser, 2, "chassis")
            Dim engine = ReadRecord(parser, 2, "engine")
            Dim seating = ReadRecord(parser, 1, "seating")(0)
            Dim entry = ReadRecord(parser, 1, "entry")(0)
            Dim markups = ReadRecord(parser, 5, "markups")
            Dim modifications As New List(Of Object)

            While Not parser.EndOfData
                Dim fields = ReadRecord(parser, 3, "modification")
                Dim level As Integer
                If Not Integer.TryParse(fields(1), NumberStyles.Integer, CultureInfo.InvariantCulture, level) Then
                    Throw New FormatException($"Invalid modification level '{fields(1)}'.")
                End If
                modifications.Add(New With {.Book = fields(0), .Level = level, .Name = fields(2)})
            End While

            Return JsonSerializer.SerializeToElement(New With {
                .SourceFormat = version,
                .SourceRules = "Rigger 2",
                .Name = name,
                .Chassis = New With {.Book = chassis(0), .Name = chassis(1)},
                .Engine = New With {.Book = engine(0), .Name = engine(1)},
                .Seating = seating,
                .Entry = entry,
                .Markups = New With {
                    .Ambulance = ReadMarkup(markups(0)),
                    .NonCommercial = ReadMarkup(markups(1)),
                    .Unusual = ReadMarkup(markups(2)),
                    .Luxury = ReadMarkup(markups(3)),
                    .Special = ReadMarkup(markups(4))
                },
                .Modifications = modifications
            })
        End Using
    End Function

    Private Function ReadRecord(parser As TextFieldParser, fieldCount As Integer, label As String) As String()
        Dim line = parser.LineNumber
        Dim fields = parser.ReadFields()
        If fields Is Nothing OrElse fields.Length <> fieldCount Then
            Throw New FormatException($"Expected {fieldCount} field(s) for {label} at line {line}.")
        End If
        Return fields
    End Function

    Private Function ReadMarkup(value As String) As Double
        Dim result As Double
        If Not Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, result) OrElse
           Not Double.IsFinite(result) Then
            Throw New FormatException($"Invalid markup '{value}'.")
        End If
        Return result
    End Function
End Module
