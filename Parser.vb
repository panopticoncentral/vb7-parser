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
''' A parser for the Visual Basic .NET language based on the grammar
''' documented in the Language Specification.
''' </summary>
Public NotInheritable Class Parser
    Implements IDisposable

    Private Enum PrecedenceLevel
        None
        [Xor]
        [Or]
        [And]
        [Not]
        Relational
        Shift
        Concatenate
        Plus
        Modulus
        IntegralDivide
        Multiply
        Negate
        Power
    End Enum

    Private NotInheritable Class ExternalSourceContext
        Public Start As Location
        Public File As String
        Public Line As Long
    End Class

    Private NotInheritable Class RegionContext
        Public Start As Location
        Public Description As String
    End Class

    Private NotInheritable Class ConditionalCompilationContext
        Public BlockActive As Boolean
        Public AnyBlocksActive As Boolean
        Public SeenElse As Boolean
    End Class

    ' The scanner we're going to be using.
    Private Scanner As Scanner

    ' The error table for the parsing
    Private ErrorTable As ArrayList

    ' External line mappings
    Private ExternalLineMappings As ArrayList

    ' Source regions
    Private SourceRegions As ArrayList

    ' Conditional compilation constants
    Private ConditionalCompilationConstants As Hashtable

    ' Whether there is an error in the construct
    Private ErrorInConstruct As Boolean

    ' Whether we're at the beginning of a line
    Private AtBeginningOfLine As Boolean

    ' Whether we're doing preprocessing or not
    Private Preprocess As Boolean

    ' The current stack of block contexts
    Private BlockContextStack As Stack = New Stack

    ' The current external source context
    Private CurrentExternalSourceContext As ExternalSourceContext

    ' The current stack of region contexts
    Private RegionContextStack As Stack = New Stack

    ' The current stack of conditional compilation states
    Private ConditionalCompilationContextStack As Stack = New Stack

    ' Determine whether we have been disposed already or not
    Private Disposed As Boolean = False

    ''' <summary>
    ''' Disposes the parser.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        If Not Disposed Then
            Disposed = True
            Scanner.Close()
        End If
    End Sub

    '*
    '* Token reading functions
    '*

    Private Function Peek() As Token
        Return Scanner.Peek()
    End Function

    Private Function PeekAheadFor(ByVal ParamArray tokens() As TokenType) As TokenType
        Dim Start As Token = Peek()
        Dim Current As Token = Start

        While Not CanEndStatement(Current.Type)
            For Each Token As TokenType In tokens
                If Current.Type = Token Then
                    Backtrack(Start)
                    Return Current.Type
                End If
            Next

            Current = Read()
        End While

        Backtrack(Start)
        Return TokenType.None
    End Function

    Private Function Read() As Token
        Return Scanner.Read()
    End Function

    Private Function ReadLocation() As Location
        Return Read().Span.Start
    End Function

    Private Sub Backtrack(ByVal token As Token)
        Scanner.Seek(token)
    End Sub

    Private Sub ResyncAt(ByVal ParamArray tokenTypes() As TokenType)
        Dim CurrentToken As Token = Peek()

        While CurrentToken.Type <> TokenType.Colon AndAlso _
              CurrentToken.Type <> TokenType.EndOfStream AndAlso _
              CurrentToken.Type <> TokenType.LineTerminator AndAlso _
              Not BeginsStatement(CurrentToken.Type)
            For Each TokenType As TokenType In tokenTypes
                ' CONSIDER: Need to check for non-reserved tokens?
                If CurrentToken.Type = TokenType Then
                    Return
                End If
            Next

            Read()
            CurrentToken = Peek()
        End While
    End Sub

    Private Function ParseTrailingComments() As CommentCollection
        Dim Comments As ArrayList = New ArrayList

        ' Link in comments that follow the statement
        While Peek().Type = TokenType.Comment
            Dim CommentToken As CommentToken = CType(Scanner.Read(), CommentToken)
            Comments.Add(New Comment(CommentToken.Comment, CommentToken.IsREM, CommentToken.Span))
        End While

        If Comments.Count > 0 Then
            Dim NewList As CommentCollection = New CommentCollection(Comments)
            Comments.Clear()
            Return NewList
        End If
    End Function

    '*
    '* Helpers
    '*

    Private Sub PushBlockContext(ByVal type As TreeType)
        BlockContextStack.Push(type)
    End Sub

    Private Sub PopBlockContext()
        BlockContextStack.Pop()
    End Sub

    Private Function CurrentBlockContextType() As TreeType
        If BlockContextStack.Count = 0 Then
            Return TreeType.SyntaxError
        Else
            Return DirectCast(BlockContextStack.Peek(), TreeType)
        End If
    End Function

    Private Shared Function GetBinaryOperator(ByVal type As TokenType) As OperatorType
        Select Case type
            Case TokenType.Ampersand
                Return OperatorType.Concatenate

            Case TokenType.Star
                Return OperatorType.Multiply

            Case TokenType.ForwardSlash
                Return OperatorType.Divide

            Case TokenType.BackwardSlash
                Return OperatorType.IntegralDivide

            Case TokenType.Caret
                Return OperatorType.Power

            Case TokenType.Plus
                Return OperatorType.Plus

            Case TokenType.Minus
                Return OperatorType.Minus

            Case TokenType.LessThan
                Return OperatorType.LessThan

            Case TokenType.LessThanEquals
                Return OperatorType.LessThanEquals

            Case TokenType.Equals
                Return OperatorType.Equals

            Case TokenType.NotEquals
                Return OperatorType.NotEquals

            Case TokenType.GreaterThan
                Return OperatorType.GreaterThan

            Case TokenType.GreaterThanEquals
                Return OperatorType.GreaterThanEquals

            Case TokenType.LessThanLessThan
                Return OperatorType.ShiftLeft

            Case TokenType.GreaterThanGreaterThan
                Return OperatorType.ShiftRight

            Case TokenType.Mod
                Return OperatorType.Modulus

            Case TokenType.Or
                Return OperatorType.Or

            Case TokenType.OrElse
                Return OperatorType.OrElse

            Case TokenType.And
                Return OperatorType.And

            Case TokenType.AndAlso
                Return OperatorType.AndAlso

            Case TokenType.Xor
                Return OperatorType.Xor

            Case TokenType.Like
                Return OperatorType.Like

            Case TokenType.Is
                Return OperatorType.Is

            Case Else
                Return OperatorType.None
        End Select
    End Function

    Private Shared Function GetOperatorPrecedence(ByVal operator As OperatorType) As PrecedenceLevel
        Select Case operator
            Case OperatorType.Power
                Return PrecedenceLevel.Power

            Case OperatorType.Negate, OperatorType.UnaryPlus
                Return PrecedenceLevel.Negate

            Case OperatorType.Multiply, OperatorType.Divide
                Return PrecedenceLevel.Multiply

            Case OperatorType.IntegralDivide
                Return PrecedenceLevel.IntegralDivide

            Case OperatorType.Modulus
                Return PrecedenceLevel.Modulus

            Case OperatorType.Plus, OperatorType.Minus
                Return PrecedenceLevel.Plus

            Case OperatorType.Concatenate
                Return PrecedenceLevel.Concatenate

            Case OperatorType.ShiftLeft, OperatorType.ShiftRight
                Return PrecedenceLevel.Shift

            Case OperatorType.Equals, _
                 OperatorType.NotEquals, _
                 OperatorType.LessThan, _
                 OperatorType.GreaterThan, _
                 OperatorType.GreaterThanEquals, _
                 OperatorType.LessThanEquals, _
                 OperatorType.Is, _
                 OperatorType.Like
                Return PrecedenceLevel.Relational

            Case OperatorType.Not
                Return PrecedenceLevel.Not

            Case OperatorType.And, OperatorType.AndAlso
                Return PrecedenceLevel.And

            Case OperatorType.Or, OperatorType.OrElse
                Return PrecedenceLevel.Or

            Case OperatorType.Xor
                Return PrecedenceLevel.Xor

            Case Else
                Return PrecedenceLevel.None
        End Select
    End Function

    Private Shared Function GetAssignmentOperator(ByVal tokenType As TokenType, Optional ByRef compoundOperator As OperatorType = OperatorType.None) As TreeType
        Dim Type As TreeType

        Select Case tokenType
            Case tokenType.Equals
                Type = TreeType.AssignmentStatement
                compoundOperator = OperatorType.None

            Case tokenType.PlusEquals
                compoundOperator = OperatorType.Plus

            Case tokenType.AmpersandEquals
                compoundOperator = OperatorType.Concatenate

            Case tokenType.StarEquals
                compoundOperator = OperatorType.Multiply

            Case tokenType.MinusEquals
                compoundOperator = OperatorType.Minus

            Case tokenType.ForwardSlashEquals
                compoundOperator = OperatorType.Divide

            Case tokenType.BackwardSlashEquals
                compoundOperator = OperatorType.IntegralDivide

            Case tokenType.CaretEquals
                compoundOperator = OperatorType.Power

            Case tokenType.LessThanLessThanEquals
                compoundOperator = OperatorType.ShiftLeft

            Case tokenType.GreaterThanGreaterThanEquals
                compoundOperator = OperatorType.ShiftRight

            Case Else
                Type = TreeType.SyntaxError
                compoundOperator = OperatorType.None
        End Select

        If compoundOperator <> TreeType.SyntaxError Then
            Type = TreeType.CompoundAssignmentStatement
        End If

        Return Type
    End Function

    Private Shared Function IsRelationalOperator(ByVal type As TokenType) As Boolean
        Return type >= TokenType.LessThan AndAlso type <= TokenType.GreaterThanEquals
    End Function

    Private Shared Function GetExitType(ByVal tokenType As TokenType) As BlockType
        Select Case tokenType
            Case tokenType.Do
                Return BlockType.Do

            Case tokenType.For
                Return BlockType.For

            Case tokenType.While
                Return BlockType.While

            Case tokenType.Select
                Return BlockType.Select

            Case tokenType.Sub
                Return BlockType.Sub

            Case tokenType.Function
                Return BlockType.Function

            Case tokenType.Property
                Return BlockType.Property

            Case tokenType.Try
                Return BlockType.Try

            Case Else
                Return BlockType.None
        End Select
    End Function

    Private Shared Function GetBlockType(ByVal type As TokenType) As BlockType
        Select Case type
            Case TokenType.While
                Return BlockType.While

            Case TokenType.Select
                Return BlockType.Select

            Case TokenType.If
                Return BlockType.If

            Case TokenType.Try
                Return BlockType.Try

            Case TokenType.SyncLock
                Return BlockType.SyncLock

            Case TokenType.With
                Return BlockType.With

            Case TokenType.Sub
                Return BlockType.Sub

            Case TokenType.Function
                Return BlockType.Function

            Case TokenType.Get
                Return BlockType.Get

            Case TokenType.Set
                Return BlockType.Set

            Case TokenType.Property
                Return BlockType.Property

            Case TokenType.Class
                Return BlockType.Class

            Case TokenType.Structure
                Return BlockType.Structure

            Case TokenType.Module
                Return BlockType.Module

            Case TokenType.Interface
                Return BlockType.Interface

            Case TokenType.Enum
                Return BlockType.Enum

            Case TokenType.Namespace
                Return BlockType.Namespace

            Case Else
                Return BlockType.None
        End Select
    End Function

    Private Sub ReportSyntaxError(ByVal syntaxError As SyntaxError)
        If ErrorInConstruct Then
            Return
        End If

        ErrorInConstruct = True

        If Not ErrorTable Is Nothing Then
            ErrorTable.Add(syntaxError)
        End If
    End Sub

    Private Sub ReportSyntaxError(ByVal errorType As SyntaxErrorType, ByVal span As Span)
        ReportSyntaxError(New SyntaxError(errorType, span))
    End Sub

    Private Sub ReportSyntaxError(ByVal errorType As SyntaxErrorType, ByVal firstToken As Token, ByVal lastToken As Token)
        ' A lexical error takes precedence over the parser error
        If firstToken.Type = TokenType.LexicalError Then
            ReportSyntaxError(CType(firstToken, ErrorToken).SyntaxError)
        Else
            ReportSyntaxError(errorType, SpanFrom(firstToken, lastToken))
        End If
    End Sub

    Private Sub ReportSyntaxError(ByVal errorType As SyntaxErrorType, ByVal token As Token)
        ReportSyntaxError(errorType, token, token)
    End Sub

    Private Function StatementEndsBlock(ByVal blockStatementType As TreeType, ByVal endStatement As Statement) As Boolean
        Select Case blockStatementType
            Case TreeType.DoBlockStatement
                Return endStatement.Type = TreeType.LoopStatement

            Case TreeType.ForBlockStatement, TreeType.ForEachBlockStatement
                Return endStatement.Type = TreeType.NextStatement

            Case TreeType.WhileBlockStatement
                Return (endStatement.Type = TreeType.EndBlockStatement) AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.While

            Case TreeType.SyncLockBlockStatement
                Return (endStatement.Type = TreeType.EndBlockStatement) AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.SyncLock

            Case TreeType.WithBlockStatement
                Return (endStatement.Type = TreeType.EndBlockStatement) AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.With

            Case TreeType.TryBlockStatement, TreeType.CatchBlockStatement
                Return endStatement.Type = TreeType.CatchStatement OrElse _
                       endStatement.Type = TreeType.FinallyStatement OrElse _
                       (endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.Try)

            Case TreeType.FinallyBlockStatement
                Return endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.Try

            Case TreeType.SelectBlockStatement, TreeType.CaseBlockStatement
                Return endStatement.Type = TreeType.CaseStatement OrElse _
                       endStatement.Type = TreeType.CaseElseStatement OrElse _
                       (endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.Select)

            Case TreeType.CaseElseBlockStatement
                Return endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.Select

            Case TreeType.IfBlockStatement, TreeType.ElseIfBlockStatement
                Return endStatement.Type = TreeType.ElseIfStatement OrElse _
                       endStatement.Type = TreeType.ElseStatement OrElse _
                       (endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.If)

            Case TreeType.ElseBlockStatement
                Return endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.If

            Case TreeType.LineIfBlockStatement
                Return False

            Case TreeType.SubDeclaration, TreeType.ConstructorDeclaration
                Return endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.Sub

            Case TreeType.FunctionDeclaration
                Return endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.Function

            Case TreeType.GetDeclaration
                Return endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.Get

            Case TreeType.SetDeclaration
                Return endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.Set

            Case TreeType.PropertyDeclaration
                Return endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.Property

            Case TreeType.ClassDeclaration
                Return endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.Class

            Case TreeType.StructureDeclaration
                Return endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.Structure

            Case TreeType.ModuleDeclaration
                Return endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.Module

            Case TreeType.InterfaceDeclaration
                Return endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.Interface

            Case TreeType.EnumDeclaration
                Return endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.Enum

            Case TreeType.NamespaceDeclaration
                Return endStatement.Type = TreeType.EndBlockStatement AndAlso CType(endStatement, EndBlockStatement).EndType = BlockType.Namespace

            Case Else
                Debug.Fail("Unexpected.")
        End Select

        Return False
    End Function

    Private Function DeclarationEndsBlock(ByVal blockDeclarationType As TreeType, ByVal endDeclaration As EndBlockDeclaration) As Boolean
        Select Case blockDeclarationType
            Case TreeType.SubDeclaration, TreeType.ConstructorDeclaration
                Return endDeclaration.EndType = BlockType.Sub

            Case TreeType.FunctionDeclaration
                Return endDeclaration.EndType = BlockType.Function

            Case TreeType.PropertyDeclaration
                Return endDeclaration.EndType = BlockType.Property

            Case TreeType.GetDeclaration
                Return endDeclaration.EndType = BlockType.Get

            Case TreeType.SetDeclaration
                Return endDeclaration.EndType = BlockType.Set

            Case TreeType.ClassDeclaration
                Return endDeclaration.EndType = BlockType.Class

            Case TreeType.StructureDeclaration
                Return endDeclaration.EndType = BlockType.Structure

            Case TreeType.ModuleDeclaration
                Return endDeclaration.EndType = BlockType.Module

            Case TreeType.InterfaceDeclaration
                Return endDeclaration.EndType = BlockType.Interface

            Case TreeType.EnumDeclaration
                Return endDeclaration.EndType = BlockType.Enum

            Case TreeType.NamespaceDeclaration
                Return endDeclaration.EndType = BlockType.Namespace

            Case Else
                Debug.Fail("Unexpected.")
        End Select

        Return False
    End Function

    Private Function ValidInContext(ByVal blockType As TreeType, ByVal declarationType As TreeType) As Boolean
        Select Case declarationType
            Case TreeType.OptionDeclaration, TreeType.ImportsDeclaration, TreeType.AttributeDeclaration
                Return blockType = TreeType.File

            Case TreeType.NamespaceDeclaration
                Return blockType = TreeType.NamespaceDeclaration OrElse _
                       blockType = TreeType.File

            Case TreeType.ClassDeclaration, TreeType.StructureDeclaration, TreeType.InterfaceDeclaration, _
                 TreeType.DelegateSubDeclaration, TreeType.DelegateFunctionDeclaration, TreeType.EnumDeclaration
                Return blockType = TreeType.ClassDeclaration OrElse _
                       blockType = TreeType.StructureDeclaration OrElse _
                       blockType = TreeType.ModuleDeclaration OrElse _
                       blockType = TreeType.InterfaceDeclaration OrElse _
                       blockType = TreeType.NamespaceDeclaration OrElse _
                       blockType = TreeType.File

            Case TreeType.ModuleDeclaration
                Return blockType = TreeType.NamespaceDeclaration OrElse _
                       blockType = TreeType.File

            Case TreeType.EventDeclaration, TreeType.SubDeclaration, TreeType.FunctionDeclaration, _
                 TreeType.PropertyDeclaration
                Return blockType = TreeType.ClassDeclaration OrElse _
                       blockType = TreeType.StructureDeclaration OrElse _
                       blockType = TreeType.ModuleDeclaration OrElse _
                       blockType = TreeType.InterfaceDeclaration

            Case TreeType.VariableListDeclaration, TreeType.ExternalSubDeclaration, _
                 TreeType.ExternalFunctionDeclaration
                Return blockType = TreeType.ClassDeclaration OrElse _
                       blockType = TreeType.StructureDeclaration OrElse _
                       blockType = TreeType.ModuleDeclaration

            Case TreeType.ConstructorDeclaration
                Return blockType = TreeType.ClassDeclaration OrElse _
                       blockType = TreeType.StructureDeclaration

            Case TreeType.GetDeclaration, TreeType.SetDeclaration
                Return blockType = TreeType.PropertyDeclaration

            Case TreeType.InheritsDeclaration
                Return blockType = TreeType.ClassDeclaration OrElse _
                       blockType = TreeType.InterfaceDeclaration

            Case TreeType.ImplementsDeclaration
                Return blockType = TreeType.ClassDeclaration OrElse _
                       blockType = TreeType.StructureDeclaration

            Case TreeType.EnumValueDeclaration
                Return blockType = TreeType.EnumDeclaration

            Case TreeType.EmptyDeclaration
                Return True

            Case Else
                Debug.Fail("Unexpected.")
        End Select
    End Function

    Private Function ValidDeclaration(ByVal blockType As TreeType, ByVal declaration As Declaration, ByVal declarations As ArrayList) As SyntaxErrorType
        If Not ValidInContext(blockType, declaration.Type) Then
            Return InvalidDeclarationTypeError(blockType, declaration)
        End If

        If declaration.Type = TreeType.InheritsDeclaration Then
            For Each ExistingDeclaration As Declaration In declarations
                If blockType = TreeType.ClassDeclaration OrElse ExistingDeclaration.Type <> TreeType.InheritsDeclaration Then
                    Return SyntaxErrorType.InheritsMustBeFirst
                End If
            Next

            If CType(declaration, InheritsDeclaration).InheritedTypes.Count > 1 AndAlso blockType <> TreeType.InterfaceDeclaration Then
                Return SyntaxErrorType.NoMultipleInheritance
            End If
        End If

        If declaration.Type = TreeType.ImplementsDeclaration Then
            For Each ExistingDeclaration As Declaration In declarations
                If ExistingDeclaration.Type <> TreeType.InheritsDeclaration AndAlso _
                   ExistingDeclaration.Type <> TreeType.ImplementsDeclaration Then
                    Return SyntaxErrorType.ImplementsInWrongOrder
                End If
            Next
        End If

        If declaration.Type = TreeType.OptionDeclaration Then
            For Each ExistingDeclaration As Declaration In declarations
                If ExistingDeclaration.Type <> TreeType.OptionDeclaration Then
                    Return SyntaxErrorType.OptionStatementWrongOrder
                End If
            Next
        End If

        If declaration.Type = TreeType.ImportsDeclaration Then
            For Each ExistingDeclaration As Declaration In declarations
                If ExistingDeclaration.Type <> TreeType.OptionDeclaration AndAlso _
                   Existingdeclaration.Type <> TreeType.ImportsDeclaration Then
                    Return SyntaxErrorType.ImportsStatementWrongOrder
                End If
            Next
        End If

        If declaration.Type = TreeType.AttributeDeclaration Then
            For Each ExistingDeclaration As Declaration In declarations
                If ExistingDeclaration.Type <> TreeType.OptionDeclaration AndAlso _
                   ExistingDeclaration.Type <> TreeType.ImportsDeclaration AndAlso _
                   ExistingDeclaration.Type <> TreeType.AttributeDeclaration Then
                    Return SyntaxErrorType.AttributesStatementWrongOrder
                End If
            Next
        End If

        Return SyntaxErrorType.None
    End Function

    Private Sub ReportMismatchedEndError(ByVal blockType As TreeType, ByVal actualEndSpan As Span)
        Dim ErrorType As SyntaxErrorType

        Select Case blockType
            Case TreeType.DoBlockStatement
                ErrorType = SyntaxErrorType.ExpectedLoop

            Case TreeType.ForBlockStatement, TreeType.ForEachBlockStatement
                ErrorType = SyntaxErrorType.ExpectedNext

            Case TreeType.WhileBlockStatement
                ErrorType = SyntaxErrorType.ExpectedEndWhile

            Case TreeType.SelectBlockStatement, TreeType.CaseBlockStatement, TreeType.CaseElseBlockStatement
                ErrorType = SyntaxErrorType.ExpectedEndSelect

            Case TreeType.SyncLockBlockStatement
                ErrorType = SyntaxErrorType.ExpectedEndSyncLock

            Case TreeType.IfBlockStatement, TreeType.ElseIfBlockStatement, TreeType.ElseBlockStatement
                ErrorType = SyntaxErrorType.ExpectedEndIf

            Case TreeType.TryBlockStatement, TreeType.CatchBlockStatement, TreeType.FinallyBlockStatement
                ErrorType = SyntaxErrorType.ExpectedEndTry

            Case TreeType.WithBlockStatement
                ErrorType = SyntaxErrorType.ExpectedEndWith

            Case TreeType.SubDeclaration, TreeType.ConstructorDeclaration
                ErrorType = SyntaxErrorType.ExpectedEndSub

            Case TreeType.FunctionDeclaration
                ErrorType = SyntaxErrorType.ExpectedEndFunction

            Case TreeType.PropertyDeclaration
                ErrorType = SyntaxErrorType.ExpectedEndProperty

            Case TreeType.GetDeclaration
                ErrorType = SyntaxErrorType.ExpectedEndGet

            Case TreeType.SetDeclaration
                ErrorType = SyntaxErrorType.ExpectedEndSet

            Case TreeType.ClassDeclaration
                ErrorType = SyntaxErrorType.ExpectedEndClass

            Case TreeType.StructureDeclaration
                ErrorType = SyntaxErrorType.ExpectedEndStructure

            Case TreeType.ModuleDeclaration
                ErrorType = SyntaxErrorType.ExpectedEndModule

            Case TreeType.InterfaceDeclaration
                ErrorType = SyntaxErrorType.ExpectedEndInterface

            Case TreeType.EnumDeclaration
                ErrorType = SyntaxErrorType.ExpectedEndEnum

            Case TreeType.NamespaceDeclaration
                ErrorType = SyntaxErrorType.ExpectedEndNamespace

            Case Else
                Debug.Fail("Unexpected.")
        End Select

        ReportSyntaxError(ErrorType, actualEndSpan)
    End Sub

    Private Sub ReportMissingBeginStatementError(ByVal blockStatementType As TreeType, ByVal endStatement As Statement)
        Dim ErrorType As SyntaxErrorType

        Select Case endStatement.Type
            Case TreeType.LoopStatement
                ErrorType = SyntaxErrorType.LoopWithoutDo

            Case TreeType.NextStatement
                ErrorType = SyntaxErrorType.NextWithoutFor

            Case TreeType.EndBlockStatement
                Select Case CType(endStatement, EndBlockStatement).EndType
                    Case BlockType.While
                        ErrorType = SyntaxErrorType.EndWhileWithoutWhile

                    Case BlockType.Select
                        ErrorType = SyntaxErrorType.EndSelectWithoutSelect

                    Case BlockType.SyncLock
                        ErrorType = SyntaxErrorType.EndSyncLockWithoutSyncLock

                    Case BlockType.If
                        ErrorType = SyntaxErrorType.EndIfWithoutIf

                    Case BlockType.Try
                        ErrorType = SyntaxErrorType.EndTryWithoutTry

                    Case BlockType.With
                        ErrorType = SyntaxErrorType.EndWithWithoutWith

                    Case Else
                        Debug.Fail("Unexpected.")
                End Select

            Case TreeType.CatchStatement
                If blockStatementType = TreeType.FinallyBlockStatement Then
                    ErrorType = SyntaxErrorType.CatchAfterFinally
                Else
                    ErrorType = SyntaxErrorType.CatchWithoutTry
                End If

            Case TreeType.FinallyStatement
                If blockStatementType = TreeType.FinallyBlockStatement Then
                    ErrorType = SyntaxErrorType.FinallyAfterFinally
                Else
                    ErrorType = SyntaxErrorType.FinallyWithoutTry
                End If

            Case TreeType.CaseStatement
                If blockStatementType = TreeType.CaseElseBlockStatement Then
                    ErrorType = SyntaxErrorType.CaseAfterCaseElse
                Else
                    ErrorType = SyntaxErrorType.CaseWithoutSelect
                End If

            Case TreeType.CaseElseStatement
                If blockStatementType = TreeType.CaseElseBlockStatement Then
                    ErrorType = SyntaxErrorType.CaseElseAfterCaseElse
                Else
                    ErrorType = SyntaxErrorType.CaseElseWithoutSelect
                End If

            Case TreeType.ElseIfStatement
                If blockStatementType = TreeType.ElseBlockStatement Then
                    ErrorType = SyntaxErrorType.ElseIfAfterElse
                Else
                    ErrorType = SyntaxErrorType.ElseIfWithoutIf
                End If

            Case TreeType.ElseStatement
                If blockStatementType = TreeType.ElseBlockStatement Then
                    ErrorType = SyntaxErrorType.ElseAfterElse
                Else
                    ErrorType = SyntaxErrorType.ElseWithoutIf
                End If

            Case Else
                Debug.Fail("Unexpected.")
        End Select

        ReportSyntaxError(ErrorType, endStatement.Span)
    End Sub

    Private Sub ReportMissingBeginDeclarationError(ByVal endDeclaration As EndBlockDeclaration)
        Dim ErrorType As SyntaxErrorType

        Select Case endDeclaration.EndType
            Case BlockType.Sub
                ErrorType = SyntaxErrorType.EndSubWithoutSub

            Case BlockType.Function
                ErrorType = SyntaxErrorType.EndFunctionWithoutFunction

            Case BlockType.Property
                ErrorType = SyntaxErrorType.EndPropertyWithoutProperty

            Case BlockType.Get
                ErrorType = SyntaxErrorType.EndGetWithoutGet

            Case BlockType.Set
                ErrorType = SyntaxErrorType.EndSetWithoutSet

            Case BlockType.Class
                ErrorType = SyntaxErrorType.EndClassWithoutClass

            Case BlockType.Structure
                ErrorType = SyntaxErrorType.EndStructureWithoutStructure

            Case BlockType.Module
                ErrorType = SyntaxErrorType.EndModuleWithoutModule

            Case BlockType.Interface
                ErrorType = SyntaxErrorType.EndInterfaceWithoutInterface

            Case BlockType.Enum
                ErrorType = SyntaxErrorType.EndEnumWithoutEnum

            Case BlockType.Namespace
                ErrorType = SyntaxErrorType.EndNamespaceWithoutNamespace

            Case Else
                Debug.Fail("Unexpected.")
        End Select

        ReportSyntaxError(ErrorType, endDeclaration.Span)
    End Sub

    Private Function InvalidDeclarationTypeError(ByVal blockType As TreeType, ByVal badDeclaration As Declaration) As SyntaxErrorType
        Dim ErrorType As SyntaxErrorType

        Select Case blockType
            Case TreeType.PropertyDeclaration
                ErrorType = SyntaxErrorType.InvalidInsideProperty

            Case TreeType.ClassDeclaration
                ErrorType = SyntaxErrorType.InvalidInsideClass

            Case TreeType.StructureDeclaration
                ErrorType = SyntaxErrorType.InvalidInsideStructure

            Case TreeType.ModuleDeclaration
                ErrorType = SyntaxErrorType.InvalidInsideModule

            Case TreeType.InterfaceDeclaration
                ErrorType = SyntaxErrorType.InvalidInsideInterface

            Case TreeType.EnumDeclaration
                ErrorType = SyntaxErrorType.InvalidInsideEnum

            Case TreeType.NamespaceDeclaration, TreeType.File
                ErrorType = SyntaxErrorType.InvalidInsideNamespace

            Case Else
                Debug.Fail("Unexpected.")
        End Select

        Return ErrorType
    End Function

    Private Sub HandleUnexpectedToken(ByVal type As TokenType)
        Dim ErrorType As SyntaxErrorType

        Select Case type
            Case TokenType.Comma
                ErrorType = SyntaxErrorType.ExpectedComma

            Case TokenType.LeftParenthesis
                ErrorType = SyntaxErrorType.ExpectedLeftParenthesis

            Case TokenType.RightParenthesis
                ErrorType = SyntaxErrorType.ExpectedRightParenthesis

            Case TokenType.Equals
                ErrorType = SyntaxErrorType.ExpectedEquals

            Case TokenType.As
                ErrorType = SyntaxErrorType.ExpectedAs

            Case TokenType.RightCurlyBrace
                ErrorType = SyntaxErrorType.ExpectedRightCurlyBrace

            Case TokenType.Period
                ErrorType = SyntaxErrorType.ExpectedPeriod

            Case TokenType.Minus
                ErrorType = SyntaxErrorType.ExpectedMinus

            Case TokenType.Is
                ErrorType = SyntaxErrorType.ExpectedIs

            Case TokenType.GreaterThan
                ErrorType = SyntaxErrorType.ExpectedGreaterThan

            Case Else
                Debug.Fail("Should give a more specific error.")
                ErrorType = SyntaxErrorType.SyntaxError
        End Select

        ReportSyntaxError(ErrorType, Peek())
    End Sub

    Private Function VerifyExpectedToken(ByVal type As TokenType) As Location
        Dim Token As Token = Peek()

        If Token.Type = type Then
            Return ReadLocation()
        Else
            HandleUnexpectedToken(type)
            Return New Location
        End If
    End Function

    Private Function CanEndStatement(ByVal tokenType As TokenType) As Boolean
        Return tokenType = tokenType.Colon OrElse _
               tokenType = tokenType.LineTerminator OrElse _
               tokenType = tokenType.EndOfStream OrElse _
               tokenType = tokenType.Comment OrElse _
               (BlockContextStack.Count > 0 AndAlso CurrentBlockContextType() = TreeType.LineIfBlockStatement AndAlso tokenType = tokenType.Else)
    End Function

    Private Function BeginsStatement(ByVal tokenType As TokenType) As Boolean
        If Not CanEndStatement(tokenType) Then
            Return False
        End If

        Select Case tokenType
            Case tokenType.AddHandler, _
                 tokenType.Call, _
                 tokenType.Case, _
                 tokenType.Catch, _
                 tokenType.Class, _
                 tokenType.Const, _
                 tokenType.Declare, _
                 tokenType.Delegate, _
                 tokenType.Dim, _
                 tokenType.Do, _
                 tokenType.Else, _
                 tokenType.ElseIf, _
                 tokenType.End, _
                 tokenType.Endif, _
                 tokenType.Enum, _
                 tokenType.Erase, _
                 tokenType.Error, _
                 tokenType.Event, _
                 tokenType.Exit, _
                 tokenType.Finally, _
                 tokenType.For, _
                 tokenType.Friend, _
                 tokenType.Function, _
                 tokenType.Get, _
                 tokenType.GoTo, _
                 tokenType.GoSub, _
                 tokenType.If, _
                 tokenType.Implements, _
                 tokenType.Imports, _
                 tokenType.Inherits, _
                 tokenType.Interface, _
                 tokenType.Loop, _
                 tokenType.Module, _
                 tokenType.MustInherit, _
                 tokenType.MustOverride, _
                 tokenType.Namespace, _
                 tokenType.Next, _
                 tokenType.NotInheritable, _
                 tokenType.NotOverridable, _
                 tokenType.Option, _
                 tokenType.Overloads, _
                 tokenType.Overridable, _
                 tokenType.Overrides, _
                 tokenType.Private, _
                 tokenType.Property, _
                 tokenType.Protected, _
                 tokenType.Public, _
                 tokenType.RaiseEvent, _
                 tokenType.ReadOnly, _
                 tokenType.ReDim, _
                 tokenType.RemoveHandler, _
                 tokenType.Resume, _
                 tokenType.Return, _
                 tokenType.Select, _
                 tokenType.Shadows, _
                 tokenType.Shared, _
                 tokenType.Static, _
                 tokenType.Stop, _
                 tokenType.Structure, _
                 tokenType.Sub, _
                 tokenType.SyncLock, _
                 tokenType.Throw, _
                 tokenType.Try, _
                 tokenType.Wend, _
                 tokenType.While, _
                 tokenType.With, _
                 tokenType.WithEvents, _
                 tokenType.WriteOnly, _
                 tokenType.Pound
                Return True

            Case Else
                Return False
        End Select
    End Function

    Private Function VerifyEndOfStatement() As Token
        Dim NextToken As Token = Peek()

        Debug.Assert(Not NextToken.Type = TokenType.Comment, "Should have dealt with these by now!")

        If NextToken.Type = TokenType.LineTerminator OrElse NextToken.Type = TokenType.EndOfStream Then
            AtBeginningOfLine = True
        ElseIf NextToken.Type = TokenType.Colon Then
            AtBeginningOfLine = False
        ElseIf NextToken.Type = TokenType.Else AndAlso CurrentBlockContextType() = TreeType.LineIfBlockStatement Then
            ' Line If statements allow Else to end the statement
            AtBeginningOfLine = False
        Else
            ' Syntax error -- a valid statement is followed by something other than
            ' a colon, end-of-line, or a comment.
            ResyncAt()
            ReportSyntaxError(SyntaxErrorType.ExpectedEndOfStatement, NextToken)
            Return VerifyEndOfStatement()
        End If

        Return Read()
    End Function

    Private Function MustEndStatement(ByVal token As Token) As Boolean
        Return token.Type = TokenType.Colon OrElse _
               token.Type = TokenType.EndOfStream OrElse _
               token.Type = TokenType.LineTerminator OrElse _
               token.Type = TokenType.Comment
    End Function

    Private Function SpanFrom(ByVal location As Location) As Span
        Dim EndLocation As Location

        If Peek().Type = TokenType.EndOfStream Then
            EndLocation = Scanner.Previous().Span.Finish
        Else
            EndLocation = Peek().Span.Start
        End If

        Return New Span(location, EndLocation)
    End Function

    Private Function SpanFrom(ByVal token As Token) As Span
        Dim StartLocation, EndLocation As Location

        If token.Type = TokenType.EndOfStream AndAlso Not Scanner.IsOnFirstToken Then
            StartLocation = Scanner.Previous().Span.Finish
            EndLocation = StartLocation
        Else
            StartLocation = token.Span.Start

            If Peek().Type = TokenType.EndOfStream AndAlso Not Scanner.IsOnFirstToken Then
                EndLocation = Scanner.Previous().Span.Finish
            Else
                EndLocation = Peek().Span.Start
            End If
        End If

        Return New Span(StartLocation, EndLocation)
    End Function

    Private Function SpanFrom(ByVal startToken As Token, ByVal endToken As Token) As Span
        Dim StartLocation, EndLocation As Location

        If startToken.Type = TokenType.EndOfStream AndAlso Not Scanner.IsOnFirstToken Then
            StartLocation = Scanner.Previous().Span.Finish
            EndLocation = StartLocation
        Else
            StartLocation = startToken.Span.Start

            If endToken.Type = TokenType.EndOfStream AndAlso Not Scanner.IsOnFirstToken Then
                EndLocation = Scanner.Previous().Span.Finish
            ElseIf endToken.Span.Start.Index = startToken.Span.Start.Index Then
                EndLocation = endToken.Span.Finish
            Else
                EndLocation = endToken.Span.Start
            End If
        End If

        Return New Span(StartLocation, EndLocation)
    End Function

    Private Function SpanFrom(ByVal startToken As Token, ByVal endTree As Tree) As Span
        Return New Span(startToken.Span.Start, endTree.Span.Finish)
    End Function

    Private Function SpanFrom(ByVal startStatement As Statement, ByVal endStatement As Statement) As Span
        If endStatement Is Nothing Then
            Return SpanFrom(startStatement.Span.Start)
        Else
            Return New Span(startStatement.Span.Start, endStatement.Span.Start)
        End If
    End Function

    '*
    '* Names
    '*

    Private Function ParseSimpleName(ByVal allowKeyword As Boolean) As SimpleName
        If Peek().Type = TokenType.Identifier Then
            Dim IdentifierToken As IdentifierToken = CType(Read(), IdentifierToken)
            Return New SimpleName(IdentifierToken.Identifier, IdentifierToken.TypeCharacter, IdentifierToken.Escaped, IdentifierToken.Span)
        Else
            ' If the token is a keyword, assume that the user meant it to 
            ' be an identifer and consume it. Otherwise, leave current token
            ' as is and let caller decide what to do.
            If IdentifierToken.IsKeyword(Peek().Type) Then
                Dim IdentifierToken As IdentifierToken = CType(Read(), IdentifierToken)

                If Not allowKeyword Then
                    ReportSyntaxError(SyntaxErrorType.InvalidUseOfKeyword, IdentifierToken)
                End If

                Return New SimpleName(IdentifierToken.Identifier, IdentifierToken.TypeCharacter, IdentifierToken.Escaped, IdentifierToken.Span)
            Else
                ReportSyntaxError(SyntaxErrorType.ExpectedIdentifier, Peek())
                Return SimpleName.GetBadSimpleName(SpanFrom(Peek(), Peek()))
            End If
        End If
    End Function

    Private Function ParseName() As Name
        Dim Start As Token = Peek()
        Dim Result As Name

        Result = ParseSimpleName(False)

        If Peek().Type = TokenType.Period Then
            Dim Qualifier As SimpleName
            Dim DotLocation As Location

            Do
                DotLocation = ReadLocation()
                Qualifier = ParseSimpleName(True)
                Result = New QualifiedName(Result, DotLocation, Qualifier, SpanFrom(Start))
            Loop While Peek().Type = TokenType.Period
        End If

        Return Result
    End Function

    Private Function ParseVariableName(ByVal allowExplicitArraySizes As Boolean) As VariableName
        Dim Start As Token = Peek()
        Dim Name As SimpleName
        Dim ArrayType As ArrayTypeName

        ' CONSIDER: Often, programmers put extra decl specifiers where they are not required. 
        ' Eg: Dim x as Integer, Dim y as Long
        ' Check for this and give a more informative error?

        Name = ParseSimpleName(False)

        If Peek().Type = TokenType.LeftParenthesis Then
            ArrayType = ParseArrayTypeName(Nothing, Nothing, allowExplicitArraySizes, False)
        End If

        Return New VariableName(Name, ArrayType, SpanFrom(Start))
    End Function

    ' This function implements some of the special name parsing logic for names in a name
    ' list such as Implements and Handles
    Private Function ParseNameListName(Optional ByVal allowLeadingMyBase As Boolean = False) As Name
        Dim Start As Token = Peek()
        Dim Result As Name

        Result = ParseSimpleName(allowLeadingMyBase AndAlso Start.Type = TokenType.MyBase)

        If Peek().Type = TokenType.Period Then
            Dim Qualifier As SimpleName
            Dim DotLocation As Location

            Do
                DotLocation = ReadLocation()
                Qualifier = ParseSimpleName(True)
                Result = New QualifiedName(Result, DotLocation, Qualifier, SpanFrom(Start))
            Loop While Peek().Type = TokenType.Period
        Else
            ReportSyntaxError(SyntaxErrorType.ExpectedPeriod, Peek())
        End If

        Return Result
    End Function

    '*
    '* Types
    '*

    Private Function ParseTypeName(ByVal allowArrayType As Boolean) As TypeName
        Dim Start As Token = Peek()
        Dim Result As TypeName
        Dim Types As IntrinsicType

        Select Case Start.Type
            Case TokenType.Boolean
                Types = IntrinsicType.Boolean

            Case TokenType.Byte
                Types = IntrinsicType.Byte

            Case TokenType.Short
                Types = IntrinsicType.Short

            Case TokenType.Integer
                Types = IntrinsicType.Integer

            Case TokenType.Long
                Types = IntrinsicType.Long

            Case TokenType.Decimal
                Types = IntrinsicType.Decimal

            Case TokenType.Single
                Types = IntrinsicType.Single

            Case TokenType.Double
                Types = IntrinsicType.Double

            Case TokenType.Date
                Types = IntrinsicType.Date

            Case TokenType.Char
                Types = IntrinsicType.Char

            Case TokenType.String
                Types = IntrinsicType.String

            Case TokenType.Object
                Types = IntrinsicType.Object

            Case TokenType.Identifier
                Dim Name As Name = ParseName()
                Result = New NamedTypeName(Name, Name.Span)

            Case Else
                ReportSyntaxError(SyntaxErrorType.ExpectedType, Start)
                Result = NamedTypeName.GetBadNamedType(SpanFrom(Start))
        End Select

        If Result Is Nothing Then
            Read()
            Result = New IntrinsicTypeName(Types, Start.Span)
        End If

        If allowArrayType AndAlso Peek().Type = TokenType.LeftParenthesis Then
            Return ParseArrayTypeName(Start, Result, False, False)
        Else
            Return Result
        End If
    End Function

    Private Function ArrayBoundsContinue() As Boolean
        Dim NextToken As Token = Peek()

        If NextToken.Type = TokenType.Comma Then
            Return True
        ElseIf NextToken.Type = TokenType.RightParenthesis OrElse MustEndStatement(NextToken) Then
            Return False
        End If

        ReportSyntaxError(SyntaxErrorType.ArgumentSyntax, NextToken)
        ResyncAt(TokenType.Comma, TokenType.RightParenthesis)

        If Peek().Type = TokenType.Comma Then
            ErrorInConstruct = False
            Return True
        End If

        Return False
    End Function

    Private Function ParseArrayTypeName(ByVal startType As Token, ByVal elementType As TypeName, ByVal allowExplicitSizes As Boolean, ByVal innerArrayType As Boolean) As ArrayTypeName
        Dim ArgumentsStart As Token = Peek()
        Dim Arguments As ArrayList
        Dim CommaLocations As ArrayList = New ArrayList
        Dim RightParenthesisLocation As Location
        Dim ArgumentCollection As ArgumentCollection

        Debug.Assert(Peek().Type = TokenType.LeftParenthesis)

        If startType Is Nothing Then
            startType = Peek()
        End If

        Read()

        If Peek().Type = TokenType.RightParenthesis OrElse Peek().Type = TokenType.Comma Then
            While Peek().Type = TokenType.Comma
                CommaLocations.Add(ReadLocation())
            End While
        Else
            Dim SizeStart As Token
            Dim Size As Expression

            If Not allowExplicitSizes Then
                If innerArrayType Then
                    ReportSyntaxError(SyntaxErrorType.NoConstituentArraySizes, Peek())
                Else
                    ReportSyntaxError(SyntaxErrorType.NoExplicitArraySizes, Peek())
                End If
            End If

            Arguments = New ArrayList

            Do
                If Arguments.Count > 0 Then
                    CommaLocations.Add(ReadLocation())
                End If

                SizeStart = Peek()
                Size = ParseExpression()

                If ErrorInConstruct Then
                    ResyncAt(TokenType.Comma, TokenType.RightParenthesis, TokenType.As)
                End If

                Arguments.Add(New Argument(Nothing, Nothing, Size, SpanFrom(SizeStart)))
            Loop While ArrayBoundsContinue()
        End If

        RightParenthesisLocation = VerifyExpectedToken(TokenType.RightParenthesis)
        ArgumentCollection = New ArgumentCollection(Arguments, CommaLocations, RightParenthesisLocation, SpanFrom(ArgumentsStart))

        If Peek().Type = TokenType.LeftParenthesis Then
            elementType = ParseArrayTypeName(Peek(), elementType, False, True)
        End If

        Return New ArrayTypeName(elementType, CommaLocations.Count + 1, ArgumentCollection, SpanFrom(startType))
    End Function

    '*
    '* Initializers
    '*

    Private Function ParseInitializer() As Initializer
        If Peek().Type = TokenType.LeftCurlyBrace Then
            Return ParseAggregateInitializer()
        Else
            Dim Expression As Expression = ParseExpression()
            Return New ExpressionInitializer(Expression, Expression.Span)
        End If
    End Function

    Private Function InitializersContinue() As Boolean
        Dim NextToken As Token = Peek()

        If NextToken.Type = TokenType.Comma Then
            Return True
        ElseIf NextToken.Type = TokenType.RightCurlyBrace OrElse MustEndStatement(NextToken) Then
            Return False
        End If

        ReportSyntaxError(SyntaxErrorType.InitializerSyntax, NextToken)
        ResyncAt(TokenType.Comma, TokenType.RightCurlyBrace)

        If Peek().Type = TokenType.Comma Then
            ErrorInConstruct = False
            Return True
        End If

        Return False
    End Function

    Private Function ParseAggregateInitializer() As AggregateInitializer
        Dim Start As Token = Peek()
        Dim Initializers As ArrayList = New ArrayList
        Dim RightBracketLocation As Location
        Dim CommaLocations As ArrayList = New ArrayList

        Debug.Assert(Start.Type = TokenType.LeftCurlyBrace)
        Read()

        If Peek().Type <> TokenType.RightCurlyBrace Then
            Do
                If Initializers.Count > 0 Then
                    CommaLocations.Add(ReadLocation())
                End If

                Initializers.Add(ParseInitializer())

                If ErrorInConstruct Then
                    ResyncAt(TokenType.Comma, TokenType.RightCurlyBrace)
                End If
            Loop While InitializersContinue()
        End If

        RightBracketLocation = VerifyExpectedToken(TokenType.RightCurlyBrace)

        Return New AggregateInitializer(New InitializerCollection(Initializers, CommaLocations, RightBracketLocation, SpanFrom(Start)), SpanFrom(Start))
    End Function

    '*
    '* Arguments
    '*

    Private Function ParseArgument(ByRef foundNamedArgument As Boolean) As Argument
        Dim Start As Token = Read()
        Dim Value As Expression
        Dim Name As SimpleName
        Dim ColonEqualsLocation As Location

        If Peek().Type = TokenType.ColonEquals Then
            If Not foundNamedArgument Then
                foundNamedArgument = True
            End If
        Else
            If foundNamedArgument Then
                ReportSyntaxError(SyntaxErrorType.ExpectedNamedArgument, Start)
                foundNamedArgument = False
            End If
        End If

        Backtrack(Start)

        If foundNamedArgument Then
            Name = ParseSimpleName(True)
            ColonEqualsLocation = ReadLocation()
            Value = ParseExpression()
        Else
            Name = Nothing
            Value = ParseExpression()
        End If

        If ErrorInConstruct Then
            ResyncAt(TokenType.Comma, TokenType.RightParenthesis)

            If Peek().Type = TokenType.Comma Then
                ErrorInConstruct = False
            End If
        End If

        Return New Argument(Name, ColonEqualsLocation, Value, SpanFrom(Start))
    End Function

    Private Function ArgumentsContinue() As Boolean
        Dim NextToken As Token = Peek()

        If NextToken.Type = TokenType.Comma Then
            Return True
        ElseIf NextToken.Type = TokenType.RightParenthesis OrElse MustEndStatement(NextToken) Then
            Return False
        End If

        ReportSyntaxError(SyntaxErrorType.ArgumentSyntax, NextToken)
        ResyncAt(TokenType.Comma, TokenType.RightParenthesis)

        If Peek().Type = TokenType.Comma Then
            ErrorInConstruct = False
            Return True
        End If

        Return False
    End Function

    Private Function ParseArguments() As ArgumentCollection
        Dim Start As Token = Peek()
        Dim Arguments As ArrayList = New ArrayList
        Dim CommaLocations As ArrayList = New ArrayList
        Dim RightParenthesisLocation As Location

        If Start.Type <> TokenType.LeftParenthesis Then
            Return Nothing
        Else
            Read()
        End If

        If Peek().Type <> TokenType.RightParenthesis Then
            Dim FoundNamedArgument As Boolean = False
            Dim ArgumentStart As Token
            Dim Argument As Argument

            Do
                If Arguments.Count > 0 Then
                    CommaLocations.Add(ReadLocation())
                End If

                ArgumentStart = Peek()

                If ArgumentStart.Type = TokenType.Comma OrElse ArgumentStart.Type = TokenType.RightParenthesis Then
                    If FoundNamedArgument Then
                        ReportSyntaxError(SyntaxErrorType.ExpectedNamedArgument, Peek())
                    End If

                    Argument = Nothing
                Else
                    Argument = ParseArgument(FoundNamedArgument)
                End If

                Arguments.Add(Argument)
            Loop While ArgumentsContinue()
        End If

        If Peek().Type = TokenType.RightParenthesis Then
            RightParenthesisLocation = ReadLocation()
        Else
            Dim CurrentToken As Token = Peek()

            ' On error, peek for ")" with "(". If ")" seen before 
            ' "(", then sync on that. Otherwise, assume missing ")"
            ' and let caller decide.
            ResyncAt(TokenType.LeftParenthesis, TokenType.RightParenthesis)

            If Peek().Type = TokenType.RightParenthesis Then
                ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek())
                RightParenthesisLocation = ReadLocation()
            Else
                Backtrack(CurrentToken)
                ReportSyntaxError(SyntaxErrorType.ExpectedRightParenthesis, Peek())
            End If
        End If

        Return New ArgumentCollection(Arguments, CommaLocations, RightParenthesisLocation, SpanFrom(Start))
    End Function

    '*
    '* Expressions
    '*

    Private Function ParseLiteralExpression() As LiteralExpression
        Dim Start As Token = Read()

        Select Case Start.Type
            Case TokenType.True, TokenType.False
                Return New BooleanLiteralExpression(Start.Type = TokenType.True, Start.Span)

            Case TokenType.IntegerLiteral
                Dim Literal As IntegerLiteralToken = CType(Start, IntegerLiteralToken)
                Return New IntegerLiteralExpression(Literal.Literal, Literal.IntegerBase, Literal.TypeCharacter, Literal.Span)

            Case TokenType.FloatingPointLiteral
                Dim Literal As FloatingPointLiteralToken = CType(Start, FloatingPointLiteralToken)
                Return New FloatingPointLiteralExpression(Literal.Literal, Literal.TypeCharacter, Literal.Span)

            Case TokenType.DecimalLiteral
                Dim Literal As DecimalLiteralToken = CType(Start, DecimalLiteralToken)
                Return New DecimalLiteralExpression(Literal.Literal, Literal.TypeCharacter, Literal.Span)

            Case TokenType.CharacterLiteral
                Dim Literal As CharacterLiteralToken = CType(Start, CharacterLiteralToken)
                Return New CharacterLiteralExpression(Literal.Literal, Literal.Span)

            Case TokenType.StringLiteral
                Dim Literal As StringLiteralToken = CType(Start, StringLiteralToken)
                Return New StringLiteralExpression(Literal.Literal, Literal.Span)

            Case TokenType.DateLiteral
                Dim Literal As DateLiteralToken = CType(Start, DateLiteralToken)
                Return New DateLiteralExpression(Literal.Literal, Literal.Span)

            Case Else
                Debug.Fail("Unexpected.")
        End Select

        Return Nothing
    End Function

    Private Function ParseCastExpression() As Expression
        Dim Start As Token = Read()
        Dim OperatorType As IntrinsicType
        Dim Operand As Expression
        Dim LeftParenthesisLocation As Location
        Dim RightParenthesisLocation As Location

        Select Case Start.Type
            Case TokenType.CBool
                OperatorType = IntrinsicType.Boolean

            Case TokenType.CChar
                OperatorType = IntrinsicType.Char

            Case TokenType.CDate
                OperatorType = IntrinsicType.Date

            Case TokenType.CDbl
                OperatorType = IntrinsicType.Double

            Case TokenType.CByte
                OperatorType = IntrinsicType.Byte

            Case TokenType.CShort
                OperatorType = IntrinsicType.Short

            Case TokenType.CInt
                OperatorType = IntrinsicType.Integer

            Case TokenType.CLng
                OperatorType = IntrinsicType.Long

            Case TokenType.CSng
                OperatorType = IntrinsicType.Single

            Case TokenType.CStr
                OperatorType = IntrinsicType.String

            Case TokenType.CDec
                OperatorType = IntrinsicType.Decimal

            Case TokenType.CObj
                OperatorType = IntrinsicType.Object

            Case Else
                Debug.Fail("Unexpected.")
                Return Nothing
        End Select

        LeftParenthesisLocation = VerifyExpectedToken(TokenType.LeftParenthesis)
        Operand = ParseExpression()
        RightParenthesisLocation = VerifyExpectedToken(TokenType.RightParenthesis)

        Return New IntrinsicCastExpression(OperatorType, LeftParenthesisLocation, Operand, RightParenthesisLocation, SpanFrom(Start))
    End Function

    Private Function ParseCTypeExpression() As Expression
        Dim Start As Token = Read()
        Dim Operand As Expression
        Dim Target As TypeName
        Dim LeftParenthesisLocation As Location
        Dim RightParenthesisLocation As Location
        Dim CommaLocation As Location

        Debug.Assert(Start.Type = TokenType.CType OrElse Start.Type = TokenType.DirectCast)

        LeftParenthesisLocation = VerifyExpectedToken(TokenType.LeftParenthesis)
        Operand = ParseExpression()

        If ErrorInConstruct Then
            ResyncAt(TokenType.Comma, TokenType.RightParenthesis)
        End If

        CommaLocation = VerifyExpectedToken(TokenType.Comma)
        Target = ParseTypeName(True)
        RightParenthesisLocation = VerifyExpectedToken(TokenType.RightParenthesis)

        If Start.Type = TokenType.CType Then
            Return New CTypeExpression(LeftParenthesisLocation, Operand, CommaLocation, Target, RightParenthesisLocation, SpanFrom(Start))
        Else
            Return New DirectCastExpression(LeftParenthesisLocation, Operand, CommaLocation, Target, RightParenthesisLocation, SpanFrom(Start))
        End If
    End Function

    Private Function ParseInstanceExpression() As Expression
        Dim Start As Token = Read()
        Dim InstanceType As InstanceType

        Select Case Start.Type
            Case TokenType.Me
                InstanceType = InstanceType.Me

            Case TokenType.MyClass
                InstanceType = InstanceType.MyClass

                If Peek().Type <> TokenType.Period Then
                    ReportSyntaxError(SyntaxErrorType.ExpectedPeriodAfterMyClass, Start)
                End If

            Case TokenType.MyBase
                InstanceType = InstanceType.MyBase

                If Peek().Type <> TokenType.Period Then
                    ReportSyntaxError(SyntaxErrorType.ExpectedPeriodAfterMyBase, Start)
                End If

            Case Else
                Debug.Fail("Unexpected.")
        End Select

        Return New InstanceExpression(InstanceType, Start.Span)
    End Function

    Private Function ParseParentheticalExpression() As Expression
        Dim Operand As Expression
        Dim Start As Token = Read()
        Dim RightParenthesisLocation As Location

        Debug.Assert(Start.Type = TokenType.LeftParenthesis)

        Operand = ParseExpression()
        RightParenthesisLocation = VerifyExpectedToken(TokenType.RightParenthesis)

        Return New ParentheticalExpression(Operand, RightParenthesisLocation, SpanFrom(Start))
    End Function

    Private Function ParseSimpleNameExpression() As Expression
        Dim Start As Token = Peek()
        Return New SimpleNameExpression(ParseSimpleName(False), SpanFrom(Start))
    End Function

    Private Function ParseDotBangExpression(ByVal start As Token, ByVal terminal As Expression) As Expression
        Dim Name As SimpleName
        Dim DotBang As Token

        Debug.Assert(Peek().Type = TokenType.Period OrElse Peek().Type = TokenType.Exclamation)

        DotBang = Read()
        Name = ParseSimpleName(True)

        If DotBang.Type = TokenType.Period Then
            Return New QualifiedExpression(terminal, DotBang.Span.Start, Name, SpanFrom(start))
        Else
            Return New DictionaryLookupExpression(terminal, DotBang.Span.Start, Name, SpanFrom(start))
        End If
    End Function

    Private Function ParseCallOrIndexExpression(ByVal start As Token, ByVal terminal As Expression) As Expression
        Dim Arguments As ArgumentCollection

        ' Because parentheses are used for array indexing, parameter passing, and array
        ' declaring (via the Redim statement), there is some ambiguity about how to handle
        ' a parenthesized list that begins with an expression. The most general case is to
        ' parse it as an argument list.

        Arguments = ParseArguments()
        Return New CallOrIndexExpression(terminal, Arguments, SpanFrom(start))
    End Function

    Private Function ParseTypeOfExpression() As Expression
        Dim Start As Token = Peek()
        Dim Value As Expression
        Dim Target As TypeName
        Dim IsLocation As Location

        Debug.Assert(Start.Type = TokenType.TypeOf)

        Read()
        Value = ParseBinaryOperatorExpression(PrecedenceLevel.Relational)

        If ErrorInConstruct Then
            ResyncAt(TokenType.Is)
        End If

        IsLocation = VerifyExpectedToken(TokenType.Is)
        Target = ParseTypeName(True)

        Return New TypeOfExpression(Value, IsLocation, Target, SpanFrom(Start))
    End Function

    Private Function ParseGetTypeExpression() As Expression
        Dim Start As Token = Read()
        Dim Target As TypeName
        Dim LeftParenthesisLocation As Location
        Dim RightParenthesisLocation As Location

        Debug.Assert(Start.Type = TokenType.GetType)
        LeftParenthesisLocation = VerifyExpectedToken(TokenType.LeftParenthesis)
        Target = ParseTypeName(True)
        RightParenthesisLocation = VerifyExpectedToken(TokenType.RightParenthesis)

        Return New GetTypeExpression(LeftParenthesisLocation, Target, RightParenthesisLocation, SpanFrom(Start))
    End Function

    Private Function ParseNewExpression() As Expression
        Dim Start As Token = Read()
        Dim TypeStart As Token
        Dim Type As TypeName
        Dim Arguments As ArgumentCollection
        Dim ArgumentsStart As Token

        Debug.Assert(Start.Type = TokenType.[New])

        TypeStart = Peek()
        Type = ParseTypeName(False)

        If ErrorInConstruct Then
            ResyncAt(TokenType.LeftParenthesis)
        End If

        ArgumentsStart = Peek()

        ' This is an ambiguity in the grammar between
        '
        ' New <Type> ( <Arguments> )
        ' New <Type> <ArrayDeclaratorList> <AggregateInitializer>
        '
        ' Try it as the first form, and if this fails, try the second.
        ' (All valid instances of the second form have a beginning that is a valid
        ' instance of the first form, so no spurious errors should result.)

        Arguments = ParseArguments()

        If (Peek().Type = TokenType.LeftCurlyBrace OrElse Peek().Type = TokenType.LeftParenthesis) AndAlso _
           Not Arguments Is Nothing Then
            Dim ArrayType As ArrayTypeName

            ' Treat this as the form of New expression that allocates an array.
            Backtrack(ArgumentsStart)
            ArrayType = ParseArrayTypeName(TypeStart, Type, True, False)

            If Peek().Type = TokenType.LeftCurlyBrace Then
                Dim Initializer As AggregateInitializer = ParseAggregateInitializer()
                Return New NewAggregateExpression(ArrayType, Initializer, SpanFrom(Start))
            Else
                HandleUnexpectedToken(TokenType.LeftCurlyBrace)
            End If
        End If

        Return New NewExpression(Type, Arguments, SpanFrom(Start))
    End Function

    Private Function ParseTerminalExpression() As Expression
        Dim Start As Token = Peek()
        Dim Terminal As Expression

        Select Case Start.Type
            Case TokenType.Identifier
                Terminal = ParseSimpleNameExpression()

            Case TokenType.IntegerLiteral, TokenType.FloatingPointLiteral, TokenType.DecimalLiteral, _
                 TokenType.CharacterLiteral, TokenType.StringLiteral, TokenType.DateLiteral, _
                 TokenType.True, TokenType.False
                Terminal = ParseLiteralExpression()

            Case TokenType.CBool, TokenType.CByte, TokenType.CShort, TokenType.CInt, TokenType.CLng, _
                 TokenType.CDec, TokenType.CSng, TokenType.CDbl, TokenType.CChar, TokenType.CStr, _
                 TokenType.CDate, TokenType.CObj
                Terminal = ParseCastExpression()

            Case TokenType.DirectCast, TokenType.CType
                Terminal = ParseCTypeExpression()

            Case TokenType.Me, TokenType.MyBase, TokenType.MyClass
                Terminal = ParseInstanceExpression()

            Case TokenType.Nothing
                Terminal = New NothingExpression(Read().Span)

            Case TokenType.LeftParenthesis
                Terminal = ParseParentheticalExpression()

            Case TokenType.Period, TokenType.Exclamation
                Terminal = ParseDotBangExpression(Start, Nothing)

            Case TokenType.TypeOf
                Terminal = ParseTypeOfExpression()

            Case TokenType.GetType
                Terminal = ParseGetTypeExpression()

            Case TokenType.[New]
                Terminal = ParseNewExpression()

            Case TokenType.Short, TokenType.Integer, TokenType.Long, TokenType.Decimal, _
                 TokenType.Single, TokenType.Double, TokenType.Byte, TokenType.Boolean, _
                 TokenType.Char, TokenType.Date, TokenType.String, TokenType.Object
                Dim ReferencedType As TypeName = ParseTypeName(False)

                Terminal = New TypeReferenceExpression(ReferencedType, ReferencedType.Span)

                If Scanner.Peek.Type = TokenType.Period Then
                    Terminal = ParseDotBangExpression(Start, Terminal)
                Else
                    HandleUnexpectedToken(TokenType.Period)
                End If

            Case Else
                ReportSyntaxError(SyntaxErrorType.ExpectedExpression, Peek())
                Terminal = Expression.GetBadExpression(SpanFrom(Peek(), Peek()))
        End Select

        ' Valid suffixes are ".", "!", and "(". Everything else is considered
        ' to end the term.

        While True
            Dim NextToken As Token = Peek()

            If NextToken.Type = TokenType.Period OrElse NextToken.Type = TokenType.Exclamation Then
                Terminal = ParseDotBangExpression(Start, Terminal)
            ElseIf NextToken.Type = TokenType.LeftParenthesis Then
                Terminal = ParseCallOrIndexExpression(Start, Terminal)
            Else
                Exit While
            End If
        End While

        Return Terminal
    End Function

    Private Function ParseUnaryOperatorExpression() As Expression
        Dim Start As Token = Peek()
        Dim Operand As Expression
        Dim OperatorType As OperatorType

        Select Case Start.Type
            Case TokenType.Minus
                OperatorType = OperatorType.Negate

            Case TokenType.Plus
                OperatorType = OperatorType.UnaryPlus

            Case TokenType.Not
                OperatorType = OperatorType.Not

            Case TokenType.AddressOf
                Read()
                Operand = ParseBinaryOperatorExpression(PrecedenceLevel.None)
                Return New AddressOfExpression(Operand, SpanFrom(Start, Operand))

            Case Else
                Return ParseTerminalExpression()
        End Select

        Read()
        Operand = ParseBinaryOperatorExpression(GetOperatorPrecedence(OperatorType))
        Return New UnaryOperatorExpression(OperatorType, Operand, SpanFrom(Start, Operand))
    End Function

    Private Function ParseBinaryOperatorExpression(ByVal pendingPrecedence As PrecedenceLevel) As Expression
        Dim Expression As Expression
        Dim Start As Token = Peek()

        Expression = ParseUnaryOperatorExpression()

        ' Parse operators that follow the term according to precedence.
        While True
            Dim OperatorType As OperatorType = GetBinaryOperator(Peek().Type)
            Dim Binary As BinaryOperatorExpression
            Dim Right As Expression
            Dim Precedence As PrecedenceLevel
            Dim OperatorLocation As Location

            If OperatorType = OperatorType.None Then
                Exit While
            End If

            Precedence = GetOperatorPrecedence(OperatorType)

            ' Only continue parsing if precedence is high enough
            If Precedence <= pendingPrecedence Then
                Exit While
            End If

            OperatorLocation = ReadLocation()
            Right = ParseBinaryOperatorExpression(Precedence)
            Expression = New BinaryOperatorExpression(Expression, OperatorType, OperatorLocation, Right, SpanFrom(Start, Right))
        End While

        Return Expression
    End Function

    Private Function ParseExpressionList(Optional ByVal requireExpression As Boolean = False) As ExpressionCollection
        Dim Start As Token = Peek()
        Dim Expressions As ArrayList = New ArrayList
        Dim CommaLocations As ArrayList = New ArrayList

        If CanEndStatement(Start.Type) AndAlso Not requireExpression Then
            Return Nothing
        End If

        Do
            If Expressions.Count > 0 Then
                CommaLocations.Add(ReadLocation())
            End If

            Expressions.Add(ParseExpression())

            If ErrorInConstruct Then
                ResyncAt(TokenType.Comma)
            End If
        Loop While Peek().Type = TokenType.Comma

        If Expressions.Count = 0 AndAlso CommaLocations.Count = 0 Then
            Return Nothing
        Else
            Return New ExpressionCollection(Expressions, CommaLocations, SpanFrom(Start))
        End If
    End Function

    Private Function ParseExpression() As Expression
        Return ParseBinaryOperatorExpression(PrecedenceLevel.None)
    End Function

    '*
    '* Statements
    '*

    Private Function ParseExpressionStatement(ByVal type As TreeType, ByVal operandOptional As Boolean) As Statement
        Dim Start As Token = Peek()
        Dim Operand As Expression

        Read()

        If Not operandOptional OrElse Not CanEndStatement(Peek().Type) Then
            Operand = ParseExpression()
        End If

        If ErrorInConstruct Then
            ResyncAt()
        End If

        Select Case type
            Case TreeType.ReturnStatement
                Return New ReturnStatement(Operand, SpanFrom(Start), ParseTrailingComments())

            Case TreeType.ErrorStatement
                Return New ErrorStatement(Operand, SpanFrom(Start), ParseTrailingComments())

            Case TreeType.ThrowStatement
                Return New ThrowStatement(Operand, SpanFrom(Start), ParseTrailingComments())

            Case Else
                Debug.Fail("Unexpected!")
        End Select
    End Function

    ' Parse a reference to a label, which can be an identifier or a line number.
    Private Sub ParseLabelReference(ByRef name As SimpleName, ByRef isLineNumber As Boolean)
        Dim Start As Token = Peek()

        If Start.Type = TokenType.Identifier Then
            name = ParseSimpleName(False)
            isLineNumber = False
        ElseIf Start.Type = TokenType.IntegerLiteral Then
            Dim IntegerLiteral As IntegerLiteralToken = CType(Start, IntegerLiteralToken)

            If IntegerLiteral.TypeCharacter <> TypeCharacter.None Then
                ReportSyntaxError(SyntaxErrorType.SyntaxError, Start)
            End If

            name = New SimpleName(CStr(IntegerLiteral.Literal), TypeCharacter.None, False, IntegerLiteral.Span)
            isLineNumber = True
            Read()
        Else
            ReportSyntaxError(SyntaxErrorType.ExpectedIdentifier, Start)
            name = SimpleName.GetBadSimpleName(SpanFrom(Start))
            isLineNumber = False
        End If
    End Sub

    Private Function ParseGotoStatement() As Statement
        Dim Start As Token = Peek()
        Dim LabelReference As LabelReferenceStatement
        Dim Name As SimpleName
        Dim IsLineNumber As Boolean

        Read()
        ParseLabelReference(Name, IsLineNumber)

        If ErrorInConstruct Then
            ResyncAt()
        End If

        Return New GotoStatement(Name, IsLineNumber, SpanFrom(Start), ParseTrailingComments())
    End Function

    Private Function ParseExitStatement() As Statement
        Dim Start As Token = Peek()
        Dim ExitType As BlockType
        Dim ExitArgumentLocation As Location

        Read()

        ExitType = GetExitType(Peek().Type)

        If ExitType = BlockType.None Then
            ReportSyntaxError(SyntaxErrorType.ExpectedExitKind, Peek())
            ResyncAt()
        Else
            ExitArgumentLocation = ReadLocation()
        End If

        Return New ExitStatement(ExitType, ExitArgumentLocation, SpanFrom(Start), ParseTrailingComments())
    End Function

    Private Function ParseEndStatement() As Statement
        Dim Start As Token = Read()
        Dim EndType As BlockType = GetBlockType(Peek().Type)

        If EndType = BlockType.None Then
            Return New EndStatement(SpanFrom(Start), ParseTrailingComments())
        End If

        Return New EndBlockStatement(EndType, ReadLocation(), SpanFrom(Start), ParseTrailingComments())
    End Function

    Private Function ParseRaiseEventStatement() As Statement
        Dim Start As Token = Peek()
        Dim Name As SimpleName
        Dim Arguments As ArgumentCollection

        Read()
        Name = ParseSimpleName(True)

        If ErrorInConstruct Then
            ResyncAt()
        End If

        Arguments = ParseArguments()

        Return New RaiseEventStatement(Name, Arguments, SpanFrom(Start), ParseTrailingComments())
    End Function

    Private Function ParseHandlerStatement() As Statement
        Dim Start As Token = Peek()
        Dim EventName As Expression
        Dim DelegateExpression As Expression
        Dim CommaLocation As Location

        Read()
        EventName = ParseExpression()

        If ErrorInConstruct Then
            ResyncAt(TokenType.Comma)
        End If

        CommaLocation = VerifyExpectedToken(TokenType.Comma)
        DelegateExpression = ParseExpression()

        If ErrorInConstruct Then
            ResyncAt()
        End If

        If Start.Type = TokenType.AddHandler Then
            Return New AddHandlerStatement(EventName, CommaLocation, DelegateExpression, SpanFrom(Start), ParseTrailingComments())
        Else
            Return New RemoveHandlerStatement(EventName, CommaLocation, DelegateExpression, SpanFrom(Start), ParseTrailingComments())
        End If
    End Function

    Private Function ParseOnErrorStatement() As Statement
        Dim Start As Token = Read()
        Dim OnErrorType As OnErrorType
        Dim NextToken As Token
        Dim Name As SimpleName
        Dim IsLineNumber As Boolean
        Dim ErrorLocation As Location
        Dim ResumeOrGoToLocation As Location
        Dim NextOrZeroOrMinusLocation As Location
        Dim OneLocation As Location

        If Peek().Type = TokenType.Error Then
            ErrorLocation = ReadLocation()
            NextToken = Peek()

            If NextToken.Type = TokenType.Resume Then
                ResumeOrGoToLocation = ReadLocation()

                If Peek().Type = TokenType.Next Then
                    NextOrZeroOrMinusLocation = ReadLocation()
                Else
                    ReportSyntaxError(SyntaxErrorType.ExpectedNext, Peek())
                End If

                OnErrorType = OnErrorType.Next
            ElseIf NextToken.Type = TokenType.GoTo Then
                ResumeOrGoToLocation = ReadLocation()
                NextToken = Peek()

                If NextToken.Type = TokenType.IntegerLiteral AndAlso _
                   CType(NextToken, IntegerLiteralToken).Literal = 0 Then
                    NextOrZeroOrMinusLocation = ReadLocation()
                    OnErrorType = OnErrorType.Zero
                ElseIf NextToken.Type = TokenType.Minus Then
                    Dim NextNextToken As Token

                    NextOrZeroOrMinusLocation = ReadLocation()
                    NextNextToken = Peek()

                    If NextNextToken.Type = TokenType.IntegerLiteral AndAlso _
                       CType(NextNextToken, IntegerLiteralToken).Literal = 1 Then
                        OneLocation = ReadLocation()
                        OnErrorType = OnErrorType.MinusOne
                    Else
                        Backtrack(NextToken)
                        GoTo Label
                    End If
                Else
Label:
                    OnErrorType = OnErrorType.Label
                    ParseLabelReference(Name, IsLineNumber)

                    If ErrorInConstruct Then
                        ResyncAt()
                    End If
                End If
            Else
                ReportSyntaxError(SyntaxErrorType.ExpectedResumeOrGoto, Peek())
                OnErrorType = OnErrorType.Bad
                ResyncAt()
            End If
        Else
            ReportSyntaxError(SyntaxErrorType.ExpectedError, Peek())
            OnErrorType = OnErrorType.Bad
            ResyncAt()
        End If

        Return New OnErrorStatement(OnErrorType, ErrorLocation, ResumeOrGoToLocation, NextOrZeroOrMinusLocation, OneLocation, Name, IsLineNumber, SpanFrom(Start), ParseTrailingComments())
    End Function

    Private Function ParseResumeStatement() As Statement
        Dim Start As Token = Read()
        Dim ResumeType As ResumeType = ResumeType.None
        Dim Name As SimpleName
        Dim IsLineNumber As Boolean
        Dim NextLocation As Location

        If Not CanEndStatement(Peek().Type) Then
            If Peek().Type = TokenType.Next Then
                NextLocation = ReadLocation()
                ResumeType = ResumeType.Next
            Else
                ParseLabelReference(Name, IsLineNumber)

                If ErrorInConstruct Then
                    ResumeType = ResumeType.None
                Else
                    ResumeType = ResumeType.Label
                End If
            End If
        End If

        Return New ResumeStatement(ResumeType, NextLocation, Name, IsLineNumber, SpanFrom(Start), ParseTrailingComments())
    End Function

    Private Function ParseReDimStatement() As Statement
        Dim Start As Token = Read()
        Dim PreserveLocation As Location
        Dim Variables As ExpressionCollection

        If Peek().Type = TokenType.Preserve Then
            PreserveLocation = ReadLocation()
        End If

        Variables = ParseExpressionList(True)

        Return New ReDimStatement(PreserveLocation, Variables, SpanFrom(Start), ParseTrailingComments())
    End Function

    Private Function ParseEraseStatement() As Statement
        Dim Start As Token = Read()
        Dim Variables As ExpressionCollection = ParseExpressionList(True)

        Return New EraseStatement(Variables, SpanFrom(Start), ParseTrailingComments())
    End Function

    Private Function ParseCallStatement(Optional ByVal target As Expression = Nothing) As Statement
        Dim Start As Token = Peek()
        Dim StartLocation As Location
        Dim CallLocation As Location
        Dim Arguments As ArgumentCollection

        If target Is Nothing Then
            StartLocation = Start.Span.Start

            If Start.Type = TokenType.Call Then
                CallLocation = ReadLocation()
            End If

            target = ParseExpression()

            If ErrorInConstruct Then
                ResyncAt()
            End If
        Else
            StartLocation = target.Span.Start
        End If

        If target.Type = TreeType.CallOrIndexExpression Then
            ' Extract the operands of the call/index expression and make
            ' them operands of the call statement.
            Dim CallOrIndexExpression As CallOrIndexExpression = CType(target, CallOrIndexExpression)

            target = CallOrIndexExpression.TargetExpression
            Arguments = CallOrIndexExpression.Arguments
        End If

        Return New CallStatement(CallLocation, target, Arguments, SpanFrom(StartLocation), ParseTrailingComments())
    End Function

    Private Function ParseMidAssignmentStatement() As Statement
        Dim Start As Token = Read()
        Dim Identifier As IdentifierToken = CType(Start, IdentifierToken)
        Dim HasTypeCharacter As Boolean
        Dim LeftParenthesisLocation As Location
        Dim Target As Expression
        Dim StartCommaLocation As Location
        Dim StartExpression As Expression
        Dim LengthCommaLocation As Location
        Dim LengthExpression As Expression
        Dim RightParenthesisLocation As Location
        Dim OperatorLocation As Location
        Dim Source As Expression

        If Identifier.TypeCharacter = TypeCharacter.StringSymbol Then
            HasTypeCharacter = True
        ElseIf Identifier.TypeCharacter <> TypeCharacter.None Then
            GoTo NotMidAssignment
        End If

        If Peek().Type = TokenType.LeftParenthesis Then
            LeftParenthesisLocation = VerifyExpectedToken(TokenType.LeftParenthesis)
        Else
            GoTo NotMidAssignment
        End If

        ' This is very unfortunate: ideally, we would continue parsing to
        ' make sure the entire statement matched the form of a Mid assignment
        ' statement. That way something like "Mid(10) = 5", where Mid is an
        ' array identifier wouldn't cause an error. Alas, it's not that simple
        ' because what about something that's in error? We could fall back on
        ' error, but we have no way of backtracking on errors at the moment.
        ' So we're going to do what the official compiler does: if we see
        ' Mid and (, you've got yourself a Mid assignment statement!
        Target = ParseExpression()

        If ErrorInConstruct Then
            ResyncAt(TokenType.Comma)
        End If

        StartCommaLocation = VerifyExpectedToken(TokenType.Comma)
        StartExpression = ParseExpression()

        If ErrorInConstruct Then
            ResyncAt(TokenType.Comma, TokenType.RightParenthesis)
        End If

        If Peek().Type = TokenType.Comma Then
            LengthCommaLocation = VerifyExpectedToken(TokenType.Comma)
            LengthExpression = ParseExpression()

            If ErrorInConstruct Then
                ResyncAt(TokenType.RightParenthesis)
            End If
        End If

        RightParenthesisLocation = VerifyExpectedToken(TokenType.RightParenthesis)
        OperatorLocation = VerifyExpectedToken(TokenType.Equals)

        Source = ParseExpression()

        If ErrorInConstruct Then
            ResyncAt()
        End If

        Return New MidAssignmentStatement(HasTypeCharacter, LeftParenthesisLocation, Target, StartCommaLocation, StartExpression, LengthCommaLocation, LengthExpression, RightParenthesisLocation, OperatorLocation, Source, SpanFrom(Start), ParseTrailingComments())

NotMidAssignment:
        Backtrack(Start)
        Return Nothing
    End Function

    Private Function ParseAssignmentStatement(ByVal target As Expression) As Statement
        Dim Type As TreeType
        Dim CompoundOperator As OperatorType
        Dim Source As Expression
        Dim Operator As Token

        Operator = Read()
        Type = GetAssignmentOperator(Operator.Type, CompoundOperator)
        Source = ParseExpression()

        If ErrorInConstruct Then
            ResyncAt()
        End If

        If CompoundOperator = TreeType.SyntaxError Then
            Return New AssignmentStatement(target, Operator.Span.Start, Source, SpanFrom(target.Span.Start), ParseTrailingComments())
        Else
            Return New CompoundAssignmentStatement(CompoundOperator, target, Operator.Span.Start, Source, SpanFrom(target.Span.Start), ParseTrailingComments())
        End If
    End Function

    Private Function ParseLocalDeclarationStatement() As Statement
        Dim Start As Token = Peek()
        Dim Modifiers As ModifierCollection
        Const ValidModifiers As ModifierTypes = ModifierTypes.Dim Or _
                                                         ModifierTypes.Const Or _
                                                         ModifierTypes.Static

        Modifiers = ParseDeclarationModifierList()
        ValidateModifierList(Modifiers, ValidModifiers)

        If Modifiers Is Nothing Then
            ReportSyntaxError(SyntaxErrorType.ExpectedModifier, Peek())
        ElseIf Modifiers.Count > 1 Then
            ReportSyntaxError(SyntaxErrorType.InvalidVariableModifiers, Modifiers.Span)
        End If

        Return New LocalDeclarationStatement(Modifiers, ParseVariableDeclarators(), SpanFrom(Start), ParseTrailingComments())
    End Function

    Private Function ParseLabelStatement() As Statement
        Dim Name As SimpleName
        Dim IsLineNumber As Boolean
        Dim Start As Token = Peek()

        ParseLabelReference(Name, IsLineNumber)
        Return New LabelStatement(Name, IsLineNumber, SpanFrom(Start), ParseTrailingComments())
    End Function

    Private Function ParseExpressionBlockStatement(ByVal blockType As TreeType) As Statement
        Dim Start As Token = Read()
        Dim Expression As Expression = ParseExpression()
        Dim StatementCollection As StatementCollection
        Dim EndStatement As Statement
        Dim Comments As CommentCollection

        If ErrorInConstruct Then
            ResyncAt()
        End If

        StatementCollection = ParseStatementBlock(SpanFrom(Start), blockType, Comments, EndStatement)

        Select Case blockType
            Case TreeType.WithBlockStatement
                Return New WithBlockStatement(Expression, StatementCollection, CType(EndStatement, EndBlockStatement), SpanFrom(Start), Comments)

            Case TreeType.SyncLockBlockStatement
                Return New SyncLockBlockStatement(Expression, StatementCollection, CType(EndStatement, EndBlockStatement), SpanFrom(Start), Comments)

            Case TreeType.WhileBlockStatement
                Return New WhileBlockStatement(Expression, StatementCollection, CType(EndStatement, EndBlockStatement), SpanFrom(Start), Comments)

            Case Else
                Debug.Fail("Unexpected!")
        End Select
    End Function

    Private Function ParseOptionalWhileUntilClause(ByRef isWhile As Boolean, ByRef whileOrUntilLocation As Location) As Expression
        Dim Expression As Expression

        If Not CanEndStatement(Peek().Type) Then
            Dim Token As Token = Peek()

            If Token.Type = TokenType.While OrElse Token.Type = TokenType.Until Then
                isWhile = (Token.Type = TokenType.While)
                whileOrUntilLocation = ReadLocation()
                Expression = ParseExpression()

                If ErrorInConstruct Then
                    ResyncAt()
                End If
            Else
                ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek())
                ResyncAt()
            End If
        End If

        Return Expression
    End Function

    Private Function ParseDoBlockStatement() As Statement
        Dim Start As Token = Read()
        Dim IsWhile As Boolean
        Dim Expression As Expression
        Dim WhileOrUntilLocation As Location
        Dim StatementCollection As StatementCollection
        Dim EndStatement As Statement
        Dim LoopStatement As LoopStatement
        Dim Comments As CommentCollection

        Expression = ParseOptionalWhileUntilClause(IsWhile, WhileOrUntilLocation)

        StatementCollection = ParseStatementBlock(SpanFrom(Start), TreeType.DoBlockStatement, Comments, EndStatement)
        LoopStatement = CType(EndStatement, LoopStatement)

        If Not Expression Is Nothing AndAlso Not LoopStatement Is Nothing AndAlso Not LoopStatement.Expression Is Nothing Then
            ReportSyntaxError(SyntaxErrorType.LoopDoubleCondition, LoopStatement.Expression.Span)
        End If

        Return New DoBlockStatement(Expression, IsWhile, WhileOrUntilLocation, StatementCollection, LoopStatement, SpanFrom(Start), Comments)
    End Function

    Private Function ParseLoopStatement() As Statement
        Dim Start As Token = Read()
        Dim IsWhile As Boolean
        Dim Expression As Expression
        Dim WhileOrUntilLocation As Location

        Expression = ParseOptionalWhileUntilClause(IsWhile, WhileOrUntilLocation)
        Return New LoopStatement(Expression, IsWhile, WhileOrUntilLocation, SpanFrom(Start), ParseTrailingComments())
    End Function

    Private Function ParseForLoopControlVariable(ByRef variableDeclarator As VariableDeclarator) As Expression
        Dim Start As Token = Peek()

        If Start.Type = TokenType.Identifier Then
            Dim Identifier As Token = Read()
            Dim NextToken As Token = Peek()
            Dim Expression As Expression

            Backtrack(Identifier)

            ' CONSIDER: Should we just always parse this as a variable declaration?
            If NextToken.Type = TokenType.As Then
                variableDeclarator = ParseForLoopVariableDeclarator(Expression)
                Return Expression
            ElseIf NextToken.Type = TokenType.LeftParenthesis Then
                ' CONSIDER: Only do this if the token previous to the As is a right parenthesis
                If PeekAheadFor(TokenType.As, TokenType.In, TokenType.Equals) = TokenType.As Then
                    variableDeclarator = ParseForLoopVariableDeclarator(Expression)
                    Return Expression
                End If
            End If
        End If

        Return ParseBinaryOperatorExpression(PrecedenceLevel.Relational)
    End Function

    Private Function ParseForBlockStatement() As Statement
        Dim Start As Token = Read()

        If Peek().Type <> TokenType.Each Then
            Dim ControlExpression As Expression
            Dim LowerBoundExpression As Expression
            Dim UpperBoundExpression As Expression
            Dim StepExpression As Expression
            Dim EqualLocation As Location
            Dim ToLocation As Location
            Dim StepLocation As Location
            Dim VariableDeclarator As VariableDeclarator
            Dim Statements As StatementCollection
            Dim NextStatement As Statement
            Dim Comments As CommentCollection

            ControlExpression = ParseForLoopControlVariable(VariableDeclarator)

            If ErrorInConstruct Then
                ResyncAt(TokenType.Equals, TokenType.To)
            End If

            If Peek().Type = TokenType.Equals Then
                EqualLocation = ReadLocation()
                LowerBoundExpression = ParseExpression()

                If ErrorInConstruct Then
                    ResyncAt(TokenType.To)
                End If
            Else
                ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek())
                ResyncAt(TokenType.To)
            End If

            If Peek().Type = TokenType.To Then
                ToLocation = ReadLocation()
                UpperBoundExpression = ParseExpression()

                If ErrorInConstruct Then
                    ResyncAt(TokenType.Step)
                End If
            Else
                ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek())
                ResyncAt(TokenType.Step)
            End If

            If Peek().Type = TokenType.Step Then
                StepLocation = ReadLocation()
                StepExpression = ParseExpression()

                If ErrorInConstruct Then
                    ResyncAt()
                End If
            End If

            Statements = ParseStatementBlock(SpanFrom(Start), TreeType.ForBlockStatement, Comments, NextStatement)

            Return New ForBlockStatement(ControlExpression, VariableDeclarator, EqualLocation, LowerBoundExpression, ToLocation, UpperBoundExpression, StepLocation, StepExpression, Statements, CType(NextStatement, NextStatement), SpanFrom(Start), Comments)
        Else
            Dim EachLocation As Location
            Dim ControlExpression As Expression
            Dim InLocation As Location
            Dim VariableDeclarator As VariableDeclarator
            Dim CollectionExpression As Expression
            Dim Statements As StatementCollection
            Dim NextStatement As Statement
            Dim Comments As CommentCollection

            EachLocation = ReadLocation()
            ControlExpression = ParseForLoopControlVariable(VariableDeclarator)

            If ErrorInConstruct Then
                ResyncAt(TokenType.In)
            End If

            If Peek().Type = TokenType.In Then
                InLocation = ReadLocation()
                CollectionExpression = ParseExpression()

                If ErrorInConstruct Then
                    ResyncAt()
                End If
            Else
                ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek())
                ResyncAt()
            End If

            Statements = ParseStatementBlock(SpanFrom(Start), TreeType.ForBlockStatement, Comments, NextStatement)

            Return New ForEachBlockStatement(EachLocation, ControlExpression, VariableDeclarator, InLocation, CollectionExpression, Statements, CType(NextStatement, NextStatement), SpanFrom(Start), Comments)
        End If
    End Function

    Private Function ParseNextStatement() As Statement
        Dim Start As Token = Read()

        Return New NextStatement(ParseExpressionList(), SpanFrom(Start), ParseTrailingComments())
    End Function

    Private Function ParseTryBlockStatement() As Statement
        Dim Start As Token = Read()
        Dim TryStatementList As StatementCollection
        Dim StatementCollection As StatementCollection
        Dim EndBlockStatement As Statement
        Dim CatchBlocks As ArrayList = New ArrayList
        Dim CatchBlockList As StatementCollection
        Dim FinallyBlock As FinallyBlockStatement
        Dim Comments As CommentCollection

        TryStatementList = ParseStatementBlock(SpanFrom(Start), TreeType.TryBlockStatement, Comments, EndBlockStatement)

        While (Not EndBlockStatement Is Nothing) AndAlso (EndBlockStatement.Type <> TreeType.EndBlockStatement)
            If EndBlockStatement.Type = TreeType.CatchStatement Then
                Dim CatchStatement As CatchStatement = CType(EndBlockStatement, CatchStatement)

                StatementCollection = ParseStatementBlock(CatchStatement.Span, TreeType.CatchBlockStatement, Nothing, EndBlockStatement)
                CatchBlocks.Add(New CatchBlockStatement(CatchStatement, StatementCollection, SpanFrom(CatchStatement, EndBlockStatement), Nothing))
            Else
                Dim FinallyStatement As FinallyStatement = CType(EndBlockStatement, FinallyStatement)

                StatementCollection = ParseStatementBlock(FinallyStatement.Span, TreeType.FinallyBlockStatement, Nothing, EndBlockStatement)
                FinallyBlock = New FinallyBlockStatement(FinallyStatement, StatementCollection, SpanFrom(FinallyStatement, EndBlockStatement), Nothing)
            End If
        End While

        If CatchBlocks.Count > 0 Then
            CatchBlockList = New StatementCollection(CatchBlocks, Nothing, _
                                New Span(CType(CatchBlocks(0), CatchBlockStatement).Span.Start, _
                                CType(CatchBlocks(CatchBlocks.Count - 1), CatchBlockStatement).Span.Finish))
        End If

        Return New TryBlockStatement(TryStatementList, CatchBlockList, FinallyBlock, CType(EndBlockStatement, EndBlockStatement), SpanFrom(Start), Comments)
    End Function

    Private Function ParseCatchStatement() As Statement
        Dim Start As Token = Read()
        Dim Name As SimpleName
        Dim AsLocation As Location
        Dim Type As TypeName
        Dim WhenLocation As Location
        Dim Filter As Expression

        If Peek().Type = TokenType.Identifier Then
            Name = ParseSimpleName(False)

            If Peek().Type = TokenType.As Then
                AsLocation = ReadLocation()
                Type = ParseTypeName(False)

                If ErrorInConstruct Then
                    ResyncAt(TokenType.When)
                End If
            End If
        End If

        If Peek().Type = TokenType.When Then
            WhenLocation = ReadLocation()
            Filter = ParseExpression()
        End If

        Return New CatchStatement(Name, AsLocation, Type, WhenLocation, Filter, SpanFrom(Start), ParseTrailingComments())
    End Function

    Private Function ParseCaseStatement() As Statement
        Dim Start As Token = Read()
        Dim CommaLocations As ArrayList
        Dim Cases As ArrayList
        Dim CasesStart As Token = Peek()

        If Peek().Type = TokenType.Else Then
            Return New CaseElseStatement(ReadLocation(), SpanFrom(Start), ParseTrailingComments())
        Else
            CommaLocations = New ArrayList
            Cases = New ArrayList

            Do
                If Cases.Count > 0 Then
                    CommaLocations.Add(ReadLocation())
                End If

                Cases.Add(ParseCase())
            Loop While Peek().Type = TokenType.Comma

            Return New CaseStatement(New CaseClauseCollection(Cases, CommaLocations, SpanFrom(CasesStart)), SpanFrom(Start), ParseTrailingComments())
        End If
    End Function

    Private Function ParseSelectBlockStatement() As Statement
        Dim Start As Token = Read()
        Dim CaseLocation As Location
        Dim SelectExpression As Expression
        Dim Statements As StatementCollection
        Dim EndBlockStatement As Statement
        Dim CaseBlocks As ArrayList = New ArrayList
        Dim CaseBlockList As StatementCollection
        Dim CaseElseBlockStatement As CaseElseBlockStatement
        Dim Comments As CommentCollection

        If Peek().Type = TokenType.Case Then
            CaseLocation = ReadLocation()
        End If

        SelectExpression = ParseExpression()

        If ErrorInConstruct Then
            ResyncAt()
        End If

        Statements = ParseStatementBlock(SpanFrom(Start), TreeType.SelectBlockStatement, Comments, EndBlockStatement)

        If Not Statements Is Nothing AndAlso Statements.Count <> 0 Then
            For Each Statement As Statement In Statements
                If Statement.Type <> TreeType.EmptyStatement Then
                    ReportSyntaxError(SyntaxErrorType.ExpectedCase, Statements.Span)
                End If
            Next
        End If

        While (Not EndBlockStatement Is Nothing) AndAlso (EndBlockStatement.Type <> TreeType.EndBlockStatement)
            Dim CaseStatement As Statement = EndBlockStatement
            Dim CaseStatements As StatementCollection
            Dim BlockType As TreeType

            If CaseStatement.Type = TreeType.CaseStatement Then
                CaseStatements = ParseStatementBlock(CaseStatement.Span, TreeType.CaseBlockStatement, Nothing, EndBlockStatement)
                CaseBlocks.Add(New CaseBlockStatement(CType(CaseStatement, CaseStatement), CaseStatements, SpanFrom(CaseStatement, EndBlockStatement), Nothing))
            Else
                CaseStatements = ParseStatementBlock(CaseStatement.Span, TreeType.CaseElseBlockStatement, Nothing, EndBlockStatement)
                CaseElseBlockStatement = New CaseElseBlockStatement(CType(CaseStatement, CaseElseStatement), CaseStatements, SpanFrom(CaseStatement, EndBlockStatement), Nothing)
            End If
        End While

        If CaseBlocks.Count > 0 Then
            CaseBlockList = New StatementCollection(CaseBlocks, Nothing, _
                                New Span(CType(CaseBlocks(0), CaseBlockStatement).Span.Start, _
                                CType(CaseBlocks(CaseBlocks.Count - 1), CaseBlockStatement).Span.Finish))
        End If

        Return New SelectBlockStatement(CaseLocation, SelectExpression, Statements, CaseBlockList, CaseElseBlockStatement, CType(EndBlockStatement, EndBlockStatement), SpanFrom(Start), Comments)
    End Function

    Private Function ParseElseIfStatement() As Statement
        Dim Start As Token = Read()
        Dim ThenLocation As Location
        Dim Expression As Expression

        Expression = ParseExpression()

        If ErrorInConstruct Then
            ResyncAt(TokenType.Then)
        End If

        If Peek().Type = TokenType.Then Then
            ThenLocation = ReadLocation()
        End If

        Return New ElseIfStatement(Expression, ThenLocation, SpanFrom(Start), ParseTrailingComments())
    End Function

    Private Function ParseIfBlockStatement() As Statement
        Dim Start As Token = Read()
        Dim Expression As Expression
        Dim ThenLocation As Location
        Dim Statements As StatementCollection
        Dim IfStatements As StatementCollection
        Dim EndBlockStatement As Statement
        Dim ElseIfBlocks As ArrayList = New ArrayList
        Dim ElseIfBlockList As StatementCollection
        Dim ElseBlockStatement As ElseBlockStatement
        Dim Comments As CommentCollection

        Expression = ParseExpression()

        If ErrorInConstruct Then
            ResyncAt(TokenType.Then)
        End If

        If Peek().Type = TokenType.Then Then
            ThenLocation = ReadLocation()

            If Not CanEndStatement(Peek().Type) Then
                Dim ElseLocation As Location
                Dim ElseStatements As StatementCollection

                ' We're in a line If context
                AtBeginningOfLine = False
                IfStatements = ParseLineIfStatementBlock()

                If Peek().Type = TokenType.Else Then
                    ElseLocation = ReadLocation()
                    ElseStatements = ParseLineIfStatementBlock()
                End If

                Return New LineIfStatement(Expression, ThenLocation, IfStatements, ElseLocation, ElseStatements, SpanFrom(Start), ParseTrailingComments())
            End If
        End If

        IfStatements = ParseStatementBlock(SpanFrom(Start), TreeType.IfBlockStatement, Comments, EndBlockStatement)

        While (Not EndBlockStatement Is Nothing) AndAlso (EndBlockStatement.Type <> TreeType.EndBlockStatement)
            Dim ElseStatement As Statement = EndBlockStatement

            If ElseStatement.Type = TreeType.ElseIfStatement Then
                Statements = ParseStatementBlock(ElseStatement.Span, TreeType.ElseIfBlockStatement, Nothing, EndBlockStatement)
                ElseIfBlocks.Add(New ElseIfBlockStatement(CType(ElseStatement, ElseIfStatement), Statements, SpanFrom(ElseStatement, EndBlockStatement), Nothing))
            Else
                Statements = ParseStatementBlock(ElseStatement.Span, TreeType.ElseBlockStatement, Nothing, EndBlockStatement)
                ElseBlockStatement = New ElseBlockStatement(CType(ElseStatement, ElseStatement), Statements, SpanFrom(ElseStatement, EndBlockStatement), Nothing)
            End If
        End While

        If ElseIfBlocks.Count > 0 Then
            ElseIfBlockList = New StatementCollection(ElseIfBlocks, Nothing, _
                                New Span(CType(ElseIfBlocks(0), Statement).Span.Start, _
                                CType(ElseIfBlocks(ElseIfBlocks.Count - 1), Statement).Span.Finish))
        End If

        Return New IfBlockStatement(Expression, ThenLocation, IfStatements, ElseIfBlockList, ElseBlockStatement, CType(EndBlockStatement, EndBlockStatement), SpanFrom(Start), Comments)
    End Function

    Private Function ParseStatement(Optional ByRef terminator As Token = Nothing) As Statement
        Dim Start As Token = Peek()
        Dim Statement As Statement

        If AtBeginningOfLine Then
            While ParsePreprocessorStatement(True)
                Start = Peek()
            End While
        End If

        ErrorInConstruct = False

        Select Case Start.Type
            Case TokenType.GoTo
                Statement = ParseGotoStatement()

            Case TokenType.Exit
                Statement = ParseExitStatement()

            Case TokenType.Stop
                Statement = New StopStatement(SpanFrom(Read()), ParseTrailingComments())

            Case TokenType.End
                Statement = ParseEndStatement()

            Case TokenType.Return
                Statement = ParseExpressionStatement(TreeType.ReturnStatement, True)

            Case TokenType.RaiseEvent
                Statement = ParseRaiseEventStatement()

            Case TokenType.AddHandler, TokenType.RemoveHandler
                Statement = ParseHandlerStatement()

            Case TokenType.Error
                Statement = ParseExpressionStatement(TreeType.ErrorStatement, False)

            Case TokenType.On
                Statement = ParseOnErrorStatement()

            Case TokenType.Resume
                Statement = ParseResumeStatement()

            Case TokenType.ReDim
                Statement = ParseReDimStatement()

            Case TokenType.Erase
                Statement = ParseEraseStatement()

            Case TokenType.Call
                Statement = ParseCallStatement()

            Case TokenType.IntegerLiteral
                If AtBeginningOfLine Then
                    Statement = ParseLabelStatement()
                Else
                    GoTo SyntaxError
                End If

            Case TokenType.Identifier
                If AtBeginningOfLine Then
                    Dim IsLabel As Boolean

                    Read()
                    IsLabel = Peek().Type = TokenType.Colon
                    Backtrack(Start)

                    If IsLabel Then
                        Statement = ParseLabelStatement()
                    Else
                        GoTo NotLabel
                    End If
                Else
NotLabel:
                    If String.Compare(CType(Start, IdentifierToken).Identifier, "Mid", True, Globalization.CultureInfo.InvariantCulture) = 0 Then
                        Statement = ParseMidAssignmentStatement()
                    End If

                    If Statement Is Nothing Then
                        GoTo AssignmentOrCallStatement
                    End If
                End If

            Case TokenType.Period, TokenType.Exclamation, _
                 TokenType.Me, TokenType.MyBase, TokenType.MyClass, _
                 TokenType.Boolean, TokenType.Byte, TokenType.Short, TokenType.Integer, TokenType.Long, _
                 TokenType.Decimal, TokenType.Single, TokenType.Double, TokenType.Date, TokenType.Char, _
                 TokenType.String, TokenType.Object, _
                 TokenType.DirectCast, TokenType.CType, _
                 TokenType.CBool, TokenType.CByte, TokenType.CShort, TokenType.CInt, TokenType.CLng, _
                 TokenType.CDec, TokenType.CSng, TokenType.CDbl, TokenType.CDate, TokenType.CChar, _
                 TokenType.CStr, TokenType.CObj, _
                 TokenType.GetType
                Dim Target As Expression

AssignmentOrCallStatement:
                Target = ParseBinaryOperatorExpression(PrecedenceLevel.Power)

                If ErrorInConstruct Then
                    ResyncAt(TokenType.Equals)
                End If

                ' Could be a function call or it could be an assignment
                If GetAssignmentOperator(Peek().Type) <> TreeType.SyntaxError Then
                    Statement = ParseAssignmentStatement(Target)
                Else
                    Statement = ParseCallStatement(Target)
                End If

            Case TokenType.Public, TokenType.Private, TokenType.Protected, TokenType.Friend, _
                 TokenType.Static, TokenType.Shared, TokenType.Shadows, TokenType.Overloads, _
                 TokenType.MustInherit, TokenType.NotInheritable, TokenType.Overrides, TokenType.NotOverridable, _
                 TokenType.Overridable, TokenType.MustOverride, TokenType.ReadOnly, TokenType.WriteOnly, _
                 TokenType.Dim, TokenType.Const, TokenType.Default, TokenType.WithEvents
                Statement = ParseLocalDeclarationStatement()

            Case TokenType.With
                Statement = ParseExpressionBlockStatement(TreeType.WithBlockStatement)

            Case TokenType.SyncLock
                Statement = ParseExpressionBlockStatement(TreeType.SyncLockBlockStatement)

            Case TokenType.While
                Statement = ParseExpressionBlockStatement(TreeType.WhileBlockStatement)

            Case TokenType.Do
                Statement = ParseDoBlockStatement()

            Case TokenType.Loop
                Statement = ParseLoopStatement()

            Case TokenType.For
                Statement = ParseForBlockStatement()

            Case TokenType.Next
                Statement = ParseNextStatement()

            Case TokenType.Throw
                Statement = ParseExpressionStatement(TreeType.ThrowStatement, True)

            Case TokenType.Try
                Statement = ParseTryBlockStatement()

            Case TokenType.Catch
                Statement = ParseCatchStatement()

            Case TokenType.Finally
                Statement = New FinallyStatement(SpanFrom(Read()), ParseTrailingComments())

            Case TokenType.Select
                Statement = ParseSelectBlockStatement()

            Case TokenType.Case
                Statement = ParseCaseStatement()

            Case TokenType.If
                Statement = ParseIfBlockStatement()

            Case TokenType.Else
                Statement = New ElseStatement(SpanFrom(Read()), ParseTrailingComments())

            Case TokenType.ElseIf
                Statement = ParseElseIfStatement()

            Case TokenType.LineTerminator, TokenType.Colon
                ' An empty statement

            Case TokenType.Comment
                Dim Comments As ArrayList = New ArrayList
                Dim LastTerminator As Token

                Do
                    Dim CommentToken As CommentToken = CType(Scanner.Read(), CommentToken)
                    Comments.Add(New Comment(CommentToken.Comment, CommentToken.IsREM, CommentToken.Span))
                    LastTerminator = Read() ' Eat the terminator of the comment
                Loop While Peek().Type = TokenType.Comment
                Backtrack(LastTerminator)

                Statement = New EmptyStatement(SpanFrom(Start), New CommentCollection(Comments))

            Case Else
SyntaxError:
                ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek())
        End Select

        terminator = VerifyEndOfStatement()

        Return Statement
    End Function

    Private Function ParseStatementBlock(ByVal blockStartSpan As Span, ByVal blockType As TreeType, ByRef Comments As CommentCollection, Optional ByRef endStatement As Statement = Nothing) As StatementCollection
        Dim Statements As ArrayList = New ArrayList
        Dim ColonLocations As ArrayList = New ArrayList
        Dim Terminator As Token
        Dim Start As Token
        Dim StatementsEnd As Location
        Dim BlockTerminated As Boolean = False

        Debug.Assert(blockType <> TreeType.LineIfBlockStatement)
        Comments = ParseTrailingComments()
        Terminator = VerifyEndOfStatement()

        If Terminator.Type = TokenType.Colon Then
            If blockType = TreeType.SubDeclaration OrElse _
               blockType = TreeType.FunctionDeclaration OrElse _
               blockType = TreeType.ConstructorDeclaration OrElse _
               blockType = TreeType.GetDeclaration OrElse _
               blockType = TreeType.SetDeclaration Then
                ReportSyntaxError(SyntaxErrorType.MethodBodyNotAtLineStart, Terminator.Span)
            End If

            ColonLocations.Add(Terminator.Span.Start)
        End If

        Start = Peek()
        StatementsEnd = Start.Span.Finish
        endStatement = Nothing

        PushBlockContext(blockType)

        While Peek().Type <> TokenType.EndOfStream
            Dim PreviousTerminator As Token = Terminator
            Dim Statement As Statement

            Statement = ParseStatement(Terminator)

            If Not Statement Is Nothing Then
                If Statement.Type >= TreeType.LoopStatement AndAlso Statement.Type <= TreeType.EndBlockStatement Then
                    If StatementEndsBlock(blockType, Statement) Then
                        endStatement = Statement
                        Backtrack(Terminator)
                        BlockTerminated = True
                        Exit While
                    Else
                        Dim StatementEndsOuterBlock As Boolean = False

                        ' If the end statement matches an outer block context, then we want to unwind
                        ' up to that level. Otherwise, we want to just give an error and keep going.
                        For Each BlockContext As TreeType In BlockContextStack
                            If StatementEndsBlock(BlockContext, Statement) Then
                                StatementEndsOuterBlock = True
                                Exit For
                            End If
                        Next

                        If StatementEndsOuterBlock Then
                            ReportMismatchedEndError(blockType, Statement.Span)
                            ' CONSIDER: Can we avoid parsing and re-parsing this statement?
                            Backtrack(PreviousTerminator)
                            ' We consider the block terminated.
                            BlockTerminated = True
                            Exit While
                        Else
                            ReportMissingBeginStatementError(blockType, Statement)
                        End If
                    End If
                End If

                Statements.Add(Statement)
            End If

            If Terminator.Type = TokenType.Colon Then
                ColonLocations.Add(Terminator.Span.Start)
                StatementsEnd = Terminator.Span.Finish
            Else
                StatementsEnd = Terminator.Span.Finish
            End If
        End While

        If Not BlockTerminated Then
            ReportMismatchedEndError(blockType, blockStartSpan)
        End If

        PopBlockContext()

        If Statements.Count = 0 AndAlso ColonLocations.Count = 0 Then
            Return Nothing
        Else
            Return New StatementCollection(Statements, ColonLocations, New Span(Start.Span.Start, StatementsEnd))
        End If
    End Function

    Private Function ParseLineIfStatementBlock() As StatementCollection
        Dim Statements As ArrayList = New ArrayList
        Dim ColonLocations As ArrayList = New ArrayList
        Dim Terminator As Token
        Dim Start As Token
        Dim StatementsEnd As Location

        Start = Peek()
        StatementsEnd = Start.Span.Finish

        PushBlockContext(TreeType.LineIfBlockStatement)

        While Not CanEndStatement(Peek().Type)
            Dim PreviousTerminator As Token = Terminator
            Dim Statement As Statement

            Statement = ParseStatement(Terminator)

            If Not Statement Is Nothing Then
                If Statement.Type >= TreeType.LoopStatement AndAlso Statement.Type <= TreeType.EndBlockStatement Then
                    ReportSyntaxError(SyntaxErrorType.EndInLineIf, Statement.Span)
                End If

                Statements.Add(Statement)
            End If

            If Terminator.Type = TokenType.Colon Then
                ColonLocations.Add(Terminator.Span.Start)
                StatementsEnd = Terminator.Span.Finish
            Else
                Backtrack(Terminator)
                Exit While
            End If
        End While

        PopBlockContext()

        If Statements.Count = 0 AndAlso ColonLocations.Count = 0 Then
            Return Nothing
        Else
            Return New StatementCollection(Statements, ColonLocations, New Span(Start.Span.Start, StatementsEnd))
        End If
    End Function

    '*
    '* Modifiers
    '*

    Private Sub ValidateModifierList(ByVal modifiers As ModifierCollection, ByVal validTypes As ModifierTypes)
        If modifiers Is Nothing Then
            Return
        End If

        For Each Modifier As Modifier In modifiers
            If (validTypes And Modifier.ModifierType) = 0 Then
                ReportSyntaxError(SyntaxErrorType.InvalidModifier, Modifier.Span)
            End If
        Next
    End Sub

    Private Function ParseDeclarationModifierList() As ModifierCollection
        Dim Modifiers As ArrayList = New ArrayList
        Dim Start As Token = Peek()
        Dim ModifierTypes As ModifierTypes
        Dim FoundTypes As ModifierTypes

        While True
            Select Case Peek().Type
                Case TokenType.Public
                    ModifierTypes = ModifierTypes.Public

                Case TokenType.Private
                    ModifierTypes = ModifierTypes.Private

                Case TokenType.Protected
                    ModifierTypes = ModifierTypes.Protected

                Case TokenType.Friend
                    ModifierTypes = ModifierTypes.Friend

                Case TokenType.Static
                    ModifierTypes = ModifierTypes.Static

                Case TokenType.Shared
                    ModifierTypes = ModifierTypes.Shared

                Case TokenType.Shadows
                    ModifierTypes = ModifierTypes.Shadows

                Case TokenType.Overloads
                    ModifierTypes = ModifierTypes.Overloads

                Case TokenType.MustInherit
                    ModifierTypes = ModifierTypes.MustInherit

                Case TokenType.NotInheritable
                    ModifierTypes = ModifierTypes.NotInheritable

                Case TokenType.Overrides
                    ModifierTypes = ModifierTypes.Overrides

                Case TokenType.Overridable
                    ModifierTypes = ModifierTypes.Overridable

                Case TokenType.NotOverridable
                    ModifierTypes = ModifierTypes.NotOverridable

                Case TokenType.MustOverride
                    ModifierTypes = ModifierTypes.MustOverride

                Case TokenType.ReadOnly
                    ModifierTypes = ModifierTypes.ReadOnly

                Case TokenType.WriteOnly
                    ModifierTypes = ModifierTypes.WriteOnly

                Case TokenType.Dim
                    ModifierTypes = ModifierTypes.Dim

                Case TokenType.Const
                    ModifierTypes = ModifierTypes.Const

                Case TokenType.Default
                    ModifierTypes = ModifierTypes.Default

                Case TokenType.WithEvents
                    ModifierTypes = ModifierTypes.WithEvents

                Case Else
                    Exit While
            End Select

            If (FoundTypes And ModifierTypes) <> 0 Then
                ReportSyntaxError(SyntaxErrorType.DuplicateModifier, Peek())
            Else
                FoundTypes = FoundTypes Or ModifierTypes
            End If

            Modifiers.Add(New Modifier(ModifierTypes, SpanFrom(Read())))
        End While

        If Modifiers.Count = 0 Then
            Return Nothing
        Else
            Return New ModifierCollection(Modifiers, SpanFrom(Start))
        End If
    End Function

    Private Function ParseParameterModifierList() As ModifierCollection
        Dim Modifiers As ArrayList = New ArrayList
        Dim Start As Token = Peek()
        Dim ModifierTypes As ModifierTypes
        Dim FoundTypes As ModifierTypes

        While True
            Select Case Peek().Type
                Case TokenType.ByVal
                    ModifierTypes = ModifierTypes.ByVal

                Case TokenType.ByRef
                    ModifierTypes = ModifierTypes.ByRef

                Case TokenType.Optional
                    ModifierTypes = ModifierTypes.Optional

                Case TokenType.ParamArray
                    ModifierTypes = ModifierTypes.ParamArray

                Case Else
                    Exit While
            End Select

            If (FoundTypes And ModifierTypes) <> 0 Then
                ReportSyntaxError(SyntaxErrorType.DuplicateModifier, Peek())
            Else
                FoundTypes = FoundTypes Or ModifierTypes
            End If

            Modifiers.Add(New Modifier(ModifierTypes, SpanFrom(Read())))
        End While

        If Modifiers.Count = 0 Then
            Return Nothing
        Else
            Return New ModifierCollection(Modifiers, SpanFrom(Start))
        End If
    End Function

    '*
    '* VariableDeclarators
    '*

    Private Function ParseVariableDeclarator() As VariableDeclarator
        Dim DeclarationStart As Token = Peek()
        Dim VariableNamesCommaLocations As ArrayList = New ArrayList
        Dim VariableNames As ArrayList = New ArrayList
        Dim AsLocation As Location
        Dim NewLocation As Location
        Dim Type As TypeName
        Dim NewArguments As ArgumentCollection
        Dim EqualsLocation As Location
        Dim Initializer As Initializer
        Dim VariableNameCollection As VariableNameCollection

        ' Parse the declarators
        Do
            Dim VariableName As VariableName

            If VariableNames.Count > 0 Then
                VariableNamesCommaLocations.Add(ReadLocation())
            End If

            VariableName = ParseVariableName(True)

            If ErrorInConstruct Then
                ResyncAt(TokenType.As, TokenType.Comma, TokenType.[New], TokenType.Equals)
            End If

            VariableNames.Add(VariableName)
        Loop While Peek().Type = TokenType.Comma

        VariableNameCollection = New VariableNameCollection(VariableNames, VariableNamesCommaLocations, SpanFrom(DeclarationStart))

        If Peek().Type = TokenType.As Then
            AsLocation = ReadLocation()

            If Peek().Type = TokenType.[New] Then
                NewLocation = ReadLocation()
                Type = ParseTypeName(False)
                NewArguments = ParseArguments()
            Else
                Type = ParseTypeName(True)

                If ErrorInConstruct Then
                    ResyncAt(TokenType.Comma, TokenType.Equals)
                End If
            End If
        End If

        If Peek.Type = TokenType.Equals AndAlso Not NewLocation.IsValid Then
            EqualsLocation = ReadLocation()
            Initializer = ParseInitializer()

            If ErrorInConstruct Then
                ResyncAt(TokenType.Comma)
            End If
        End If

        Return New VariableDeclarator(VariableNameCollection, AsLocation, NewLocation, Type, NewArguments, EqualsLocation, Initializer, SpanFrom(DeclarationStart))
    End Function

    Private Function ParseVariableDeclarators() As VariableDeclaratorCollection
        Dim Start As Token = Peek()
        Dim VariableDeclarators As ArrayList = New ArrayList
        Dim DeclarationsCommaLocations As ArrayList = New ArrayList

        ' Parse the declarations
        Do
            If VariableDeclarators.Count > 0 Then
                DeclarationsCommaLocations.Add(ReadLocation())
            End If

            VariableDeclarators.Add(ParseVariableDeclarator())
        Loop While Peek().Type = TokenType.Comma

        Return New VariableDeclaratorCollection(VariableDeclarators, DeclarationsCommaLocations, SpanFrom(Start))
    End Function

    Private Function ParseForLoopVariableDeclarator(ByRef controlExpression As Expression) As VariableDeclarator
        Dim Start As Token = Peek()
        Dim AsLocation As Location
        Dim Type As TypeName
        Dim VariableName As VariableName
        Dim VariableNames As ArrayList = New ArrayList
        Dim VariableNameCollection As VariableNameCollection

        VariableName = ParseVariableName(False)
        VariableNames.Add(VariableName)
        VariableNameCollection = New VariableNameCollection(VariableNames, Nothing, SpanFrom(Start))

        If ErrorInConstruct Then
            ' If we see As before a In or Each, then assume that we are still on the Control Variable Declaration. 
            ' Otherwise, don't resync and allow the caller to decide how to recover.
            If PeekAheadFor(TokenType.As, TokenType.In, TokenType.Equals) = TokenType.As Then
                ResyncAt(TokenType.As)
            End If
        End If

        If Peek().Type = TokenType.As Then
            AsLocation = ReadLocation()
            Type = ParseTypeName(True)
        End If

        controlExpression = New SimpleNameExpression(VariableName.Name, VariableName.Span)

        Return New VariableDeclarator(VariableNameCollection, AsLocation, Nothing, Type, Nothing, Nothing, Nothing, SpanFrom(Start))
    End Function

    '*
    '* CaseClauses
    '*

    Private Function ParseCase() As CaseClause
        Dim Start As Token = Peek()

        If Start.Type = TokenType.Is OrElse IsRelationalOperator(Start.Type) Then
            Dim IsLocation As Location
            Dim OperatorToken As Token
            Dim Operator As OperatorType = OperatorType.None
            Dim Operand As Expression

            If Start.Type = TokenType.Is Then
                IsLocation = ReadLocation()
            End If

            If IsRelationalOperator(Peek().Type) Then
                OperatorToken = Read()
                Operator = GetBinaryOperator(OperatorToken.Type)
                Operand = ParseExpression()

                If ErrorInConstruct Then
                    ResyncAt()
                End If

                Return New ComparisonCaseClause(IsLocation, Operator, OperatorToken.Span.Start, Operand, SpanFrom(Start))
            Else
                ReportSyntaxError(SyntaxErrorType.ExpectedRelationalOperator, Peek())
                ResyncAt()
            End If
        Else
            Dim Lower As Expression
            Dim Upper As Expression
            Dim ToLocation As Location

            Lower = ParseExpression()

            If ErrorInConstruct Then
                ResyncAt(TokenType.To)
            End If

            If Peek().Type = TokenType.To Then
                ToLocation = ReadLocation()
                Upper = ParseExpression()

                If ErrorInConstruct Then
                    ResyncAt()
                End If
            End If

            Return New RangeCaseClause(Lower, ToLocation, Upper, SpanFrom(Start))
        End If
    End Function

    '*
    '* Attributes
    '*

    Private Function ParseAttributes(Optional ByVal attributeTypesAllowed As AttributeTypes = AttributeTypes.Regular) As AttributeCollection
        Dim Start As Token = Peek()
        Dim Attributes As ArrayList = New ArrayList
        Dim RightBracketLocation As Location
        Dim CommaLocations As ArrayList = New ArrayList

        If Start.Type <> TokenType.LessThan Then
            Return Nothing
        End If

        Read()

        Do
            Dim AttributeStart As Token
            Dim AttributeTypes As AttributeTypes = AttributeTypes.Regular
            Dim AttributeTypeLocation As Location = New Location
            Dim ColonLocation As Location = New Location
            Dim Name As Name
            Dim Arguments As ArgumentCollection

            If Attributes.Count > 0 Then
                CommaLocations.Add(ReadLocation())
            End If

            AttributeStart = Peek()

            If AttributeStart.Type = TokenType.Assembly Then
                AttributeTypes = AttributeTypes.Assembly
                AttributeTypeLocation = ReadLocation()
                ColonLocation = VerifyExpectedToken(TokenType.Colon)
            ElseIf AttributeStart.Type = TokenType.Module Then
                AttributeTypes = AttributeTypes.Module
                AttributeTypeLocation = ReadLocation()
                ColonLocation = VerifyExpectedToken(TokenType.Colon)
            End If

            If (AttributeTypes And attributeTypesAllowed) = 0 Then
                ReportSyntaxError(SyntaxErrorType.IncorrectAttributeType, AttributeStart)
            End If

            Name = ParseName()
            Arguments = ParseArguments()

            Attributes.Add(New Attribute(AttributeTypes, AttributeTypeLocation, ColonLocation, Name, Arguments, SpanFrom(AttributeStart)))
        Loop While Peek().Type = TokenType.Comma

        RightBracketLocation = VerifyExpectedToken(TokenType.GreaterThan)

        Return New AttributeCollection(Attributes, CommaLocations, RightBracketLocation, SpanFrom(Start))
    End Function

    '*
    '* Declaration statements
    '*

    Private Function ParseNameList(Optional ByVal allowLeadingMyBase As Boolean = False) As NameCollection
        Dim Start As Token = Read()
        Dim CommaLocations As ArrayList = New ArrayList
        Dim Names As ArrayList = New ArrayList

        Do
            If Names.Count > 0 Then
                CommaLocations.Add(ReadLocation())
            End If

            Names.Add(ParseNameListName(allowLeadingMyBase))

            If ErrorInConstruct Then
                ResyncAt(TokenType.Comma)
            End If
        Loop While Peek().Type = TokenType.Comma

        Return New NameCollection(Names, CommaLocations, SpanFrom(Start))
    End Function

    Private Function ParsePropertyDeclaration(ByVal startLocation As Location, ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection) As Declaration
        Const RegularValidModifiers As ModifierTypes = _
            ModifierTypes.AccessModifiers Or _
            ModifierTypes.Shadows Or ModifierTypes.Shared Or _
            ModifierTypes.Overridable Or ModifierTypes.NotOverridable Or _
            ModifierTypes.MustOverride Or ModifierTypes.Overrides Or _
            ModifierTypes.Overloads Or ModifierTypes.Default Or _
            ModifierTypes.ReadOnly Or ModifierTypes.WriteOnly
        Const InterfaceValidModifiers As ModifierTypes = _
            ModifierTypes.Shadows Or _
            ModifierTypes.Overloads Or _
            ModifierTypes.Default Or _
            ModifierTypes.ReadOnly Or _
            ModifierTypes.WriteOnly

        Dim ValidModifiers As ModifierTypes
        Dim PropertyLocation As Location
        Dim Name As SimpleName
        Dim Parameters As ParameterCollection
        Dim AsLocation As Location
        Dim ReturnType As TypeName
        Dim ReturnTypeAttributes As AttributeCollection
        Dim ImplementsList As NameCollection
        Dim Accessors As DeclarationCollection
        Dim EndBlockDeclaration As EndBlockDeclaration
        Dim InInterface As Boolean = CurrentBlockContextType() = TreeType.InterfaceDeclaration
        Dim Comments As CommentCollection

        If InInterface Then
            ValidModifiers = InterfaceValidModifiers
        Else
            ValidModifiers = RegularValidModifiers
        End If

        ValidateModifierList(modifiers, ValidModifiers)
        PropertyLocation = ReadLocation()
        Name = ParseSimpleName(False)

        If ErrorInConstruct Then
            ResyncAt(TokenType.LeftParenthesis, TokenType.As)
        End If

        Parameters = ParseParameters()

        If Peek().Type = TokenType.As Then
            AsLocation = ReadLocation()
            ReturnTypeAttributes = ParseAttributes()
            ReturnType = ParseTypeName(True)

            If ErrorInConstruct Then
                ResyncAt(TokenType.Implements)
            End If
        End If

        If InInterface Then
            Comments = ParseTrailingComments()
        Else
            If Peek().Type = TokenType.Implements Then
                ImplementsList = ParseNameList()
            End If

            If modifiers Is Nothing OrElse (modifiers.ModifierTypes And ModifierTypes.MustOverride) = 0 Then
                Accessors = ParseDeclarationBlock(SpanFrom(startLocation), TreeType.PropertyDeclaration, Comments, EndBlockDeclaration)
            Else
                Comments = ParseTrailingComments()
            End If
        End If

        Return New PropertyDeclaration(attributes, modifiers, PropertyLocation, Name, Parameters, AsLocation, _
            ReturnTypeAttributes, ReturnType, ImplementsList, Accessors, EndBlockDeclaration, SpanFrom(startLocation), _
            Comments)
    End Function

    Private Function ParseExternalDeclaration(ByVal startLocation As Location, ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection) As Declaration
        Const ValidModifiers As ModifierTypes = ModifierTypes.AccessModifiers Or ModifierTypes.Shadows Or ModifierTypes.Overloads
        Dim DeclareLocation As Location
        Dim CharsetLocation As Location
        Dim Charset As Charset = Charset.Auto
        Dim MethodType As TreeType = TreeType.SyntaxError
        Dim SubOrFunctionLocation As Location
        Dim Name As SimpleName
        Dim LibLocation As Location
        Dim LibLiteral As StringLiteralExpression
        Dim AliasLocation As Location
        Dim AliasLiteral As StringLiteralExpression
        Dim Parameters As ParameterCollection
        Dim AsLocation As Location
        Dim ReturnType As TypeName
        Dim ReturnTypeAttributes As AttributeCollection

        ValidateModifierList(modifiers, ValidModifiers)

        DeclareLocation = ReadLocation()

        Select Case Peek().Type
            Case TokenType.Ansi
                Charset = Charset.Ansi
                CharsetLocation = ReadLocation()

            Case TokenType.Unicode
                Charset = Charset.Unicode
                CharsetLocation = ReadLocation()

            Case TokenType.Auto
                Charset = Charset.Auto
                CharsetLocation = ReadLocation()
        End Select

        If Peek().Type = TokenType.Sub Then
            MethodType = TreeType.ExternalSubDeclaration
            SubOrFunctionLocation = ReadLocation()
        ElseIf Peek().Type = TokenType.Function Then
            MethodType = TreeType.ExternalFunctionDeclaration
            SubOrFunctionLocation = ReadLocation()
        Else
            ReportSyntaxError(SyntaxErrorType.ExpectedSubOrFunction, Peek())
        End If

        Name = ParseSimpleName(False)

        If ErrorInConstruct Then
            ResyncAt(TokenType.Lib, TokenType.LeftParenthesis)
        End If

        If Peek().Type = TokenType.Lib Then
            LibLocation = ReadLocation()

            If Peek().Type = TokenType.StringLiteral Then
                Dim Literal As StringLiteralToken = CType(Read(), StringLiteralToken)
                LibLiteral = New StringLiteralExpression(Literal.Literal, Literal.Span)
            Else
                ReportSyntaxError(SyntaxErrorType.ExpectedStringLiteral, Peek())
                ResyncAt(TokenType.Alias, TokenType.LeftParenthesis)
            End If
        Else

            ReportSyntaxError(SyntaxErrorType.ExpectedLib, Peek())
        End If

        If Peek().Type = TokenType.Alias Then
            AliasLocation = ReadLocation()

            If Peek().Type = TokenType.StringLiteral Then
                Dim Literal As StringLiteralToken = CType(Read(), StringLiteralToken)
                AliasLiteral = New StringLiteralExpression(literal.Literal, literal.Span)
            Else
                ReportSyntaxError(SyntaxErrorType.ExpectedStringLiteral, Peek())
                ResyncAt(TokenType.LeftParenthesis)
            End If
        End If

        Parameters = ParseParameters()

        If MethodType = TreeType.ExternalFunctionDeclaration Then
            If Peek().Type = TokenType.As Then
                AsLocation = ReadLocation()
                ReturnTypeAttributes = ParseAttributes()
                ReturnType = ParseTypeName(True)

                If ErrorInConstruct Then
                    ResyncAt()
                End If
            End If

            Return New ExternalFunctionDeclaration(attributes, modifiers, DeclareLocation, CharsetLocation, _
                Charset, SubOrFunctionLocation, Name, LibLocation, LibLiteral, AliasLocation, AliasLiteral, _
                Parameters, AsLocation, ReturnTypeAttributes, ReturnType, SpanFrom(startLocation), ParseTrailingComments())
        ElseIf MethodType = TreeType.ExternalSubDeclaration Then
            Return New ExternalSubDeclaration(attributes, modifiers, DeclareLocation, CharsetLocation, _
                Charset, SubOrFunctionLocation, Name, LibLocation, LibLiteral, AliasLocation, AliasLiteral, _
                Parameters, SpanFrom(startLocation), ParseTrailingComments())
        Else
            Return Declaration.GetBadDeclaration(SpanFrom(startLocation), ParseTrailingComments())
        End If
    End Function

    Private Function ParseMethodDeclaration(ByVal startLocation As Location, ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection) As Declaration
        Const ValidMethodModifiers As ModifierTypes = _
            ModifierTypes.AccessModifiers Or _
            ModifierTypes.Shadows Or ModifierTypes.Shared Or _
            ModifierTypes.Overridable Or ModifierTypes.NotOverridable Or _
            ModifierTypes.MustOverride Or ModifierTypes.Overrides Or _
            ModifierTypes.Overloads
        Const ValidConstructorModifiers As ModifierTypes = _
            ModifierTypes.AccessModifiers Or ModifierTypes.Shared
        Const ValidInterfaceModifiers As ModifierTypes = _
            ModifierTypes.Shadows Or ModifierTypes.Overloads

        Dim MethodType As TreeType
        Dim SubOrFunctionLocation As Location
        Dim Name As SimpleName
        Dim Parameters As ParameterCollection
        Dim AsLocation As Location
        Dim ReturnType As TypeName
        Dim ReturnTypeAttributes As AttributeCollection
        Dim ImplementsList As NameCollection
        Dim HandlesList As NameCollection
        Dim AllowKeywordsForName As Boolean = False
        Dim ValidModifiers As ModifierTypes = ValidMethodModifiers
        Dim Statements As StatementCollection
        Dim EndStatement As Statement
        Dim EndDeclaration As EndBlockDeclaration
        Dim InInterface As Boolean = CurrentBlockContextType() = TreeType.InterfaceDeclaration
        Dim Comments As CommentCollection

        If Not AtBeginningOfLine Then
            ReportSyntaxError(SyntaxErrorType.MethodMustBeFirstStatementOnLine, Peek())
        End If

        If Peek().Type = TokenType.Sub Then
            SubOrFunctionLocation = ReadLocation()

            If Peek().Type = TokenType.[New] Then
                MethodType = TreeType.ConstructorDeclaration
                AllowKeywordsForName = True
                ValidModifiers = ValidConstructorModifiers
            Else
                MethodType = TreeType.SubDeclaration
            End If
        Else
            SubOrFunctionLocation = ReadLocation()
            MethodType = TreeType.FunctionDeclaration
        End If

        If InInterface Then
            ValidModifiers = ValidInterfaceModifiers
        End If

        ValidateModifierList(modifiers, ValidModifiers)
        Name = ParseSimpleName(AllowKeywordsForName)

        If ErrorInConstruct Then
            ResyncAt(TokenType.LeftParenthesis, TokenType.As)
        End If

        Parameters = ParseParameters()

        If MethodType = TreeType.FunctionDeclaration AndAlso Peek().Type = TokenType.As Then
            AsLocation = ReadLocation()
            ReturnTypeAttributes = ParseAttributes()
            ReturnType = ParseTypeName(True)

            If ErrorInConstruct Then
                ResyncAt(TokenType.Implements, TokenType.Handles)
            End If
        End If

        If InInterface Then
            Comments = ParseTrailingComments()
        Else
            If Peek().Type = TokenType.Implements Then
                ImplementsList = ParseNameList()
            ElseIf Peek().Type = TokenType.Handles Then
                HandlesList = ParseNameList(True)
            End If

            If modifiers Is Nothing OrElse (modifiers.ModifierTypes And ModifierTypes.MustOverride) = 0 Then
                Statements = ParseStatementBlock(SpanFrom(startLocation), MethodType, Comments, EndStatement)
            Else
                Comments = ParseTrailingComments()
            End If

            If Not EndStatement Is Nothing Then
                EndDeclaration = New EndBlockDeclaration(CType(EndStatement, EndBlockStatement))
            End If
        End If

        If MethodType = TreeType.SubDeclaration Then
            Return New SubDeclaration(attributes, modifiers, SubOrFunctionLocation, Name, Parameters, ImplementsList, _
                HandlesList, Statements, EndDeclaration, SpanFrom(startLocation), Comments)
        ElseIf MethodType = TreeType.FunctionDeclaration Then
            Return New FunctionDeclaration(attributes, modifiers, SubOrFunctionLocation, Name, _
                Parameters, AsLocation, ReturnTypeAttributes, ReturnType, ImplementsList, HandlesList, Statements, _
                EndDeclaration, SpanFrom(startLocation), Comments)
        Else
            Return New ConstructorDeclaration(attributes, modifiers, SubOrFunctionLocation, Name, _
                Parameters, Statements, EndDeclaration, SpanFrom(startLocation), Comments)
        End If
    End Function

    Private Function ParseAccessorDeclaration(ByVal startLocation As Location, ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection) As Declaration
        Dim AccessorType As TreeType
        Dim GetOrSetLocation As Location
        Dim Parameters As ParameterCollection
        Dim Statements As StatementCollection
        Dim EndStatement As Statement
        Dim EndDeclaration As EndBlockDeclaration
        Dim Comments As CommentCollection

        If Not AtBeginningOfLine Then
            ReportSyntaxError(SyntaxErrorType.MethodMustBeFirstStatementOnLine, Peek())
        End If

        ValidateModifierList(modifiers, ModifierTypes.None)

        If Peek().Type = TokenType.Get Then
            AccessorType = TreeType.GetDeclaration
        Else
            AccessorType = TreeType.SetDeclaration
        End If
        GetOrSetLocation = ReadLocation()

        If AccessorType = TreeType.SetDeclaration Then
            Parameters = ParseParameters()
        End If

        Statements = ParseStatementBlock(SpanFrom(startLocation), AccessorType, Comments, EndStatement)

        If Not EndStatement Is Nothing Then
            EndDeclaration = New EndBlockDeclaration(CType(EndStatement, EndBlockStatement))
        End If

        If AccessorType = TreeType.GetDeclaration Then
            Return New GetAccessorDeclaration(attributes, modifiers, GetOrSetLocation, Statements, _
                EndDeclaration, SpanFrom(startLocation), Comments)
        Else
            Return New SetAccessorDeclaration(attributes, modifiers, GetOrSetLocation, Parameters, Statements, _
                EndDeclaration, SpanFrom(startLocation), Comments)
        End If
    End Function

    Private Function ParseEventDeclaration(ByVal startLocation As Location, ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection) As Declaration
        Const RegularValidModifiers As ModifierTypes = _
            ModifierTypes.AccessModifiers Or ModifierTypes.Shadows Or ModifierTypes.Shared
        Const InterfaceValidModifiers As ModifierTypes = ModifierTypes.Shadows

        Dim EventLocation As Location
        Dim Name As SimpleName
        Dim AsLocation As Location
        Dim EventType As TypeName
        Dim Parameters As ParameterCollection
        Dim ImplementsList As NameCollection
        Dim InInterface As Boolean = CurrentBlockContextType() = TreeType.InterfaceDeclaration
        Dim ValidModifiers As ModifierTypes

        If InInterface Then
            ValidModifiers = InterfaceValidModifiers
        Else
            ValidModifiers = RegularValidModifiers
        End If

        ValidateModifierList(modifiers, ValidModifiers)

        EventLocation = ReadLocation()
        Name = ParseSimpleName(False)

        If Peek().Type = TokenType.As Then
            AsLocation = ReadLocation()
            EventType = ParseTypeName(False)

            If ErrorInConstruct Then
                ResyncAt()
            End If
        Else
            Parameters = ParseParameters()

            ' Give a good error if they attempt to do a return type
            If Peek().Type = TokenType.As Then
                Dim ErrorStart As Token = Peek()

                ResyncAt(TokenType.Implements)
                ReportSyntaxError(SyntaxErrorType.EventsCantBeFunctions, ErrorStart, Peek())
            End If
        End If

        If Peek().Type = TokenType.Implements Then
            ImplementsList = ParseNameList()
        End If

        Return New EventDeclaration(attributes, modifiers, EventLocation, Name, Parameters, AsLocation, _
            Nothing, EventType, ImplementsList, SpanFrom(startLocation), ParseTrailingComments())
    End Function

    Private Function ParseVariableListDeclaration(ByVal startLocation As Location, ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection) As Declaration
        Dim ValidModifiers As ModifierTypes

        If Not modifiers Is Nothing AndAlso (modifiers.ModifierTypes And ModifierTypes.Const) <> 0 Then
            ValidModifiers = ModifierTypes.Const Or ModifierTypes.AccessModifiers Or _
                             ModifierTypes.Shadows
        Else
            ValidModifiers = ModifierTypes.Dim Or ModifierTypes.AccessModifiers Or _
                             ModifierTypes.Shadows Or ModifierTypes.Shared Or _
                             ModifierTypes.ReadOnly Or ModifierTypes.WithEvents
        End If

        ValidateModifierList(modifiers, ValidModifiers)

        If modifiers Is Nothing Then
            ReportSyntaxError(SyntaxErrorType.ExpectedModifier, Peek())
        End If

        Return New VariableListDeclaration(attributes, modifiers, ParseVariableDeclarators(), SpanFrom(startLocation), ParseTrailingComments())
    End Function

    Private Function ParseEndDeclaration() As Declaration
        Dim Start As Token = Read()
        Dim EndType As BlockType = GetBlockType(Peek().Type)

        Select Case EndType
            Case BlockType.Sub
                If Not AtBeginningOfLine Then
                    ReportSyntaxError(SyntaxErrorType.EndSubNotAtLineStart, SpanFrom(Start))
                End If

            Case BlockType.Function
                If Not AtBeginningOfLine Then
                    ReportSyntaxError(SyntaxErrorType.EndFunctionNotAtLineStart, SpanFrom(Start))
                End If

            Case BlockType.Get
                If Not AtBeginningOfLine Then
                    ReportSyntaxError(SyntaxErrorType.EndGetNotAtLineStart, SpanFrom(Start))
                End If

            Case BlockType.Set
                If Not AtBeginningOfLine Then
                    ReportSyntaxError(SyntaxErrorType.EndSetNotAtLineStart, SpanFrom(Start))
                End If

            Case BlockType.None
                ReportSyntaxError(SyntaxErrorType.UnrecognizedEnd, Peek())
                Return Declaration.GetBadDeclaration(SpanFrom(Start), ParseTrailingComments())
        End Select

        Return New EndBlockDeclaration(EndType, ReadLocation(), SpanFrom(Start), ParseTrailingComments())
    End Function

    Private Function ParseTypeDeclaration(ByVal startLocation As Location, ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection, ByVal blockType As TreeType) As Declaration
        Dim ValidModifiers As ModifierTypes
        Dim KeywordLocation As Location
        Dim Name As SimpleName
        Dim Members As DeclarationCollection
        Dim EndBlockDeclaration As EndBlockDeclaration
        Dim Comments As CommentCollection

        If blockType = TreeType.ModuleDeclaration Then
            ValidModifiers = ModifierTypes.AccessModifiers
        Else
            ValidModifiers = ModifierTypes.AccessModifiers Or ModifierTypes.Shadows

            If blockType = TreeType.ClassDeclaration Then
                ValidModifiers = ValidModifiers Or ModifierTypes.MustInherit Or ModifierTypes.NotInheritable
            End If
        End If

        ValidateModifierList(modifiers, ValidModifiers)

        KeywordLocation = ReadLocation()

        Name = ParseSimpleName(False)

        If ErrorInConstruct Then
            ResyncAt()
        End If

        Members = ParseDeclarationBlock(SpanFrom(startLocation), blockType, Comments, EndBlockDeclaration)

        Select Case blockType
            Case TreeType.ClassDeclaration
                Return New ClassDeclaration(attributes, modifiers, KeywordLocation, Name, Members, EndBlockDeclaration, SpanFrom(startLocation), Comments)

            Case TreeType.ModuleDeclaration
                Return New ModuleDeclaration(attributes, modifiers, KeywordLocation, Name, Members, EndBlockDeclaration, SpanFrom(startLocation), Comments)

            Case TreeType.InterfaceDeclaration
                Return New InterfaceDeclaration(attributes, modifiers, KeywordLocation, Name, Members, EndBlockDeclaration, SpanFrom(startLocation), Comments)

            Case TreeType.StructureDeclaration
                Return New StructureDeclaration(attributes, modifiers, KeywordLocation, Name, Members, EndBlockDeclaration, SpanFrom(startLocation), Comments)

            Case Else
                Debug.Fail("unexpected!")
        End Select
    End Function

    Private Function ParseEnumDeclaration(ByVal startLocation As Location, ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection) As Declaration
        Const ValidModifiers As ModifierTypes = ModifierTypes.AccessModifiers Or ModifierTypes.Shadows
        Dim KeywordLocation As Location
        Dim Name As SimpleName
        Dim AsLocation As Location
        Dim Type As TypeName
        Dim Members As DeclarationCollection
        Dim EndBlockDeclaration As EndBlockDeclaration
        Dim Comments As CommentCollection

        ValidateModifierList(modifiers, ValidModifiers)

        KeywordLocation = ReadLocation()

        Name = ParseSimpleName(False)

        If ErrorInConstruct Then
            ResyncAt(TokenType.As)
        End If

        If Peek().Type = TokenType.As Then
            AsLocation = ReadLocation()
            Type = ParseTypeName(False)

            If ErrorInConstruct Then
                ResyncAt()
            End If
        End If

        Members = ParseDeclarationBlock(SpanFrom(startLocation), TreeType.EnumDeclaration, Comments, EndBlockDeclaration)

        If Members.Count = 0 Then
            ReportSyntaxError(SyntaxErrorType.EmptyEnum, Name.Span)
        End If

        Return New EnumDeclaration(attributes, modifiers, KeywordLocation, Name, AsLocation, Type, Members, EndBlockDeclaration, SpanFrom(startLocation), Comments)
    End Function

    Private Function ParseDelegateDeclaration(ByVal startLocation As Location, ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection) As Declaration
        Const ValidModifiers As ModifierTypes = ModifierTypes.AccessModifiers Or ModifierTypes.Shadows Or ModifierTypes.Shared
        Dim DelegateLocation As Location
        Dim MethodType As TreeType = TreeType.SyntaxError
        Dim SubOrFunctionLocation As Location
        Dim Name As SimpleName
        Dim Parameters As ParameterCollection
        Dim AsLocation As Location
        Dim ReturnType As TypeName
        Dim ReturnTypeAttributes As AttributeCollection

        ValidateModifierList(modifiers, ValidModifiers)

        DelegateLocation = ReadLocation()

        If Peek().Type = TokenType.Sub Then
            SubOrFunctionLocation = ReadLocation()
            MethodType = TreeType.SubDeclaration
        ElseIf Peek().Type = TokenType.Function Then
            SubOrFunctionLocation = ReadLocation()
            MethodType = TreeType.FunctionDeclaration
        Else
            ReportSyntaxError(SyntaxErrorType.ExpectedSubOrFunction, Peek())
            MethodType = TreeType.SubDeclaration
        End If

        Name = ParseSimpleName(False)

        If ErrorInConstruct Then
            ResyncAt(TokenType.LeftParenthesis, TokenType.As)
        End If

        Parameters = ParseParameters()

        If MethodType = TreeType.FunctionDeclaration AndAlso Peek().Type = TokenType.As Then
            AsLocation = ReadLocation()
            ReturnTypeAttributes = ParseAttributes()
            ReturnType = ParseTypeName(True)

            If ErrorInConstruct Then
                ResyncAt()
            End If
        End If

        If MethodType = TreeType.SubDeclaration Then
            Return New DelegateSubDeclaration(attributes, modifiers, DelegateLocation, SubOrFunctionLocation, _
                Name, Parameters, SpanFrom(startLocation), ParseTrailingComments())
        Else
            Return New DelegateFunctionDeclaration(attributes, modifiers, DelegateLocation, SubOrFunctionLocation, _
                Name, Parameters, AsLocation, ReturnTypeAttributes, ReturnType, SpanFrom(startLocation), ParseTrailingComments())
        End If
    End Function

    Private Function ParseTypeListDeclaration(ByVal startLocation As Location, ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection, ByVal listType As TreeType) As Declaration
        Dim CommaLocations As ArrayList = New ArrayList
        Dim Types As ArrayList = New ArrayList
        Dim ListStart As Token

        Read()

        If Not attributes Is Nothing Then
            ReportSyntaxError(SyntaxErrorType.SpecifiersInvalidOnTypeListDeclaration, attributes.Span)
        End If

        If Not modifiers Is Nothing Then
            ReportSyntaxError(SyntaxErrorType.SpecifiersInvalidOnTypeListDeclaration, modifiers.Span)
        End If

        ListStart = Peek()

        Do
            If Types.Count > 0 Then
                CommaLocations.Add(ReadLocation())
            End If

            Types.Add(ParseTypeName(False))

            If ErrorInConstruct Then
                ResyncAt(TokenType.Comma)
            End If
        Loop While Peek().Type = TokenType.Comma

        If listType = TreeType.InheritsDeclaration Then
            Return New InheritsDeclaration(New TypeNameCollection(Types, CommaLocations, SpanFrom(ListStart)), SpanFrom(startLocation), ParseTrailingComments())
        Else
            Return New ImplementsDeclaration(New TypeNameCollection(Types, CommaLocations, SpanFrom(ListStart)), SpanFrom(startLocation), ParseTrailingComments())
        End If
    End Function

    Private Function ParseNamespaceDeclaration(ByVal startLocation As Location, ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection) As Declaration
        Dim KeywordLocation As Location
        Dim Name As Name
        Dim Members As DeclarationCollection
        Dim EndBlockDeclaration As EndBlockDeclaration
        Dim Comments As CommentCollection

        If Not attributes Is Nothing Then
            ReportSyntaxError(SyntaxErrorType.SpecifiersInvalidOnNamespaceDeclaration, attributes.Span)
        End If

        If Not modifiers Is Nothing Then
            ReportSyntaxError(SyntaxErrorType.SpecifiersInvalidOnNamespaceDeclaration, modifiers.Span)
        End If

        KeywordLocation = ReadLocation()

        Name = ParseName()

        If ErrorInConstruct Then
            ResyncAt()
        End If

        Members = ParseDeclarationBlock(SpanFrom(startLocation), TreeType.NamespaceDeclaration, Comments, EndBlockDeclaration)

        Return New NamespaceDeclaration(attributes, modifiers, KeywordLocation, Name, Members, EndBlockDeclaration, SpanFrom(startLocation), Comments)
    End Function

    Private Function ParseImportsDeclaration(ByVal startLocation As Location, ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection) As Declaration
        Dim ImportMembers As ArrayList = New ArrayList
        Dim CommaLocations As ArrayList = New ArrayList
        Dim ListStart As Token
        Dim ImportCollection As ImportCollection

        If Not attributes Is Nothing Then
            ReportSyntaxError(SyntaxErrorType.SpecifiersInvalidOnImportsDeclaration, attributes.Span)
        End If

        If Not modifiers Is Nothing Then
            ReportSyntaxError(SyntaxErrorType.SpecifiersInvalidOnImportsDeclaration, modifiers.Span)
        End If

        Read()
        ListStart = Peek()

        Do
            If ImportMembers.Count > 0 Then
                CommaLocations.Add(ReadLocation())
            End If

            If PeekAheadFor(TokenType.Equals, TokenType.Comma, TokenType.Period) = TokenType.Equals Then
                Dim ImportStart As Token = Peek()
                Dim Name As SimpleName
                Dim EqualsLocation As Location
                Dim AliasedName As Name

                Name = ParseSimpleName(False)
                EqualsLocation = ReadLocation()
                AliasedName = ParseName()

                If ErrorInConstruct Then
                    ResyncAt()
                End If

                ImportMembers.Add(New AliasImport(Name, EqualsLocation, AliasedName, SpanFrom(ImportStart)))
            Else
                Dim ImportStart As Token = Peek()
                Dim Name As Name

                Name = ParseName()

                If ErrorInConstruct Then
                    ResyncAt()
                End If

                ImportMembers.Add(New NameImport(name, SpanFrom(ImportStart)))
            End If
        Loop While Peek().Type = TokenType.Comma

        Return New ImportsDeclaration(New ImportCollection(ImportMembers, CommaLocations, SpanFrom(ListStart)), SpanFrom(startLocation), ParseTrailingComments())
    End Function

    Private Function ParseOptionDeclaration(ByVal startLocation As Location, ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection) As Declaration
        Dim OptionType As OptionType
        Dim OptionTypeLocation As Location
        Dim OptionArgumentLocation As Location

        Read()

        If Not attributes Is Nothing Then
            ReportSyntaxError(SyntaxErrorType.SpecifiersInvalidOnOptionDeclaration, attributes.Span)
        End If

        If Not modifiers Is Nothing Then
            ReportSyntaxError(SyntaxErrorType.SpecifiersInvalidOnOptionDeclaration, modifiers.Span)
        End If

        If Peek().Type = TokenType.Identifier Then
            Dim OptionTypeToken As IdentifierToken = CType(Read(), IdentifierToken)

            OptionTypeLocation = OptionTypeToken.Span.Start

            If String.Compare(OptionTypeToken.Identifier, "Explicit", True, Globalization.CultureInfo.InvariantCulture) = 0 Then
                If Peek().Type = TokenType.Identifier Then
                    Dim OptionArgumentToken As IdentifierToken = CType(Read(), IdentifierToken)

                    OptionArgumentLocation = OptionArgumentToken.Span.Start

                    If String.Compare(OptionArgumentToken.Identifier, "Off", True, Globalization.CultureInfo.InvariantCulture) = 0 Then
                        OptionType = OptionType.ExplicitOff
                    Else
                        OptionType = OptionType.SyntaxError
                        ReportSyntaxError(SyntaxErrorType.InvalidOptionExplicitType, SpanFrom(startLocation))
                    End If
                ElseIf Peek().Type = TokenType.On Then
                    OptionArgumentLocation = ReadLocation()
                    OptionType = OptionType.ExplicitOn
                Else
                    OptionType = OptionType.Explicit
                End If
            ElseIf String.Compare(OptionTypeToken.Identifier, "Strict", True, Globalization.CultureInfo.InvariantCulture) = 0 Then
                If Peek().Type = TokenType.Identifier Then
                    Dim OptionArgumentToken As IdentifierToken = CType(Read(), IdentifierToken)

                    OptionArgumentLocation = OptionArgumentToken.Span.Start

                    If String.Compare(OptionArgumentToken.Identifier, "Off", True, Globalization.CultureInfo.InvariantCulture) = 0 Then
                        OptionType = OptionType.StrictOff
                    Else
                        OptionType = OptionType.SyntaxError
                        ReportSyntaxError(SyntaxErrorType.InvalidOptionStrictType, SpanFrom(startLocation))
                    End If
                ElseIf Peek().Type = TokenType.On Then
                    OptionArgumentLocation = ReadLocation()
                    OptionType = OptionType.StrictOn
                Else
                    OptionType = OptionType.Strict
                End If
            ElseIf String.Compare(OptionTypeToken.Identifier, "Compare", True, Globalization.CultureInfo.InvariantCulture) = 0 Then
                If Peek().Type = TokenType.Identifier Then
                    Dim OptionArgumentToken As IdentifierToken = CType(Read(), IdentifierToken)

                    OptionArgumentLocation = OptionArgumentToken.Span.Start

                    If String.Compare(OptionArgumentToken.Identifier, "Binary", True, Globalization.CultureInfo.InvariantCulture) = 0 Then
                        OptionType = OptionType.CompareBinary
                    ElseIf String.Compare(OptionArgumentToken.Identifier, "Text", True, Globalization.CultureInfo.InvariantCulture) = 0 Then
                        OptionType = OptionType.CompareText
                    Else
                        GoTo InvalidCompareType
                    End If
                Else
InvalidCompareType:
                    OptionType = OptionType.SyntaxError
                    ReportSyntaxError(SyntaxErrorType.InvalidOptionCompareType, SpanFrom(startLocation))
                End If
            Else
                GoTo InvalidType
            End If
        Else
InvalidType:
            OptionType = OptionType.SyntaxError
            ReportSyntaxError(SyntaxErrorType.InvalidOptionType, SpanFrom(startLocation))
        End If

        If ErrorInConstruct Then
            ResyncAt()
        End If

        Return New OptionDeclaration(OptionType, OptionTypeLocation, OptionArgumentLocation, SpanFrom(startLocation), ParseTrailingComments())
    End Function

    Private Function ParseAttributeDeclaration() As Declaration
        Dim Attributes As AttributeCollection

        Attributes = ParseAttributes(AttributeTypes.Module Or AttributeTypes.Assembly)

        Return New AttributeDeclaration(Attributes, Attributes.Span, ParseTrailingComments())
    End Function

    Private Function ParseDeclaration(Optional ByRef terminator As Token = Nothing) As Declaration
        Dim Start As Token
        Dim StartLocation As Location
        Dim Declaration As Declaration
        Dim Attributes As AttributeCollection
        Dim Modifiers As ModifierCollection

        If AtBeginningOfLine Then
            While ParsePreprocessorStatement(False)
                ' Loop
            End While
        End If

        Start = Peek()

        ErrorInConstruct = False

        StartLocation = Peek().Span.Start
        If Peek().Type <> TokenType.LessThan OrElse PeekAheadFor(TokenType.Assembly, TokenType.Module) = 0 Then
            Attributes = ParseAttributes()
            Modifiers = ParseDeclarationModifierList()
        End If

        Select Case Peek().Type
            Case TokenType.End
                If Attributes Is Nothing AndAlso Modifiers Is Nothing Then
                    Declaration = ParseEndDeclaration()
                Else
                    GoTo Identifier
                End If

            Case TokenType.Property
                Declaration = ParsePropertyDeclaration(StartLocation, Attributes, Modifiers)

            Case TokenType.Declare
                Declaration = ParseExternalDeclaration(StartLocation, Attributes, Modifiers)

            Case TokenType.Sub, TokenType.Function
                Declaration = ParseMethodDeclaration(StartLocation, Attributes, Modifiers)

            Case TokenType.Get, TokenType.Set
                Declaration = ParseAccessorDeclaration(StartLocation, Attributes, Modifiers)

            Case TokenType.Event
                Declaration = ParseEventDeclaration(StartLocation, Attributes, Modifiers)

            Case TokenType.Delegate
                Declaration = ParseDelegateDeclaration(StartLocation, Attributes, Modifiers)

            Case TokenType.Class
                Declaration = ParseTypeDeclaration(StartLocation, Attributes, Modifiers, TreeType.ClassDeclaration)

            Case TokenType.Structure
                Declaration = ParseTypeDeclaration(StartLocation, Attributes, Modifiers, TreeType.StructureDeclaration)

            Case TokenType.Module
                Declaration = ParseTypeDeclaration(StartLocation, Attributes, Modifiers, TreeType.ModuleDeclaration)

            Case TokenType.Interface
                Declaration = ParseTypeDeclaration(StartLocation, Attributes, Modifiers, TreeType.InterfaceDeclaration)

            Case TokenType.Enum
                Declaration = ParseEnumDeclaration(StartLocation, Attributes, Modifiers)

            Case TokenType.Namespace
                Declaration = ParseNamespaceDeclaration(StartLocation, Attributes, Modifiers)

            Case TokenType.Implements
                Declaration = ParseTypeListDeclaration(StartLocation, Attributes, Modifiers, TreeType.ImplementsDeclaration)

            Case TokenType.Inherits
                Declaration = ParseTypeListDeclaration(StartLocation, Attributes, Modifiers, TreeType.InheritsDeclaration)

            Case TokenType.Imports
                Declaration = ParseImportsDeclaration(StartLocation, Attributes, Modifiers)

            Case TokenType.Option
                Declaration = ParseOptionDeclaration(StartLocation, Attributes, Modifiers)

            Case TokenType.LessThan
                Declaration = ParseAttributeDeclaration()

            Case TokenType.Identifier
Identifier:
                Declaration = ParseVariableListDeclaration(StartLocation, Attributes, Modifiers)

            Case TokenType.LineTerminator, TokenType.Colon, TokenType.EndOfStream
                If Attributes Is Nothing AndAlso Modifiers Is Nothing Then
                    ' An empty declaration
                Else
                    ReportSyntaxError(SyntaxErrorType.ExpectedIdentifier, Peek())
                End If

            Case TokenType.Comment
                Dim Comments As ArrayList = New ArrayList
                Dim LastTerminator As Token

                Do
                    Dim CommentToken As CommentToken = CType(Scanner.Read(), CommentToken)
                    Comments.Add(New Comment(CommentToken.Comment, CommentToken.IsREM, CommentToken.Span))
                    LastTerminator = Read() ' Eat the terminator of the comment
                Loop While Peek().Type = TokenType.Comment
                Backtrack(LastTerminator)

                Declaration = New EmptyDeclaration(SpanFrom(Start), New CommentCollection(Comments))

            Case Else
                ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek())
        End Select

        terminator = VerifyEndOfStatement()

        Return Declaration
    End Function

    Private Function ParseDeclarationInEnum(Optional ByRef terminator As Token = Nothing) As Declaration
        Dim Start As Token
        Dim StartLocation As Location
        Dim Attributes As AttributeCollection
        Dim Name As SimpleName
        Dim EqualsLocation As Location
        Dim Expression As Expression
        Dim Declaration As Declaration

        If AtBeginningOfLine Then
            While ParsePreprocessorStatement(False)
                ' Loop
            End While
        End If

        Start = Peek()

        If Start.Type = TokenType.Comment Then
            Dim Comments As ArrayList = New ArrayList
            Dim LastTerminator As Token

            Do
                Dim CommentToken As CommentToken = CType(Scanner.Read(), CommentToken)
                Comments.Add(New Comment(CommentToken.Comment, CommentToken.IsREM, CommentToken.Span))
                LastTerminator = Read() ' Eat the terminator of the comment
            Loop While Peek().Type = TokenType.Comment
            Backtrack(LastTerminator)

            Declaration = New EmptyDeclaration(SpanFrom(Start), New CommentCollection(Comments))
            GoTo HaveStatement
        End If

        If Start.Type = TokenType.LineTerminator OrElse Start.Type = TokenType.Colon Then
            GoTo HaveStatement
        End If

        ErrorInConstruct = False

        StartLocation = Peek().Span.Start
        Attributes = ParseAttributes()

        If Peek().Type = TokenType.End AndAlso Attributes Is Nothing Then
            Declaration = ParseEndDeclaration()
            GoTo HaveStatement
        End If

        Name = ParseSimpleName(False)

        If ErrorInConstruct Then
            ResyncAt(TokenType.Equals)
        End If

        If Peek().Type = TokenType.Equals Then
            EqualsLocation = ReadLocation()
            Expression = ParseExpression()

            If ErrorInConstruct Then
                ResyncAt()
            End If
        End If

        Declaration = New EnumValueDeclaration(Attributes, Name, EqualsLocation, Expression, SpanFrom(StartLocation), ParseTrailingComments())

HaveStatement:
        terminator = VerifyEndOfStatement()

        Return Declaration
    End Function

    Private Function ParseDeclarationBlock(ByVal blockStartSpan As Span, ByVal blockType As TreeType, ByRef Comments As CommentCollection, Optional ByRef endDeclaration As EndBlockDeclaration = Nothing) As DeclarationCollection
        Dim Declarations As ArrayList = New ArrayList
        Dim ColonLocations As ArrayList = New ArrayList
        Dim Terminator As Token
        Dim Start As Token
        Dim DeclarationsEnd As Location
        Dim BlockTerminated As Boolean = False

        Comments = ParseTrailingComments()
        Terminator = VerifyEndOfStatement()

        If Terminator.Type = TokenType.Colon Then
            ColonLocations.Add(Terminator.Span.Start)
        End If

        Start = Peek()
        DeclarationsEnd = Start.Span.Finish
        endDeclaration = Nothing

        PushBlockContext(blockType)

        While Peek().Type <> TokenType.EndOfStream
            Dim PreviousTerminator As Token = Terminator
            Dim Declaration As Declaration

            If blockType = TreeType.EnumDeclaration Then
                Declaration = ParseDeclarationInEnum(Terminator)
            Else
                Declaration = ParseDeclaration(Terminator)
            End If

            If Not Declaration Is Nothing Then
                Dim ErrorType As SyntaxErrorType

                If Declaration.Type = TreeType.EndBlockDeclaration Then
                    Dim PotentialEndDeclaration As EndBlockDeclaration = CType(Declaration, EndBlockDeclaration)

                    If DeclarationEndsBlock(blockType, PotentialEndDeclaration) Then
                        endDeclaration = PotentialEndDeclaration
                        Backtrack(Terminator)
                        BlockTerminated = True
                        Exit While
                    Else
                        Dim DeclarationEndsOuterBlock As Boolean = False

                        ' If the end Declaration matches an outer block context, then we want to unwind
                        ' up to that level. Otherwise, we want to just give an error and keep going.
                        For Each BlockContext As TreeType In BlockContextStack
                            If DeclarationEndsBlock(BlockContext, PotentialEndDeclaration) Then
                                DeclarationEndsOuterBlock = True
                                Exit For
                            End If
                        Next

                        If DeclarationEndsOuterBlock Then
                            ReportMismatchedEndError(blockType, Declaration.Span)
                            ' CONSIDER: Can we avoid parsing and re-parsing this declaration?
                            Backtrack(PreviousTerminator)
                            ' We consider the block terminated.
                            BlockTerminated = True
                            Exit While
                        Else
                            ReportMissingBeginDeclarationError(PotentialEndDeclaration)
                        End If
                    End If
                Else
                    ErrorType = ValidDeclaration(blockType, Declaration, Declarations)

                    If ErrorType <> SyntaxErrorType.None Then
                        ReportSyntaxError(ErrorType, Declaration.Span)
                    End If
                End If

                Declarations.Add(Declaration)
            End If

            If Terminator.Type = TokenType.Colon Then
                ColonLocations.Add(Terminator.Span.Start)
            End If

            DeclarationsEnd = Terminator.Span.Finish
        End While

        If Not BlockTerminated Then
            ReportMismatchedEndError(blockType, blockStartSpan)
        End If

        PopBlockContext()

        If Declarations.Count = 0 AndAlso ColonLocations.Count = 0 Then
            Return Nothing
        Else
            Return New DeclarationCollection(Declarations, ColonLocations, New Span(Start.Span.Start, DeclarationsEnd))
        End If
    End Function

    '*
    '* Parameters
    '*

    Private Function ParseParameter() As Parameter
        Dim Start As Token = Peek()
        Dim Attributes As AttributeCollection
        Dim Modifiers As ModifierCollection
        Dim VariableName As VariableName
        Dim AsLocation As Location
        Dim Type As TypeName
        Dim EqualsLocation As Location
        Dim Initializer As Initializer

        Attributes = ParseAttributes()
        Modifiers = ParseParameterModifierList()
        VariableName = ParseVariableName(False)

        If ErrorInConstruct Then
            ' If we see As before a comma or RParen, then assume that
            ' we are still on the same parameter. Otherwise, don't resync
            ' and allow the caller to decide how to recover.
            If PeekAheadFor(TokenType.As, TokenType.Comma, TokenType.RightParenthesis) = TokenType.As Then
                ResyncAt(TokenType.As)
            End If
        End If

        If Peek().Type = TokenType.As Then
            AsLocation = ReadLocation()
            Type = ParseTypeName(True)
        End If

        If ErrorInConstruct Then
            ResyncAt(TokenType.Equals, TokenType.Comma, TokenType.RightParenthesis)
        End If

        If Peek().Type = TokenType.Equals Then
            EqualsLocation = ReadLocation()
            Initializer = ParseInitializer()
        End If

        If ErrorInConstruct Then
            ResyncAt(TokenType.Comma, TokenType.RightParenthesis)
        End If

        Return New Parameter(Attributes, Modifiers, VariableName, AsLocation, Type, EqualsLocation, Initializer, SpanFrom(Start))
    End Function

    Private Function ParametersContinue() As Boolean
        Dim NextToken As Token = Peek()

        If NextToken.Type = TokenType.Comma Then
            Return True
        ElseIf NextToken.Type = TokenType.RightParenthesis OrElse MustEndStatement(NextToken) Then
            Return False
        End If

        ReportSyntaxError(SyntaxErrorType.ParameterSyntax, NextToken)
        ResyncAt(TokenType.Comma, TokenType.RightParenthesis)

        If Peek().Type = TokenType.Comma Then
            ErrorInConstruct = False
            Return True
        End If

        Return False
    End Function

    Private Function ParseParameters() As ParameterCollection
        Dim Start As Token = Peek()
        Dim Parameters As ArrayList = New ArrayList
        Dim CommaLocations As ArrayList = New ArrayList
        Dim RightParenthesisLocation As Location

        If Start.Type <> TokenType.LeftParenthesis Then
            Return Nothing
        Else
            Read()
        End If

        If Peek().Type <> TokenType.RightParenthesis Then
            Do
                If Parameters.Count > 0 Then
                    CommaLocations.Add(ReadLocation())
                End If

                Parameters.Add(ParseParameter())

                If ErrorInConstruct Then
                    ResyncAt(TokenType.Comma, TokenType.RightParenthesis)
                End If
            Loop While ParametersContinue()
        End If

        If Peek().Type = TokenType.RightParenthesis Then
            RightParenthesisLocation = ReadLocation()
        Else
            Dim CurrentToken As Token = Peek()

            ' On error, peek for ")" with "(". If ")" seen before 
            ' "(", then sync on that. Otherwise, assume missing ")"
            ' and let caller decide.
            ResyncAt(TokenType.LeftParenthesis, TokenType.RightParenthesis)

            If Peek().Type = TokenType.RightParenthesis Then
                ReportSyntaxError(SyntaxErrorType.SyntaxError, Peek())
                RightParenthesisLocation = ReadLocation()
            Else
                Backtrack(CurrentToken)
                ReportSyntaxError(SyntaxErrorType.ExpectedRightParenthesis, Peek())
            End If
        End If

        Return New ParameterCollection(Parameters, CommaLocations, RightParenthesisLocation, SpanFrom(Start))
    End Function

    '*
    '* Files
    '*

    Private Function ParseFile() As File
        Dim Declarations As ArrayList = New ArrayList
        Dim ColonLocations As ArrayList = New ArrayList
        Dim Terminator As Token
        Dim Start As Token = Peek()

        While Peek().Type <> TokenType.EndOfStream
            Dim Declaration As Declaration

            Declaration = ParseDeclaration(Terminator)

            If Not Declaration Is Nothing Then
                Dim ErrorType As SyntaxErrorType = SyntaxErrorType.None

                ErrorType = ValidDeclaration(TreeType.File, Declaration, Declarations)

                If ErrorType <> SyntaxErrorType.None Then
                    ReportSyntaxError(ErrorType, Declaration.Span)
                End If

                Declarations.Add(Declaration)
            End If

            If Terminator.Type = TokenType.Colon Then
                ColonLocations.Add(Terminator.Span.Start)
            End If
        End While

        If Declarations.Count = 0 AndAlso ColonLocations.Count = 0 Then
            Return New File(Nothing, SpanFrom(Start))
        Else
            Return New File(New DeclarationCollection(Declarations, ColonLocations, SpanFrom(Start)), SpanFrom(Start))
        End If
    End Function

    '*
    '* Preprocessor statements
    '*

    Private Sub ParseExternalSourceStatement(ByVal start As Token)
        Dim Line As Long
        Dim File As String

        ' Consume the ExternalSource keyword
        Read()

        If Not CurrentExternalSourceContext Is Nothing Then
            ResyncAt()
            ReportSyntaxError(SyntaxErrorType.NestedExternalSourceStatement, SpanFrom(start))
        Else
            VerifyExpectedToken(TokenType.LeftParenthesis)

            If Peek().Type <> TokenType.StringLiteral Then
                ReportSyntaxError(SyntaxErrorType.ExpectedStringLiteral, Peek())
                ResyncAt()
                Return
            End If

            File = CType(Read(), StringLiteralToken).Literal
            VerifyExpectedToken(TokenType.Comma)

            If Peek().Type <> TokenType.IntegerLiteral Then
                ReportSyntaxError(SyntaxErrorType.ExpectedIntegerLiteral, Peek())
                ResyncAt()
                Return
            End If

            Line = CType(Read(), IntegerLiteralToken).Literal

            VerifyExpectedToken(TokenType.RightParenthesis)

            CurrentExternalSourceContext = New ExternalSourceContext
            With CurrentExternalSourceContext
                .File = File
                .Line = Line
                .Start = Peek().Span.Start
            End With
        End If
    End Sub

    Private Sub ParseRegionStatement(ByVal start As Token, ByVal statementLevel As Boolean)
        Dim Description As String
        Dim RegionContext As RegionContext

        If statementLevel = True Then
            ResyncAt()
            ReportSyntaxError(SyntaxErrorType.RegionInsideMethod, SpanFrom(start))
            Return
        End If

        ' Consume the Region keyword
        Read()

        If Peek().Type <> TokenType.StringLiteral Then
            ReportSyntaxError(SyntaxErrorType.ExpectedStringLiteral, Peek())
            ResyncAt()
            Return
        End If

        Description = CType(Read(), StringLiteralToken).Literal

        RegionContext = New RegionContext
        RegionContext.Description = Description
        RegionContext.Start = Peek().Span.Start
        RegionContextStack.Push(RegionContext)
    End Sub

    Private Sub ParseEndPreprocessingStatement(ByVal start As Token, ByVal statementLevel As Boolean)
        ' Consume the End keyword
        Read()

        If Peek().Type = TokenType.Identifier Then
            Dim EndDirective As IdentifierToken = CType(Read(), IdentifierToken)

            If String.Compare(EndDirective.Identifier, "ExternalSource", True, Globalization.CultureInfo.InvariantCulture) = 0 Then
                If CurrentExternalSourceContext Is Nothing Then
                    ReportSyntaxError(SyntaxErrorType.EndExternalSourceWithoutExternalSource, SpanFrom(start))
                    ResyncAt()
                Else
                    If Not ExternalLineMappings Is Nothing Then
                        With CurrentExternalSourceContext
                            ExternalLineMappings.Add(New ExternalLineMapping(.Start, start.Span.Start, .File, .Line))
                        End With
                    End If
                    CurrentExternalSourceContext = Nothing
                End If

                Return
            ElseIf String.Compare(EndDirective.Identifier, "Region", True, Globalization.CultureInfo.InvariantCulture) = 0 Then
                If statementLevel = True Then
                    ResyncAt()
                    ReportSyntaxError(SyntaxErrorType.RegionInsideMethod, SpanFrom(start))
                    Return
                End If

                If RegionContextStack.Count = 0 Then
                    ReportSyntaxError(SyntaxErrorType.EndRegionWithoutRegion, SpanFrom(start))
                    ResyncAt()
                Else
                    Dim RegionContext As RegionContext = CType(RegionContextStack.Pop(), RegionContext)

                    If Not SourceRegions Is Nothing Then
                        SourceRegions.Add(New SourceRegion(RegionContext.Start, start.Span.Start, RegionContext.Description))
                    End If
                End If

                Return
            End If
        ElseIf Peek().Type = TokenType.If Then
            ' Read the If keyword
            Read()

            If ConditionalCompilationContextStack.Count = 0 Then
                ReportSyntaxError(SyntaxErrorType.CCEndIfWithoutCCIf, SpanFrom(start))
            Else
                ConditionalCompilationContextStack.Pop()
            End If

            Return
        End If

        ResyncAt()
        ReportSyntaxError(SyntaxErrorType.ExpectedEndKind, Peek())
    End Sub

    Private Function EvaluateCCLiteral(ByVal expression As LiteralExpression) As Object
        Select Case expression.Type
            Case TreeType.IntegerLiteralExpression
                Return CType(expression, IntegerLiteralExpression).Literal

            Case TreeType.FloatingPointLiteralExpression
                Return CType(expression, FloatingPointLiteralExpression).Literal

            Case TreeType.StringLiteralExpression
                Return CType(expression, StringLiteralExpression).Literal

            Case TreeType.CharacterLiteralExpression
                Return CType(expression, CharacterLiteralExpression).Literal

            Case TreeType.DateLiteralExpression
                Return CType(expression, DateLiteralExpression).Literal

            Case TreeType.DecimalLiteralExpression
                Return CType(expression, DecimalLiteralExpression).Literal

            Case TreeType.BooleanLiteralExpression
                Return CType(expression, BooleanLiteralExpression).Literal

            Case Else
                Debug.Fail("Unexpected!")
                Return Nothing
        End Select
    End Function

    Private Function TypeCodeOfCastExpression(ByVal castType As IntrinsicType) As TypeCode
        Select Case castType
            Case IntrinsicType.Boolean
                Return TypeCode.Boolean

            Case IntrinsicType.Byte
                Return TypeCode.Byte

            Case IntrinsicType.Char
                Return TypeCode.Char

            Case IntrinsicType.Date
                Return TypeCode.DateTime

            Case IntrinsicType.Decimal
                Return TypeCode.Decimal

            Case IntrinsicType.Double
                Return TypeCode.Double

            Case IntrinsicType.Integer
                Return TypeCode.Int32

            Case IntrinsicType.Long
                Return TypeCode.Int64

            Case IntrinsicType.Object
                Return TypeCode.Object

            Case IntrinsicType.Short
                Return TypeCode.Int16

            Case IntrinsicType.Single
                Return TypeCode.Single

            Case IntrinsicType.String
                Return TypeCode.String

            Case Else
                Debug.Fail("Unexpected!")
                Return TypeCode.Empty
        End Select
    End Function

    Private Function EvaluateCCCast(ByVal expression As IntrinsicCastExpression) As Object
        ' This cast is safe because only intrinsics are ever returned
        Dim Operand As IConvertible = CType(EvaluateCCExpression(expression.Operand), IConvertible)
        Dim OperandType As TypeCode
        Dim CastType As TypeCode = TypeCodeOfCastExpression(expression.IntrinsicType)

        If CastType = TypeCode.Empty Then
            Return Nothing
        End If

        If Operand Is Nothing Then
            Operand = 0
        End If

        OperandType = Operand.GetTypeCode()

        If CastType = OperandType OrElse CastType = TypeCode.Object Then
            Return Operand
        End If

        Select Case OperandType
            Case TypeCode.Boolean
                If CastType = TypeCode.Byte Then
                    Operand = 255
                Else
                    Operand = -1
                End If
                OperandType = TypeCode.Int32

            Case TypeCode.String
                If CastType <> TypeCode.Char Then
                    ReportSyntaxError(SyntaxErrorType.CantCastStringInCCExpression, expression.Span)
                    Return Nothing
                End If

            Case TypeCode.Char
                If CastType <> TypeCode.String Then
                    ReportSyntaxError(SyntaxErrorType.InvalidCCCast, expression.Span)
                    Return Nothing
                End If

            Case TypeCode.DateTime
                ReportSyntaxError(SyntaxErrorType.InvalidCCCast, expression.Span)
                Return Nothing
        End Select

        Select Case expression.IntrinsicType
            Case IntrinsicType.Boolean
                Return CBool(Operand)

            Case IntrinsicType.Byte
                Return CByte(Operand)

            Case IntrinsicType.Short
                Return CShort(Operand)

            Case IntrinsicType.Integer
                Return CInt(Operand)

            Case IntrinsicType.Long
                Return CLng(Operand)

            Case IntrinsicType.Decimal
                Return CDec(Operand)

            Case IntrinsicType.Single
                Return CSng(Operand)

            Case IntrinsicType.Double
                Return CDbl(Operand)

            Case IntrinsicType.Char
                If OperandType = TypeCode.String Then
                    Return CChar(DirectCast(Operand, String))
                End If

                ReportSyntaxError(SyntaxErrorType.InvalidCCCast, expression.Span)
                Return Nothing

            Case IntrinsicType.String
                If OperandType = TypeCode.Char Then
                    Return CStr(DirectCast(Operand, Char))
                End If

                ReportSyntaxError(SyntaxErrorType.CantCastStringInCCExpression, expression.Span)
                Return Nothing

            Case IntrinsicType.Date
                ReportSyntaxError(SyntaxErrorType.InvalidCCCast, expression.Span)
                Return Nothing

            Case Else
                Debug.Fail("Unexpected!")
                Return Nothing
        End Select
    End Function

    Private Function EvaluateCCUnaryOperator(ByVal expression As UnaryOperatorExpression) As Object
        ' This cast is safe because only intrinsics are ever returned
        Dim Operand As IConvertible = CType(EvaluateCCExpression(expression.Operand), IConvertible)
        Dim OperandType As TypeCode

        If Operand Is Nothing Then
            Operand = 0
        End If

        OperandType = Operand.GetTypeCode()

        If OperandType = TypeCode.String OrElse OperandType = TypeCode.Char OrElse OperandType = TypeCode.DateTime Then
            ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
            Return Nothing
        End If

        Select Case expression.Operator
            Case OperatorType.UnaryPlus
                If OperandType = TypeCode.Boolean Then
                    ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
                    Return Nothing
                Else
                    Return Operand
                End If

            Case OperatorType.Negate
                If OperandType = TypeCode.Boolean OrElse OperandType = TypeCode.Byte Then
                    ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
                    Return Nothing
                Else
                    Return CompilerServices.ObjectType.NegObj(Operand)
                End If

            Case OperatorType.Not
                If OperandType = TypeCode.Decimal OrElse OperandType = TypeCode.Single OrElse _
                   OperandType = TypeCode.Double Then
                    ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
                    Return Nothing
                Else
                    Return CompilerServices.ObjectType.NotObj(Operand)
                End If

            Case Else
                Debug.Fail("Unexpected!")
                Return Nothing
        End Select
    End Function

    Private Function EitherIsTypeCode(ByVal x As TypeCode, ByVal y As TypeCode, ByVal type As TypeCode) As Boolean
        Return x = type OrElse y = type
    End Function

    Private Function IsEitherTypeCode(ByVal x As TypeCode, ByVal type1 As TypeCode, ByVal type2 As TypeCode) As Boolean
        Return x = type1 OrElse x = type2
    End Function

    Private Function EvaluateCCBinaryOperator(ByVal expression As BinaryOperatorExpression) As Object
        ' This cast is safe because only intrinsics are ever returned
        Dim LeftOperand As IConvertible = CType(EvaluateCCExpression(expression.LeftOperand), IConvertible)
        Dim RightOperand As IConvertible = CType(EvaluateCCExpression(expression.RightOperand), IConvertible)
        Dim LeftOperandType, RightOperandType As TypeCode

        If LeftOperand Is Nothing Then
            LeftOperand = 0
        End If

        If RightOperand Is Nothing Then
            RightOperand = 0
        End If

        LeftOperandType = LeftOperand.GetTypeCode()
        RightOperandType = RightOperand.GetTypeCode()

        If EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.DateTime) Then
            ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
            Return Nothing
        End If

        If expression.Operator <> OperatorType.Concatenate AndAlso _
           expression.Operator <> OperatorType.Plus AndAlso _
           expression.Operator <> OperatorType.Equals AndAlso _
           expression.Operator <> OperatorType.NotEquals AndAlso _
           (EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.Char) OrElse _
            EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.String)) Then
            ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
            Return Nothing
        End If

        Select Case expression.Operator
            Case OperatorType.Plus
                If EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.String) OrElse _
                   EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.Char) Then
                    If Not IsEitherTypeCode(LeftOperandType, TypeCode.Char, TypeCode.String) OrElse _
                       Not IsEitherTypeCode(LeftOperandType, TypeCode.Char, TypeCode.String) Then
                        ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
                        Return Nothing
                    Else
                        Return CStr(LeftOperand) + CStr(RightOperand)
                    End If
                Else
                    Return CompilerServices.ObjectType.AddObj(LeftOperand, RightOperand)
                End If

            Case OperatorType.Minus
                Return CompilerServices.ObjectType.SubObj(LeftOperand, RightOperand)

            Case OperatorType.Multiply
                Return CompilerServices.ObjectType.MulObj(LeftOperand, RightOperand)

            Case OperatorType.IntegralDivide
                Return CompilerServices.ObjectType.IDivObj(LeftOperand, RightOperand)

            Case OperatorType.Divide
                Return CompilerServices.ObjectType.DivObj(LeftOperand, RightOperand)

            Case OperatorType.Modulus
                Return CompilerServices.ObjectType.ModObj(LeftOperand, RightOperand)

            Case OperatorType.Power
                Return CompilerServices.ObjectType.PowObj(LeftOperand, RightOperand)

            Case OperatorType.ShiftLeft
                Return CompilerServices.ObjectType.ShiftLeftObj(LeftOperand, CInt(RightOperand))

            Case OperatorType.ShiftRight
                Return CompilerServices.ObjectType.ShiftRightObj(LeftOperand, CInt(RightOperand))

            Case OperatorType.And
                Return CompilerServices.ObjectType.BitAndObj(LeftOperand, CInt(RightOperand))

            Case OperatorType.Or
                Return CompilerServices.ObjectType.BitOrObj(LeftOperand, CInt(RightOperand))

            Case OperatorType.Xor
                Return CompilerServices.ObjectType.BitXorObj(LeftOperand, CInt(RightOperand))

            Case OperatorType.AndAlso
                Return CBool(LeftOperand) AndAlso CBool(RightOperand)

            Case OperatorType.OrElse
                Return CBool(LeftOperand) OrElse CBool(RightOperand)

            Case OperatorType.Equals
                If (EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.String) OrElse _
                    EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.Char)) AndAlso _
                   (Not IsEitherTypeCode(LeftOperandType, TypeCode.Char, TypeCode.String) OrElse _
                    Not IsEitherTypeCode(LeftOperandType, TypeCode.Char, TypeCode.String)) Then
                    ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
                    Return Nothing
                End If

                Return CompilerServices.ObjectType.ObjTst(LeftOperand, RightOperand, False) = 0

            Case OperatorType.NotEquals
                If (EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.String) OrElse _
                    EitherIsTypeCode(LeftOperandType, RightOperandType, TypeCode.Char)) AndAlso _
                   (Not IsEitherTypeCode(LeftOperandType, TypeCode.Char, TypeCode.String) OrElse _
                    Not IsEitherTypeCode(LeftOperandType, TypeCode.Char, TypeCode.String)) Then
                    ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
                    Return Nothing
                End If

                Return CompilerServices.ObjectType.ObjTst(LeftOperand, RightOperand, False) <> 0

            Case OperatorType.LessThan
                Return CompilerServices.ObjectType.ObjTst(LeftOperand, RightOperand, False) = -1

            Case OperatorType.GreaterThan
                Return CompilerServices.ObjectType.ObjTst(LeftOperand, RightOperand, False) = 1

            Case OperatorType.LessThanEquals
                Return CompilerServices.ObjectType.ObjTst(LeftOperand, RightOperand, False) <> 1

            Case OperatorType.GreaterThanEquals
                Return CompilerServices.ObjectType.ObjTst(LeftOperand, RightOperand, False) <> -1

            Case OperatorType.Concatenate
                If Not IsEitherTypeCode(LeftOperandType, TypeCode.Char, TypeCode.String) OrElse _
                   Not IsEitherTypeCode(LeftOperandType, TypeCode.Char, TypeCode.String) Then
                    ReportSyntaxError(SyntaxErrorType.InvalidCCOperator, expression.Span)
                    Return Nothing
                Else
                    Return CStr(LeftOperand) & CStr(RightOperand)
                End If

            Case Else
                Debug.Fail("Unexpected!")
                Return Nothing
        End Select
    End Function

    Private Function EvaluateCCExpression(ByVal expression As Expression) As Object
        Select Case expression.Type
            Case TreeType.SyntaxError
                ' Do nothing

            Case TreeType.NothingExpression
                Return Nothing

            Case TreeType.IntegerLiteralExpression, TreeType.FloatingPointLiteralExpression, _
                 TreeType.StringLiteralExpression, TreeType.CharacterLiteralExpression, _
                 TreeType.DateLiteralExpression, TreeType.DecimalLiteralExpression, _
                 TreeType.BooleanLiteralExpression
                Return EvaluateCCLiteral(CType(expression, LiteralExpression))

            Case TreeType.ParentheticalExpression
                Return EvaluateCCExpression(CType(expression, ParentheticalExpression).Operand)

            Case TreeType.SimpleNameExpression
                Return ConditionalCompilationConstants(CType(expression, SimpleNameExpression).Name.Name)

            Case TreeType.IntrinsicCastExpression
                Return EvaluateCCCast(CType(expression, IntrinsicCastExpression))

            Case TreeType.UnaryOperatorExpression
                Return EvaluateCCUnaryOperator(CType(expression, UnaryOperatorExpression))

            Case TreeType.BinaryOperatorExpression
                Return EvaluateCCBinaryOperator(CType(expression, BinaryOperatorExpression))

            Case Else
                ReportSyntaxError(SyntaxErrorType.CCExpressionRequired, expression.Span)
        End Select

        Return Nothing
    End Function

    Private Sub ParseConditionalConstantStatement()
        Dim Identifier As IdentifierToken
        Dim Expression As Expression

        ' Consume the Const keyword
        Read()

        If Peek().Type = TokenType.Identifier Then
            Identifier = CType(Read(), IdentifierToken)
        Else
            ReportSyntaxError(SyntaxErrorType.ExpectedIdentifier, Peek())
            ResyncAt()
            Return
        End If

        VerifyExpectedToken(TokenType.Equals)
        Expression = ParseExpression()

        If Not ErrorInConstruct Then
            ConditionalCompilationConstants.Add(Identifier.Identifier, EvaluateCCExpression(Expression))
        Else
            ResyncAt()
        End If
    End Sub

    Private Sub ParseConditionalIfStatement(ByVal start As Token)
        Dim Expression As Expression
        Dim CCContext As ConditionalCompilationContext

        ' Consume the If
        Read()

        Expression = ParseExpression()

        If ErrorInConstruct Then
            ResyncAt(TokenType.Then)
        End If

        If Peek().Type = TokenType.Then Then
            ' Consume the Then keyword
            Read()
        End If

        CCContext = New ConditionalCompilationContext
        With CCContext
            .BlockActive = CBool(EvaluateCCExpression(Expression))
            .AnyBlocksActive = .BlockActive
        End With
        ConditionalCompilationContextStack.Push(CCContext)
    End Sub

    Private Sub ParseConditionalElseIfStatement(ByVal start As Token)
        Dim Expression As Expression
        Dim CCContext As ConditionalCompilationContext

        ' Consume the If
        Read()

        Expression = ParseExpression()

        If ErrorInConstruct Then
            ResyncAt(TokenType.Then)
        End If

        If Peek().Type = TokenType.Then Then
            ' Consume the Then keyword
            Read()
        End If

        If ConditionalCompilationContextStack.Count = 0 Then
            ReportSyntaxError(SyntaxErrorType.CCElseIfWithoutCCIf, SpanFrom(start))
        Else
            CCContext = CType(ConditionalCompilationContextStack.Peek(), ConditionalCompilationContext)

            If CCContext.SeenElse Then
                ReportSyntaxError(SyntaxErrorType.CCElseIfAfterCCElse, SpanFrom(start))
                CCContext.BlockActive = False
            ElseIf CCContext.BlockActive Then
                CCContext.BlockActive = False
            ElseIf Not CCContext.AnyBlocksActive AndAlso CBool(EvaluateCCExpression(Expression)) Then
                CCContext.BlockActive = True
                CCContext.AnyBlocksActive = True
            End If
        End If
    End Sub

    Private Sub ParseConditionalElseStatement(ByVal start As Token)
        Dim CCContext As ConditionalCompilationContext

        ' Consume the else
        Read()

        If ConditionalCompilationContextStack.Count = 0 Then
            ReportSyntaxError(SyntaxErrorType.CCElseWithoutCCIf, SpanFrom(start))
        Else
            CCContext = CType(ConditionalCompilationContextStack.Peek(), ConditionalCompilationContext)

            If CCContext.SeenElse Then
                ReportSyntaxError(SyntaxErrorType.CCElseAfterCCElse, SpanFrom(start))
                CCContext.BlockActive = False
            Else
                CCContext.SeenElse = True

                If CCContext.BlockActive Then
                    CCContext.BlockActive = False
                ElseIf Not CCContext.AnyBlocksActive Then
                    CCContext.BlockActive = True
                End If
            End If
        End If
    End Sub

    Private Function ParsePreprocessorStatement(ByVal statementLevel As Boolean) As Boolean
        Dim Start As Token = Peek()

        Debug.Assert(AtBeginningOfLine, "Must be at beginning of line!")

        If Not Preprocess Then
            Return False
        End If

        If Start.Type = TokenType.Pound Then
            ErrorInConstruct = False

            ' Consume the pound
            Read()

            Select Case Peek().Type
                Case TokenType.Const
                    ParseConditionalConstantStatement()

                Case TokenType.If
                    ParseConditionalIfStatement(Start)

                Case TokenType.Else
                    ParseConditionalElseStatement(Start)

                Case TokenType.ElseIf
                    ParseConditionalElseIfStatement(Start)

                Case TokenType.Identifier
                    Dim Directive As IdentifierToken = CType(Peek(), IdentifierToken)

                    If String.Compare(Directive.Identifier, "ExternalSource", True, Globalization.CultureInfo.InvariantCulture) = 0 Then
                        ParseExternalSourceStatement(Start)
                    ElseIf String.Compare(Directive.Identifier, "Region", True, Globalization.CultureInfo.InvariantCulture) = 0 Then
                        ParseRegionStatement(Start, statementLevel)
                    End If

                Case TokenType.End
                    ParseEndPreprocessingStatement(Start, statementLevel)

                Case Else
                    ResyncAt()
                    ReportSyntaxError(SyntaxErrorType.InvalidPreprocessorStatement, SpanFrom(Start))
            End Select

            ParseTrailingComments()

            If Peek().Type <> TokenType.LineTerminator AndAlso Peek().Type <> TokenType.EndOfStream Then
                ReportSyntaxError(SyntaxErrorType.ExpectedEndOfStatement, Peek())
                ResyncAt()
            End If

            Read()
            Return True
        Else
            ' If we're in a false conditional compilation statement, then keep reading lines as if they
            ' were preprocessing statements until we are done.
            If Start.Type <> TokenType.EndOfStream AndAlso _
               ConditionalCompilationContextStack.Count > 0 AndAlso _
               Not CType(ConditionalCompilationContextStack.Peek(), ConditionalCompilationContext).BlockActive Then
                ResyncAt()
                Read()

                Return True
            Else
                Return False
            End If
        End If
    End Function

    '*
    '* Public APIs
    '*

    Private Sub StartParsing(ByVal scanner As Scanner, _
                             ByVal errorTable As ArrayList, _
                             Optional ByVal preprocess As Boolean = False, _
                             Optional ByVal conditionalCompilationConstants As Hashtable = Nothing, _
                             Optional ByVal sourceRegions As ArrayList = Nothing, _
                             Optional ByVal externalLineMappings As ArrayList = Nothing)
        Me.Scanner = scanner
        Me.ErrorTable = errorTable
        Me.Preprocess = preprocess
        If conditionalCompilationConstants Is Nothing Then
            Me.ConditionalCompilationConstants = New Hashtable
        Else
            ' We have to clone this because the same hashtable could be used for
            ' multiple parses.
            Me.ConditionalCompilationConstants = CType(conditionalCompilationConstants.Clone(), Hashtable)
        End If
        Me.ExternalLineMappings = externalLineMappings
        Me.SourceRegions = sourceRegions
        ErrorInConstruct = False
        AtBeginningOfLine = True
        BlockContextStack.Clear()
    End Sub

    Private Sub FinishParsing()
        If Not CurrentExternalSourceContext Is Nothing Then
            ReportSyntaxError(SyntaxErrorType.ExpectedEndExternalSource, Peek())
        End If

        If Not RegionContextStack.Count = 0 Then
            ReportSyntaxError(SyntaxErrorType.ExpectedEndRegion, Peek())
        End If

        If Not ConditionalCompilationContextStack.Count = 0 Then
            ReportSyntaxError(SyntaxErrorType.ExpectedCCEndIf, Peek())
        End If

        StartParsing(Nothing, Nothing, False, Nothing, Nothing, Nothing)
    End Sub

    ''' <summary>
    ''' Parse an entire file.
    ''' </summary>
    ''' <param name="scanner">The scanner to use to fetch the tokens.</param>
    ''' <param name="errorTable">The list of errors produced during parsing.</param>
    ''' <returns>A file-level parse tree.</returns>
    Public Function ParseFile(ByVal scanner As Scanner, ByVal errorTable As ArrayList) As File
        Dim File As File

        StartParsing(scanner, errorTable, True, ConditionalCompilationConstants, SourceRegions, ExternalLineMappings)
        File = ParseFile()
        FinishParsing()

        Return File
    End Function

    ''' <summary>
    ''' Parse an entire file.
    ''' </summary>
    ''' <param name="scanner">The scanner to use to fetch the tokens.</param>
    ''' <param name="errorTable">The list of errors produced during parsing.</param>
    ''' <param name="conditionalCompilationConstants">Pre-defined conditional compilation constants.</param>
    ''' <param name="sourceRegions">Source regions defined in the file.</param>
    ''' <param name="externalLineMappings">External line mappings defined in the file.</param>
    ''' <returns>A file-level parse tree.</returns>
    Public Function ParseFile(ByVal scanner As Scanner, _
                              ByVal errorTable As ArrayList, _
                              ByVal conditionalCompilationConstants As Hashtable, _
                              ByVal sourceRegions As ArrayList, _
                              ByVal externalLineMappings As ArrayList) As File
        Dim File As File

        StartParsing(scanner, errorTable, True, conditionalCompilationConstants, sourceRegions, externalLineMappings)
        File = ParseFile()
        FinishParsing()

        Return File
    End Function

    ''' <summary>
    ''' Parse a declaration.
    ''' </summary>
    ''' <param name="scanner">The scanner to use to fetch the tokens.</param>
    ''' <param name="errorTable">The list of errors produced during parsing.</param>
    ''' <returns>A declaration-level parse tree.</returns>
    Public Function ParseDeclaration(ByVal scanner As Scanner, ByVal errorTable As ArrayList) As Declaration
        Dim Declaration As Declaration

        StartParsing(scanner, errorTable)
        Declaration = ParseDeclaration()
        FinishParsing()

        Return Declaration
    End Function

    ''' <summary>
    ''' Parse a statement.
    ''' </summary>
    ''' <param name="scanner">The scanner to use to fetch the tokens.</param>
    ''' <param name="errorTable">The list of errors produced during parsing.</param>
    ''' <returns>A statement-level parse tree.</returns>
    Public Function ParseStatement(ByVal scanner As Scanner, ByVal errorTable As ArrayList) As Statement
        Dim Statement As Statement

        StartParsing(scanner, errorTable)
        Statement = ParseStatement()
        FinishParsing()

        Return Statement
    End Function

    ''' <summary>
    ''' Parse an expression.
    ''' </summary>
    ''' <param name="scanner">The scanner to use to fetch the tokens.</param>
    ''' <param name="errorTable">The list of errors produced during parsing.</param>
    ''' <returns>An expression-level parse tree.</returns>
    Public Function ParseExpression(ByVal scanner As Scanner, ByVal errorTable As ArrayList) As Expression
        Dim Expression As Expression

        StartParsing(scanner, errorTable)
        Expression = ParseExpression()
        FinishParsing()

        Return Expression
    End Function

    ''' <summary>
    ''' Parse a type name.
    ''' </summary>
    ''' <param name="scanner">The scanner to use to fetch the tokens.</param>
    ''' <param name="errorTable">The list of errors produced during parsing.</param>
    ''' <returns>A typename-level parse tree.</returns>
    Public Function ParseTypeName(ByVal scanner As Scanner, ByVal errorTable As ArrayList) As TypeName
        Dim TypeName As TypeName

        StartParsing(scanner, errorTable)
        TypeName = ParseTypeName(True)
        FinishParsing()

        Return TypeName
    End Function
End Class
