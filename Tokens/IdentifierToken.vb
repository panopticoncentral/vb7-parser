'
' Visual Basic .NET Parser
'
' Copyright (C) 2004, Microsoft Corporation. All rights reserved.
'
' THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
' EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
' MERCHANTIBILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
'

''' <summary>
''' An identifier.
''' </summary>
Public NotInheritable Class IdentifierToken
    Inherits Token

    ' Returns the token type of the string.
    Friend Shared Function TokenTypeFromString(ByVal s As String) As TokenType
        Dim Keyword As Object
        Static KeywordTable As Hashtable

        If KeywordTable Is Nothing Then
            Dim Table As Hashtable
            ' NOTE: These have to be in the same order as the enum!
            Dim Keywords() As String = { _
                "AddHandler", "AddressOf", "Alias", "And", "AndAlso", "Ansi", "As", "Assembly", "Auto", "Boolean", "ByRef", _
                "Byte", "ByVal", "Call", "Case", "Catch", "CBool", "CByte", "CChar", "CDate", "CDec", "CDbl", "Char", "CInt", _
                "Class", "CLng", "CObj", "Const", "CShort", "CSng", "CStr", "CType", "Date", "Decimal", "Declare", "Default", _
                "Delegate", "Dim", "DirectCast", "Do", "Double", "Each", "Else", "ElseIf", "End", "Endif", "Enum", "Erase", _
                "Error", "Event", "Exit", "False", "Finally", "For", "Friend", "Function", "Get", "GetType", "GoSub", "GoTo", _
                "Handles", "If", "Implements", "Imports", "In", "Inherits", "Integer", "Interface", "Is", "Let", "Lib", "Like", _
                "Long", "Loop", "Me", "Mod", "Module", "MustInherit", "MustOverride", "MyBase", "MyClass", "Namespace", "New", _
                "Next", "Not", "Nothing", "NotInheritable", "NotOverridable", "Object", "On", "Option", "Optional", "Or", _
                "OrElse", "Overloads", "Overridable", "Overrides", "ParamArray", "Preserve", "Private", "Property", "Protected", _
                "Public", "RaiseEvent", "ReadOnly", "ReDim", "REM", "RemoveHandler", "Resume", "Return", "Select", "Set", _
                "Shadows", "Shared", "Short", "Single", "Static", "Step", "Stop", "String", "Structure", "Sub", "SyncLock", _
                "Then", "Throw", "To", "True", "Try", "TypeOf", "Unicode", "Until", "Variant", "Wend", "When", "While", _
                "With", "WithEvents", "WriteOnly", "Xor"}

            Table = New Hashtable(CaseInsensitiveHashCodeProvider.DefaultInvariant, CaseInsensitiveComparer.DefaultInvariant)
            Debug.Assert(Keywords.Length = (TokenType.Xor - TokenType.AddHandler + 1))

            For Type As TokenType = TokenType.AddHandler To TokenType.Xor
                With Table
                    .Add(Keywords(Type - TokenType.AddHandler), Type)
                    .Add(Scanner.MakeFullWidth(Keywords(Type - TokenType.AddHandler)), Type)
                End With
            Next

            KeywordTable = Table
        End If

        Keyword = KeywordTable(s)

        If Keyword Is Nothing Then
            Return TokenType.Identifier
        Else
            Return DirectCast(Keyword, TokenType)
        End If
    End Function

    ''' <summary>
    ''' Determines if a token type is a keyword.
    ''' </summary>
    ''' <param name="type">The token type to check.</param>
    ''' <returns>True if the token type is a keyword, False otherwise.</returns>
    Public Shared Function IsKeyword(ByVal type As TokenType) As Boolean
        Return type >= TokenType.AddHandler AndAlso type <= TokenType.Xor
    End Function

    Private ReadOnly _Identifier As String
    Private ReadOnly _Escaped As Boolean                  ' Whether the identifier was escaped (i.e. [a])
    Private ReadOnly _TypeCharacter As TypeCharacter      ' The type character that followed, if any

    ''' <summary>
    ''' The identifier name.
    ''' </summary>
    Public ReadOnly Property Identifier() As String
        Get
            Return _Identifier
        End Get
    End Property

    ''' <summary>
    ''' Whether the identifier is escaped.
    ''' </summary>
    Public ReadOnly Property Escaped() As Boolean
        Get
            Return _Escaped
        End Get
    End Property

    ''' <summary>
    ''' The type character of the identifier.
    ''' </summary>
    Public ReadOnly Property TypeCharacter() As TypeCharacter
        Get
            Return _TypeCharacter
        End Get
    End Property

    ''' <summary>
    ''' Constructs a new identifier token.
    ''' </summary>
    ''' <param name="type">The token type of the identifier.</param>
    ''' <param name="identifier">The text of the identifier</param>
    ''' <param name="escaped">Whether the identifier is escaped.</param>
    ''' <param name="typeCharacter">The type character of the identifier.</param>
    ''' <param name="span">The location of the identifier.</param>
    Public Sub New(ByVal type As TokenType, ByVal identifier As String, ByVal escaped As Boolean, ByVal typeCharacter As TypeCharacter, ByVal span As Span)
        MyBase.New(type, span)

        If type <> TokenType.Identifier AndAlso Not IsKeyword(type) Then
            Throw New ArgumentOutOfRangeException("type")
        End If

        If identifier Is Nothing OrElse identifier = "" Then
            Throw New ArgumentException("Identifier cannot be empty.", "identifier")
        End If

        If typeCharacter < typeCharacter.None OrElse typeCharacter > typeCharacter.DecimalChar Then
            Throw New ArgumentOutOfRangeException("typeCharacter")
        End If

        If typeCharacter <> typeCharacter.None AndAlso escaped Then
            Throw New ArgumentException("Escaped identifiers cannot have type characters.")
        End If

        _Identifier = identifier
        _Escaped = escaped
        _TypeCharacter = typeCharacter
    End Sub
End Class