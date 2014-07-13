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
''' The base class for all tokens. Contains line and column information as well as token type.
''' </summary>
Public MustInherit Class Token
    Private ReadOnly _Type As TokenType
    Private ReadOnly _Span As Span      ' A span ends at the first character beyond the token

    ''' <summary>
    ''' The type of the token.
    ''' </summary>
    Public ReadOnly Property Type() As TokenType
        Get
            Return _Type
        End Get
    End Property

    ''' <summary>
    ''' The span of the token in the source text.
    ''' </summary>
    Public ReadOnly Property Span() As Span
        Get
            Return _Span
        End Get
    End Property

    Protected Sub New(ByVal type As TokenType, ByVal span As Span)
        Debug.Assert([Enum].IsDefined(GetType(TokenType), type))
        _Type = type
        _Span = span
    End Sub
End Class