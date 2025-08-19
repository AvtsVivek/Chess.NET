namespace Chess.Services
{
    public class XmlConstants
    {
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
        public const string MoveCommandsElementName = "PieceMoveCommands";

        /// <summary>
        /// The element name for individual moves in the XML document.
        /// </summary>
        public const string MoveElementName = "Move";

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
