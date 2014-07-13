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
''' The type of a tree.
''' </summary>
Public Enum TreeType
    SyntaxError

    ' Collections
    ArgumentCollection
    ExpressionCollection
    InitializerCollection
    VariableNameCollection
    VariableDeclaratorCollection
    ParameterCollection
    CaseClauseCollection
    AttributeCollection
    NameCollection
    TypeNameCollection
    ImportCollection
    ModifierCollection
    StatementCollection
    DeclarationCollection

    ' Comments
    Comment

    ' Names
    SimpleName
    VariableName
    QualifiedName

    ' Types
    IntrinsicType
    NamedType
    ArrayType

    ' Initializers
    AggregateInitializer
    ExpressionInitializer

    ' Arguments
    Argument

    ' Expressions
    SimpleNameExpression
    TypeReferenceExpression
    QualifiedExpression
    DictionaryLookupExpression
    CallOrIndexExpression
    NewExpression
    NewAggregateExpression
    StringLiteralExpression
    CharacterLiteralExpression
    DateLiteralExpression
    IntegerLiteralExpression
    FloatingPointLiteralExpression
    DecimalLiteralExpression
    BooleanLiteralExpression
    BinaryOperatorExpression
    UnaryOperatorExpression
    AddressOfExpression
    IntrinsicCastExpression
    InstanceExpression
    NothingExpression
    ParentheticalExpression
    CTypeExpression
    DirectCastExpression
    TypeOfExpression
    GetTypeExpression

    ' Statements
    EmptyStatement
    GotoStatement
    ExitStatement
    StopStatement
    EndStatement
    ReturnStatement
    RaiseEventStatement
    AddHandlerStatement
    RemoveHandlerStatement
    ErrorStatement
    OnErrorStatement
    ResumeStatement
    ReDimStatement
    EraseStatement
    CallStatement
    AssignmentStatement
    CompoundAssignmentStatement
    MidAssignmentStatement
    LocalDeclarationStatement
    LabelStatement
    DoBlockStatement
    ForBlockStatement
    ForEachBlockStatement
    WhileBlockStatement
    SyncLockBlockStatement
    WithBlockStatement
    IfBlockStatement
    ElseIfBlockStatement
    ElseBlockStatement
    LineIfBlockStatement
    ThrowStatement
    TryBlockStatement
    CatchBlockStatement
    FinallyBlockStatement
    SelectBlockStatement
    CaseBlockStatement
    CaseElseBlockStatement
    LoopStatement
    NextStatement
    CatchStatement
    FinallyStatement
    CaseStatement
    CaseElseStatement
    ElseIfStatement
    ElseStatement
    EndBlockStatement

    ' Modifiers
    Modifier

    ' VariableDeclarators
    VariableDeclarator

    ' CaseClauses
    ComparisonCaseClause
    RangeCaseClause

    ' Attributes
    Attribute

    ' Declaration statements
    EmptyDeclaration
    NamespaceDeclaration
    VariableListDeclaration
    SubDeclaration
    FunctionDeclaration
    ConstructorDeclaration
    ExternalSubDeclaration
    ExternalFunctionDeclaration
    PropertyDeclaration
    GetDeclaration
    SetDeclaration
    EventDeclaration
    EndBlockDeclaration
    InheritsDeclaration
    ImplementsDeclaration
    ImportsDeclaration
    OptionDeclaration
    AttributeDeclaration
    EnumValueDeclaration
    ClassDeclaration
    StructureDeclaration
    ModuleDeclaration
    InterfaceDeclaration
    EnumDeclaration
    DelegateSubDeclaration
    DelegateFunctionDeclaration

    ' Parameters
    Parameter

    ' Imports
    NameImport
    AliasImport

    ' Files
    File
End Enum