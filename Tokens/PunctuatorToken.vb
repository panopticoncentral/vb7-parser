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
''' A punctuation token.
''' </summary>
Public NotInheritable Class PunctuatorToken
    Inherits Token

    ' Returns the token type of the string.
    Friend Shared Function TokenTypeFromString(ByVal s As String) As TokenType
        Dim Punctuator As Object
        Static PunctuatorTable As Hashtable

        If PunctuatorTable Is Nothing Then
            Dim Table As Hashtable
            ' NOTE: These have to be in the same order as the enum!
            Dim Punctuators() As String = { _
                "(", ")", "{", "}", "!", "#", ",", ".", ":", ":=", "&", "&=", "*", "*=", "+", "+=", "-", "-=", "/", "/=", _
                "\", "\=", "^", "^=", "<", "<=", "=", "<>", ">", ">=", "<<", "<<=", ">>", ">>="}

            Table = New Hashtable

            Debug.Assert(Punctuators.Length = (TokenType.GreaterThanGreaterThanEquals - TokenType.LeftParenthesis + 1))

            For Type As TokenType = TokenType.LeftParenthesis To TokenType.GreaterThanGreaterThanEquals
                With Table
                    .Add(Punctuators(Type - TokenType.LeftParenthesis), Type)
                    .Add(Scanner.MakeFullWidth(Punctuators(Type - TokenType.LeftParenthesis)), Type)
                End With
            Next

            PunctuatorTable = Table
        End If

        Punctuator = PunctuatorTable(s)

        If Punctuator Is Nothing Then
            Return TokenType.None
        Else
            Return DirectCast(Punctuator, TokenType)
        End If
    End Function

    ''' <summary>
    ''' Constructs a new punctuator token.
    ''' </summary>
    ''' <param name="type">The punctuator token type.</param>
    ''' <param name="span">The location of the punctuator.</param>
    Public Sub New(ByVal type As TokenType, ByVal span As Span)
        MyBase.New(type, span)

        If (type < TokenType.LeftParenthesis OrElse type > TokenType.GreaterThanGreaterThanEquals) Then
            Throw New ArgumentOutOfRangeException("type")
        End If
    End Sub
End Class