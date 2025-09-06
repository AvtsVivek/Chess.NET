namespace Chess.Services
{
    public class XmlConstants
    {
        /// <summary>
        /// Represents the XML element name used for storing instructions in a document.
        /// </summary>
        public const string InstructionsElementName = "Instructions";

        /// <summary>
        /// Represents the XML element name used to identify warning messages.
        /// </summary>
        public const string WarningElementName = "Warning";

        /// <summary>
        /// Represents the name of the XML element used to store warning notes.
        /// </summary>
        /// <remarks>This constant is typically used as the element name when serializing or deserializing
        /// XML data that includes warning notes. The value of this constant is <c>"Warning"</c>.</remarks>
        public const string GeneralNotesElementName = "GeneralNotes";

        /// <summary>
        /// Represents the name of the metadata element.
        /// </summary>
        /// <remarks>This constant is used to identify metadata elements in the context where it is
        /// applied.</remarks>
        public const string MetadataElementName = "Metadata";

        /// <summary>
        /// Represents the XML element name for a title.
        /// </summary>
        /// <remarks>This constant is typically used when working with XML documents to identify or
        /// reference the "Title" element. It ensures consistency and reduces the risk of hardcoding string
        /// literals.</remarks>
        public const string TitleElementName = "Title";

        /// <summary>
        /// Represents the XML element name used to identify an User in a document.
        /// </summary>
        /// <remarks>This constant can be used when working with XML documents to ensure consistency in
        /// referencing the "User" element.</remarks>
        public const string UserElementName = "User";

        /// <summary>
        /// Represents the XML element name used to identify created date values.
        /// </summary>
        public const string CreatedDateElementName = "CreatedDate";


        /// <summary>
        /// Represents the XML element name used to identify the modified date in a document or data structure.
        /// </summary>
        public const string ModifiedDateElementName = "ModifiedDate";

        /// <summary>
        /// Represents the XML element name used for descriptions.
        /// </summary>
        /// <remarks>This constant is typically used to identify or reference the "Description" element in
        /// XML-based data structures or configurations.</remarks>
        public const string DescriptionElementName = "Description";

        /// <summary>
        /// Represents the XML element name used to identify the version in a configuration or data file.
        /// </summary>
        public const string VersionElementName = "Version";

        /// <summary>
        /// The root element name for the XML document representing chess moves.
        /// </summary>
        public const string RootElementName = "ChessMoves";

        /// <summary>
        /// The element name for the starting positions in the XML document.
        /// </summary>
        public const string StartPositionsElementName = "StartPositions";

        /// <summary>
        /// Represents the XML element name used to identify pieces in a serialized format.
        /// </summary>
        public const string PiecesElementName = "Pieces";

        /// <summary>
        /// Represents the XML element name for the "Blacks" configuration or data element.
        /// </summary>
        /// <remarks>This constant is typically used as a key or identifier for XML serialization or
        /// deserialization processes where the "Blacks" element is involved. It ensures consistency in naming across
        /// the application.</remarks>
        public const string BlacksElementName = "Blacks";

        /// <summary>
        /// Represents the XML element name for whites in a configuration or data structure.
        /// </summary>
        /// <remarks>This constant is typically used to identify or reference the "Whites" element in
        /// XML-based operations, such as serialization, deserialization, or XML document parsing.</remarks>
        public const string WhitesElementName = "Whites";

        /// <summary>
        /// Represents the XML element name used to define commands in the XML document.
        /// </summary>
        public const string PieceMoveCommandsElementName = "PieceMoveCommands";

        /// <summary>
        /// The element name for individual moves in the XML document.
        /// </summary>
        public const string MoveElementName = "Move";

        /// <summary>
        /// The element name for Notes for a command in the XML document.
        /// </summary>
        public const string CommandNotesElementName = "Notes";

        /// <summary>
        /// Represents the attribute name used to specify the color of a piece.
        /// </summary>
        public const string PieceColorAttributeName = "Color";

        /// <summary>
        /// Represents the name of the attribute used to specify the source position.
        /// </summary>
        public const string SourcePositionAttributeName = "Source";

        /// <summary>
        /// Represents the name of the attribute used to specify the target position.
        /// </summary>
        public const string TargetPositionAttributeName = "Target";

        /// <summary>
        /// Represents the name of the XML attribute used to identify a row.
        /// </summary>
        public const string RowAttributeName = "Row";

        /// <summary>
        /// Represents the name of the attribute used to define a column in a data structure.
        /// </summary>
        public const string ColumnAttributeName = "Column";
    }
}
