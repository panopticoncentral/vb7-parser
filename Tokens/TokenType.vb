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
''' The type of a token.
''' </summary>
Public Enum TokenType
    None

    LexicalError
    EndOfStream
    LineTerminator
    Comment
    Identifier

    StringLiteral
    CharacterLiteral
    DateLiteral
    IntegerLiteral
    FloatingPointLiteral
    DecimalLiteral

    [AddHandler]
    [AddressOf]
    [Alias]
    [And]
    [AndAlso]
    [Ansi]
    [As]
    [Assembly]
    [Auto]
    [Boolean]
    [ByRef]
    [Byte]
    [ByVal]
    [Call]
    [Case]
    [Catch]
    [CBool]
    [CByte]
    [CChar]
    [CDate]
    [CDec]
    [CDbl]
    [Char]
    [CInt]
    [Class]
    [CLng]
    [CObj]
    [Const]
    [CShort]
    [CSng]
    [CStr]
    [CType]
    [Date]
    [Decimal]
    [Declare]
    [Default]
    [Delegate]
    [Dim]
    [DirectCast]
    [Do]
    [Double]
    [Each]
    [Else]
    [ElseIf]
    [End]
    [Endif]
    [Enum]
    [Erase]
    [Error]
    [Event]
    [Exit]
    [False]
    [Finally]
    [For]
    [Friend]
    [Function]
    [Get]
    [GetType]
    [GoSub]
    [GoTo]
    [Handles]
    [If]
    [Implements]
    [Imports]
    [In]
    [Inherits]
    [Integer]
    [Interface]
    [Is]
    [Let]
    [Lib]
    [Like]
    [Long]
    [Loop]
    [Me]
    [Mod]
    [Module]
    [MustInherit]
    [MustOverride]
    [MyBase]
    [MyClass]
    [Namespace]
    [New]
    [Next]
    [Not]
    [Nothing]
    [NotInheritable]
    [NotOverridable]
    [Object]
    [On]
    [Option]
    [Optional]
    [Or]
    [OrElse]
    [Overloads]
    [Overridable]
    [Overrides]
    [ParamArray]
    [Preserve]
    [Private]
    [Property]
    [Protected]
    [Public]
    [RaiseEvent]
    [ReadOnly]
    [ReDim]
    [REM]
    [RemoveHandler]
    [Resume]
    [Return]
    [Select]
    [Set]
    [Shadows]
    [Shared]
    [Short]
    [Single]
    [Static]
    [Step]
    [Stop]
    [String]
    [Structure]
    [Sub]
    [SyncLock]
    [Then]
    [Throw]
    [To]
    [True]
    [Try]
    [TypeOf]
    [Unicode]
    [Until]
    [Variant]
    [Wend]
    [When]
    [While]
    [With]
    [WithEvents]
    [WriteOnly]
    [Xor]

    LeftParenthesis         ' (
    RightParenthesis        ' )
    LeftCurlyBrace          ' {
    RightCurlyBrace         ' }
    Exclamation             ' !
    Pound                   ' #
    Comma                   ' ,
    Period                  ' .
    Colon                   ' :
    ColonEquals             ' :=
    Ampersand               ' &
    AmpersandEquals         ' &=
    Star                    ' *
    StarEquals              ' *=
    Plus                    ' +
    PlusEquals              ' +=
    Minus                   ' -
    MinusEquals             ' -=
    ForwardSlash            ' /
    ForwardSlashEquals      ' /=
    BackwardSlash           ' \
    BackwardSlashEquals     ' \=
    Caret                   ' ^
    CaretEquals             ' ^=
    LessThan                ' <
    LessThanEquals          ' <=
    Equals                  ' =
    NotEquals               ' <>
    GreaterThan             ' >
    GreaterThanEquals       ' >=
    LessThanLessThan        ' <<
    LessThanLessThanEquals  ' <<=
    GreaterThanGreaterThan  ' >>
    GreaterThanGreaterThanEquals ' >>=
End Enum