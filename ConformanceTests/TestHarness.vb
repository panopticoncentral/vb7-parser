'
' Visual Basic .NET Parser
'
' Copyright (C) 2004, Microsoft Corporation. All rights reserved.
'
' THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
' EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
' MERCHANTIBILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
'

Imports System.IO
Imports System.Reflection
Imports System.Text
Imports System.Xml
Imports System.Xml.Serialization
Imports VBParser

' UNDONE: Need conformance tests for full-width characters

Public Enum ScenarioType
    Token
    Expression
    Statement
    Declaration
    TypeName
    File
    Max = File
End Enum

Public Structure Test
    Public Element As XmlElement
    Public Code As String
    Public Result As String
    Public Errors As String
End Structure

Public Structure Scenario
    Public Element As XmlElement
    Public Document As XmlDocument
    Public Name As String
    Public Type As ScenarioType
    Public Tests() As Test
End Structure

Friend Class TestHarness
    Public Scenarios()() As Scenario

    Public Sub New()
        Dim Assem As [Assembly] = [Assembly].GetExecutingAssembly()
        Dim Resources() As String = Assem.GetManifestResourceNames()
        Dim ScenarioLists(ScenarioType.Max) As ArrayList
        Dim ScenarioCount As Integer = 0

        For Index As Integer = 0 To ScenarioType.Max
            ScenarioLists(Index) = New ArrayList
        Next

        For Each ResourceName As String In Resources
            Dim Stream As Stream = Assem.GetManifestResourceStream(ResourceName)
            Dim Scenario As Scenario
            Dim Tests As ArrayList

            If Not ResourceName.EndsWith(".xml") Then
                GoTo Continue
            End If

            If Tests Is Nothing Then
                Tests = New ArrayList
            Else
                Tests.Clear()
            End If

            Scenario.Document = New XmlDocument
            Scenario.Document.Load(Stream)
            Stream.Close()
            ScenarioCount += 1

            Scenario.Element = Scenario.Document("scenario")
            Scenario.Name = Scenario.Element.GetAttribute("name")

            Select Case Scenario.Element.GetAttribute("type")
                Case "token"
                    Scenario.Type = ScenarioType.Token

                Case "expression"
                    Scenario.Type = ScenarioType.Expression

                Case "statement"
                    Scenario.Type = ScenarioType.Statement

                Case "declaration"
                    Scenario.Type = ScenarioType.Declaration

                Case "typename"
                    Scenario.Type = ScenarioType.TypeName

                Case "file"
                    Scenario.Type = ScenarioType.File
            End Select

            For Each ChildNode As XmlNode In Scenario.Element.ChildNodes
                If TypeOf ChildNode Is XmlElement Then
                    Dim Test As Test

                    Test.Element = CType(ChildNode, XmlElement)
                    Test.Code = Test.Element("code").InnerText

                    If Not Test.Element("result") Is Nothing Then
                        Dim StringWriter As StringWriter = New StringWriter
                        Dim Writer As XmlTextWriter = New XmlTextWriter(StringWriter)

                        Writer.Formatting = Formatting.Indented
                        Test.Element("result").WriteContentTo(Writer)
                        Writer.Close()
                        StringWriter.Close()

                        Test.Result = StringWriter.ToString()
                    Else
                        Test.Result = Nothing
                    End If

                    If Not Test.Element("errors") Is Nothing Then
                        Dim StringWriter As StringWriter = New StringWriter
                        Dim Writer As XmlTextWriter = New XmlTextWriter(StringWriter)

                        Writer.Formatting = Formatting.Indented
                        Test.Element("errors").WriteContentTo(Writer)
                        Writer.Close()
                        StringWriter.Close()

                        Test.Errors = StringWriter.ToString()
                    Else
                        Test.Errors = Nothing
                    End If

                    Tests.Add(Test)
                End If
            Next

            Scenario.Tests = New Test(Tests.Count - 1) {}
            Tests.CopyTo(Scenario.Tests)

            ScenarioLists(Scenario.Type).Add(Scenario)
