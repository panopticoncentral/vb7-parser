'
' Visual Basic .NET Parser
'
' Copyright (C) 2004, Microsoft Corporation. All rights reserved.
'
' THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
' EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
' MERCHANTIBILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
'

Imports System.Xml
Imports VBParser

Public Class XmlTokenSerializer
    Private ReadOnly Writer As XmlWriter
    Private ReadOnly TypeCharacters() As String = {" ", "$", "%", "&", "S", "I", "L", "!", "#", "@", "F", "R", "D"}

    Public Sub New(ByVal Writer As XmlWriter)
        Me.Writer = Writer
    End Sub

    Private Sub Serialize(ByVal Span As Span)
        Writer.WriteAttributeString("startLine", CStr(Span.Start.Line))
        Writer.WriteAttributeString("startCol", CStr(Span.Start.Column))
        Writer.WriteAttributeString("endLine", CStr(Span.Finish.Line))
        Writer.WriteAttributeString("endCol", CStr(Span.Finish.Column))
    End Sub

    Private Sub Serialize(ByVal TypeCharacter As TypeCharacter)
        If TypeCharacter <> TypeCharacter.None Then
            Writer.WriteAttributeString("typeChar", TypeCharacters(TypeCharacter))
        End If
    End Sub

    Public Sub Serialize(ByVal Token As Token)
        Writer.WriteStartElement(Token.Type.ToString())
        Serialize(Token.Span)

        Select Case Token.Type
            Case TokenType.LexicalError
                With CType(Token, ErrorToken)
                    Writer.WriteAttributeString("errorNumber", CStr(.SyntaxError.Type))
                    Writer.WriteString(.SyntaxError.ToString())
                End With

            Case TokenType.Comment
                With CType(Token, CommentToken)
                    Writer.WriteAttributeString("isRem", CStr(.IsREM))
                    Writer.WriteString(.Comment)
                End With

            Case TokenType.Identifier
                With CType(Token, IdentifierToken)
                    Writer.WriteAttributeString("escaped", CStr(.Escaped))
                    Serialize(.TypeCharacter)
                    Writer.WriteString(.Identifier)
                End With

            Case TokenType.StringLiteral
                With CType(Token, StringLiteralToken)
                    Writer.WriteString(.Literal)
                End With

            Case TokenType.CharacterLiteral
                With CType(Token, CharacterLiteralToken)
                    Writer.WriteString(.Literal)
                End With

            Case TokenType.DateLiteral
                With CType(Token, DateLiteralToken)
                    Writer.WriteString(CStr(.Literal))
                End With

            Case TokenType.IntegerLiteral
                With CType(Token, IntegerLiteralToken)
                    Writer.WriteAttributeString("base", .IntegerBase.ToString())
                    Serialize(.TypeCharacter)
                    Writer.WriteString(CStr(.Literal))
                End With

            Case TokenType.FloatingPointLiteral
                With CType(Token, FloatingPointLiteralToken)
                    Serialize(.TypeCharacter)
                    Writer.WriteString(CStr(.Literal))
                End With

            Case TokenType.DecimalLiteral
                With CType(Token, DecimalLiteralToken)
                    Serialize(.TypeCharacter)
                    Writer.WriteString(CStr(.Literal))
                End With

            Case Else
                ' Fall through
        End Select

        Writer.WriteEndElement()
    End Sub

    Public Sub Serialize(ByVal Tokens() As Token)
        For Each Token As Token In Tokens
            Serialize(Token)
        Next
    End Sub

End Class
