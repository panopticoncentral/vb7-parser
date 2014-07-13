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

Public Class XmlTreeSerializer
    Private ReadOnly Writer As XmlWriter
    Private ReadOnly TypeCharacters() As String = {" ", "$", "%", "&", "S", "I", "L", "!", "#", "@", "F", "R", "D"}

    Public Sub New(ByVal Writer As XmlWriter)
        Me.Writer = Writer
    End Sub

    Private Shared Function GetOperatorToken(ByVal type As OperatorType) As TokenType
        Select Case type
            Case OperatorType.Concatenate
                Return TokenType.Ampersand

            Case OperatorType.Multiply
                Return TokenType.Star

            Case OperatorType.Divide
                Return TokenType.ForwardSlash

            Case OperatorType.IntegralDivide
                Return TokenType.BackwardSlash

            Case OperatorType.Power
                Return TokenType.Caret

            Case OperatorType.Plus, OperatorType.UnaryPlus
                Return TokenType.Plus

            Case OperatorType.Minus, OperatorType.Negate
                Return TokenType.Minus

            Case OperatorType.LessThan
                Return TokenType.LessThan

            Case OperatorType.LessThanEquals
                Return TokenType.LessThanEquals

            Case OperatorType.Equals
                Return TokenType.Equals

            Case OperatorType.NotEquals
                Return TokenType.NotEquals

            Case OperatorType.GreaterThan
                Return TokenType.GreaterThan

            Case OperatorType.GreaterThanEquals
                Return TokenType.GreaterThanEquals

            Case OperatorType.ShiftLeft
                Return TokenType.LessThanLessThan

            Case OperatorType.ShiftRight
                Return TokenType.GreaterThanGreaterThan

            Case OperatorType.Modulus
                Return TokenType.Mod

            Case OperatorType.Or
                Return TokenType.Or

            Case OperatorType.OrElse
                Return TokenType.OrElse

            Case OperatorType.And
                Return TokenType.And

            Case OperatorType.AndAlso
                Return TokenType.AndAlso

            Case OperatorType.Xor
                Return TokenType.Xor

            Case OperatorType.Like
                Return TokenType.Like

            Case OperatorType.Is
                Return TokenType.Is

            Case OperatorType.Not
                Return TokenType.Not

            Case Else
                Return TokenType.LexicalError
        End Select
    End Function

    Private Shared Function GetCompoundAssignmentOperatorToken(ByVal compoundOperator As OperatorType) As TokenType
        Select Case compoundOperator
            Case OperatorType.Plus
                Return TokenType.PlusEquals

            Case OperatorType.Concatenate
                Return TokenType.AmpersandEquals

            Case OperatorType.Multiply
                Return TokenType.StarEquals

            Case OperatorType.Minus
                Return TokenType.MinusEquals

            Case OperatorType.Divide
                Return TokenType.ForwardSlashEquals

            Case OperatorType.IntegralDivide
                Return TokenType.BackwardSlashEquals

            Case OperatorType.Power
                Return TokenType.CaretEquals

            Case OperatorType.ShiftLeft
                Return TokenType.LessThanLessThanEquals

            Case OperatorType.ShiftRight
                Return TokenType.GreaterThanGreaterThanEquals

            Case Else
                Debug.Fail("Unexpected!")
        End Select
    End Function

    Private Shared Function GetBlockTypeToken(ByVal blockType As BlockType) As TokenType
        Select Case blockType
            Case blockType.Class
                Return TokenType.Class

            Case blockType.Enum
                Return TokenType.Enum

            Case blockType.Function
                Return TokenType.Function

            Case blockType.Get
                Return TokenType.Get

            Case blockType.If
                Return TokenType.If

            Case blockType.Interface
                Return TokenType.Interface

            Case blockType.Module
                Return TokenType.Module

            Case blockType.Namespace
                Return TokenType.Namespace

            Case blockType.Property
                Return TokenType.Property

            Case blockType.Select
                Return TokenType.Select

            Case blockType.Set
                Return TokenType.Set

            Case blockType.Structure
                Return TokenType.Structure

            Case blockType.Sub
                Return TokenType.Sub

            Case blockType.SyncLock
                Return TokenType.SyncLock

            Case blockType.Try
                Return TokenType.Try

            Case blockType.While
                Return TokenType.While

            Case blockType.With
                Return TokenType.With

            Case blockType.None
                Return TokenType.LexicalError

            Case blockType.Do
                Return TokenType.Do

            Case blockType.For
                Return TokenType.For

            Case Else
                Debug.Fail("Unexpected!")
        End Select
    End Function

    Private Sub SerializeSpan(ByVal Span As Span)
        Writer.WriteAttributeString("startLine", CStr(Span.Start.Line))
        Writer.WriteAttributeString("startCol", CStr(Span.Start.Column))
        Writer.WriteAttributeString("endLine", CStr(Span.Finish.Line))
        Writer.WriteAttributeString("endCol", CStr(Span.Finish.Column))
    End Sub

    Private Sub SerializeLocation(ByVal Location As Location)
        Writer.WriteAttributeString("line", CStr(Location.Line))
        Writer.WriteAttributeString("col", CStr(Location.Column))
    End Sub

    Protected Sub SerializeToken(ByVal TokenType As TokenType, ByVal Location As Location)
        If Location.IsValid Then
            Writer.WriteStartElement(TokenType.ToString())
            SerializeLocation(Location)
            Writer.WriteEndElement()
        End If
    End Sub

    Private Sub SerializeTypeCharacter(ByVal TypeCharacter As TypeCharacter)
        If TypeCharacter <> TypeCharacter.None Then
            Writer.WriteAttributeString("typeChar", TypeCharacters(TypeCharacter))
        End If
    End Sub

    Private Sub SerializeList(ByVal List As Tree)
        If TypeOf List Is CommaDelimitedCollection Then
            With CType(List, CommaDelimitedCollection)
                Dim CommaEnumerator As LocationCollection.LocationEnumerator
                Dim MoreCommas As Boolean = False

                If Not .CommaLocations Is Nothing Then
                    CommaEnumerator = .CommaLocations.GetEnumerator()
                    MoreCommas = CommaEnumerator.MoveNext()
                End If

                For Each Child As Tree In .Children
                    If Not child Is Nothing Then
                        While MoreCommas AndAlso CommaEnumerator.Current.Before(Child.Span.Start)
                            SerializeToken(TokenType.Comma, CommaEnumerator.Current)
                            MoreCommas = CommaEnumerator.MoveNext()
                        End While

                        Serialize(Child)
                    End If
                Next

                While MoreCommas
                    SerializeToken(TokenType.Comma, CommaEnumerator.Current)
                    MoreCommas = CommaEnumerator.MoveNext()
                End While
            End With
        ElseIf TypeOf List Is ColonDelimitedCollection Then
            With CType(List, ColonDelimitedCollection)
                Dim ColonEnumerator As LocationCollection.LocationEnumerator
                Dim MoreColons As Boolean = False

                If Not .ColonLocations Is Nothing Then
                    ColonEnumerator = .ColonLocations.GetEnumerator()
                    MoreColons = ColonEnumerator.MoveNext()
                End If

                For Each Child As Tree In .Children
                    While MoreColons AndAlso ColonEnumerator.Current.Before(Child.Span.Start)
                        SerializeToken(TokenType.Colon, ColonEnumerator.Current)
                        MoreColons = ColonEnumerator.MoveNext()
                    End While

                    Serialize(Child)
                Next

                While MoreColons
                    SerializeToken(TokenType.Colon, ColonEnumerator.Current)
                    MoreColons = ColonEnumerator.MoveNext()
                End While
            End With
        Else
            For Each Child As Tree In List.Children
                Serialize(Child)
            Next
        End If

        Select Case List.Type
            Case TreeType.ArgumentCollection
                With CType(List, ArgumentCollection)
                    If .RightParenthesisLocation.IsValid Then
                        SerializeToken(TokenType.RightParenthesis, .RightParenthesisLocation)
                    End If
                End With

            Case TreeType.InitializerCollection
                With CType(List, InitializerCollection)
                    If .RightCurlyBraceLocation.IsValid Then
                        SerializeToken(TokenType.RightCurlyBrace, .RightCurlyBraceLocation)
                    End If
                End With

            Case TreeType.ParameterCollection
                With CType(List, ParameterCollection)
                    If .RightParenthesisLocation.IsValid Then
                        SerializeToken(TokenType.RightParenthesis, .RightParenthesisLocation)
                    End If
                End With

            Case TreeType.AttributeCollection
                With CType(List, AttributeCollection)
                    If .RightBracketLocation.IsValid Then
                        SerializeToken(TokenType.GreaterThan, .RightBracketLocation)
                    End If
                End With
        End Select
    End Sub

    Private Sub SerializeName(ByVal Name As Tree)
        Select Case Name.Type
            Case TreeType.SimpleName
                With CType(Name, SimpleName)
                    SerializeTypeCharacter(.TypeCharacter)
                    Writer.WriteAttributeString("escaped", CStr(.Escaped))
                    Writer.WriteString(.Name)
                End With

            Case TreeType.VariableName
                With CType(Name, VariableName)
                    Serialize(.Name)
                    Serialize(.ArrayType)
                End With

            Case TreeType.QualifiedName
                With CType(Name, QualifiedName)
                    Serialize(.Qualifier)
                    SerializeToken(TokenType.Period, .DotLocation)
                    Serialize(.Name)
                End With
        End Select
    End Sub

    Private Sub SerializeType(ByVal Type As Tree)
        Select Case Type.Type
            Case TreeType.IntrinsicType
                Writer.WriteAttributeString("intrinsicType", CType(Type, IntrinsicTypeName).IntrinsicType.ToString())

            Case TreeType.NamedType
                With CType(Type, NamedTypeName)
                    Serialize(.Name)
                End With

            Case TreeType.ArrayType
                With CType(Type, ArrayTypeName)
                    Writer.WriteAttributeString("rank", CStr(.Rank))
                    Serialize(.ElementTypeName)
                    Serialize(.Arguments)
                End With
        End Select
    End Sub

    Private Sub SerializeInitializer(ByVal Initializer As Tree)
        Select Case Initializer.Type
            Case TreeType.AggregateInitializer
                With CType(Initializer, AggregateInitializer)
                    Serialize(.Elements)
                End With

            Case TreeType.ExpressionInitializer
                With CType(Initializer, ExpressionInitializer)
                    Serialize(.Expression)
                End With
        End Select
    End Sub

    Private Sub SerializeExpression(ByVal Expression As Tree)
        Writer.WriteAttributeString("isConstant", CStr(CType(Expression, Expression).IsConstant))

        Select Case Expression.Type
            Case TreeType.StringLiteralExpression
                With CType(Expression, StringLiteralExpression)
                    Writer.WriteString(.Literal)
                End With

            Case TreeType.CharacterLiteralExpression
                With CType(Expression, CharacterLiteralExpression)
                    Writer.WriteString(.Literal)
                End With

            Case TreeType.DateLiteralExpression
                With CType(Expression, DateLiteralExpression)
                    Writer.WriteString(CStr(.Literal))
                End With

            Case TreeType.IntegerLiteralExpression
                With CType(Expression, IntegerLiteralExpression)
                    SerializeTypeCharacter(.TypeCharacter)
                    Writer.WriteAttributeString("base", .IntegerBase.ToString())
                    Writer.WriteString(CStr(.Literal))
                End With

            Case TreeType.FloatingPointLiteralExpression
                With CType(Expression, FloatingPointLiteralExpression)
                    SerializeTypeCharacter(.TypeCharacter)
                    Writer.WriteString(CStr(.Literal))
                End With

            Case TreeType.DecimalLiteralExpression
                With CType(Expression, DecimalLiteralExpression)
                    SerializeTypeCharacter(.TypeCharacter)
                    Writer.WriteString(CStr(.Literal))
                End With

            Case TreeType.BooleanLiteralExpression
                With CType(Expression, BooleanLiteralExpression)
                    Writer.WriteString(CStr(.Literal))
                End With

            Case TreeType.GetTypeExpression
                With CType(Expression, GetTypeExpression)
                    SerializeToken(TokenType.LeftParenthesis, .LeftParenthesisLocation)
                    Serialize(.Target)
                    SerializeToken(TokenType.RightParenthesis, .RightParenthesisLocation)
                End With

            Case TreeType.CTypeExpression, TreeType.DirectCastExpression
                With CType(Expression, CastTypeExpression)
                    SerializeToken(TokenType.LeftParenthesis, .LeftParenthesisLocation)
                    Serialize(.Operand)
                    SerializeToken(TokenType.Comma, .CommaLocation)
                    Serialize(.Target)
                    SerializeToken(TokenType.RightParenthesis, .RightParenthesisLocation)
                End With

            Case TreeType.TypeOfExpression
                With CType(Expression, TypeOfExpression)
                    Serialize(.Operand)
                    SerializeToken(TokenType.Is, .IsLocation)
                    Serialize(.Target)
                End With

            Case TreeType.IntrinsicCastExpression
                With CType(Expression, IntrinsicCastExpression)
                    Writer.WriteAttributeString("intrinsicType", .IntrinsicType.ToString())
                    SerializeToken(TokenType.LeftParenthesis, .LeftParenthesisLocation)
                    Serialize(.Operand)
                    SerializeToken(TokenType.RightParenthesis, .RightParenthesisLocation)
                End With

                ' Case TreeType.SimpleNameExpression
                ' Case TreeType.TypeReferenceExpression
                ' Accept default behavior

            Case TreeType.QualifiedExpression
                With CType(Expression, QualifiedExpression)
                    Serialize(.Qualifier)
                    SerializeToken(TokenType.Period, .DotLocation)
                    Serialize(.Name)
                End With

            Case TreeType.DictionaryLookupExpression
                With CType(Expression, DictionaryLookupExpression)
                    Serialize(.Qualifier)
                    SerializeToken(TokenType.Exclamation, .BangLocation)
                    Serialize(.Name)
                End With

            Case TreeType.InstanceExpression
                With CType(Expression, InstanceExpression)
                    Writer.WriteAttributeString("type", .InstanceType.ToString())
                End With

                ' Case TreeType.NewAggregateExpression
                ' Case TreeType.NewExpression
                ' Case TreeType.CallOrIndexExpression
                ' Case TreeType.UnaryPlusExpression
                ' Case TreeType.NegateExpression
                ' Case TreeType.NotExpression
                ' Case TreeType.AddressOfExpression
                ' Accept default behavior

            Case TreeType.ParentheticalExpression
                With CType(Expression, ParentheticalExpression)
                    Serialize(.Operand)
                    SerializeToken(TokenType.RightParenthesis, .RightParenthesisLocation)
                End With

            Case TreeType.BinaryOperatorExpression
                With CType(Expression, BinaryOperatorExpression)
                    Writer.WriteAttributeString("operator", .Operator.ToString())
                    Serialize(.LeftOperand)
                    SerializeToken(GetOperatorToken(.Operator), .OperatorLocation)
                    Serialize(.RightOperand)
                End With


            Case TreeType.UnaryOperatorExpression
                With CType(Expression, UnaryOperatorExpression)
                    SerializeToken(GetOperatorToken(.Operator), .Span.Start)
                    Serialize(.Operand)
                End With
                ' Case TreeType.MeExpression
                ' Case TreeType.MyClassExpression
                ' Case TreeType.MyBaseExpression
                ' Case TreeType.NothingExpression
                ' Accept default behavior

            Case Else
                For Each Child As Tree In Expression.Children
                    Serialize(Child)
                Next
        End Select
    End Sub

    Private Sub SerializeStatementComments(ByVal Statement As Tree)
        With CType(Statement, Statement)
            If Not .Comments Is Nothing Then
                For Each Comment As Comment In .Comments
                    Serialize(Comment)
                Next
            End If
        End With
    End Sub

    Private Sub SerializeStatement(ByVal Statement As Tree)
        Select Case Statement.Type
            Case TreeType.GotoStatement, TreeType.LabelStatement
                With CType(Statement, LabelReferenceStatement)
                    Writer.WriteAttributeString("isLineNumber", CStr(.IsLineNumber))
                    SerializeStatementComments(Statement)
                    Serialize(.Name)
                End With

                ' Case TreeType.StopStatement
                ' Case TreeType.EndStatement

            Case TreeType.ExitStatement
                With CType(Statement, ExitStatement)
                    Writer.WriteAttributeString("exitType", .ExitType.ToString())
                    SerializeStatementComments(Statement)
                    SerializeToken(GetBlockTypeToken(.ExitType), .ExitArgumentLocation)
                End With

            Case TreeType.ReturnStatement, TreeType.ErrorStatement, TreeType.ThrowStatement
                With CType(Statement, ExpressionStatement)
                    SerializeStatementComments(Statement)
                    Serialize(.Expression)
                End With

            Case TreeType.RaiseEventStatement
                With CType(Statement, RaiseEventStatement)
                    SerializeStatementComments(Statement)
                    Serialize(.Name)
                    Serialize(.Arguments)
                End With

            Case TreeType.AddHandlerStatement, TreeType.RemoveHandlerStatement
                With CType(Statement, HandlerStatement)
                    SerializeStatementComments(Statement)
                    Serialize(.Name)
                    SerializeToken(TokenType.Comma, .CommaLocation)
                    Serialize(.DelegateExpression)
                End With

            Case TreeType.OnErrorStatement
                With CType(Statement, OnErrorStatement)
                    Writer.WriteAttributeString("onErrorType", .OnErrorType.ToString())
                    SerializeStatementComments(Statement)
                    SerializeToken(TokenType.Error, .ErrorLocation)

                    Select Case .OnErrorType
                        Case OnErrorType.Zero
                            SerializeToken(TokenType.GoTo, .ResumeOrGoToLocation)
                            Writer.WriteStartElement("Zero")
                            SerializeLocation(.NextOrZeroOrMinusLocation)
                            Writer.WriteEndElement()

                        Case OnErrorType.MinusOne
                            SerializeToken(TokenType.GoTo, .ResumeOrGoToLocation)
                            SerializeToken(TokenType.Minus, .NextOrZeroOrMinusLocation)
                            Writer.WriteStartElement("One")
                            SerializeLocation(.OneLocation)
                            Writer.WriteEndElement()

                        Case OnErrorType.Label
                            SerializeToken(TokenType.GoTo, .ResumeOrGoToLocation)
                            Serialize(.Name)

                        Case OnErrorType.Next
                            SerializeToken(TokenType.Resume, .ResumeOrGoToLocation)
                            SerializeToken(TokenType.Next, .NextOrZeroOrMinusLocation)

                        Case OnErrorType.Bad
                            ' Do nothing
                    End Select
                End With

            Case TreeType.ResumeStatement
                With CType(Statement, ResumeStatement)
                    Writer.WriteAttributeString("resumeType", .ResumeType.ToString())
                    SerializeStatementComments(Statement)
                    Select Case .ResumeType
                        Case ResumeType.Next
                            SerializeToken(TokenType.Next, .NextLocation)

                        Case ResumeType.Label
                            Serialize(.Name)

                        Case ResumeType.None
                            ' Do nothing
                    End Select
                End With

            Case TreeType.ReDimStatement
                With CType(Statement, ReDimStatement)
                    SerializeStatementComments(Statement)
                    SerializeToken(TokenType.Preserve, .PreserveLocation)
                    Serialize(.Variables)
                End With

            Case TreeType.EraseStatement
                With CType(Statement, EraseStatement)
                    SerializeStatementComments(Statement)
                    Serialize(.Variables)
                End With

            Case TreeType.CallStatement
                With CType(Statement, CallStatement)
                    SerializeStatementComments(Statement)
                    SerializeToken(TokenType.Call, .CallLocation)
                    Serialize(.TargetExpression)
                    Serialize(.Arguments)
                End With

            Case TreeType.AssignmentStatement
                With CType(Statement, AssignmentStatement)
                    SerializeStatementComments(Statement)
                    Serialize(.TargetExpression)
                    SerializeToken(TokenType.Equals, .OperatorLocation)
                    Serialize(.SourceExpression)
                End With

            Case TreeType.MidAssignmentStatement
                With CType(Statement, MidAssignmentStatement)
                    Writer.WriteAttributeString("hasTypeCharacter", CStr(.HasTypeCharacter))
                    SerializeStatementComments(Statement)
                    SerializeToken(TokenType.LeftParenthesis, .LeftParenthesisLocation)
                    Serialize(.TargetExpression)
                    SerializeToken(TokenType.Comma, .StartCommaLocation)
                    Serialize(.StartExpression)
                    SerializeToken(TokenType.Comma, .LengthCommaLocation)
                    Serialize(.LengthExpression)
                    SerializeToken(TokenType.RightParenthesis, .RightParenthesisLocation)
                    SerializeToken(TokenType.Equals, .OperatorLocation)
                    Serialize(.SourceExpression)
                End With

            Case TreeType.CompoundAssignmentStatement
                With CType(Statement, CompoundAssignmentStatement)
                    SerializeStatementComments(Statement)
                    Serialize(.TargetExpression)
                    SerializeToken(GetCompoundAssignmentOperatorToken(.CompoundOperator), .OperatorLocation)
                    Serialize(.SourceExpression)
                End With

            Case TreeType.LocalDeclarationStatement
                With CType(Statement, LocalDeclarationStatement)
                    SerializeStatementComments(Statement)
                    Serialize(.Modifiers)
                    Serialize(.VariableDeclarators)
                End With

            Case TreeType.EndBlockStatement
                With CType(Statement, EndBlockStatement)
                    Writer.WriteAttributeString("endType", .EndType.ToString())
                    SerializeStatementComments(Statement)
                    SerializeToken(GetBlockTypeToken(.EndType), .EndArgumentLocation)
                End With

            Case TreeType.WithBlockStatement, TreeType.SyncLockBlockStatement, TreeType.WhileBlockStatement
                With CType(Statement, ExpressionBlockStatement)
                    SerializeStatementComments(Statement)
                    Serialize(.Expression)
                    Serialize(.Statements)
                    Serialize(.EndStatement)
                End With

            Case TreeType.DoBlockStatement
                With CType(Statement, DoBlockStatement)
                    If Not .Expression Is Nothing Then
                        Writer.WriteAttributeString("isWhile", Str(.IsWhile))
                        SerializeStatementComments(Statement)
                        If .IsWhile Then
                            SerializeToken(TokenType.While, .WhileOrUntilLocation)
                        Else
                            SerializeToken(TokenType.Until, .WhileOrUntilLocation)
                        End If
                        Serialize(.Expression)
                    Else
                        SerializeStatementComments(Statement)
                    End If
                    Serialize(.Statements)
                    Serialize(.EndStatement)
                End With

            Case TreeType.LoopStatement
                With CType(Statement, LoopStatement)
                    If Not .Expression Is Nothing Then
                        Writer.WriteAttributeString("isWhile", Str(.IsWhile))
                        SerializeStatementComments(Statement)
                        If .IsWhile Then
                            SerializeToken(TokenType.While, .WhileOrUntilLocation)
                        Else
                            SerializeToken(TokenType.Until, .WhileOrUntilLocation)
                        End If
                        Serialize(.Expression)
                    Else
                        SerializeStatementComments(Statement)
                    End If
                End With

            Case TreeType.NextStatement
                With CType(Statement, NextStatement)
                    SerializeStatementComments(Statement)
                    Serialize(.Variables)
                End With

            Case TreeType.ForBlockStatement
                With CType(Statement, ForBlockStatement)
                    SerializeStatementComments(Statement)
                    Serialize(.ControlExpression)
                    Serialize(.ControlVariableDeclarator)
                    SerializeToken(TokenType.Equals, .EqualsLocation)
                    Serialize(.LowerBoundExpression)
                    SerializeToken(TokenType.To, .ToLocation)
                    Serialize(.UpperBoundExpression)
                    SerializeToken(TokenType.Step, .StepLocation)
                    Serialize(.StepExpression)
                    Serialize(.Statements)
                    Serialize(.NextStatement)
                End With

            Case TreeType.ForEachBlockStatement
                With CType(Statement, ForEachBlockStatement)
                    SerializeStatementComments(Statement)
                    SerializeToken(TokenType.Each, .EachLocation)
                    Serialize(.ControlExpression)
                    Serialize(.ControlVariableDeclarator)
                    SerializeToken(TokenType.In, .InLocation)
                    Serialize(.CollectionExpression)
                    Serialize(.Statements)
                    Serialize(.NextStatement)
                End With

                ' Case TreeType.TryBlockStatement

            Case TreeType.CatchStatement
                With CType(Statement, CatchStatement)
                    SerializeStatementComments(Statement)
                    Serialize(.Name)
                    SerializeToken(TokenType.As, .AsLocation)
                    Serialize(.ExceptionType)
                    SerializeToken(TokenType.When, .WhenLocation)
                    Serialize(.FilterExpression)
                End With

                ' Case TreeType.CatchBlockStatement
                ' Case TreeType.FinallyStatement
                ' Case TreeType.FinallyBlockStatement

                ' Case TreeType.CaseStatement

            Case TreeType.CaseElseStatement
                With CType(Statement, CaseElseStatement)
                    SerializeStatementComments(Statement)
                    SerializeToken(TokenType.Else, .ElseLocation)
                End With

                ' Case TreeType.CaseBlockStatement
                ' Case TreeType.CaseElseBlockStatement

            Case TreeType.SelectBlockStatement
                With CType(Statement, SelectBlockStatement)
                    SerializeStatementComments(Statement)
                    SerializeToken(TokenType.Case, .CaseLocation)
                    Serialize(.Expression)
                    Serialize(.Statements)
                    Serialize(.CaseBlockStatements)
                    Serialize(.CaseElseBlockStatement)
                    Serialize(.EndStatement)
                End With

            Case TreeType.ElseIfStatement
                With CType(Statement, ElseIfStatement)
                    SerializeStatementComments(Statement)
                    Serialize(.Expression)
                    SerializeToken(TokenType.Then, .ThenLocation)
                End With

                ' Case TreeType.ElseBlockStatement
                ' Case TreeType.ElseStatement
                ' Case TreeType.ElseIfBlockStatement

            Case TreeType.IfBlockStatement
                With CType(Statement, IfBlockStatement)
                    SerializeStatementComments(Statement)
                    Serialize(.Expression)
                    SerializeToken(TokenType.Then, .ThenLocation)
                    Serialize(.Statements)
                    Serialize(.ElseIfBlockStatements)
                    Serialize(.ElseBlockStatement)
                    Serialize(.EndStatement)
                End With

            Case TreeType.LineIfBlockStatement
                With CType(Statement, LineIfStatement)
                    SerializeStatementComments(Statement)
                    Serialize(.Expression)
                    SerializeToken(TokenType.Then, .ThenLocation)
                    Serialize(.IfStatements)
                    SerializeToken(TokenType.Else, .ElseLocation)
                    Serialize(.ElseStatements)
                End With

                ' Case TreeType.EmptyStatement
            Case Else
                SerializeStatementComments(Statement)
                For Each Child As Tree In Statement.Children
                    Serialize(Child)
                Next
        End Select
    End Sub

    Private Sub SerializeCaseClause(ByVal CaseClause As Tree)
        Select Case CaseClause.Type
            Case TreeType.ComparisonCaseClause
                With CType(CaseClause, ComparisonCaseClause)
                    SerializeToken(TokenType.Is, .IsLocation)
                    SerializeToken(GetOperatorToken(.ComparisonOperator), .OperatorLocation)
                    Serialize(.Operand)
                End With

            Case TreeType.RangeCaseClause
                With CType(CaseClause, RangeCaseClause)
                    Serialize(.LowerExpression)
                    SerializeToken(TokenType.To, .ToLocation)
                    Serialize(.UpperExpression)
                End With

            Case Else
                Debug.Fail("Unexpected.")
        End Select
    End Sub

    Private Sub SerializeDeclarationComments(ByVal Declaration As Tree)
        With CType(Declaration, Declaration)
            If Not .Comments Is Nothing Then
                For Each Comment As Comment In .Comments
                    Serialize(Comment)
                Next
            End If
        End With
    End Sub

    Private Sub SerializeDeclaration(ByVal Declaration As Tree)
        Select Case Declaration.Type
            Case TreeType.EndBlockDeclaration
                With CType(Declaration, EndBlockDeclaration)
                    Writer.WriteAttributeString("endType", .EndType.ToString())
                    SerializeDeclarationComments(Declaration)
                    SerializeToken(GetBlockTypeToken(.EndType), .EndArgumentLocation)
                End With

                ' Case TreeType.VariableListDeclaration

            Case TreeType.EventDeclaration
                With CType(Declaration, EventDeclaration)
                    SerializeDeclarationComments(Declaration)
                    Serialize(.Attributes)
                    Serialize(.Modifiers)
                    SerializeToken(TokenType.Event, .KeywordLocation)
                    Serialize(.Name)
                    Serialize(.Parameters)
                    SerializeToken(TokenType.As, .AsLocation)
                    Serialize(.ResultTypeAttributes)
                    Serialize(.ResultType)
                    Serialize(.ImplementsList)
                End With

            Case TreeType.SubDeclaration, TreeType.FunctionDeclaration, TreeType.ConstructorDeclaration
                With CType(Declaration, MethodDeclaration)
                    SerializeDeclarationComments(Declaration)
                    Serialize(.Attributes)
                    Serialize(.Modifiers)

                    Select Case Declaration.Type
                        Case TreeType.SubDeclaration
                            SerializeToken(TokenType.Sub, .KeywordLocation)

                        Case TreeType.FunctionDeclaration
                            SerializeToken(TokenType.Function, .KeywordLocation)

                        Case TreeType.ConstructorDeclaration
                            SerializeToken(TokenType.[New], .KeywordLocation)
                    End Select

                    Serialize(.Name)
                    Serialize(.Parameters)
                    SerializeToken(TokenType.As, .AsLocation)
                    Serialize(.ResultTypeAttributes)
                    Serialize(.ResultType)
                    Serialize(.ImplementsList)
                    Serialize(.HandlesList)
                    Serialize(.Statements)
                    Serialize(.EndStatement)
                End With

            Case TreeType.ExternalSubDeclaration, TreeType.ExternalFunctionDeclaration
                With CType(Declaration, ExternalDeclaration)
                    SerializeDeclarationComments(Declaration)
                    Serialize(.Attributes)
                    Serialize(.Modifiers)

                    SerializeToken(TokenType.Declare, .KeywordLocation)

                    Select Case .Charset
                        Case Charset.Auto
                            SerializeToken(TokenType.Auto, .CharsetLocation)

                        Case Charset.Ansi
                            SerializeToken(TokenType.Ansi, .CharsetLocation)

                        Case Charset.Unicode
                            SerializeToken(TokenType.Unicode, .CharsetLocation)
                    End Select

                    Select Case Declaration.Type
                        Case TreeType.ExternalSubDeclaration
                            SerializeToken(TokenType.Sub, .SubOrFunctionLocation)

                        Case TreeType.ExternalFunctionDeclaration
                            SerializeToken(TokenType.Function, .SubOrFunctionLocation)
                    End Select

                    Serialize(.Name)
                    SerializeToken(TokenType.Lib, .LibLocation)
                    Serialize(.LibLiteral)
                    SerializeToken(TokenType.Alias, .AliasLocation)
                    Serialize(.AliasLiteral)
                    Serialize(.Parameters)
                    SerializeToken(TokenType.As, .AsLocation)
                    Serialize(.ResultTypeAttributes)
                    Serialize(.ResultType)
                End With

            Case TreeType.PropertyDeclaration
                With CType(Declaration, PropertyDeclaration)
                    SerializeDeclarationComments(Declaration)
                    Serialize(.Attributes)
                    Serialize(.Modifiers)
                    SerializeToken(TokenType.Event, .KeywordLocation)
                    Serialize(.Name)
                    Serialize(.Parameters)
                    SerializeToken(TokenType.As, .AsLocation)
                    Serialize(.ResultTypeAttributes)
                    Serialize(.ResultType)
                    Serialize(.ImplementsList)
                    Serialize(.Accessors)
                    Serialize(.EndDeclaration)
                End With

            Case TreeType.GetDeclaration
                With CType(Declaration, GetAccessorDeclaration)
                    SerializeDeclarationComments(Declaration)
                    Serialize(.Attributes)
                    Serialize(.Modifiers)
                    SerializeToken(TokenType.Get, .GetLocation)
                    Serialize(.Statements)
                    Serialize(.EndStatement)
                End With

            Case TreeType.SetDeclaration
                With CType(Declaration, SetAccessorDeclaration)
                    SerializeDeclarationComments(Declaration)
                    Serialize(.Attributes)
                    Serialize(.Modifiers)
                    SerializeToken(TokenType.Set, .SetLocation)
                    Serialize(.Parameters)
                    Serialize(.Statements)
                    Serialize(.EndStatement)
                End With

            Case TreeType.EnumValueDeclaration
                With CType(Declaration, EnumValueDeclaration)
                    SerializeDeclarationComments(Declaration)
                    Serialize(.Attributes)
                    Serialize(.Modifiers)
                    Serialize(.Name)
                    SerializeToken(TokenType.Equals, .EqualsLocation)
                    Serialize(.Expression)
                End With

            Case TreeType.DelegateSubDeclaration
                With CType(Declaration, DelegateSubDeclaration)
                    SerializeDeclarationComments(Declaration)
                    Serialize(.Attributes)
                    Serialize(.Modifiers)
                    SerializeToken(TokenType.Delegate, .KeywordLocation)
                    SerializeToken(TokenType.Sub, .SubOrFunctionLocation)
                    Serialize(.Name)
                    Serialize(.Parameters)
                End With

            Case TreeType.DelegateFunctionDeclaration
                With CType(Declaration, DelegateFunctionDeclaration)
                    SerializeDeclarationComments(Declaration)
                    Serialize(.Attributes)
                    Serialize(.Modifiers)
                    SerializeToken(TokenType.Delegate, .KeywordLocation)
                    SerializeToken(TokenType.Function, .SubOrFunctionLocation)
                    Serialize(.Name)
                    Serialize(.Parameters)
                    SerializeToken(TokenType.As, .AsLocation)
                    Serialize(.ResultTypeAttributes)
                    Serialize(.ResultType)
                End With

            Case TreeType.ClassDeclaration, TreeType.StructureDeclaration, _
                 TreeType.ModuleDeclaration, TreeType.InterfaceDeclaration
                With CType(Declaration, BlockDeclaration)
                    SerializeDeclarationComments(Declaration)
                    Serialize(.Attributes)
                    Serialize(.Modifiers)

                    Select Case Declaration.Type
                        Case TreeType.ClassDeclaration
                            SerializeToken(TokenType.Class, .KeywordLocation)

                        Case TreeType.StructureDeclaration
                            SerializeToken(TokenType.Structure, .KeywordLocation)

                        Case TreeType.ModuleDeclaration
                            SerializeToken(TokenType.Module, .KeywordLocation)

                        Case TreeType.InterfaceDeclaration
                            SerializeToken(TokenType.Interface, .KeywordLocation)
                    End Select

                    Serialize(.Name)
                    Serialize(.Declarations)
                    Serialize(.EndStatement)
                End With

            Case TreeType.NamespaceDeclaration
                With CType(Declaration, NamespaceDeclaration)
                    SerializeDeclarationComments(Declaration)
                    Serialize(.Attributes)
                    Serialize(.Modifiers)

                    SerializeToken(TokenType.Namespace, .NamespaceLocation)
                    Serialize(.Name)
                    Serialize(.Declarations)
                    Serialize(.EndStatement)
                End With

            Case TreeType.EnumDeclaration
                With CType(Declaration, EnumDeclaration)
                    SerializeDeclarationComments(Declaration)
                    Serialize(.Attributes)
                    Serialize(.Modifiers)
                    SerializeToken(TokenType.Enum, .KeywordLocation)
                    Serialize(.Name)
                    SerializeToken(TokenType.As, .AsLocation)
                    Serialize(.ElementType)
                    Serialize(.Declarations)
                    Serialize(.EndStatement)
                End With

                ' Case TreeType.InheritsDeclaration, TreeType.ImplementsDeclaration
                ' Case TreeType.ImportsDeclaration

            Case TreeType.OptionDeclaration
                With CType(Declaration, OptionDeclaration)
                    Writer.WriteAttributeString("type", .OptionType.ToString())
                    SerializeDeclarationComments(Declaration)

                    Select Case .OptionType
                        Case OptionType.SyntaxError

                        Case OptionType.Explicit, OptionType.ExplicitOn, _
                             OptionType.ExplicitOff
                            Writer.WriteStartElement("Explicit")
                            SerializeLocation(.OptionTypeLocation)
                            Writer.WriteEndElement()

                            If .OptionType = OptionType.ExplicitOn Then
                                SerializeToken(TokenType.On, .OptionArgumentLocation)
                            ElseIf .OptionType = OptionType.ExplicitOff Then
                                Writer.WriteStartElement("Off")
                                SerializeLocation(.OptionArgumentLocation)
                                Writer.WriteEndElement()
                            End If

                        Case OptionType.Strict, OptionType.StrictOn, _
                             OptionType.StrictOff

                            Writer.WriteStartElement("Strict")
                            SerializeLocation(.OptionTypeLocation)
                            Writer.WriteEndElement()

                            If .OptionType = OptionType.StrictOn Then
                                SerializeToken(TokenType.On, .OptionArgumentLocation)
                            ElseIf .OptionType = OptionType.StrictOff Then
                                Writer.WriteStartElement("Off")
                                SerializeLocation(.OptionArgumentLocation)
                                Writer.WriteEndElement()
                            End If

                        Case OptionType.CompareBinary, OptionType.CompareText
                            Writer.WriteStartElement("Compare")
                            SerializeLocation(.OptionTypeLocation)
                            Writer.WriteEndElement()

                            If .OptionType = OptionType.CompareBinary Then
                                Writer.WriteStartElement("Binary")
                                SerializeLocation(.OptionArgumentLocation)
                                Writer.WriteEndElement()
                            Else
                                Writer.WriteStartElement("Text")
                                SerializeLocation(.OptionArgumentLocation)
                                Writer.WriteEndElement()
                            End If
                    End Select
                End With

                ' Case TreeType.AttributeDeclaration
                ' Case TreeType.EmptyDeclaration

            Case Else
                SerializeDeclarationComments(Declaration)
                For Each Child As Tree In Declaration.Children
                    Serialize(Child)
                Next
        End Select
    End Sub

    Private Sub SerializeImport(ByVal Import As Tree)
        Select Case Import.Type
            Case TreeType.NameImport
                With CType(Import, NameImport)
                    Serialize(.Name)
                End With

            Case TreeType.AliasImport
                With CType(Import, AliasImport)
                    Serialize(.AliasedName)
                    SerializeToken(TokenType.Equals, .EqualsLocation)
                    Serialize(.Name)
                End With
        End Select
    End Sub

    Public Sub Serialize(ByVal Tree As Tree)
        If Tree Is Nothing Then
            Return
        End If

        Writer.WriteStartElement(Tree.Type.ToString())
        If Tree.IsBad Then
            Writer.WriteAttributeString("isBad", CStr(True))
        End If
        SerializeSpan(Tree.Span)

        Select Case Tree.Type
            Case TreeType.ArgumentCollection To TreeType.DeclarationCollection
                SerializeList(Tree)

            Case TreeType.Comment
                With CType(Tree, Comment)
                    Writer.WriteAttributeString("isRem", CStr(.IsREM))

                    If Not .Comment Is Nothing Then
                        Writer.WriteString(.Comment)
                    End If
                End With

            Case TreeType.SimpleName To TreeType.QualifiedName
                SerializeName(Tree)

            Case TreeType.IntrinsicType To TreeType.ArrayType
                SerializeType(Tree)

                ' Case TreeType.AggregateInitializer To TreeType.ExpressionInitializer
                ' Accept default behavior

            Case TreeType.Argument
                With CType(Tree, Argument)
                    Serialize(.Name)
                    SerializeToken(TokenType.ColonEquals, .ColonEqualsLocation)
                    Serialize(.Expression)
                End With

            Case TreeType.SimpleNameExpression To TreeType.GetTypeExpression
                SerializeExpression(Tree)

            Case TreeType.EmptyStatement To TreeType.EndBlockStatement
                SerializeStatement(Tree)

            Case TreeType.Modifier
                With CType(Tree, Modifier)
                    Writer.WriteAttributeString("type", .ModifierType.ToString())
                End With

            Case TreeType.VariableDeclarator
                With CType(Tree, VariableDeclarator)
                    Serialize(.VariableNames)
                    SerializeToken(TokenType.As, .AsLocation)
                    SerializeToken(TokenType.[New], .NewLocation)
                    Serialize(.VariableType)
                    Serialize(.Arguments)
                    SerializeToken(TokenType.Equals, .EqualsLocation)
                    Serialize(.Initializer)
                End With

            Case TreeType.ComparisonCaseClause To TreeType.RangeCaseClause
                SerializeCaseClause(Tree)

            Case TreeType.Attribute
                With CType(Tree, Attribute)
                    Writer.WriteAttributeString("type", .AttributeType.ToString())

                    Select Case .AttributeType
                        Case AttributeTypes.Assembly
                            SerializeToken(TokenType.Colon, .ColonLocation)
                            SerializeToken(TokenType.Assembly, .AttributeTypeLocation)

                        Case AttributeTypes.Module
                            SerializeToken(TokenType.Module, .AttributeTypeLocation)
                            SerializeToken(TokenType.Colon, .ColonLocation)
                    End Select

                    Serialize(.Name)
                    Serialize(.Arguments)
                End With

            Case TreeType.EmptyDeclaration To TreeType.DelegateFunctionDeclaration
                SerializeDeclaration(Tree)

            Case TreeType.Parameter
                With CType(Tree, Parameter)
                    Serialize(.Attributes)
                    Serialize(.Modifiers)
                    Serialize(.VariableName)
                    SerializeToken(TokenType.As, .AsLocation)
                    Serialize(.ParameterType)
                    SerializeToken(TokenType.Equals, .EqualsLocation)
                    Serialize(.Initializer)
                End With

            Case TreeType.NameImport To TreeType.AliasImport
                SerializeImport(Tree)

                ' Case TreeType.File

            Case Else
                For Each Child As Tree In Tree.Children
                    Serialize(Child)
                Next
        End Select

        Writer.WriteEndElement()
    End Sub
End Class