Continue:
        Next

        Scenarios = New Scenario(ScenarioType.Max)() {}

        For Index As Integer = 0 To ScenarioType.Max
            Scenarios(Index) = New Scenario(ScenarioLists(Index).Count - 1) {}
            ScenarioLists(index).CopyTo(Scenarios(Index))
        Next
    End Sub

    Private Sub RunScanTest(ByVal Test As Test, ByVal FormatResults As Boolean, ByRef Result As String, ByRef ErrorResult As String)
        Dim Scanner As Scanner = New Scanner(Test.Code)
        Dim StringWriter As StringWriter = New StringWriter
        Dim Writer As XmlTextWriter = New XmlTextWriter(StringWriter)
        Dim Serializer As XmlTokenSerializer = New XmlTokenSerializer(Writer)
        Dim Tokens() As Token

        If FormatResults Then
            Writer.Formatting = Formatting.Indented
        End If

        Tokens = Scanner.ReadToEnd()
        Serializer.Serialize(Tokens)
        Result = StringWriter.ToString()

        Writer.Close()
        StringWriter.Close()
    End Sub

    Private Sub RunParseTest(ByVal ScenarioType As ScenarioType, ByVal Test As Test, ByVal FormatResults As Boolean, ByRef Result As String, ByRef ErrorResult As String)
        Dim StringWriter As StringWriter = New StringWriter
        Dim Writer As XmlTextWriter = New XmlTextWriter(StringWriter)
        Dim Serializer As XmlTreeSerializer = New XmlTreeSerializer(Writer)
        Dim ErrorStringWriter As StringWriter = New StringWriter
        Dim ErrorWriter As XmlTextWriter = New XmlTextWriter(ErrorStringWriter)
        Dim ErrorSerializer As XmlErrorSerializer = New XmlErrorSerializer(ErrorWriter)
        Dim Errors As ArrayList = New ArrayList

        If FormatResults Then
            Writer.Formatting = Formatting.Indented
            ErrorWriter.Formatting = Formatting.Indented
        End If

        Select Case ScenarioType
            Case ScenarioType.Expression
                Dim Scanner As Scanner = New Scanner(Test.Code)
                Dim Parser As Parser = New Parser
                Dim Expression As Expression = Parser.ParseExpression(Scanner, Errors)
                Serializer.Serialize(Expression)

            Case ScenarioType.Statement
                Dim Scanner As Scanner = New Scanner(Test.Code)
                Dim Parser As Parser = New Parser
                Dim Statement As Statement = Parser.ParseStatement(Scanner, Errors)
                Serializer.Serialize(Statement)

            Case ScenarioType.Declaration
                Dim Scanner As Scanner = New Scanner(Test.Code.Trim())
                Dim Parser As Parser = New Parser
                Dim Declaration As Declaration = Parser.ParseDeclaration(Scanner, Errors)
                Serializer.Serialize(Declaration)

            Case ScenarioType.TypeName
                Dim Scanner As Scanner = New Scanner(Test.Code)
                Dim Parser As Parser = New Parser
                Dim TypeName As TypeName = Parser.ParseTypeName(Scanner, Errors)
                Serializer.Serialize(TypeName)

            Case ScenarioType.File
                Dim Scanner As Scanner = New Scanner(Test.Code.Trim())
                Dim Parser As Parser = New Parser
                Dim File As VBParser.File = Parser.ParseFile(Scanner, Errors)
                Serializer.Serialize(File)
        End Select

        Result = StringWriter.ToString()
        Writer.Close()
        StringWriter.Close()

        ErrorSerializer.Serialize(Errors)
        ErrorResult = ErrorStringWriter.ToString()
        ErrorWriter.Close()
        ErrorStringWriter.Close()
    End Sub

    Public Sub RunSingleTest(ByVal ScenarioType As ScenarioType, ByVal Test As Test, ByRef Result As String, ByRef ErrorResult As String, Optional ByVal FormatResults As Boolean = True)
        Select Case ScenarioType
            Case ScenarioType.Token
                RunScanTest(Test, FormatResults, Result, ErrorResult)

            Case ScenarioType.Expression, ScenarioType.Statement, ScenarioType.Declaration, ScenarioType.TypeName, ScenarioType.File
                RunParseTest(ScenarioType, Test, FormatResults, Result, ErrorResult)

            Case Else
                Debug.Fail("Unexpected.")
        End Select
    End Sub

    Public Function RunTest(ByVal ScenarioType As ScenarioType, ByVal Test As Test, ByRef AddedResults As Boolean) As Boolean
        Dim ActualResult As String
        Dim ActualErrors As String

        RunSingleTest(ScenarioType, Test, ActualResult, ActualErrors, Not Test.Result Is Nothing)

        If Not Test.Result Is Nothing Then
            Dim Result As String = Test.Result
            Dim Errors As String = Test.Errors

            Return ActualResult = Result AndAlso ActualErrors = Errors
        Else
            Dim ResultElement As XmlElement = Test.Element.OwnerDocument.CreateElement("result")
            Dim ResultFragment As XmlDocumentFragment = Test.Element.OwnerDocument.CreateDocumentFragment()

            ResultFragment.InnerXml = ActualResult
            ResultElement.AppendChild(ResultFragment)
            Test.Element.AppendChild(ResultElement)

            If ActualErrors <> "" Then
                Dim ErrorsElement As XmlElement = Test.Element.OwnerDocument.CreateElement("errors")
                Dim ErrorsFragment As XmlDocumentFragment = Test.Element.OwnerDocument.CreateDocumentFragment()

                ErrorsFragment.InnerXml = ActualErrors
                ErrorsElement.AppendChild(ErrorsFragment)
                Test.Element.AppendChild(ErrorsElement)
            End If

            AddedResults = True

            Return True
        End If
    End Function
End Class